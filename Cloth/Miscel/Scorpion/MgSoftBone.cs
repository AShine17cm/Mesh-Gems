using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Mg.Games
{
    //使得 骨骼链  发生弯曲
    public class MgSoftBone
    {
        public bool IsActive { get; set; }

        public float soft = 0.5f;
        public Transform[] pivots;
        public AutoRoll[] rolls;
        //
        public int countSam = 8;
        [Header("最大倾斜角")] public float max = 80f;
        public float degreeSpd = 180f;
        public float vaultDegree = 1f;
        public float tickBack = 0.05f;
        //
        Vector3[] nodes;
        Vector3[] fwds;
        float[] spaces;
        int countNode;
        public void InitComponent(object actorArg)
        {
            Init();

            for(int i=0;i<rolls.Length;i++)
            {
                rolls[i].Init(countSam, max, degreeSpd, vaultDegree, tickBack);
            }
        }

        public void SetupComponent()
        {
            Vector3 p0 = pivots[0].position;
            Vector3 vec;
            //节点 距离
            for (int i = 1; i < countNode; i++)
            {
                vec = pivots[i].position - pivots[i - 1].position;
                fwds[i] = vec.normalized;
            }
            spaces[0] = 0;
            fwds[0] = pivots[0].forward;

            for (int i = 0; i < countNode; i++)
            {
                nodes[i] = pivots[i].position;
            }
            for (int i = 0; i < rolls.Length; i++)
            {
                rolls[i].Setup();
            }
        }
        public void StopComponent() { }
        //
        void UpdateBones()
        {
            //if (!motor.isStop)
            {
                float tick = 0.02f;
                UpdateLink(tick);
                for(int i=0;i<rolls.Length;i++)
                {
                    rolls[i].UpdateRoll(tick);
                }
            }
        }
        //初始化节点链
        public void Init()
        {
            this.countNode = pivots.Length;

            nodes = new Vector3[countNode];
            fwds = new Vector3[countNode];
            spaces = new float[countNode];

            Vector3 p0 = pivots[0].position;
            Vector3 vec;
            //节点 距离
            for (int i = 1; i < countNode; i++)
            {
                vec = pivots[i].position - pivots[i - 1].position;
                spaces[i] = vec.magnitude;
                fwds[i] = vec.normalized;
            }
            spaces[0] = 0;
            fwds[0] = pivots[0].forward;

            for (int i = 0; i < countNode; i++)
            {
                nodes[i] = pivots[i].position;
            }

        }
        public void UpdateLink(float tick)
        {
            Vector3 delta = pivots[0].position - nodes[0];
            nodes[0] = pivots[0].position;
            Refresh(delta, tick);
        }
        //从 根节点 开始刷新
        public void Refresh(Vector3 delta, float tick)
        {
            float space;
            for (int i = 1; i < countNode; i++)
            {
                space = spaces[i];
                Vector3 virPos = nodes[i] - delta * soft;               //向后 虚位
                Vector3 virDir = (virPos - nodes[i - 1]).normalized;    //指向 上层节点
                Vector3 conPos = nodes[i - 1] + virDir * space;         //距离钳制的 临时位置

                delta = nodes[i] - conPos;                              //更新 Delta,为下一个节点

                nodes[i] = conPos;
            }
            for (int i = 1; i < countNode; i++)
            {
                fwds[i] = (nodes[i - 1] - nodes[i]).normalized;
                pivots[i].position = nodes[i];
                pivots[i].forward = fwds[i];
            }
            pivots[0].forward = pivots[1].forward;
        }
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (nodes != null && nodes.Length > 1)
            {
                for (int i = 1; i < countNode; i++)
                {
                    Handles.color = (i % 2 == 0 ? Color.blue : Color.green);
                    Handles.DrawLine(nodes[i - 1], nodes[i]);
                }
            }
        }
#endif
    }
}
