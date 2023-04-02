using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

//源自TrailView,播放序列帧
namespace Mg.Games
{
    public class TrailEffect : MonoBehaviour
    {
        bool isAlive = true;
        //序列帧相关
        public int frameCount = 32;
        public float frameTime = 0.1f;

        public float length = 5f;
        public float space = 0.1f;
        public float fadeTime = 1f;
        public Material mat;
        public Color tint;
        public Color tintFade;
        public float wide = 0.05f;

        int countSeam;
        int countSeg;
        int countVertex;

        float sqrSpace;

        int frame = 0;
        float frameTimer = 0;    //序列帧计时

        public int countSam = 10;
        int idxSam;
        Vector3[] samPos;
        Vector3 sumPos, avgPos;

        TrailDot[] nodes;   //Wld 空间
        int countNode;
        int head;           //最新点

        Transform tr;
        Transform trHide;
        Mesh mesh;
        MeshRenderer render;
        MaterialPropertyBlock block;
        VertexVNU[] points;

        Vector3 dir;
        Vector3 lastPoint;
        int idFrame;
        float sqr;
        bool dirtyShape = false;
        //bool canFade = false;
        void Awake()
        {
            GameObject goHide = new GameObject("FxTrail");
            MeshFilter filter = goHide.AddComponent<MeshFilter>();
            render = goHide.AddComponent<MeshRenderer>();
            render.lightProbeUsage = LightProbeUsage.Off;
            render.reflectionProbeUsage = ReflectionProbeUsage.Off;
            render.shadowCastingMode = ShadowCastingMode.Off;
            render.receiveShadows = false;

            this.mesh = new Mesh();
            filter.sharedMesh = mesh;

            tr = transform;
            trHide = goHide.transform;
            //材质

            render.sharedMaterial = mat;
            render.SetPropertyBlock(block);

            SetupTrail();

            ShowTail();//Flight 无初始化?

            //ItemSystem.SYS.AddItem(this);
        }
       public  void ShowTail()
        {
            isAlive = true;
            InitTrail();
            RefreshMesh();//5.17
            render.enabled = true;
        }
        public void HideTail()
        {                           //主动调用 因为有些/飞机 从不需要隐藏
            isAlive = false;
            render.enabled = false;
        }
        void SetupTrail()
        {                                   //根据参数 初始化
            this.countSeam = Mathf.CeilToInt(length / space) + 1;
            this.countSeg = countSeam - 1;
            this.countVertex = countSeam * 2;

            var triangles = new NativeArray<UInt16>(countSeg * 6, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            int offsetT = 0;
            int offsetV = 0;
            for (int i = 0; i < countSeg; i++)
            {                                           //Primitive s
                offsetT = (i * 6);
                offsetV = (i * 2);
                triangles[offsetT + 0] = (UInt16)(offsetV + 0);
                triangles[offsetT + 1] = (UInt16)(offsetV + 1);
                triangles[offsetT + 2] = (UInt16)(offsetV + 3);

                triangles[offsetT + 3] = (UInt16)(offsetV + 0);
                triangles[offsetT + 4] = (UInt16)(offsetV + 3);
                triangles[offsetT + 5] = (UInt16)(offsetV + 2);
            }
            points = new VertexVNU[countVertex];

            SubMeshDescriptor desc = new SubMeshDescriptor()
            {
                baseVertex = 0,
                bounds = default,
                indexStart = 0,
                indexCount = countSeg * 6,
                firstVertex = 0,
                topology = MeshTopology.Triangles,
                vertexCount = countVertex,
            };
            mesh.subMeshCount = 1;
            //mesh.SetSubMesh(0, desc, GeoHelper.silence);
            //mesh.UploadMeshData(false);
            triangles.Dispose();
            this.sqrSpace = space * space;
            //this.fadeNode = fadeTime / countSeg;

            nodes = new TrailDot[countSeam];
            samPos = new Vector3[countSam];
        }
        void InitTrail()
        {
            countNode = 0;
            head = -1;
            //time = Time.time;

            this.dir = tr.forward;
            Vector3 pos = tr.position;
            InitSam(pos);

            //AddPoint(pos);                      //刷新 第一个点
            //AddPoint(pos);

            trHide.position = pos;
            lastPoint = pos;
        }
        void InitSam(Vector3 pos)
        {
            
            sumPos = Vector3.zero;
            for (int i = 0; i < countSam; i++)
            {
                samPos[i] = pos+dir*(-space*i);
                sumPos = sumPos + samPos[i];

                //samPos[i] = pos;
                //sumPos += pos;
                AddPoint(samPos[i]);
            }
            avgPos = sumPos / countSam;
            idxSam = 0;
        }
        void UpdateSam()
        {
            idxSam = (idxSam + 1) % countSam;
            sumPos -= samPos[idxSam];
            samPos[idxSam] = tr.position;
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
            //lastPoint = lastPoint + dir * space;
            lastPoint = lastPoint + tmp;

            //RefineHead(lastPoint);      //更新头
            TrailDot node = new TrailDot(lastPoint, dir);
            nodes[head] = node;

            for (int i = 0; i < countAdd - 1; i++)
            {                           //新建 点
                lastPoint = lastPoint + tmp;// dir * space;
                AddPoint(lastPoint);
            }
            if (countAdd >= 1)
            {
                //AddPoint(ori + dir * dist);
                AddPoint(ori + tmp2);
            }
            trHide.position = tr.position;
            //canFade = true;
        }
        Vector3 tmp, tmp2;
        void RefreshMesh()
        {                                   //刷新 Mesh上的点
            Vector3 offset = trHide.position;
            TrailDot node = nodes[0];
            float u = 0;
            float stepU = 1f / Mathf.Max(1, countNode - 1);
            int idx = 0;
            //var points = new NativeArray<VertexVNU>(countVertex, Allocator.Temp);
            VertexVNU vx = new VertexVNU();
            for (int i = 0, k = 0, h = head; i < countSeam; i++, k++, h = (h - 1 + countSeam) % countSeam)
            {
                if (k < countNode)
                {                           //初始阶段 countNode后的点 使用最后一个node
                    node = nodes[h];
                    u = stepU * k;
                }
                idx = i * 2;

                tmp = node.pt - offset;
                vx.normal = node.dir;
                vx.pos = tmp;
                vx.uv = new Vector2(u, 0);
                points[idx] = vx;
                vx.uv.y = 1f;
                points[idx + 1] = vx;
            }
            //points.Dispose();
            //Local Space
            Bounds bd = new Bounds();
            Vector3 vec = nodes[(head - countNode + 1 + countSeam) % countSeam].pt - nodes[head].pt;
            bd.center = vec / 2f;
            bd.size = new Vector3(Mathf.Abs(vec.x), Mathf.Abs(vec.y), Mathf.Abs(vec.z));
            mesh.bounds = bd;
        }

        //void Update()
        //{
        //    UpdateItem();
        //}
        public void UpdateItem()
        {
            if (!isAlive) return;

            dirtyShape = false;
            dir = tr.position - lastPoint;
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
                    //time = Time.time;
                    dirtyShape = true;
                }
            }
            //if (canFade)
            //{
            //    float dt = Time.time - time;
            //    if (dt > fadeNode)
            //    {                               //消减 尾巴
            //        int d = Mathf.FloorToInt(dt / fadeNode);
            //        int countNew = countNode - d;
            //        if (countNew < 2)
            //        {
            //            countNew = 1;
            //            canFade = false;
            //        }
            //        if (countNew != countNode)
            //        {
            //            countNode = countNew;
            //            time = Time.time - (dt - d * fadeNode);
            //            dirtyShape = true;
            //        }
            //    }
            //}
            if (dirtyShape)
            {
                RefreshMesh();
            }
            //序列帧更新
            frameTimer += Time.deltaTime;
            if (frameTimer >= frameTime)
            {
                frameTimer -= frameTime;
                frame = (frame + 1) % frameCount;
                block.SetInt(idFrame, frame);
                render.SetPropertyBlock(block);
            }
        }

        private void OnDestroy()
        {
            if (trHide != null)
            {
                Destroy(trHide.gameObject);
            }
        }
        //#if UNITY_EDITOR
        //        private void OnDrawGizmos()
        //        {
        //            if (trHide == null) return;
        //            BoundHelper.DrawBounds(trHide);
        //        }
        //#endif
    }
}