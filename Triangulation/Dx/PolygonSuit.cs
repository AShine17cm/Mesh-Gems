#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public delegate void TriangulationAct(List<Vector2> points, List<int> tris);
/* 用于多边形的测试 */
public class PolygonSuit : MonoBehaviour
{
    [Serializable]
    public class Suit
    {
        public Transform root;
        public Color tint = Color.white;
        public Color tintTris = Color.blue;
        public Color triLabel = new Color(0, 1, 0, 0.3f);
        public Color ptLabel = new Color(1, 0, 0, 0.9f);
        public List<Vector2> points;                    //使用 Local Space
        public List<int> tris;
        public Vector2 rootPos;
        public List<Vector2> trianglePoints;           //三角剖分 可能会增加顶点
        public void Init()
        {
            if (root == null || root.childCount < 3) return;

            points.Clear();
            int count = root.childCount;
            Vector3 a;
            a = root.position;
            rootPos = new Vector2(a.x, a.y);
            for (int i = 0; i < count; i++)
            {
                a = root.GetChild(i).localPosition;
                points.Add(new Vector2(a.x, a.y));
            }
        }
        public void Draw(bool drawTris = false)
        {
            Vector2 a, b, c, bary;
            Gizmos.color = tint;
            GUIStyle style = new GUIStyle(GUIStyle.none);
            style.normal.textColor = ptLabel;
            /* 轮廓线 在world space, 使用rootPos做偏移 */
            for (int i = 0; i < points.Count; i++)
            {
                a = rootPos + points[i];
                b = rootPos + points[(i + 1) % points.Count];
                Gizmos.DrawLine(a, b);
                Handles.Label(new Vector3(a.x, a.y, 0), "" + i,style);
            }

            if (drawTris)
            {
                Gizmos.color = tintTris;
                style.normal.textColor = triLabel;
                /* 三角面 */
                for (int i = 0; i < tris.Count / 3; i++)
                {
                    a = rootPos + trianglePoints[tris[i * 3 + 0]];
                    b = rootPos + trianglePoints[tris[i * 3 + 1]];
                    c = rootPos + trianglePoints[tris[i * 3 + 2]];
                    bary = (a + b + c) / 3;
                    Gizmos.DrawLine(a, b);
                    Gizmos.DrawLine(b, c);
                    Gizmos.DrawLine(c, a);
                    Handles.Label(new Vector3(bary.x, bary.y, 0), "" + i,style);
                }
            }
        }
        
    }
    public bool drawTris = true;
    public List<Suit> suits;
    public int targetSuit = -1;
    /* 使用算法做三角剖分 */
    public void DoTriangulation(TriangulationAct method)
    {
        for(int i = 0; i < suits.Count; i++)
        {
            if (targetSuit >= 0 && i != targetSuit) continue;
            suits[i].Init();
            suits[i].tris.Clear();
            suits[i].trianglePoints.Clear();
            suits[i].trianglePoints.AddRange(suits[i].points);
            method(suits[i].trianglePoints, suits[i].tris);
        }
    }
    private void OnDrawGizmos()
    {
        for (int i = 0; i < suits.Count; i++)
        {
            if (suits[i] == null) continue;
            suits[i].Init();
            suits[i].Draw(drawTris);
        }
    }

}
#endif
