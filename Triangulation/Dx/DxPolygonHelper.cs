#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Mg.Triangulation;

public class DxPolygonHelper : MonoBehaviour
{
    public List<Transform> trans;
    public Transform target;
    public bool dxTriangle = false;         //在三角形内
    public TestResult result;

    List<Vector2> points = new List<Vector2>(128);
    Vector2 targetPt;
    public enum TestResult
    {
        None = 0,
        InsideTriangle = 1,
        OutsideTriangle = 2,
    }
    void Start()
    {

    }
    void RefreshPoints()
    {
        Vector3 pos = target.position;
        targetPt = new Vector2(pos.x, pos.y);

        points.Clear();
        for (int i = 0; i < trans.Count; i++)
        {
            pos = trans[i].position;
            points.Add(new Vector2(pos.x, pos.y));
        }
    }

    void Update()
    {
        if (dxTriangle)
        {
            dxTriangle = false;
            RefreshPoints();

            bool isInside = PolygonHelper.IsInsideTriangle(points[0], points[1], points[2], targetPt);
            result = isInside ? TestResult.InsideTriangle : TestResult.OutsideTriangle;
        }
    }

    private void OnDrawGizmos()
    {
        /* 投影到 XY平面 */
        {
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            for (int i = 0; i < trans.Count; i++)
            {
                Vector3 pos = trans[i].position;
                Handles.Label(pos, "" + i);
                Gizmos.DrawLine(pos, trans[(i + 1) % trans.Count].position);
            }
        }

        /* 目标测试点 */
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(target.position, Vector3.one * 20f);
    }
}
#endif