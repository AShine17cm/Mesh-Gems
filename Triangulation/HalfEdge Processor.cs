using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Mg.Triangulation
{
    public partial class HalfEdgeProcessor
    {
        public List<Face> holes = new List<Face>(128);
        public List<Face> faces = new List<Face>(128);              //不包含 hole
        public List<Vertex> vertices = new List<Vertex>(128);
        public List<HalfEdge> edges = new List<HalfEdge>(128);      //不包含 border,但是可以通过 flip得到
        //public List<HalfEdge> borders = new List<HalfEdge>(128);  // 这些 half edge 是无序的

        /* 用于判定共享边 */
        struct Edge
        {
            public int a, b;
            public Edge(int a, int b)
            {
                this.a = a;
                this.b = b;
            }
            //是否 为对边
            public bool IsFlipPair(Edge test)
            {
                return a == test.b && b == test.a;
            }
            //共端点
            public bool IsSharePts(Edge test)
            {
                return a == test.a && b == test.b || a == test.b && b == test.a;
            }
            //共端点，切同方向
            public bool IsSame(Edge test)
            {
                return a == test.a && b == test.b;
            }
        }
        /* 
         * 假设所有三角面定义良好，都是一个时针方形 
         * 如果不是一个时针方形，半边的 flip 可能会指向同一个方向
         * (硬边，比如Cube 可能在corner处，一个point 被分割为多个vertex)
         */
        public void Process(List<Vector3> points, List<int> tris,float wieldDist)
        {
            if (wieldDist > 0)
            {
                WieldVertexs(points, tris,wieldDist*wieldDist);
            }
            /* 初始化顶点数据，顺序存储, 
             * 此时半边不存在,出边稍后设置 */
            Vertex vertex;
            for (int i = 0; i < points.Count; i++)
            {
                vertex = new Vertex(i, points[i]);
                vertices.Add(vertex);
            }
            /* 初始化三角面数据，顺序存储, 此时半边不存在 */
            Face face;
            for (int i = 0; i < tris.Count / 3; i++)
            {
                face = new Face();
                faces.Add(face);
            }

            /* 生成半边, 按面添加 */
            HalfEdge edge;
            for (int i = 0; i < tris.Count / 3; i++)
            {
                /* 一个三角面的半边数据 */
                for (int k = 0; k < 3; k++)
                {
                    edge = new HalfEdge();                      //进入边
                    edge.enter_pt = vertices[tris[i * 3 + k]];  //进入点
                    edge.face = faces[i];                       //所包围的面  等位存储
                    edge.flip = null;
                    edge.face.edge = edge;                      //完善 face的边信息
                    edge.dxId = edges.Count;
                    edges.Add(edge);
                }
                /* 完善半边信息  下一个边
                 * 完善顶点信息 out_edge
                 */
                for (int k = 0; k < 3; k++)
                {
                    edge = edges[i * 3 + k];
                    edge.next = edges[i * 3 + (k + 1) % 3];
                    /* 完善顶点信息 */
                    edge.enter_pt.out_edge = edge.next;
                }
            }

            /* 寻找 edge 的对边 */
            for (int i = 0; i < tris.Count / 3; i++)
            {
                int countPair = 0;
                // <a,b>  <b,c> <c,a>
                for (int k = 0; k < tris.Count / 3; k++)
                {
                    /* 排除自身 */
                    if (i == k) continue;

                    bool hasPair = false;
                    for (int m = 0; m < 3; m++)
                    {
                        /* 使用进入点<i*3+m> */
                        //int idHe = i * 3 + m;
                        Edge he = new Edge(tris[i * 3 + (m - 1 + 3) % 3], tris[i * 3 + m]);
                        for (int n = 0; n < 3; n++)
                        {
                            Edge heX = new Edge(tris[k * 3 + (n - 1 + 3) % 3], tris[k * 3 + n]);
                            /* 对边，假设所有三角形一个时针方向 */
                            //if(he.IsSharePts(heX))
                            if (he.IsFlipPair(heX))
                            {
                                edges[i * 3 + m].flip = edges[k * 3 + n];
                                edges[k * 3 + n].flip = edges[i * 3 + m];
                                hasPair = true;
                                countPair += 1;
                                break;
                            }
                        }
                        /* 2个三角形 最多一个共享边 */
                        if (hasPair)
                        {
                            break;
                        }
                    }
                    /* 一个内部三角形需要3个pair */
                    if (countPair == 3)
                    {
                        break;
                    }

                }//遍历三角面
            }//寻找Edge的边界边

            /* 寻找Hole */
            tmpEdges.Clear();
            for (int i = 0; i < edges.Count; i++)
            {
                if (null == edges[i].flip)
                {
                    tmpEdges.Add(edges[i]);
                }
            }
            //return;

            /* borders 这些边是无序的 */
            List<HalfEdge> borders = new List<HalfEdge>(tmpEdges.Count);

            HalfEdge border;
            Vertex tmpVertex;
            /* 创建对边 */
            for (int i = 0; i < tmpEdges.Count; i++)
            {
                border = tmpEdges[i];
                tmpVertex = border.PreEdge.enter_pt;
                /* 创建对边 */
                HalfEdge borderFlip = new HalfEdge();
                borderFlip.enter_pt = tmpVertex;
                borderFlip.next = null;
                borderFlip.face = null;
                /* 互设 对边 */
                borderFlip.flip = border;
                border.flip = borderFlip;
                /* 使用对边id 的负值 */
                borderFlip.dxId = -border.dxId;
                borders.Add(borderFlip);
            }
            /* 查找 next, next必然在border集合中 */
            int enter_id;
            for (int i = 0; i < borders.Count; i++)
            {
                border = borders[i];
                enter_id = border.enter_pt.id;
                /* 查找 next */
                for (int k = 0; k < borders.Count; k++)
                {
                    if (enter_id == borders[k].flip.enter_pt.id)
                    {
                        border.next = borders[k];
                        break;
                    }
                }
            }
            /* 创建 face */
            HalfEdge stopEdge;
            for (int i = 0; i < borders.Count; i++)
            {
                border = borders[i];
                if (null != border.face) continue;
                /* 对每一个未归属于Face的边界 添加 */
                Face holeFace = new Face();
                holeFace.edge = border;
                holes.Add(holeFace);

                stopEdge = border;
                int counter = 0;
                while (true)
                {
                    /* 如果未正确的构造edge的loop,就会无限循环下去 */
                    counter++;
                    if (counter > borders.Count)
                    {
                        break;
                    }

                    border.face = holeFace;
                    if(stopEdge == border.next)
                    {
                        break;
                    }
                    border = border.next;
                }
            }
        }

        /* 焊接 硬边的顶点 */
        public void WieldVertexs(List<Vector3> vertices,List<int> tris,float sqrVault)
        {
            int count = vertices.Count;
            List<int> reMap = new List<int>(count);
            /* 默认映射到 自己 */
            for(int i = 0; i < count; i++)
            {
                reMap.Add(i);
            }
            for(int i = 0; i < count; i++)
            {
                if (reMap[i] <i) continue;         //此点已经被映射过
                Vector3 pt = vertices[i];
                for(int k = i+1; k < count; k++)
                {
                    Vector3 test = vertices[k];
                    /* 映射到点 <i> */
                    if ((test - pt).sqrMagnitude < sqrVault)
                    {
                        reMap[k] = i;
                    }
                }
            }
            /* 重新映射顶点 */
            int countT = tris.Count;
            for(int i = 0; i < countT; i++)
            {
                tris[i] = reMap[tris[i]];
            }
        }
    }
}