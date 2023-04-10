#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Mg.Triangulation;

public class DxConvexRecursive : MonoBehaviour
{
    public Transform polygonTr;
    public PolygonSuit suit;
    public bool dxConvex = false;       //测试全套
    public bool dxSuite = false;
    public int stepCount = 1;           //逐步测试
    public bool doOutline = true;
    List<Vector2> points;
    bool hasTri;

    List<int> tris = new List<int>(128);

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (dxConvex)
        {
            dxConvex = false;
            hasTri = true;
            tris.Clear();

            points = new List<Vector2>(128);
            int count = polygonTr.childCount;
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = polygonTr.GetChild(i).position;
                points.Add(new Vector2(pos.x, pos.y));
            }

            Triangulation.ConvexRecursive(points, tris,stepCount);
        }
        if (dxSuite)
        {
            dxSuite = false;
            suit.DoTriangulation(DoSuit);
        }
    }
    void DoSuit(List<Vector2> points, List<int> tris)
    {
        Triangulation.ConvexRecursive(points, tris);
    }

    private void OnDrawGizmos()
    {
        /* 投影到 XY平面 */
        if (doOutline)
        {
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            int count = polygonTr.childCount;
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = polygonTr.GetChild(i).position;
                Vector3 posB = polygonTr.GetChild((i + 1) % count).position;
                Handles.Label(pos, "" + i);
                Gizmos.DrawLine(pos, posB);
            }
        }
        if (hasTri)
        {
            float k = 0.3f;
            Gizmos.color = new Color(k, k, k, k);
            for(int i = 0; i < tris.Count / 3; i++)
            {
                Gizmos.DrawLine(points[tris[i * 3 + 0]], points[tris[i * 3 + 1]]);
                Gizmos.DrawLine(points[tris[i * 3 + 1]], points[tris[i * 3 + 2]]);
                Gizmos.DrawLine(points[tris[i * 3 + 2]], points[tris[i * 3 + 0]]);
            }
        }
    }
}
#endif