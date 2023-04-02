using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mg.Triangulation
{
    public class PolygonHelper
    {
        /* 判断点是否在多边形内 */
        public static bool IsInsidePolygon(List<Vector2> points, Vector2 point)
        {
            float x = point.x;
            float y = point.y;
            float crossY;
            int count = points.Count;
            int countCross = 0;
            for (int i = 0; i < count; i++)
            {
                Vector2 p0 = points[i];
                Vector2 p1 = points[(i + 1) % count];
                if ((p0.x - x) * (p1.x - x) > 0) continue;              //x 同侧
                if ((p0.y - y) > 0 && (p1.y - y) > 0) continue;         //y 顶部, 射线为 y朝下

                if ((p0.x - x) == 0 && (p1.x - x) > 0) continue;        //与一个点相交，一个点必被两个线段共享，只记录 X轴左侧的
                if ((p1.x - x) == 0 && (p0.x - x) > 0) continue;

                if ((p0.x - x) == 0 && (p1.x - x) == 0) continue;       //与射线重合 不计数

                crossY = p0.y + (p1.y - p0.y) * (x - p0.x) / (p1.x - p0.x);
                if (crossY > y) continue;                               //虚拟交点 在 point 点上方
                countCross += 1;
            }
            return countCross % 2 == 1;                                 //交点次数 为奇数，在多边形内部
        }
        /* 
         * 判断点是否在多边形内  --   位于一个点集上的多边形 
         * 从点 P 向下打射线
         */
        public static bool IsInsidePolygon(List<Vector2> points, List<int> pts, Vector2 point)
        {
            float x = point.x;
            float y = point.y;
            float crossY;
            int count = pts.Count;
            int countCross = 0;
            for (int i = 0; i < count; i++)
            {
                Vector2 p0 = points[pts[i]];
                Vector2 p1 = points[pts[(i + 1) % count]];
                if ((p0.x - x) * (p1.x - x) > 0) continue;              //x 同侧
                if ((p0.y - y) > 0 && (p1.y - y) > 0) continue;         //y 顶部, 射线为 y朝下

                if ((p0.x - x) == 0 && (p1.x - x) > 0) continue;        //与一个点相交，一个点必被两个线段共享，只记录 X轴左侧的
                if ((p1.x - x) == 0 && (p0.x - x) > 0) continue;

                if ((p0.x - x) == 0 && (p1.x - x) == 0) continue;       //与射线重合 不计数

                crossY = p0.y + (p1.y - p0.y) * (x - p0.x) / (p1.x - p0.x);
                if (crossY > y) continue;                               //虚拟交点 在 point 点上方

                countCross += 1;
            }
            return countCross % 2 == 1;                                 //交点次数 为奇数，在多边形内部
        }
        /* 
         * 判断点是否在多边形内  --   位于一个点集上的多边形 
         * 从点 P 向下打射线
         */
        public static bool IsInsideTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 point)
        {
            float x = point.x;
            float y = point.y;
            float crossY;
            int count = 3;
            int countCross = 0;
            for (int i = 0; i < count; i++)
            {
                Vector2 p0;
                Vector2 p1;
                if (i == 0) { p0 = a; p1 = b; }
                else if (i == 1) { p0 = b; p1 = c; }
                else { p0 = c; p1 = a; }

                if ((p0.x - x) * (p1.x - x) > 0) continue;              //x 同侧
                if ((p0.y - y) > 0 && (p1.y - y) > 0) continue;         //y 顶部, 射线为 y朝下

                if ((p0.x - x) == 0 && (p1.x - x) > 0) continue;        //与一个点相交，一个点必被两个线段共享，只记录 X轴左侧的
                if ((p0.x - x) == 0 && (p1.x - x) == 0) continue;       //与射线重合 不计数

                crossY = p0.y + (p1.y - p0.y) * (x - p0.x) / (p1.x - p0.x);
                if (crossY > y) continue;                               //虚拟交点 在 point 点上方

                countCross += 1;
            }
            return countCross % 2 == 1;                                 //交点次数 为奇数，在多边形内部
        }
        /* 判断两个线段是否相交 */
        public static bool IsLineCross(Vector2 pA, Vector2 pB, Vector2 p0, Vector2 p1)
        {
            Vector2 dirAB = (pB - pA).normalized;
            Vector2 dir01 = (p1 - p0).normalized;
            Vector2 crossPt = CrossPoint(pA, p0, dirAB, dir01);
            return
                (p0.x - crossPt.x) * (p1.x - crossPt.x) <= 0 &&
                (pA.x - crossPt.x) * (pB.x - crossPt.x) <= 0;
        }
        /* 判定 线段是否与多边形相交 如果p0,p1不在点集内，p0,p1可取值-1 */
        public static bool IsCrossPolygon(List<Vector2> points, List<int> ptsLoop, int idx0, int idx1)
        {
            Vector2 p0 = points[idx0], p1 = points[idx1];
            int countLoop = ptsLoop.Count;
            int ep0, ep1;
            bool cross = false;
            for (int s = 0; s < countLoop; s++)
            {
                ep0 = ptsLoop[s];
                ep1 = ptsLoop[(s + 1) % countLoop];
                /* 排除包含端点<idx0,idx1>的边  */
                if ((idx0 - ep0) * (idx0 - ep1) * (idx1 - ep0) * (idx1 - ep1) == 0) continue;
                if (PolygonHelper.IsLineCross(p0, p1, points[ep0], points[ep1]))
                {
                    cross = true;
                    break;
                }
            }
            return cross;
        }
        /* 直线的交点 */
        public static Vector2 CrossPoint(Vector2 p0, Vector2 p1, Vector2 dir0, Vector2 dir1)
        {
            float x = 0, y = 0;
            if (Mathf.Abs(dir0.x * dir1.x) > float.Epsilon)
            {
                float tan0 = dir0.y / dir0.x;
                float tan1 = dir1.y / dir1.x;
                //y = m0.y + (x - m0.x) * pen0.y / pen0.x;
                //y = m1.y + (x - m1.x) * pen1.y / pen1.x;
                //y = m0.y + (x - m0.x) * tan0;
                //y = m1.y + (x - m1.x) * tan1;
                x = (p1.y - p0.y) - p1.x * tan1 + p0.x * tan0;
                x = x / (tan0 - tan1);//取反
                y = p0.y + (x - p0.x) * tan0;
            }
            /* pen1 平行于 X 轴 */
            else if (Mathf.Abs(dir0.x) > float.Epsilon)
            {
                x = p1.x;
                y = p0.y + (x - p0.x) * dir0.y / dir0.x;
            }
            /* pen0 平行于 X 轴 */
            else //if (Mathf.Abs(pen1.x > float.Epsilon))
            {
                x = p0.x;
                y = p1.y + (x - p1.x) * dir1.y / dir1.x;
            }
            return new Vector2(x, y);
        }

    }
}