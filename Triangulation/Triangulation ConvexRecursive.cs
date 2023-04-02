using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mg.Triangulation
{
    /* 
     * 通过删除凸点 不断剖分 (网站上的原始算法有问题，对swirl形状 无法完全剖分)
     * https://jz.docin.com/p-758865445.html
     */
    public partial class Triangulation
    {
        /* maxLimit 用于调试 */
        public static void ConvexRecursive(List<Vector2> points, List<int> tris,int maxLimit=int.MaxValue)
        {
            /* 建立对原始点的引用 */
            List<int> pts = new List<int>(128);
            List<int> ptsSub = new List<int>(128);
            for (int i = 0; i < points.Count; i++)
            {
                pts.Add(i);
            }
            /* 三角形顶点 */
            Vector2 a, b, c;
            /* a,c的相邻点 */
            Vector2 adjacentX, adjacentY;
            /* 三角形 顶点 */
            int apexM,apex0,apex1;
            int countPt = pts.Count;
            int counter = 0;
            /* 检查pts中的每一个点 是否凸点 */
            for (int i = 0; i < countPt;)
            {
                apex0 = pts[i];
                apexM = pts[(i + 1) % countPt];
                apex1 = pts[(i + 2) % countPt];
                a = points[apex0];
                b = points[apexM];
                c = points[pts[(i + 2) % countPt]];
                adjacentX = points[pts[(i - 1 + countPt) % countPt]];
                //adjacentY = points[pts[(i + 3) % countPt]];
                /* 去掉b后的点集 */
                ptsSub.Clear();
                ptsSub.AddRange(pts);
                ptsSub.RemoveAt((i + 1) % countPt);

                /* 
                 * 反包(三角形可能反包 剩下的点) 临近点是否在 三角形<a,b,c>内
                 * 必须两个邻接点 都在三角形内部
                 */
                if (PolygonHelper.IsInsideTriangle(a, b, c, adjacentX))//一个点在内，可能构成反包，也可能是相交
                {
                    //i += 1;
                    i = (i + 1) % countPt;
                    continue;
                }
                //if (PolygonHelper.IsInsideTriangle(a, b, c, adjacentY))
                //{
                //    i += 1;
                //    continue;
                //}
                /*
                 *点在 剩余的多边形内
                 */
                if (PolygonHelper.IsInsidePolygon(points, ptsSub, b))
                {
                    //i += 1;
                    i = (i + 1) % countPt;
                    continue;
                }
                /*
                 点与 剩余的多边形相交
                 */
                int countSub = ptsSub.Count;
                int s0, s1;
                bool cross = false;
                for (int s = 0; s < countSub; s++)
                {
                    s0 = ptsSub[s];
                    s1 = ptsSub[(s + 1) % countSub];
                    /* 排除重边  <s,s+1>  端点重合的边 */
                    if ((apex0 - s0) * (apex0 - s1) * (apex1 - s0) * (apex1 - s1) == 0) continue;
                    if (PolygonHelper.IsLineCross(a, c, points[ptsSub[s]], points[ptsSub[(s + 1) % countSub]]))
                    {
                        cross = true;
                        break;
                    }
                }
                if (cross)
                {
                    //i += 1;
                    i = (i + 1) % countPt;
                    continue;
                }
                /*
                 * 可以删除的凸点, 添加三角形 <a,b,c>
                 * i 不变,下一次还从点 i 开始检查
                 */
                tris.Add(pts[i]);
                tris.Add(pts[(i + 1) % countPt]);
                tris.Add(pts[(i + 2) % countPt]);
                pts.RemoveAt((i + 1) % countPt);
                countPt = pts.Count;
                //i = i;
                /* 只剩下3个点(如果三角形去掉一点，会剩下2个点 ) */
                if (countPt == 3)
                {
                    tris.Add(pts[0]);
                    tris.Add(pts[1]);
                    tris.Add(pts[2]);
                    break;
                }else if (countPt < 3)
                {
                    break;
                }
                /* 循环 */
                //if (i + 1 == countPt)
                //{
                //    i = 0;
                //}
                i = (i + 1) % countPt;
                /* 步进调试 */
                counter += 1;
                if (counter >= maxLimit) { break; }
            }
        }
    }
}