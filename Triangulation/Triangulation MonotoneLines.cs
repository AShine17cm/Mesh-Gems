#define TEST
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mg.Triangulation
{
    /* 
     * 沿着时针方向旋转(+/-)
     * 不超过180度
     */
    public partial class Triangulation
    {
        /* 单向旋转 不超过180 */
        public class Monoline
        {
            public List<int> pts;       //一个单向旋转线段
#if TEST
            public List<int> subs;      //与pts 构成互补的图
            public int end;
#endif
            public Monoline(List<int> of_Pts,List<int> of_Subs,int end)
            {
                pts = new List<int>(of_Pts.Count);
                pts.AddRange(of_Pts);
#if TEST
                if (of_Subs != null)
                {
                    subs = new List<int>(of_Subs.Count);
                    subs.AddRange(of_Subs);
                }
                this.end = end;
#endif
            }
            public void Triangulation(List<int> tris, List<Vector2> points)
            {
                if (points == null)
                {
                    /* 不额外添加点 */
                    for (int i = 1; i < pts.Count - 1; i++)
                    {
                        tris.Add(pts[0]);
                        tris.Add(pts[i]);
                        tris.Add(pts[i + 1]);
                    }
                }
                else
                {
                    /* 插入一个点 */
                    Vector2 mid = (points[pts[0]] + points[pts[pts.Count - 1]]) / 2;
                    points.Add(mid);
                    int idxMid = points.Count - 1;
                    for (int i = 0; i < pts.Count - 1; i++)
                    {
                        tris.Add(idxMid);
                        tris.Add(pts[i]);
                        tris.Add(pts[i+1]);
                    }
                }
            }
#if TEST
            public void DrawDebugInfo(List<Vector2> points,bool showSub)
            {
                /* 连线 和 垂线 */
                for (int p = 0; p < pts.Count - 1; p++)
                {
                    Vector2 a = points[pts[p]];
                    Vector2 b = points[pts[p + 1]];
                    Vector2 vec = b - a;
                    vec = new Vector2(-vec.y, vec.x);
                    //vec.Normalize();
                    Vector2 mid = (a + b) / 2;
                    Gizmos.DrawLine(a, b);
                    Gizmos.DrawLine(mid, mid+vec/3);
                }
                /* 黑色的转折线 */
                if (end > 0&&!showSub)
                {
                    Vector2 a = points[pts[pts.Count - 1]];
                    Vector2 b = points[end];
                    Vector2 vec = b - a;
                    vec = new Vector2(-vec.y, vec.x);
                    Vector2 mid = (a + b) / 2;

                    Gizmos.color = Color.black;
                    Gizmos.DrawLine(a, b);
                    Gizmos.DrawLine(mid, mid + vec / 2);
                }
                if (showSub&&subs!=null)
                {
                    /* 余下的多边形 */
                    Gizmos.color = new Color(0, 1, 0, 0.6f);    //绿色
                    int idx0 = pts[0];
                    int idx1 = pts[pts.Count - 1];
                    int idxA, idxB;
                    for(int p = 0; p < subs.Count; p++)
                    {
                        idxA = subs[p];
                        idxB = subs[(p + 1) % subs.Count];
                        if(idxA==idx0&&idxB==idx1||
                            idxA == idx1 && idxB == idx0)
                        {
                            continue;
                        }
                        Vector2 a = points[idxA];
                        Vector2 b = points[idxB];
                        Vector2 vec = b - a;
                        vec = new Vector2(-vec.y, vec.x);
                        Vector2 mid = (a + b) / 2;
                        Gizmos.DrawLine(a, b);
                        Gizmos.DrawLine(mid, mid + vec / 2);
                    }
                }
            }
#endif
        }
#if TEST
        public class DebugMonoline
        {
            public List<Monoline> monolines = new List<Monoline>(128);

            /* 展示N个 单调线 */
            public void Draw(List<Vector2> points, int maxLines) 
            {
                int count = monolines.Count;
                Monoline ML;
                for (int i = 0; i < maxLines && i < count; i++)
                {
                    ML = monolines[i];
                    if (i % 2 == 0)
                    {
                        Gizmos.color = new Color(0, 0, 1, 0.6f);    //蓝色
                    }
                    else
                    {
                        Gizmos.color = new Color(1, 0, 0, 0.6f);    //红色
                    }
                    ML.DrawDebugInfo(points,false);
                }
            }
            /* 展示一个单调线 和 它的互补多边形 */
            public void DrawSingle(List<Vector2> points, int index)
            {
                int count = monolines.Count;
                Monoline ML;
                if (index >= count)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(points[0], 1f);
                }
                else
                {
                    ML = monolines[index];
                    Gizmos.color = new Color(1, 0, 0, 0.6f);    //红色
                    ML.DrawDebugInfo(points,true);
                }
            }
        }
#endif
        public static void Monotone(List<Vector2> points, List<int> tris, DebugMonoline debug)
        {
            List<int> loop = new List<int>(128);                    //待测的Loop
            List<int> subLoop = new List<int>(128);                 //切除单调旋转线后 剩余部分
            List<int> clockLoop = new List<int>(128);               //切出来的单调旋转线
            List<Monoline> monolines = new List<Monoline>(128);     //可三角化的单调旋转线
            for (int i = 0; i < points.Count; i++)
            {
                loop.Add(i);
            }

            int loopPts = loop.Count;
            /* 初始点 */
            Vector2 initDir, initCrossDir;
            /* 上一个点 */
            Vector2 lastDir, lastCrossDir;
            /* 当前点 */
            Vector2 dirN, crossDirN;
            /* 前一个时针序，每次初始为0 */
            float lastClock;
            for (int i = 0; i < loopPts;)
            {
                initDir = points[loop[(i + 1) % loopPts]] - points[loop[i]];
                /* 垂直方向-顺时针  不影响结果 */
                initCrossDir = new Vector2(-initDir.y, initDir.x);
                lastDir = initDir;
                lastCrossDir = initCrossDir;
                /* 初始化 clock-wise为 0 */
                lastClock = 0;
                int k = i + 1;
                /* 检查 单调旋转线的结束 */
                while (true)
                {
                    bool tryDevide = false;
                    dirN = points[loop[(k + 1) % loopPts]] - points[loop[k % loopPts]];
                    crossDirN = new Vector2(-dirN.y, dirN.x);
                    //float dotDir = Vector2.Dot(lastDir, dirN);  与上一个线段大于90的转角?  会检测到摇摆
                    float clock = Vector2.Dot(lastCrossDir, dirN);
                    /* 旋转方向 发生改变 (第一次==0) */
                    if (lastClock * clock < 0)
                    {
                        tryDevide = true;
                    }

                    /* 自开始旋转了 180度 ? */
                    float clock_180 = Vector2.Dot(initCrossDir, dirN);
                    if (lastClock * clock_180 < 0)
                    {
                        tryDevide = true;
                    }
                    lastClock = clock;
                    lastCrossDir = crossDirN;

                    if (tryDevide)
                    {
                        subLoop.Clear();
                        subLoop.AddRange(loop);
                        /*  k - <?> 不包括 k */
                        if (k > subLoop.Count)
                        {
                            int countA = subLoop.Count - (i + 1);
                            int countB = k % subLoop.Count;
                            subLoop.RemoveRange(i + 1, countA);
                            subLoop.RemoveRange(0, countB);
                        }
                        else
                        {
                            subLoop.RemoveRange(i + 1, k - (i + 1));    //i,k 在loop上
                        }

                        /* 单调旋转线 */
                        clockLoop.Clear();
                        for (int m = i; m <= k; m++)
                        {
                            clockLoop.Add(loop[m%loopPts]);
                        }
                        bool canDevide = true;
                        /* 
                         * 反包 邻接点是否在 单调旋转线 内 
                         * 必须两个点都在 单调旋转线内
                         */
                        Vector2 adjacentX = points[loop[(i - 1 + loopPts) % loopPts]];
                        //Vector2 adjacentY = points[pts[(k + 1) % countPt]];
                        if (PolygonHelper.IsInsidePolygon(points, clockLoop, adjacentX))//一个点在内，可能反包，也可能相交
                        {
                            //无法分离
                            canDevide = false;
                        }

                        /* 线段上任一点(非端点) 是否在剩下的多边形内 */
                        Vector2 monoPt = points[clockLoop[1]];
                        if (PolygonHelper.IsInsidePolygon(points, subLoop, monoPt))
                        {
                            //无法分离
                            canDevide = false;
                        }
                        /* 端点连线是否 与剩下的多边形相交 */
                        if (PolygonHelper.IsCrossPolygon(points, subLoop, clockLoop[0], clockLoop[clockLoop.Count - 1]))
                        {
                            //无法分离
                            canDevide = false;
                        }
                        /* 
                         * 可分割?
                         * i 保持不变？
                         * */
                        if (canDevide)
                        {
                            monolines.Add(new Monoline(clockLoop,subLoop,loop[(k + 1) % loopPts]));
                            loop.Clear();
                            loop.AddRange(subLoop);
                            loopPts = loop.Count;
                        }
                        i = (i + 1) % loopPts;  //删除<i+1,k-(i+1)>内的点后，在剩下的loop上实际只移动了一个位置
                        break;
                    }//tryDevide 单调性可否分离?

                    /* 没有检测到可分离的单调线，继续k */
                    k = k + 1;
                    /* 一个回路，最后结果是个凸多边形 */
                    if (k % loopPts == i)
                    {
                        monolines.Add(new Monoline(loop,null, -1));
                        loop.Clear();
                        loopPts = -1;
                        break;
                    }
                }//while(true)

                if (loopPts == 3)
                {
                    monolines.Add(new Monoline(loop,null, -3));
                    break;
                }else if (loopPts < 3)
                {
                    break;
                }
            }
            /* 连接三角形 */
            for (int i = 0; i < monolines.Count; i++)
            {
                Monoline ML = monolines[i];
                if (ML.pts.Count > 3)
                {
                    ML.Triangulation(tris, points);
                }
                else
                {
                    ML.Triangulation(tris, null);
                }
            }
#if TEST
            debug?.monolines.AddRange(monolines);
#endif
        }


    }
}