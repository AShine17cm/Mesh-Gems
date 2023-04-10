using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mg.Cloth
{
    /*
     相对Rope 添加了硬度，用于做 应力的平衡，方便做特殊造型
     */
    [Serializable]
    public class Tail:Rope
    {
        float[] stiffs;

        public override void InitRuntime(RopeAttribute att_rope, LayerMask mask)
        {
            base.InitRuntime(att_rope, mask);
            TailAttribute att = att_rope as TailAttribute;
            stiffs = new float[countNode];
            stiffs[0] =att.stiffness* att.stiffCurve.Evaluate(0);
            float step = 1f / (countNode - 1);
            float t;
            for (int i = 1; i < countNode; i++)
            {
                t = step * i;
                stiffs[i] = att.stiffness * att.stiffCurve.Evaluate(t);
            }
        }
        /* 做一次模拟运算 */
        public override void Simulate(Vector3 dirForce, float tick)
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
                float dot = Vector3.Dot(axis, dirForce);        //cos +/-
                float proj = Mathf.Sqrt(1 - dot * dot);         //sin
                /* ------------ Stiff ----------------
                 * 与力（重力）方向“一致”时,应力平衡 
                 * （就一行代码，可以合进 基础属性中）
                 */
                proj = Mathf.Clamp(proj - dot * stiffs[i], 0, proj);
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

    }
}