using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

namespace Mg.Games
{
    //一根主线  其它偏移 {Float Hex Model}
    public class TrailViewMultiOffset : MonoBehaviour
    {
        bool isAlive = true;
        public Vector3[] offsets;

        public float length = 5f;
        public float space = 0.1f;
        public float fadeTime = 1f;

        public Material mat;
        public bool doUpdate = false;

        int countSeam;
        int countSeg;
        int countVertex;

        float sqrSpace;
        float fadeNode;     //单位消减
        float time;

        public int countSam = 3;
        int idxSam;
        Vector3[] samPos;
        Vector3 sumPos, avgPos;

        TrailDot[] nodes;   //Wld 空间
        int countNode;
        int head;           //最新点

        Transform trPos;            //位置检测
        Transform trTrail;
        Mesh mesh;
        MeshRenderer render;
        VertexVNU[] points;
        Vector3 dir;
        Vector3 lastPoint;
        float sqr;
        bool dirtyShape = false;
        bool canFade = false;

        int countJet;
        int jetV;

        void Awake()
        {
            GameObject goHide = new GameObject("TrailHide");
            //goHide.hideFlags = HideFlags.HideAndDontSave;
            MeshFilter filter = goHide.AddComponent<MeshFilter>();
            this.render = goHide.AddComponent<MeshRenderer>();
            render.lightProbeUsage = LightProbeUsage.Off;
            render.reflectionProbeUsage = ReflectionProbeUsage.Off;
            render.shadowCastingMode = ShadowCastingMode.Off;
            render.receiveShadows = false;

            this.mesh = new Mesh();
            filter.sharedMesh = mesh;

            trPos = transform;
            trTrail = goHide.transform;
            this.render.sharedMaterial = mat;
            //SetupTrail();

            //ItemSystem.SYS.AddItem(this);
        }
        public void StartTail()   //Hex Seat掉落使用，Hex格子数量改变时 重新生成
        {
            SetupTrail();
            InitTrail();

            render.enabled = true;
            //trTrail.gameObject.SetActive(true);
        }

