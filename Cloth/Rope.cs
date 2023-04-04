using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mg.Cloth
{
    /*
     单轴-单层(嵌套子节点)，具有平行约束, 最大弯曲度(柔性)约束
     1-2-3-4
     1-2-3-4 (平行约束)
     1-
       2-
          3-
            4-
     */
    [Serializable]
    public class Rope
    {
        public Transform pivot;
        public AnimationCurve bendCurve;        //弯曲度
        public AnimationCurve dampCurve;        //衰减速度
        public float damping = 0.1f;            // 会乘以0.1f
        [Range(0.01f,10)] public float timeScale = 1f;
        public float gravity = 9.8f;
        public int bendDegree = 60;
        public float radius = 0.3f;
        public float physicsRadius = 0.5f;      //物理碰撞半径

        private float[] damps;                  //衰减速度
        private float[] dists;                  //单轴-距离约束
        private float[] bendCoss;               //弯曲-变形量 <Cos>  可改为各项异性
        private float[] bendSins;

        private Transform[] bones;
        private Vector3[] initDirs;             //初始轴向 local-space
        private Vector3[] nodes;                //节点-位置
        private Vector3[] nodesOld;
        private Vector3[] nodesTmp;
        private float[] nodesTouch;              //在物理表面上

        private float lastTick;
        private int countNode;
        private LayerMask mask;

        private bool hasPair = false;
        private Rope constrainPair;       //如果有的话
        private float radiusB = 0;
        private float diameterAB, sqrAB;

        private float delayTouch = 1f;
#if UNITY_EDITOR
        public void InitEditor()
        {
            if (pivot == null && dampCurve.keys.Length == 0)
            {
                dampCurve = AnimationCurve.Constant(0, 1, 1.0f);
                bendCurve = AnimationCurve.Constant(0, 1, 1.0f);
                damping = 1.0f;
                bendDegree = 60;
                radius = 0.3f;
                physicsRadius = 0.5f;
            }
        }
        /* 用于在Editor里 更新 Mass和 Bend */
        public void Refresh_Damp_Bend()
        {
            float step = 1f / (countNode - 1);
            float t;
            for (int i = 1; i < countNode; i++)
            {
                t = step * i;
                damps[i] = (damping * 0.1f) * dampCurve.Evaluate(t);

                float bendRad = bendDegree * bendCurve.Evaluate(t);
                bendCoss[i] = Mathf.Cos(Mathf.Deg2Rad * bendRad);
                bendSins[i] = Mathf.Sin(Mathf.Deg2Rad * bendRad);
            }
        }
#endif
        public Vector3 ParallelNode(int idx)    //平行的节点
        {
            if (idx < nodes.Length)
                return nodes[idx];
            return nodes[nodes.Length - 1];
        }
        public void InitRuntime(LayerMask mask, Rope constrainPair)
        {
            lastTick = 0.1f;
            timeScale = Mathf.Max(timeScale, 0.001f);
            bendDegree = Mathf.Clamp(bendDegree, 10, 359);

            this.mask = mask;

            /* 平行约束 */
            hasPair = (null != constrainPair);
            if (hasPair)
            {
                radiusB = constrainPair.radius;
                diameterAB = radius + radiusB;
                sqrAB = diameterAB * diameterAB;
            }
            countNode = 1;
            Transform testTr = pivot;
            while (testTr.childCount > 0)
            {
                countNode += 1;
                testTr = testTr.GetChild(0);
            }

            damps = new float[countNode];
            dists = new float[countNode];
            bendCoss = new float[countNode];
            bendSins = new float[countNode];

            bones = new Transform[countNode];
            nodes = new Vector3[countNode];
            nodesOld = new Vector3[countNode];
            nodesTmp = new Vector3[countNode];
            initDirs = new Vector3[countNode];
            nodesTouch = new float[countNode];

            bones[0] = pivot;   //第一个节点
            nodes[0] = pivot.position;
            nodesOld[0] = nodes[0];
            initDirs[0] = Vector3.zero;
            nodesTouch[0] = 0;
            dists[0] = 0;
            damps[0] = 0;
            bendCoss[0] = 1;
            bendSins[0] = 0;
            float step = 1f / (countNode - 1);
            float t;
            for (int i = 1; i < countNode; i++)
            {
                t = step * i;
                bones[i] = bones[i - 1].GetChild(0);
                nodes[i] = bones[i].position;
                nodesOld[i] = nodes[i];
                dists[i] = (nodes[i] - bones[i - 1].position).magnitude;    //某个节点可能会有缩放，所以在world-space中计算
                initDirs[i] = bones[i].localPosition.normalized;                //初始轴向-local
                damps[i] = (damping*0.1f) * dampCurve.Evaluate(t);
                nodesTouch[i] = 0;

                float bendRad = bendDegree * bendCurve.Evaluate(t);
                bendCoss[i] = Mathf.Cos(Mathf.Deg2Rad * bendRad);
                bendSins[i] = Mathf.Sin(Mathf.Deg2Rad * bendRad);
            }
        }
        /* World Space
         * dirForce:    可以 local+World重力 混合
         */
        public void Refresh(Vector3 dirForce, float tick)
        {
            tick *= timeScale;
            Vector3 pos = pivot.position;
            /* 保存前一帧的数据 */
            Array.Copy(nodes, nodesTmp, countNode);
            nodes[0] = pos;
            Simulate(dirForce, tick);
            //检测物理碰撞<Head-Tail> 和 末端的
            Vector3 tail = nodes[countNode - 1];
            if (Physics.Linecast(pos, tail, mask) || Physics.Linecast(tail, tail + Vector3.down * physicsRadius))
            {
                SimulatePhysics();
            }

            /* 应用计算结果 到骨骼节点 */
            for (int i = 1; i < countNode; i++)
            {
                bones[i].position = nodes[i];
            }
            /* 保存前一帧的数据 */
            Array.Copy(nodesTmp, nodesOld, countNode);
            lastTick = tick;
        }
        /* 做一次模拟运算 */
        void Simulate(Vector3 dirForce, float tick)
        {
            Vector3 pos;
            Vector3 virDir;
            Vector3 init_wldDir;
            Vector3 vecOld;

            for (int i = 1; i < countNode; i++)
            {
                pos = nodes[i];
                /* 物理约束, 受力平衡 */
                nodesTouch[i] -= tick;
                if (nodesTouch[i] >= 0)
                {
                    virDir = (pos - nodes[i - 1]).normalized;
                    pos = nodes[i - 1] + virDir * dists[i];
                    nodes[i] = pos;
                    continue;
                }

                /* 加速度 在切线上的投影 */
                Vector3 axis = (pos - nodes[i - 1]).normalized;//需要重新计算
                float dot = Vector3.Dot(axis, dirForce);
                float proj = Mathf.Sqrt(1 - dot * dot);
                /* 加速度  大小 */
                float deltaSpd = gravity * proj * tick;
 
                /* 加速度  切方向 */
                Vector3 planarVec = Vector3.Cross(axis, dirForce);
                Vector3 tanForce = Vector3.Cross(planarVec, axis).normalized;

                vecOld = pos - nodesOld[i];
                /* 合成速度 */
                Vector3 deltaMv = vecOld / lastTick*tick  + (tanForce * deltaSpd*tick  * 0.5f);
                float mag = deltaMv.magnitude;
                float damp = 1 - Mathf.Clamp(damps[i] * mag / tick, 0, 0.9999f);//防止出现负值
                deltaMv = deltaMv *damp;

                pos = pos + deltaMv;
                virDir = (pos - nodes[i - 1]).normalized;
                pos = nodes[i - 1] + virDir * dists[i];

                /* 平行约束 */
                if (hasPair)
                {
                    Vector3 pairPos = constrainPair.ParallelNode(i);
                    Vector3 vecBA = pairPos - pos;
                    float sqr = vecBA.sqrMagnitude;
                    if (sqr < sqrAB)
                    {
                        float distBA = Mathf.Sqrt(sqr);
                        Vector3 dirBA = vecBA / distBA;
                        pos = pairPos + dirBA * diameterAB;   //平行约束

                        virDir = (pos - nodes[i - 1]).normalized;//修正 虚方向
                    }
                }
                /* 弯曲约束 */
                init_wldDir = bones[i - 1].TransformDirection(initDirs[i]);
                float dotBend = Vector3.Dot(init_wldDir, virDir);
                if (dotBend < bendCoss[i])
                {
                    Vector3 planarDir = Vector3.Cross(init_wldDir, virDir);
                    Vector3 init_wldDir_X = Vector3.Cross(planarDir, init_wldDir);
                    init_wldDir_X.Normalize();
                    virDir = init_wldDir * bendCoss[i] + init_wldDir_X * bendSins[i];
                    pos = nodes[i - 1] + virDir * dists[i];
                }
                nodes[i] = pos;   //最终的位置
            }

        }
        /* 做一次物理模拟， 检测与地面的物理碰撞 强约束 */
        void SimulatePhysics()
        {
            Vector3 virPos;
            Vector3 virDir;
            Vector3 cpPos;     //钳制的位置 clamp-pos
            Vector3 vecOld;
            Vector3 xPoint;
            RaycastHit hit;
            bool isHit;
            for (int i = 1; i < countNode; i++)
            {
                isHit = false;
                virPos = nodes[i];                                              //向后的虚位
                /* 假设 前点在碰撞体外部 */
                vecOld = nodes[i] - nodesOld[i];
                xPoint = nodes[i] + vecOld.normalized * physicsRadius;
                /* 运动方向 */
                if (Physics.Linecast(nodesOld[i], xPoint, out hit, mask))
                {
                    virPos = hit.point + hit.normal * physicsRadius;
                    isHit = true;
                }
                /* 链接方向-此时已经插入 */
                else if (Physics.Linecast(nodes[i - 1], nodes[i], out hit, mask))
                {
                    virPos = hit.point + hit.normal * physicsRadius;
                    isHit = true;
                }
                if (isHit)
                {
                    nodesTouch[i] = delayTouch;
                }
                if (!isHit) continue;

                //virDir = (virPos - nodes[i - 1]).normalized;                    //指向 上层节点
                //cpPos = nodes[i - 1] + virDir * dists[i];                       //距离钳制的 临时位置
                //nodes[i] = cpPos;   //最终的位置
                nodes[i] = virPos;

            }
        }
#if UNITY_EDITOR
        public void OnDrawGizoms()
        {
            if (nodes == null) return;
            Gizmos.color = Color.red;
            for (int i = 0; i < countNode; i++)
            {
                Vector3 vec = nodes[i] - nodesOld[i];
                if (vec.sqrMagnitude > 1e10)
                {
                    vec.Normalize();
                    Gizmos.DrawLine(nodes[i], nodes[i] + vec * 0.5f);
                }
                if (i > 0)
                    Gizmos.DrawLine(nodes[i], nodes[i - 1]);
            }
        }
#endif
    }
}