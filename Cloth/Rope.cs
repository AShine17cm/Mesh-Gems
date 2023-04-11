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
    public partial class Rope
    {
        public Transform pivot;
        public int attributeIdx = 0;
        public bool physics = false;
        public bool rotations = true;

        protected float gravity = 10f;
        protected float physicsRadius = 0.5f;      //物理碰撞半径
        protected float stasis = 1f;

        protected float[] damps;                  //衰减速度
        protected float[] dists;                  //单轴-距离约束
        protected float[] bendCoss;               //弯曲-变形量 <Cos>  可改为各项异性
        protected float[] bendSins;
        protected float[] adjCoss;
        protected float[] adjSins;

        protected Transform[] bones;
        protected Vector3[] initDirs;             //初始轴向 local-space
        protected Quaternion[] initRotations;
        protected Vector3[] nodes;                //节点-位置
        protected Vector3[] nodesOld;
        protected Vector3[] nodesTmp;
        protected Vector3[] constrains;
        protected float[] nodesTouch;              //在物理表面上, 计时长度

        protected int countNode;
        protected LayerMask mask;

        public virtual void InitRuntime(RopeAttribute att, LayerMask mask)
        {
            gravity = att.gravity;
            physicsRadius = att.physicsRadius;
            stasis = att.physicsStasis;

            this.mask = mask;
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
            adjCoss = new float[countNode];
            adjSins = new float[countNode];

            bones = new Transform[countNode];
            nodes = new Vector3[countNode];
            nodesOld = new Vector3[countNode];
            nodesTmp = new Vector3[countNode];
            initDirs = new Vector3[countNode];
            initRotations = new Quaternion[countNode];
            constrains = new Vector3[countNode];
            nodesTouch = new float[countNode];

            bones[0] = pivot;   //第一个节点
            nodes[0] = pivot.position;
            nodesOld[0] = nodes[0];
            initDirs[0] = Vector3.zero;
            initRotations[0] = Quaternion.identity;
            nodesTouch[0] = 0;
            dists[0] = 0;
            damps[0] = 0;
            bendCoss[0] = 1;
            bendSins[0] = 0;
            adjCoss[0] = 1;
            adjSins[0] = 1;

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
                initRotations[i] = bones[i].localRotation;
                damps[i] = (att.damping * 0.1f) * att.dampCurve.Evaluate(t);
                nodesTouch[i] = 0;

                float bendRad = Mathf.Deg2Rad * att.bendDegree * att.bendCurve.Evaluate(t);
                bendCoss[i] = Mathf.Cos(bendRad);
                bendSins[i] = Mathf.Sin(bendRad);
                float adjRad = Mathf.Deg2Rad * att.adjacentDegree * att.adjacentCurve.Evaluate(t);
                adjCoss[i] = Mathf.Cos(adjRad);
                adjSins[i] = Mathf.Sin(adjRad);
            }
        }
        public void ClearConstrains()
        {
            for (int i = 0; i < countNode; i++)
            {
                constrains[i] = Vector3.zero;
            }
        }
        /* World Space
         * dirForce:    可以 local+World重力 混合
         */
        public virtual void Refresh(Vector3 dirForce, float tick)
        {
            Vector3 pos = pivot.position;
            /* 保存前一帧的数据 */
            Array.Copy(nodes, nodesTmp, countNode);
            nodes[0] = pos;

            Simulate(dirForce, tick);
            //检测物理碰撞<Head-Tail> 和 末端的
            if (physics)
            {
                Vector3 tail = nodes[countNode - 1];
                if (Physics.Linecast(pos, tail, mask) || Physics.Linecast(tail, tail + Vector3.down * physicsRadius))
                {
                    SimulatePhysics();
                }
            }
            /* 应用计算结果 到骨骼节点 */
            for (int i = 1; i < countNode; i++)
            {
                bones[i].position = nodes[i];
            }
            /* 保持节点之间的相对旋转 */
            if (rotations)
            {
                for(int i = 1; i < countNode; i++)
                {
                    Vector3 localPos = bones[i].localPosition;
                    Quaternion rot = Quaternion.FromToRotation(initDirs[i], localPos.normalized);
                    bones[i].localRotation = initRotations[i] * rot;
                }
                for (int i = 1; i < countNode; i++)
                {
                    bones[i].position = nodes[i];
                }
            }

            /* 保存前一帧的数据 */
            Array.Copy(nodesTmp, nodesOld, countNode);
        }
        /* 做一次模拟运算 */
        public virtual void Simulate(Vector3 dirForce, float tick)
        {
            Vector3 pos;
            Vector3 virAxis;
            Vector3 init_wldDir;
            Vector3 vecOld;
            Vector3 preAxis = (nodes[1] - nodes[0]).normalized;

            for (int i = 1; i < countNode; i++)
            {
                pos = nodes[i];
                /* 物理约束, 受力平衡 */
                nodesTouch[i] -= tick;
                if (nodesTouch[i] >= 0)
                {
                    virAxis = (pos - nodes[i - 1]).normalized;
                    pos = nodes[i - 1] + virAxis * dists[i];
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
                /* 合成速度, Hub传入的tick固定 */
                Vector3 deltaMv = vecOld  + (tanForce * deltaSpd * tick * 0.5f);

                float mag = deltaMv.magnitude;
                float damp = 1 - Mathf.Clamp(damps[i] * mag / tick, 0, 0.9999f);//防止出现负值
                deltaMv = deltaMv * damp;

                deltaMv += constrains[i];//xxxxx

                pos = pos + deltaMv;
                virAxis = (pos - nodes[i - 1]).normalized;
                pos = nodes[i - 1] + virAxis * dists[i];

                /* 弯曲约束 */
                init_wldDir = bones[i - 1].TransformDirection(initDirs[i]);
                float dot_init = Vector3.Dot(init_wldDir, virAxis);
                if (dot_init < bendCoss[i])
                {
                    Vector3 planar = Vector3.Cross(init_wldDir, virAxis);
                    Vector3 tan = Vector3.Cross(planar, init_wldDir);
                    tan.Normalize();
                    virAxis = init_wldDir * bendCoss[i] + tan * bendSins[i];
                    pos = nodes[i - 1] + virAxis * dists[i];
                }
                /* 与前一个轴的角度 */
                float dot_preAxis = Vector3.Dot(preAxis, virAxis);
                if (dot_preAxis < adjCoss[i])
                {
                    Vector3 planar = Vector3.Cross(preAxis, virAxis);
                    Vector3 tan = Vector3.Cross(planar, preAxis);
                    tan.Normalize();
                    virAxis = preAxis * adjCoss[i] + tan * adjSins[i];
                    pos = nodes[i - 1] + virAxis * dists[i];
                }

                nodes[i] = pos;   //最终的位置
                preAxis = virAxis;
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
        /* 瞬移 */
        public void Teleport(Vector3 delta)
        {
            for(int i = 0; i < countNode; i++)
            {
                nodes[i] += delta;
                nodesOld[i] += delta;
            }
        }
#if UNITY_EDITOR
        public virtual void OnDrawGizoms(RopeAttribute att)
        {
            if (pivot == null) return;
            Color line = new Color(1, 0, 0, 0.7f);
            Transform testTr = pivot;
            while (testTr.childCount > 0)
            {
                Transform tmp = testTr.GetChild(0);
                Vector3 p0 = testTr.position;
                Vector3 p1 = tmp.position;

                Gizmos.color = line;
                Gizmos.DrawLine(p1, p0);
                testTr = tmp;
            }
        }
#endif
    }
}