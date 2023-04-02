using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Mg.Games
{
    //没有物理碰撞
    [Serializable]
    public class CableFree
    {
        public Material mat;
        [Header("链子基本形状")] public TailBumpEnum bump = TailBumpEnum.BumpDown;
        [Header("最小 节点距离")] public float minSpace = 0.02f;
        [Header("最大 节点距离")] public float maxSpace = 0.2f;

        [Header("单节 质量")] public float qSeg = 0.01f;
        [Header("终点 质量")] public float qEnd = 5f;

        [Header("节点数量")] public int countNode = 40;
        public float scaleTime = 4f;

        [Header("环切面")] public int countSeg = 6;

        Transform trCable;  //无旋转

        Vector3[] nodes;    //world space

        float[] qualitys;
        float[] softs;

        float scaleTimer;
        float space;
        float lenFrom, lenTo;

        [NonSerialized] public bool doScale = false;

        [NonSerialized] public float scale;
        public System.Action onScaleUp;
        public System.Action onScaleDown;
        public Vector3 Head
        {
            get { return nodes[0]; }
        }
        public Vector3 Tail
        {
            get { return nodes[countNode - 1]; }
        }
        public Vector3 TailDir
        {
            get { return normals[countNode * countRingV - 1]; }
        }
        //初始化节点链,偏移p0,在World坐标内
        public MeshRenderer Init(GameObject gameObject, Vector3 dirForce)
        {
            this.trCable = gameObject.transform;
            trCable.rotation = Quaternion.identity;

            Vector3 p0 = trCable.position;
            this.space = minSpace;
            this.scale = 0 / (maxSpace - minSpace);

            nodes = new Vector3[countNode];
            qualitys = new float[countNode];
            softs = new float[countNode];
            //节点 距离
            for (int i = 0; i < countNode; i++)
            {
                nodes[i] = p0 + dirForce * (i * minSpace);
            }
            //计算 每节重量
            for (int i = countNode - 1; i >= 0; i -= 1)
            {
                qualitys[i] = qEnd + qSeg * i;
            }
            switch (bump)
            {
                case TailBumpEnum.BumpUp://向上凸起
                    for (int i = 0; i < countNode; i++)
                    {
                        //softs[i] = 0.5f;//平滑，接近直线
                        //softs[i] = -0.8f + 0.04f * (1f * i * i) / (countNode);
                        softs[i] = -1f + 0.05f * i;
                    }
                    break;
                case TailBumpEnum.BumpDown://向下 凸起
                    for (int i = 0; i < countNode; i++)
                    {
                        //i的次方越高，凸点越靠下
                        //softs[i] = 1f - 0.05f * i;
                        softs[i] = 0.8f - 0.12f * (1f * i * i * i * i) / (countNode * countNode * countNode);
                    }
                    break;
            }

          return  InitMesh(countNode, countSeg, gameObject, dirForce);
        }
        public void UpdateLink(float tick, Vector3 dirForce, Vector3 delta, float amtMimic)
        {
            if (doScale)
            {
                ScaleLength(tick);

                if (!doScale)//exchange
                {
                    if (lenTo > minSpace)
                    {
                        if (onScaleUp != null)
                            onScaleUp();
                    }
                    else if (onScaleDown != null)
                    {
                        onScaleDown();
                    }
                }
            }
            trCable.rotation = Quaternion.identity;

            //模拟比重 7.15
            Vector3 delta0 = trCable.position - nodes[0];
            delta0 = delta0 * amtMimic;
            for (int i = 0; i < countNode; i++)
            {
                nodes[i] += delta0;
            }
            //
            nodes[0] = trCable.position;
            Refresh(delta, dirForce, tick);
            RefreshMesh(trCable.position, dirForce);
        }
        //从 根节点 开始刷新
        void Refresh(Vector3 delta, Vector3 dirForce, float tick)
        {
            Vector3 virPos;
            Vector3 virDir;
            Vector3 conPos;
            for (int i = 1; i < countNode; i++)
            {
                //重力
                virPos = nodes[i] - delta * softs[i];               //向后 虚位
                virDir = (virPos - nodes[i - 1]).normalized;    //指向 上层节点
                virDir = (virDir + dirForce * (qualitys[i] * tick)).normalized;                    //重力 下拉
                conPos = nodes[i - 1] + virDir * space;         //距离钳制的 临时位置

                //障碍 探测
                delta = nodes[i] - conPos;                              //更新 Delta,为下一个节点
                nodes[i] = conPos;
            }
        }


        #region Scale Length
        public void StartScaledown()
        {
            scaleTimer = 0;
            lenFrom = space;
            lenTo = minSpace;
            doScale = true;
        }
        public void StartScaleup()
        {
            scaleTimer = 0;
            lenFrom = space;
            lenTo = maxSpace;
            doScale = true;
        }
        public void ScaleLength(float tick) //缩放 长度
        {
            scaleTimer += tick;
            if (scaleTimer >= scaleTime)
            {
                scaleTimer = scaleTime;
                doScale = false;//exchange
            }
            this.space = Mathf.Lerp(lenFrom, lenTo, scaleTimer / scaleTime);
            this.scale = (space - minSpace) / (maxSpace - minSpace);

            Vector3 tmp;
            Vector3 delta = Vector3.zero;
            for (int i = 1; i < countNode; i++)
            {
                //(nodes[i]+delta) 加上 上层节点的位移
                tmp = nodes[i - 1] + ((nodes[i] + delta) - nodes[i - 1]).normalized * space;

                delta = tmp - nodes[i];//用来修正 下一个

                nodes[i] = tmp;
            }
        }
        #endregion
        #region Display Mesh
        int countRingV;         //环切
        Vector3[] vertices;
        Vector3[] normals;
        Mesh mesh;
        Bounds bd;
        MeshRenderer InitMesh(int countNode, int countSeg, GameObject gameObject, Vector3 dirForce)
        {
            this.countRingV = countSeg + 1;
            int segLen = countNode - 1;
            Vector3 pos0 = trCable.position;
            vertices = new Vector3[countNode * countRingV];
            normals = new Vector3[countNode * countRingV];
            Vector2[] uv = new Vector2[countNode * countRingV];
            int[] tris = new int[segLen * countSeg * 6];

            //环切 角度
            float ang = Mathf.PI * 2f / countSeg;
            Vector2[] sincos = new Vector2[countRingV];
            for (int r = 0; r < countRingV; r++)
            {
                float arc = ang * r;
                sincos[r] = new Vector2(Mathf.Sin(arc), Mathf.Cos(arc));
            }

            for (int n = 0; n < countNode; n++)
            {
                int offsetN = n * countRingV;
                float v = (float)n / (countNode - 1);
                for (int r = 0; r < countRingV; r++)
                {
                    vertices[offsetN + r] = nodes[n] - pos0;
                    normals[offsetN + r] = dirForce;
                    uv[offsetN + r] = sincos[r];
                }
            }
            int offsetNT, offsetT;
            int offsetNV, offsetV;
            for (int n = 0; n < countNode - 1; n++)
            {
                offsetNT = n * countSeg * 6;
                offsetNV = n * countRingV;
                for (int r = 0; r < countSeg; r++)
                {
                    offsetT = offsetNT + r * 6;
                    offsetV = offsetNV + r;
                    tris[offsetT + 0] = offsetV + 0;
                    tris[offsetT + 1] = offsetV + countRingV;
                    tris[offsetT + 2] = offsetV + 1;

                    tris[offsetT + 3] = offsetV + 1;
                    tris[offsetT + 4] = offsetV + countRingV;
                    tris[offsetT + 5] = offsetV + countRingV + 1;
                }
            }
            //创建 Mesh
            MeshFilter mf = gameObject.AddComponent<MeshFilter>();
            MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            this.mesh = new Mesh();
            mf.sharedMesh = mesh;

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;//        11.2
            mesh.triangles = tris;
            return mr;
        }
        //刷新 Mesh,偏移 pos0,在world 坐标内
        void RefreshMesh(Vector3 pos0, Vector3 dirForce)
        {
            int offsetV;
            Vector3 dir;
            Vector3 vertex;
            for (int n = 0; n < countNode; n++)
            {
                offsetV = n * countRingV;
                vertex = nodes[n] - pos0;
                for (int r = 0; r < countRingV; r++)
                {
                    //vertices[offsetV + r] = nodes[n] - pos0;
                    vertices[offsetV + r] = vertex;
                }
                //
                if (n == 0)
                {
                    for (int r = 0; r < countRingV; r++)
                    {
                        normals[offsetV + r] = -dirForce;// trRoot.up;
                    }
                }
                else
                {
                    dir = nodes[n - 1] - nodes[n];
                    for (int r = 0; r < countRingV; r++)
                    {
                        normals[offsetV + r] = dir; //Shader里面 做归一
                    }
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            //Bounds
            bd.center = (nodes[0] + nodes[countNode - 1]) / 2f - pos0;
            Vector3 vec = nodes[0] - nodes[countNode - 1];
            bd.size = new Vector3(Mathf.Abs(vec.x), Mathf.Abs(vec.y), Mathf.Abs(vec.z));
            mesh.bounds = bd;
        }
        #endregion
#if UNITY_EDITOR
        public void OnDrawGizmos()
        {
            if (nodes == null || nodes.Length < 2) return;

            for (int i = 1; i < countNode; i++)
            {
                Handles.color = (i % 2 == 0 ? Color.blue : Color.green);
                Handles.DrawLine(nodes[i - 1], nodes[i]);
            }
        }
#endif
    }
}