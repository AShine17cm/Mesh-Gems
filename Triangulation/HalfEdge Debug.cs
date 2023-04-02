#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Mg.Triangulation
{
    /* 用于画出一些调试信息 */
    public partial class HalfEdgeProcessor
    {
        public List<HalfEdge> dxEdges = new List<HalfEdge>(128);
        /* 用于调试画线 */
        [Serializable]
        public class DebugInfo
        {
            public DrawKind kind = DrawKind.Hole;
            public int specialIndex = -1;
            /* 半边的展示参数 */
            public float offsetHead = 0.2f;
            public float offsetTail = 0.2f;
            public float offsetPos = 0.02f;
            public float arrowLen = 1f;
            public float edgeLabel = 0.3f;
            [Header("小于0 不画出")]
            public float normalLen = -1;
            public Color text = Color.white;
            [Header("循环变化染色")]
            public List<Color> tints;
        }
        [Flags]
        public enum DrawKind : int
        {
            None = 0,
            Hole = 1,
            Face = 2,
            EdgeNumber = 4,
            FaceNumber = 8,
        }
        public void Draw(DebugInfo debugInfo)
        {
            GUIStyle style = new GUIStyle(GUIStyle.none);
            style.normal.textColor = debugInfo.text;
            int count;
            int countTint = debugInfo.tints.Count;
            DrawKind kind = debugInfo.kind;
            int specialIdx = debugInfo.specialIndex;

            if ((kind & DrawKind.Face) != 0)
            {
                count = faces.Count;
                for (int i = 0; i < count; i++)
                {
                    /* 单独画出 */
                    if (specialIdx > 0 && i != specialIdx) continue;
                    /* 循环染色 */
                    Gizmos.color = debugInfo.tints[i % countTint];
                    DrawFace(faces[i], i, debugInfo, style);
                }
            }
            /* 画洞口 等于画边界 */
            if ((kind & DrawKind.Hole) != 0)
            {
                count = holes.Count;
                for (int i = 0; i < count; i++)
                {
                    /* 单独画出 */
                    if (specialIdx > 0 && i != specialIdx) continue;
                    /* 循环染色 */
                    Gizmos.color = debugInfo.tints[i % countTint];
                    DrawFace(holes[i], i, debugInfo, style);
                }
            }

        }
        /* 画出一个面的边界 和 id, 颜色在外面设定 */
        void DrawFace(Face face, int id, DebugInfo info, GUIStyle labelStyle)
        {
            bool edgeNum = (DrawKind.EdgeNumber & info.kind) != 0;
            bool faceNum = (DrawKind.FaceNumber & info.kind) != 0;
            tmpEdges.Clear();
            Get_FaceBorders(face, tmpEdges);
            Vector3 normal = face.Normal;

            Vector3 a_1, a, b, b_1;
            Vector3 dir0, dir1, dir2;
            Vector3 offsetTail, offsetHead;
            Vector3 offsetPos;
            Vector3 bary = Vector3.zero;
            int count = tmpEdges.Count;
            int edgeId;
            Color lineColor = Gizmos.color;
            Color arrowColor = new Color(0, 0, 0, 0.7f);
            Color normalColor = new Color(1, 1, 1, 0.5f);
            for (int i = 0; i < count; i++)
            {
                a_1 = tmpEdges[((i - 2) + count) % count].enter_pt.Val;
                a = tmpEdges[(i - 1 + count) % count].enter_pt.Val;
                b = tmpEdges[i].enter_pt.Val;
                b_1 = tmpEdges[(i + 1) % count].enter_pt.Val;
                edgeId = tmpEdges[i].dxId;
                /* 将边向面内偏移 */
                dir0 = (a - a_1).normalized;
                dir1 = (b - a).normalized;
                dir2 = (b_1 - b).normalized;

                float distAB = (b - a).magnitude;
                /* 前后裁剪，不能大于a,b点距离,不然箭头画出来，方向是反的 */
                if (distAB * 0.7f > (info.offsetTail + info.offsetHead))
                {
                    offsetTail = dir1 * info.offsetTail;
                    offsetHead = -dir1 * info.offsetHead;
                }
                else
                {
                    offsetHead = Vector3.zero;
                    offsetTail = Vector3.zero;
                }
                /* 偏移方向 */
                Vector3 cornerDir = (-dir1 + dir2).normalized;
                Vector3 nm = Vector3.Cross(dir1, cornerDir);
                Vector3 orth = Vector3.Cross(cornerDir, nm).normalized;
                offsetPos = orth * info.offsetPos;
                Vector3 ptA = a + offsetPos + offsetTail;
                Vector3 ptB = b + offsetPos + offsetHead;

                /* a,b点连线 */
                Gizmos.color = lineColor;
                Gizmos.DrawLine(ptA, ptB);
                /* 箭头, 使之偏向 a,b线 */
                Gizmos.color = arrowColor;
                Gizmos.DrawLine(ptB, ptB + (cornerDir - dir1).normalized * info.arrowLen);

                bary += a;
                /* 边的id */
                if (edgeNum)
                {
                    Vector3 mid = (ptA + ptB) / 2;// + orth * info.edgeLabel;
                    Handles.Label(mid, "" + edgeId, labelStyle);
                }
            }
            /* 面的id */
            if (faceNum)
            {
                bary = bary / count;
                Handles.Label(bary, "" + id, labelStyle);
            }
            /* 面的法线 */
            if (info.normalLen > 0)
            {
                Gizmos.color = normalColor;
                Gizmos.DrawLine(bary, bary + normal * info.normalLen);
            }

        }
    }
}
#endif