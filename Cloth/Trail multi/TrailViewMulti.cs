using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

namespace Mg.Games
{
    public struct VertexVNU
    {
        public Vector3 normal;
        public Vector3 pos;
        public Vector2 uv;
    }
    //使用 喷射点集合 {FlightBlade}
    public class TrailViewMulti : MonoBehaviour
    {
        bool isAlive = true;
        [Header("喷射点")] public Transform[] jets;
        int countJet;
        int jetV;

        public float length = 5f;
        public float space = 0.1f;
        public float fadeTime = 1f;
        public int countSam = 3;

        public Material mat;
        public Color tint;
        public float wide=0.05f;

        int countSeam;      //平行线段
        int countSeg;       
        int countVertex;    //全部 顶点

        float sqrSpace;
        float fadeNode;     //单位消减

        //单轨×N
        float[] time;       //时间点
        int[] idxSam;
        int[] countNode;
        int[] head;         //最新点
        Vector3[][] samPos;
        Vector3[] sumPos, avgPos;
        Vector3[] lastPoint;
        Vector3[] dirs;
        TrailDot[][] nodes; //Wld 空间
        bool[] canFade;

        Transform tr;
        Transform trTrail;           //平移 TrTrail,刷新Mesh顶点
        MeshRenderer render;
        Mesh mesh;
        VertexVNU[] points;

        float sqr;
        bool dirtyShape = false;

        void Awake()
        {
            GameObject goHide = new GameObject("Trail_Object");
            //goHide.hideFlags = HideFlags.HideAndDontSave;
            MeshFilter filter = goHide.AddComponent<MeshFilter>();
            this.render = goHide.AddComponent<MeshRenderer>();
            render.lightProbeUsage = LightProbeUsage.Off;
            render.reflectionProbeUsage = ReflectionProbeUsage.Off;
            render.shadowCastingMode = ShadowCastingMode.Off;
            render.receiveShadows = false;

            this.mesh = new Mesh();
            filter.sharedMesh = mesh;

            tr = transform;
            trTrail = goHide.transform;

            SetupTrail();

            ShowTail();

            //ItemSystem.SYS.AddItem(this);
        }
        void ShowTail()
        {
            InitTrail();
            render.enabled = true;
        }
        void SetupTrail()
        {                                   //根据参数 初始化
            this.countJet = jets.Length;
            this.countSeam = Mathf.CeilToInt(length / space) + 1;
            this.countSeg = countSeam - 1;
            this.countVertex = countJet * countSeam * 2;
            this.jetV = countSeam * 2;
            //单轨×N
            this.time = new float[countJet];
            this.idxSam = new int[countJet];
            this.sumPos = new Vector3[countJet];
            this.avgPos = new Vector3[countJet];
            this.countNode = new int[countJet];
            this.head = new int[countJet];
            this.lastPoint = new Vector3[countJet];
            this.dirs = new Vector3[countJet];
            this.canFade = new bool[countJet];

            nodes = new TrailDot[countJet][];
            samPos = new Vector3[countJet][];
            for (int j = 0; j < countJet; j++)
            {
                nodes[j] = new TrailDot[countSeam];
                samPos[j] = new Vector3[countSam];
            }
            var triangles = new NativeArray<ushort>(countSeg *6* countJet,Allocator.Temp,NativeArrayOptions.UninitializedMemory);

            //Triangle
            for (ushort j = 0; j < countJet; j++)
            {
                ushort offsetT = 0;
                ushort offsetV = 0;
                ushort offsetJetT = (ushort)((countSeg * 6) * j);
                ushort offsetJetV = (ushort)(jetV * j);

                for (ushort i = 0; i < countSeg; i++)
                {                                           //Primitive s
                    offsetT = (ushort)(offsetJetT + i * 6);
                    offsetV = (ushort)(offsetJetV + i * 2);
                    triangles[offsetT + 0] = (ushort)(offsetV + 0);
                    triangles[offsetT + 1] = (ushort)(offsetV + 1);
                    triangles[offsetT + 2] = (ushort)(offsetV + 3);

                    triangles[offsetT + 3] = (ushort)(offsetV + 0);
                    triangles[offsetT + 4] = (ushort)(offsetV + 3);
                    triangles[offsetT + 5] = (ushort)(offsetV + 2);
                }
            }
            points = new VertexVNU[countVertex];
            //mesh.SetVertexBufferParams(countVertex, GeoHelper.vnu);
            //mesh.SetVertexBufferData<VertexVNU>(points, 0, 0, countVertex, 0, GeoHelper.silence);
            //mesh.SetIndexBufferParams(countSeg * 6*countJet, IndexFormat.UInt16);
            //mesh.SetIndexBufferData<ushort>(triangles, 0, 0, countSeg * 6*countJet, GeoHelper.silence);
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
        }
        void InitTrail()
        {
            trTrail.position = tr.position;
            for(int j=0;j<countJet;j++)
            {
                countNode[j] = 0;
                head[j] = -1;
                time[j] = Time.time;
                canFade[j] = false;
                //
                idxSam[j] = 0;
                sumPos[j] = Vector3.zero;
                for (int i = 0; i < countSam; i++)
                {
                    samPos[j][i] = jets[j].position;
                    sumPos[j] += jets[j].position;
                }
                avgPos[j] = sumPos[j] / countSam;
                dirs[j] = jets[j].forward;

                AddPoint(j,jets[j].position);                      //刷新 第一个点
                AddPoint(j,jets[j].position);

                lastPoint[j] = jets[j].position;
            }
        }

