using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Mg.Triangulation
{
    /* 边界, 可以通过一个<Null Face>集合得到 */
    public partial class HalfEdgeProcessor
    {
        /*
          flip
         <---- 
                ( pt )
          ---->    |
            face   |
                   |
                   v
         */
        public static List<Face> tmpFaces = new List<Face>(128);
        public static List<Vertex> tmpVertices = new List<Vertex>(128);
        public static List<HalfEdge> tmpEdges = new List<HalfEdge>(128);

        public class HalfEdge
        {
            public int dxId;                    //用于调试
            public Vertex enter_pt;             //进入的点
            public HalfEdge flip;               //对边，可能为null, 如果是边界上的半边
            public HalfEdge next;               //面上的 顺序下一边
            public Face face;                   //所囊括的面

            /* 返回前一个边 */
            public HalfEdge PreEdge
            {
                get
                {
                    Vertex stopVertex = enter_pt;
                    HalfEdge edge = this;
                    while (stopVertex != edge.next.enter_pt)
                    {
                        edge = edge.next;
                    }
                    return edge;
                }
            }
            /* 端点 <a,b> */
            public void GetEndPoints(out Vector3 a,out Vector3 b)
            {
                a = PreEdge.enter_pt.Val;
                b = enter_pt.Val;
            }
            public bool IsBorder(List<Face> holes)
            {
                return holes.Contains(flip.face);
            }
        }
        public class Vertex
        {
            public int id;              //便于比较两个点是否相同
            public float x, y, z;
            //离开此点一个半边
            public HalfEdge out_edge;

            public Vertex(int id,Vector3 v3)
            {
                this.id = id;
                this.Val = v3;
            }
            public Vertex(int id,Vector2 v2)
            {
                this.id = id;
                this.x = v2.x;
                this.y = v2.y;
            }
            
            public Vector3 Val
            {
                set 
                { 
                    x = value.x;
                    y = value.y;
                    z = value.z;
                }
                get { return new Vector3(x, y, z); }
            }

            /* 获取顶点的法线 (共顶点的面的法线) */
            public Vector3 Normal
            {
                get
                {
                    tmpFaces.Clear();
                    Get_Vertex_Faces(this, tmpFaces);
                    Vector3 nm = Vector3.zero;
                    for(int i = 0; i < tmpFaces.Count; i++)
                    {
                        nm += tmpFaces[i].Normal;
                    }
                    return nm.normalized;
                }
            }
        }
        public class Face
        {
            public HalfEdge edge;               //面的任一边界边

            /* 返回面的法线 */
            public Vector3 Normal
            {
                get
                {

                    HalfEdge edgeX = edge;
                    HalfEdge stopEdge = edgeX;
                    Vector3 a, b,c;
                    Vector3 vec_ab, vec_bc;
                    Vector3 normal;
                    while (true)
                    {
                        a = edgeX.enter_pt.Val;
                        b = edgeX.next.enter_pt.Val;
                        c = edgeX.next.next.enter_pt.Val;

                        vec_ab = a - b;
                        vec_bc = c - b;
                        normal = Vector3.Cross(vec_ab, vec_bc);
                        /* 法线正常 */
                        if (Mathf.Abs(normal.x) + Mathf.Abs(normal.y) + Mathf.Abs(normal.z) >float.Epsilon)
                        {
                            return normal.normalized;
                        }
                        else
                        {
                            edgeX = edge.next;
                        }
                        if (stopEdge == edgeX)
                        {
                            return Vector3.up;
                        }
                    }
                }
            }
        }
        /* 端点 和 邻接面 */
        //public void AdjacencyQueryCode(HalfEdge edge)
        //{
        //    Vertex vertex0 = edge.enter_pt;
        //    Vertex vertex1 = edge.flip.enter_pt;

        //    Face face0 = edge.face;
        //    Face face1 = edge.flip.face;
        //}
        /* 获取共享一个顶点的<面> */
        public static void Get_Vertex_Faces(Vertex vertex,List<Face> resultFaces)
        {
            HalfEdge edge = vertex.out_edge.flip;       //使用进入边
            Face face = edge.face;
            Face stopFace = face;
            while(true)
            {
                /* 共享顶点的 <面> */
                resultFaces.Add(face);

                edge = edge.next.flip;                  //也是一个 进入Vertex的线
                face = edge.face;
                
                if (stopFace == face)
                {
                    break;
                }
            };
        }
        /* 获取进入一个点的所有半边 */
        public static void Get_Vertex_InEdges(Vertex vertex,List<HalfEdge> edges)
        {
            HalfEdge edge = vertex.out_edge.flip;       //使用进入边
            HalfEdge stopEdge = edge;
            while (true)
            {
                edges.Add(edge);

                edge = edge.next.flip;                  //下一个
                if (stopEdge == edge)
                {
                    break;
                }
            }
        }
        /* 获取从一个点出发的所有半边 */
        public static void Get_Vertex_OutEdges(Vertex vertex,List<HalfEdge> edges)
        {
            HalfEdge edge = vertex.out_edge.flip;       //使用进入边
            HalfEdge stopEdge = edge;
            while (true)
            {
                edges.Add(edge.flip);                   //添加 翻转边

                edge = edge.next.flip;                  //下一个
                if (stopEdge == edge)
                {
                    break;
                }
            }
        }
        /* 获取 一个面的<边界> */
        public static void Get_FaceBorders(Face face,List<HalfEdge> resultEdges)
        {
            HalfEdge edge = face.edge;
            HalfEdge stopEdge = edge;
            while(true)
            {
                /* 结果< 边 > */
                resultEdges.Add(edge);
                edge = edge.next;
                /* 循环到 初始边 */
                if(stopEdge== edge)
                {
                    break;
                }
            };
        }
        /* 获取一个 半边的循环 */
        public static void Get_EdgeLoop(HalfEdge ofEdge,List<HalfEdge> resultEdges)
        {
            HalfEdge edge = ofEdge;
            HalfEdge stopEdge = edge;
            while (true)
            {
                /* 结果< 边 > */
                resultEdges.Add(edge);
                edge = edge.next;
                /* 循环到 初始边 */
                if (stopEdge == edge)
                {
                    break;
                }
            }
        }
        /* 获取一个循环边 的所有<顶点> */
        public static void Get_EdgeLoop_Vertices(HalfEdge ofEdge,List<Vertex> resultVertices)
        {
            Vertex vertex = ofEdge.enter_pt;
            Vertex stopVertex = vertex;
            HalfEdge edge = ofEdge;
            while (true)
            {
                /* 结果 <顶点> */
                resultVertices.Add(vertex);

                edge = edge.next;
                vertex = edge.enter_pt;
                /* 循环到初始顶点 */
                if (stopVertex == vertex)
                {
                    break;
                }
            }
        }
    }
}