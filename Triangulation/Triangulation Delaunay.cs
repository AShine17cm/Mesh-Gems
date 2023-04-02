#define TEST
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mg.Triangulation
{
    /* 一堆三角剖分算法 */
    public partial class Triangulation
    {
        /* 有外接圆信息的三角形  用于Delaunay */
        public struct Triangle
        {
            public int a, b, c;
            public float radius;       //外接圆 半径
            public float sqrRadius;
            public Vector2 center;     //外接圆 圆心
#if TEST
            public Vector2 m0, m1;
            public Vector2 pen0, pen1;
#endif
            public Triangle(int a, int b, int c, List<Vector2> points)
            {
                this.a = a;
                this.b = b;
                this.c = c;
                Vector2 ab = points[b] - points[a];
                Vector2 bc = points[c] - points[b];
                /* ab的垂线(perpendicular) 逆时针翻转 */
                Vector2 pen0 = new Vector2(ab.y, -ab.x);
                Vector2 pen1 = new Vector2(bc.y, -bc.x);
                Vector2 m0 = (points[a] + points[b]) / 2;
                Vector2 m1 = (points[b] + points[c]) / 2;
                pen0.Normalize();
                pen1.Normalize();
                float x = 0, y = 0;
                if (Mathf.Abs(pen0.x * pen1.x) > float.Epsilon)
                {
                    float tan0 = pen0.y / pen0.x;
                    float tan1 = pen1.y / pen1.x;
                    //y = m0.y + (x - m0.x) * pen0.y / pen0.x;
                    //y = m1.y + (x - m1.x) * pen1.y / pen1.x;
                    //y = m0.y + (x - m0.x) * tan0;
                    //y = m1.y + (x - m1.x) * tan1;
                    x = (m1.y - m0.y) - m1.x * tan1 + m0.x * tan0;
                    x = x / (tan0 - tan1);//取反
                    y = m0.y + (x - m0.x) * tan0;
                }
                /* pen1 平行于 X 轴 */
                else if (Mathf.Abs(pen0.x) > float.Epsilon)
                {
                    x = m1.x;
                    y = m0.y + (x - m0.x) * pen0.y / pen0.x;
                }
                /* pen0 平行于 X 轴 */
                else //if (Mathf.Abs(pen1.x > float.Epsilon))
                {
                    x = m0.x;
                    y = m1.y + (x - m1.x) * pen1.y / pen1.x;
                }

                center = new Vector2(x, y);
                radius = (points[a] - center).magnitude;
                sqrRadius = radius * radius;
#if TEST
                this.m0 = m0;
                this.m1 = m1;
                this.pen0 = pen0;
                this.pen1 = pen1;
#endif
            }
            public void Offset(int offset)
            {
                a -= offset;
                b -= offset;
                c -= offset;
            }
            /* 点包含 */
            public bool Contains(Vector2 pt)
            {
                return (pt - center).sqrMagnitude < sqrRadius;
            }
            /* 是否引用了点 */
            public bool Contains(int pt)
            {
                return (a - pt) * (b - pt) * (c - pt) == 0;
            }
            public bool Contains(int pt0, int pt1, int pt2, int pt3)
            {
                return
                    a == pt0 || a == pt1 || a == pt2 || a == pt3 ||
                    b == pt0 || b == pt1 || b == pt2 || b == pt3 ||
                    c == pt0 || c == pt1 || c == pt2 || c == pt3;
            }
            /* 三角形相等 */
            public bool Equals(Triangle triangleB)
            {
                return this.a == triangleB.a && this.b == triangleB.b && this.c == triangleB.c;
            }
        }
        /* 三角形的边 用于重边检测 */
        public struct Border
        {
            public int a, b;
            public int state;
            public Border(int a, int b, int state)
            {
                this.a = a;
                this.b = b;
                this.state = state;
            }
            /* 短点是否相同，无方向 */
            public bool Equals(Border borderB)
            {
                return
                    a == borderB.a && b == borderB.b ||
                    a == borderB.b && b == borderB.a;
            }
            public static void AddSegments(List<Border> borders, Triangle tri)
            {
                borders.Add(new Border(tri.a, tri.b, -1));
                borders.Add(new Border(tri.b, tri.c, -1));
                borders.Add(new Border(tri.c, tri.a, -1));
            }
        }
        /*
         Delaunay  空圆法
         边界点越大，结果越接近凸包
         doShape:是否保证所有三角形位于points所构成的边界内
         */
        public static void Delaunay_BowyerWatson(List<Vector2> points, List<int> tris)
        {
            int countP = points.Count;
            Vector2 tmp;
            /* 寻找边界 */
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < countP; i++)
            {
                tmp = points[i];
                if (tmp.x < minX) minX = tmp.x;
                if (tmp.x > maxX) maxX = tmp.x;
                if (tmp.y < minY) minY = tmp.y;
                if (tmp.y > maxY) maxY = tmp.y;
            }
            float extentX = (maxY - minY) / 2;
            float extentY = (maxX - minX) / 2;
            /*
                <minX,maxY>     <maxX,maxY>
                <minX.minY>     <maxX,minY>
             */
            points.Insert(0, new Vector2(minX - extentX, minY - extentY));
            points.Insert(1, new Vector2(minX - extentX, maxY + extentY));
            points.Insert(2, new Vector2(maxX + extentX, maxY + extentY));
            points.Insert(3, new Vector2(maxX + extentX, minY - extentY));
            /* 用于记录三角形 */
            List<Triangle> triangles = new List<Triangle>(128);
            List<Border> borders = new List<Border>(128);
            /* 初始三角形 */
            triangles.Add(new Triangle(0, 1, 2, points));
            triangles.Add(new Triangle(0, 2, 3, points));
            /* 逐一插入点 */
            for (int i = 4; i < countP + 4; i++)
            {
                tmp = points[i];
                borders.Clear();
                for (int t = triangles.Count - 1; t >= 0; t -= 1)
                {
                    /* 非空圆 */
                    if (triangles[t].Contains(tmp))
                    {
                        /* 将所有边加入一个集合中 */
                        Border.AddSegments(borders, triangles[t]);
                        triangles.RemoveAt(t);
                    }
                }
                /* 检查 在多边形内部的-重合边 */
                for (int s = 0; s < borders.Count; s++)
                {
                    Border border = borders[s];
                    for (int e = s + 1; e < borders.Count + s; e++)
                    {
                        if (border.Equals(borders[e % borders.Count]))
                        {
                            borders[s] = new Border(border.a, border.b, 1);
                            break;
                        }
                    }
                }
                /* 删除重边 */
                for (int s = borders.Count - 1; s >= 0; s -= 1)
                {
                    if (borders[s].state == 1)
                    {
                        borders.RemoveAt(s);
                    }
                }
                /* 对星型多边形  插入多个三角形 */
                for (int s = 0; s < borders.Count; s++)
                {
                    triangles.Add(new Triangle(borders[s].a, borders[s].b, i, points));
                }
            }
            /* 去掉边界点 */
            for (int i = triangles.Count - 1; i >= 0; i -= 1)
            {
                if (triangles[i].Contains(0, 1, 2, 3))
                {
                    triangles.RemoveAt(i);
                }
            }
            /* 检查是否在多边形内 有些边界边未存在于最后的三角形内 */
            //if (keepShape)
            //{
            //    /* 移除插入的4个点,还原多边形 */
            //    points.RemoveRange(0, 4);
            //    Triangle tri;
            //    for(int i = 0; i < triangles.Count; i++)
            //    {
            //        tri = triangles[i];
            //        tri.Offset(4);
            //        triangles[i] = tri;
            //    }

            //    /* 重心一定在三角形内部，外接圆圆心不一定在三角形内部 */
            //    Vector2 bary;
            //    for (int i = triangles.Count - 1; i >= 0; i -= 1)
            //    {
            //        bary = (points[triangles[i].a] + points[triangles[i].b])*0.5f/3 + points[triangles[i].c]*2/3;
            //        //bary = bary / 3;
            //        if (!PolygonHelper.IsInsidePolygon(points, bary))
            //        {
            //            triangles.RemoveAt(i);
            //        }
            //    }
            //}
            /* 整理到普通的点引用 */
            Triangle tria;
            for (int i = 0; i < triangles.Count; i++)
            {
                tria = triangles[i];
                tris.Add(tria.a);
                tris.Add(tria.b);
                tris.Add(tria.c);
            }
        }
    }
}
