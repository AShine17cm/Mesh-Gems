using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mg.Triangulation;
using DebugInfo = Mg.Triangulation.HalfEdgeProcessor.DebugInfo;
/* 使用三角剖分 得到一下基本的三角形 */
public class dxHalfEdge : MonoBehaviour
{
    public Transform trMeshes;
    public bool dxMeshes = false;
    public int testIdx = -1;
    public float wieldDist = 0.00001f;
    public DebugInfo debugInfo;
    List<HalfEdgeProcessor> heProcessors = new List<HalfEdgeProcessor>(128);
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (dxMeshes)
        {
            dxMeshes = false;
            heProcessors.Clear();
            int count = trMeshes.childCount;
            for (int i = 0; i < count; i++)
            {
                if (testIdx >= 0 && i != testIdx) continue;
                Transform tr = trMeshes.GetChild(i);
                Process(tr);
            }
        }
    }
    void Process(Transform tr)
    {
        MeshFilter mf = tr.GetComponent<MeshFilter>();
        Mesh mesh = mf.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        int[] tris = mesh.triangles;
        /* 将顶点转换到世界坐标，方便debug 画线 */
        List<Vector3> points = new List<Vector3>(128);
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 wldPos = tr.TransformPoint(vertices[i]);
            points.Add(wldPos);
        }
        List<int> triangles = new List<int>(tris);
        HalfEdgeProcessor processor = new HalfEdgeProcessor();
        processor.Process(points, triangles,wieldDist);
        heProcessors.Add(processor);
    }

    private void OnDrawGizmos()
    {
        int count = heProcessors.Count;
        for(int i = 0; i < count; i++)
        {
            heProcessors[i].Draw(debugInfo);
        }
    }
}