        void SetupTrail()           //根据参数 初始化数据容器
        {
            this.countSeam = Mathf.CeilToInt(length / space) + 1;
            this.countSeg = countSeam - 1;

            this.countJet = offsets.Length;//线条数量
            this.jetV = countSeam * 2;
            this.countVertex = countSeam * 2 * countJet;

            var triangles = new NativeArray<ushort>(countSeg * 3 * 2 * countJet,Allocator.Temp,NativeArrayOptions.UninitializedMemory);

            for (int j = 0; j < countJet; j++)
            {
                int offsetJetV = jetV * j;
                int offsetJetT = countSeg * 6 * j;
                int offsetT = 0;
                int offsetV = 0;
                for (int i = 0; i < countSeg; i++)
                {                                           //Primitive s
                    offsetT = offsetJetT + i * 6;
                    offsetV = offsetJetV + i * 2;
                    triangles[offsetT + 0] = (ushort)(offsetV + 0);
                    triangles[offsetT + 1] = (ushort)(offsetV + 1);
                    triangles[offsetT + 2] = (ushort)(offsetV + 3);

                    triangles[offsetT + 3] = (ushort)(offsetV + 0);
                    triangles[offsetT + 4] = (ushort)(offsetV + 3);
                    triangles[offsetT + 5] = (ushort)(offsetV + 2);
                }
            }

            points = new VertexVNU[countVertex];
            SubMeshDescriptor desc = new SubMeshDescriptor()
            {
                baseVertex = 0,
                bounds = default,
                indexStart = 0,
                indexCount = countSeg * 6*countJet,
                firstVertex = 0,
                topology = MeshTopology.Triangles,
                vertexCount = countVertex,
            };
            mesh.subMeshCount = 1;
            //mesh.SetSubMesh(0, desc, GeoHelper.silence);

            triangles.Dispose();
            this.sqrSpace = space * space;
            this.fadeNode = fadeTime / countSeg;

            nodes = new TrailDot[countSeam];
            samPos = new Vector3[countSam];
        }
        public void InitTrail()                         //位置发生 跳跃时，重新设定初始位置
        {
            this.doUpdate = true;
            countNode = 0;
            head = -1;
            time = Time.time;

            InitSam();

            this.dir = trPos.forward;
            AddPoint(trPos.position);                      //刷新 第一个点
            AddPoint(trPos.position);

            trTrail.position = trPos.position;
            lastPoint = trPos.position;
        }
        void InitSam()
        {
            for(int i=0;i<countSeam;i++)
            {
                nodes[i].pt = trPos.position;
                //nodes[i].dir = trPos.forward;
            }
            sumPos = Vector3.zero;
            for (int i = 0; i < countSam; i++)
            {
                samPos[i] = trPos.position;
                sumPos += trPos.position;
            }
            avgPos = sumPos / countSam;
            idxSam = 0;

        }
        void UpdateSam()
        {
            idxSam = (idxSam + 1) % countSam;
            sumPos -= samPos[idxSam];
            samPos[idxSam] = trPos.position;
            sumPos += samPos[idxSam];
            avgPos = sumPos / countSam;
        }
        void AddPoint(Vector3 atPos)
        {                                   //添加点
            countNode = Mathf.Min(countSeam, countNode + 1);//记录数
            head = (head + 1) % countSeam;                  //记录头
            TrailDot node = new TrailDot(atPos, dir);
            nodes[head] = node;                             //记录 数据
        }
        void InsertNodes(float dist)
        {
            Vector3 ori = lastPoint;
            int countAdd = Mathf.CeilToInt(dist / space) - 1;
            tmp2 = dir;
            dir = dir / dist;

            tmp = dir * space;
            lastPoint = lastPoint + tmp;
            TrailDot node = new TrailDot(lastPoint, dir);
            nodes[head] = node;

            for (int i = 0; i < countAdd - 1; i++)
            {                           //新建 点
                lastPoint = lastPoint + tmp;
                AddPoint(lastPoint);
            }
            if (countAdd >= 1)
            {
                AddPoint(ori + tmp2);
            }
            trTrail.position = trPos.position;
            canFade = true;
        }
        Vector3 tmp,tmp2;
        //刷新 Mesh上的点
        void RefreshMesh()
        {                                   
            VertexVNU vx = new VertexVNU();
            for (int j = 0; j < countJet; j++)
            {
                Vector3 dx = trPos.right * offsets[j].x + trPos.up * offsets[j].y + trPos.forward * offsets[j].z;
                Vector3 offset = trTrail.position - dx;
                TrailDot node = nodes[0];
                float u = 0;
                float stepU = 1f / Mathf.Max(1, countNode - 1);
                int idx = 0;
                int offsetV = jetV * j;
                for (int i = 0, k = 0, h = head; i < countSeam; i++, k++, h = (h - 1 + countSeam) % countSeam)
                {
                    if (k < countNode)
                    {                           //初始阶段 countNode后的点 使用最后一个node
                        node = nodes[h];
                        u = stepU * k;
                    }
                    idx = offsetV + i * 2;

                    tmp = node.pt - offset;
                    vx.normal = node.dir;
                    vx.pos = tmp;
                    vx.uv = new Vector2(u, 0);
                    points[idx] = vx;
                    vx.uv.y = 1f;
                    points[idx + 1] = vx;
                }
            }
            //mesh.SetVertexBufferData(points, 0, 0, countVertex, 0, GeoHelper.silence);
            //Local Space
            Bounds bd = new Bounds();
            Vector3 vec = nodes[(head - countNode + 1 + countSeam) % countSeam].pt - nodes[head].pt;
            //Offset
            vec = vec + trPos.right * offsets[countJet - 1].x + trPos.up * offsets[countJet - 1].y + trPos.forward * offsets[countJet - 1].z;

            bd.size = new Vector3(Mathf.Abs(vec.x), Mathf.Abs(vec.y), Mathf.Abs(vec.z));
            bd.center = vec / 2f;
            mesh.bounds = bd;

        }
        public void UpdateItem()
        {
            if (!doUpdate||!isAlive) { return; }

            dirtyShape = false;
            this.dir = trPos.position - lastPoint;
            sqr = dir.sqrMagnitude;
            if (sqr > sqrSpace)
            {                               //大于阈值
                UpdateSam();
                dir = avgPos - lastPoint;
                sqr = dir.sqrMagnitude;
                if (sqr > sqrSpace)
                {                           //使用采样点 再检查一次
                    float dist = Mathf.Sqrt(sqr);
                    InsertNodes(dist);
                    time = Time.time;
                    dirtyShape = true;
                }
            }
            if (canFade)
            {
                float dt = Time.time - time;
                if (dt > fadeNode)
                {                               //消减 尾巴
                    int d = Mathf.FloorToInt(dt / fadeNode);
                    int countNew = countNode - d;
                    if (countNew < 2)
                    {
                        countNew = 1;
                        canFade = false;
                    }
                    if (countNew != countNode)
                    {
                        countNode = countNew;
                        time = Time.time - (dt - d * fadeNode);
                        dirtyShape = true;
                    }
                }
            }
            if (dirtyShape)
            {
                RefreshMesh();
            }
        }
        public void DisplayTail(bool isShow) //trTrail.GO 不是此Mono.GO
        {
            isAlive = isShow;
            //trTrail.gameObject.SetActive(isShow);
            render.enabled = isShow;
        }
        private void OnDestroy()
        {
            //IsDestroyed = true;
            if (trTrail != null)
            {
                Destroy(trTrail.gameObject);
            }
        }
//#if UNITY_EDITOR
//        private void OnDrawGizmos()
//        {
//            if (offsets == null || offsets.Length == 0) return;

//            Transform tr = transform;

//            Vector3 atPos = tr.position;
//            for (int j = 0; j < offsets.Length; j++)
//            {
//                Vector3 dx = tr.right * offsets[j].x + tr.up * offsets[j].y + tr.forward * offsets[j].z;
//                Gizmos.DrawLine(atPos, atPos + dx);
//            }

//            if (trTrail == null) return;
//            BoundHelper.DrawBounds(trTrail);
//        }
//#endif
    }
}