        void AddPoint(int ofJet,Vector3 atPos)
        {                                   //添加点
            countNode[ofJet] = Mathf.Min(countSeam, countNode[ofJet] + 1);//记录数
            head[ofJet] = (head[ofJet] + 1) % countSeam;                  //记录头
            TrailDot node = new TrailDot(atPos, dirs[ofJet]);
            nodes[ofJet][head[ofJet]] = node;                             //记录 数据
        }
        void InsertNodes(int ofJet, float dist)
        {
            Vector3 ori = lastPoint[ofJet];
            int countAdd = Mathf.CeilToInt(dist / space) - 1;
            tmp2 = dirs[ofJet];
            dirs[ofJet] = dirs[ofJet] / dist;

            tmp = dirs[ofJet] * space;
            lastPoint[ofJet] = lastPoint[ofJet] + tmp;

            //更新头
            TrailDot node = new TrailDot(lastPoint[ofJet], dirs[ofJet]);
            nodes[ofJet][head[ofJet]] = node;

            for (int i = 0; i < countAdd - 1; i++)
            {                           //新建 点
                lastPoint[ofJet] = lastPoint[ofJet] + tmp;
                AddPoint(ofJet, lastPoint[ofJet]);
            }
            if (countAdd >= 1)
            {
                AddPoint(ofJet, ori + tmp2);
            }
            trTrail.position = tr.position;
            canFade[ofJet] = true;
        }
        Vector3 tmp,tmp2;
        void RefreshMesh()
        {                                   //刷新 Mesh上的点
            Vector3 offset = trTrail.position;
            VertexVNU vx = new VertexVNU();
            for (int j=0;j<countJet;j++)
            {
                TrailDot node = nodes[j][0];
                float u = 0;
                float stepU = 1f / Mathf.Max(1, countNode[j] - 1);
                int idx = 0;
                int offsetV = jetV * j;
                for (int i = 0, k = 0, h = head[j]; i < countSeam; i++, k++, h = (h - 1 + countSeam) % countSeam)
                {
                    if (k < countNode[j])
                    {                           //初始阶段 countNode后的点 使用最后一个node
                        node = nodes[j][h];
                        u = stepU * k;
                    }
                    idx =offsetV+ i * 2;

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
            Vector3 vec = nodes[countJet - 1][(head[countJet - 1] - countNode[countJet - 1] + 1 + countSeam) % countSeam].pt - nodes[0][head[0]].pt;
            bd.size = new Vector3(Mathf.Abs(vec.x), Mathf.Abs(vec.y), Mathf.Abs(vec.z));
            bd.center = vec / 2f;
            mesh.bounds = bd;
        }
        public void UpdateItem()
        {
            if (!isAlive) return;

            dirtyShape = false;
            //canFade = false;
            for(int j=0;j<countJet;j++)
            {
                dirs[j] = jets[j].position - lastPoint[j];
                sqr = dirs[j].sqrMagnitude;
                if (sqr > sqrSpace)
                {                               //大于阈值
                    idxSam[j] = (idxSam[j] + 1) % countSam;
                    int idx = idxSam[j];
                    sumPos[j] -= samPos[j][idx];
                    samPos[j][idx] = jets[j].position;
                    sumPos[j] += samPos[j][idx];
                    avgPos[j] = sumPos[j] / countSam;
                    //
                    dirs[j] = avgPos[j] - lastPoint[j];
                    sqr = dirs[j].sqrMagnitude;
                    if (sqr > sqrSpace)
                    {                           //使用采样点 再检查一次
                        float dist = Mathf.Sqrt(sqr);
                        InsertNodes(j, dist);
                        time[j] = Time.time;
                        dirtyShape = true;
                    }
                }
                if (canFade[j])
                {
                    float dt = Time.time - time[j];
                    if (dt > fadeNode)
                    {                               //消减 尾巴
                        int d = Mathf.FloorToInt(dt / fadeNode);
                        int countNew = countNode[j] - d;
                        if (countNew < 2)
                        {
                            countNew = 1;
                            canFade[j] = false;
                        }
                        if (countNew != countNode[j])
                        {
                            countNode[j] = countNew;
                            time[j] = Time.time - (dt - d * fadeNode);
                            dirtyShape = true;
                        }
                    }
                }
            }

            if (dirtyShape)
            {
                RefreshMesh();
            }
        }

        //void Update()
        //{
        //    UpdateItem();
        //}
        private void OnEnable()
        {
            isAlive = true;
            render.enabled = true;
            //trTrail.gameObject.SetActive(true);
        }
        private void OnDisable()
        {
            isAlive = false;
            if (trTrail != null)
                render.enabled = false;
            //trTrail.gameObject.SetActive(false);
        }
        private void OnDestroy()
        {
            if (trTrail != null)
            {
                Destroy(trTrail.gameObject);
            }
        }
//#if UNITY_EDITOR
//        private void OnDrawGizmos()
//        {
//            //Vector3 atPos = transform.position;
//            if (jets == null) return;
//            Transform ty = null;
//            for(int j=0;j<jets.Length;j++)
//            {
//                ty = jets[j];
//                if(ty!=null)
//                {
//                    Gizmos.DrawLine(ty.position,jets[j].position+ty.forward);
//                }
//            }
//            if (trTrail == null) return;
//            BoundHelper.DrawBounds(trTrail);
//        }
//#endif
    }
}