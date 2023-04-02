using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* 将 平面上的一组点连接成一个不相交的轮廓线 */
public class DxMonotonePolygon : MonoBehaviour
{
    public List<Transform> trans;
    public bool doTest = false;
    public List<Vector2> lineLoops;        //轮廓线

    MonotonePolygon mPoly;

    void TryConnect()
    {
        List<Vector2> points = new List<Vector2>(128);
        for (int i = 0; i < trans.Count; i++)
        {
            Vector3 pos = trans[i].position;
            points.Add(new Vector2(pos.x, pos.y));
        }

        mPoly = new MonotonePolygon(points);
        mPoly.Process();
    }
    void Update()
    {
        if (doTest)
        {
            doTest = false;
            TryConnect();
        }
    }
    private void OnDrawGizmos()
    {
        if (mPoly==null||mPoly.outPoints.Count < 2) return;
        List<Vector2> lineLoops = mPoly.outPoints;
        int count = lineLoops.Count;
        for (int i = 0; i < count; i++)
        {
            Gizmos.DrawLine(lineLoops[i], lineLoops[(i + 1) % count]);
        }
    }

}
