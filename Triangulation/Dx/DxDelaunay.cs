#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Mg.Triangulation;
using Triangle = Mg.Triangulation.Triangulation.Triangle;
public class DxDelaunay : MonoBehaviour
{
    public Transform polygonTr;
    public bool dxCircumcircle = false; //测试单个外接圆
    public bool dxDelaunay = false;     //测试全套
    //public bool keepShape = true;      //保证在多边形内部  有些边界边不存在于最后的三角形内
    public Triangle tri;
    List<Vector2> points;
    bool hasTri;

    bool hasDelaunay;
    List<int> tris = new List<int>(128);

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (dxCircumcircle)
        {
            dxCircumcircle = false;
            points = new List<Vector2>(128);
            int count = polygonTr.childCount;
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = polygonTr.GetChild(i).position;
                points.Add(new Vector2(pos.x, pos.y));
            }

            hasTri = true;
            tri = new Triangle(0, 1, 2, points);
        }
        if (dxDelaunay)
        {
            dxDelaunay = false;
            hasTri = false;
            hasDelaunay = true;
            tris.Clear();

            points = new List<Vector2>(128);
            int count = polygonTr.childCount;
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = polygonTr.GetChild(i).position;
                points.Add(new Vector2(pos.x, pos.y));
            }
            Triangulation.Delaunay_BowyerWatson(points, tris);
        }
    }

    private void OnDrawGizmos()
    {
        /* 投影到 XY平面 */
        if (!hasDelaunay)
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
            /* 外接圆 */
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(tri.center, points[0]);
            Gizmos.DrawWireSphere(tri.center,tri.radius);
            /* 垂线 */
            Gizmos.color = Color.green;
            Gizmos.DrawLine(tri.m0, tri.m0 + tri.pen0*60);
            Gizmos.DrawLine(tri.m1, tri.m1 + tri.pen1*60);
        }
        if (hasDelaunay)
        {
            Gizmos.color = new Color(0.5f, 0.5f, 0, 0.4f);
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