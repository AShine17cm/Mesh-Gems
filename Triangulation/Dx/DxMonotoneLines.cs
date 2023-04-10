#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Mg.Triangulation;
using DebugMonoline = Mg.Triangulation.Triangulation.DebugMonoline;

public class DxMonotoneLines : MonoBehaviour
{
    public PolygonSuit suits;
    public Transform polygonTr;
    [Header("测试算法")]
    public bool dxMonoline = false;       //测试全套
    public bool dxSuites = false;
    [Header("初始轮廓线")]
    public bool doOutline = true;
    [Header("展示计算结果中的N段 单调线")]
    [Range(1, 50)]
    public int debugLines = 50;
    [Space(5)]
    [Header("展示一个单调线 和 它的互补多边形 ")]
    public bool doSingle = false;
    public bool nextSingleLoop = false;
    [Range(0, 50)]
    public int singleLoopIndex = 0;

    List<Vector2> points;
    bool hasTri;

    List<int> tris = new List<int>(128);
    DebugMonoline debug;
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (dxMonoline)
        {
            dxMonoline = false;
            hasTri = true;
            tris.Clear();

            points = new List<Vector2>(128);
            int count = polygonTr.childCount;
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = polygonTr.GetChild(i).position;
                points.Add(new Vector2(pos.x, pos.y));
            }
            debug = new DebugMonoline();
            Triangulation.Monotone(points, tris, debug);
        }
        /* 在基础图形上运行算法 */
        if (dxSuites)
        {
            dxSuites = false;
            suits.DoTriangulation(DoSuit);
        }
        if (debug != null && nextSingleLoop)
        {
            nextSingleLoop = false;
            singleLoopIndex += 1;
        }
    }
    void DoSuit(List<Vector2> points,List<int> tris)
    {
        Triangulation.Monotone(points, tris, null);
    }
    private void OnDrawGizmos()
    {
        /* 投影到 XY平面 */
        if (doOutline&&polygonTr!=null)
        {
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            int count = polygonTr.childCount;
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = polygonTr.GetChild(i).position;
                Vector3 posB = polygonTr.GetChild((i + 1) % count).position;
                Handles.Label(pos, "" + i);
                Gizmos.DrawLine(pos, posB);
            }
        }
        if (debug != null)
        {
            if (doSingle)
            {
                debug.DrawSingle(points, singleLoopIndex);
            }
            else
            {
                debug.Draw(points, debugLines);
            }
        }
        if (hasTri && false)
        {
            float k = 0.3f;
            Gizmos.color = new Color(k, k, k, k);
            for (int i = 0; i < tris.Count / 3; i++)
            {
                Gizmos.DrawLine(points[tris[i * 3 + 0]], points[tris[i * 3 + 1]]);
                Gizmos.DrawLine(points[tris[i * 3 + 1]], points[tris[i * 3 + 2]]);
                Gizmos.DrawLine(points[tris[i * 3 + 2]], points[tris[i * 3 + 0]]);
            }
        }
    }
}
#endif