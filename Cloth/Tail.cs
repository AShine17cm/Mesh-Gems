using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mg.Cloth
{
    /*
     相对Rope 添加了硬度，用于做 应力的平衡，方便做特殊造型
     */
    [Serializable]
    public class Tail
    {
        public Transform pivot;
        public int attributeIdx = 0;
        public int pairIdx = -1;

        float timeScale = 1f;
        float gravity = 9.8f;
        float radius = 0.3f;
        float physicsRadius = 0.5f;      //物理碰撞半径
        float stasis = 1f;

        float[] damps;                  //衰减速度
        float[] dists;                  //单轴-距离约束
        float[] bendCoss;               //弯曲-变形量 <Cos>  可改为各项异性
        float[] bendSins;
        float[] stiffs;

        Transform[] bones;
        Vector3[] initDirs;             //初始轴向 local-space
        Vector3[] nodes;                //节点-位置
        Vector3[] nodesOld;
        Vector3[] nodesTmp;
        float[] nodesTouch;              //在物理表面上

        float lastTick;
        int countNode;
        LayerMask mask;

        bool hasPair = false;
        Tail constrainPair;       //如果有的话

#if UNITY_EDITOR
        /* 用于在Editor里 更新 Damp 和 Bend */
        public void Refresh_Damp_Bend(TailAttribute att)
        {
            timeScale = att.timeScale;
            gravity = att.gravity;
            radius = att.radius;
            physicsRadius = att.physicsRadius;
            stasis = att.physicsStasis;
            float step = 1f / (countNode - 1);
            float t;
            for (int i = 1; i < countNode; i++)
            {
                t = step * i;
                damps[i] = (att.damping * 0.1f) * att.dampCurve.Evaluate(t);

                float bendRad =att.bendDegree * att.bendCurve.Evaluate(t);
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
        public void InitRuntime(TailAttribute att, LayerMask mask, Tail constrainPair)
        {
            lastTick = 0.1f;
            timeScale = att.timeScale;
            gravity = att.gravity;
            radius = att.radius;
            physicsRadius = att.physicsRadius;
            stasis = att.physicsStasis;

            this.mask = mask;
            /* 平行约束 */
            hasPair = (null != constrainPair);
            this.constrainPair = constrainPair;

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
            stiffs = new float[countNode];

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
            stiffs[0] =att.stiffness* att.stiffCurve.Evaluate(0);
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
                damps[i] = (att.damping * 0.1f) * att.dampCurve.Evaluate(t);
                nodesTouch[i] = 0;

                float bendRad = att.bendDegree * att.bendCurve.Evaluate(t);
                bendCoss[i] = Mathf.Cos(Mathf.Deg2Rad * bendRad);
                bendSins[i] = Mathf.Sin(Mathf.Deg2Rad * bendRad);
                stiffs[i] = att.stiffness * att.stiffCurve.Evaluate(t);
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
                float dot = Vector3.Dot(axis, dirForce);        //cos +/-
                float proj = Mathf.Sqrt(1 - dot * dot);         //sin
                proj = Mathf.Clamp(proj - dot * stiffs[i], 0, proj);//与力（重力）方向“一致”时,应力平衡
                /* 加速度  大小 */
                float deltaSpd = gravity * proj * tick;

                /* 加速度  切方向 */
                Vector3 planarVec = Vector3.Cross(axis, dirForce);
                Vector3 tanForce = Vector3.Cross(planarVec, axis).normalized;

                vecOld = pos - nodesOld[i];
                /* 合成速度 */
                Vector3 deltaMv = vecOld / lastTick * tick + (tanForce * deltaSpd * tick * 0.5f);

                float mag = deltaMv.magnitude;
                float damp = 1 - Mathf.Clamp(damps[i] * mag / tick, 0, 0.9999f);//防止出现负值
                deltaMv = deltaMv * damp;

                pos = pos + deltaMv;
                virDir = (pos - nodes[i - 1]).normalized;
                pos = nodes[i - 1] + virDir * dists[i];

                /* 平行约束 */
                if (hasPair)
                {
                    bool isIntersect = false;
                    Vector3 vecInter = constrainPair.TestIntersect(pos, radius, ref isIntersect);
                    if (isIntersect)
                    {
                        pos = pos + vecInter;
                        virDir = (pos - nodes[i - 1]).normalized;//修正 虚方向
                        pos = nodes[i - 1] + virDir * dists[i];
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
                    nodesTouch[i] = stasis;
                }
                if (!isHit) continue;

                //virDir = (virPos - nodes[i - 1]).normalized;                    //指向 上层节点
                //cpPos = nodes[i - 1] + virDir * dists[i];                       //距离钳制的 临时位置
                //nodes[i] = cpPos;   //最终的位置
                nodes[i] = virPos;
            }
        }

        Vector3 TestIntersect(Vector3 pos, float radiusB, ref bool isIntersect)
        {
            isIntersect = false;
            float diameterAB = radius + radiusB;
            float sqrAB = diameterAB * diameterAB;

            Vector3 pairPos;
            Vector3 vec;
            Vector3 delta = Vector3.zero;
            for (int i = 0; i < countNode; i++)
            {
                pairPos = nodes[i];
                vec = pos - pairPos;
                float sqr = vec.sqrMagnitude;
                if (sqr < sqrAB)
                {
                    isIntersect = true;
                    float distAB = Mathf.Sqrt(sqr);
                    //Vector3 dirBA = vec / distAB;
                    //return dirBA * (diameterAB - distAB);
                    if (distAB < 1e-4f)
                    {
                        delta += vec;
                    }
                    else
                    {
                        delta = delta + vec * ((diameterAB / distAB) - 1);
                    }
                }
            }
            return delta;
        }
#if UNITY_EDITOR
        public void OnDrawGizoms(TailAttribute att, Tail pair)
        {
            if (pivot == null) return;
            Color line = new Color(1, 0, 0, 0.7f);
            Color sphere = new Color(0.6f, 0.6f, 0.6f, 0.5f);
            Color constrain = new Color(0, 1, 1, 0.5f);

            Transform testPair = null;
            if (pair != null)
            {
                testPair = pair.pivot;
            }

            Transform testTr = pivot;
            while (testTr.childCount > 0)
            {
                Transform tmp = testTr.GetChild(0);
                Vector3 p0 = testTr.position;
                Vector3 p1 = tmp.position;

                Gizmos.color = line;
                Gizmos.DrawLine(p1, p0);
                Gizmos.color = sphere;
                Gizmos.DrawWireSphere(p1, att.radius);
                //平行约束
                if (testPair != null)
                {
                    Gizmos.color = constrain;
                    Vector3 pPair = testPair.position;
                    Gizmos.DrawLine(p0, p0 + (p0 - pPair).normalized * att.radius * 3f);
                    if (testPair.childCount > 0)
                        testPair = testPair.GetChild(0);
                    else
                        testPair = null;
                }
                testTr = tmp;

            }
            if (testPair != null)
            {
                Gizmos.color = constrain;
                Vector3 p0 = testTr.position;
                Vector3 pPair = testPair.position;
                Gizmos.DrawLine(p0, p0 + (p0 - pPair).normalized * att.radius * 3f);
            }
        }
#endif
    }
}