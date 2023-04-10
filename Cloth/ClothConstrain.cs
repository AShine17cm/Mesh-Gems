using System;
using System.Collections.Generic;
using UnityEngine;
namespace Mg.Cloth
{
    /*  用于平行约束 
        不直接修改节点，只对节点产生平衡的应力
     */
    [Serializable]
    public class ConstrainPair
    {
        public int main = -1;
        public int vice = -1;
        public float elastic = 1.0f;
        public float areaMin = 0.6f;
        public void Init()
        {
            elastic = Mathf.Clamp(elastic, 0.01f, 10f);
            areaMin = Mathf.Clamp(areaMin, 0.01f, 1.0f);
        }
        public bool IsLegal(int count)
        {
            return main != vice && main >= 0 && vice >= 0 && main < count && vice < count;
        }
        public bool IsEqual(ConstrainPair b)
        {
            return main == b.main && vice == b.vice ||
                main == b.vice && vice == b.main;
        }
    }
    public class ClothConstrain
    {
        Rope a, vice;
        float[] dists;
        int count;
        float elastic;
        float areaMin;
        Vector2[] areas;//x: 左三角，y:右三角
        /* 主轴/副轴 的区别不大 */
        public ClothConstrain(Rope main, Rope vice, float elastic, float areaMin)
        {
            this.a = main;
            this.vice = vice;
            this.elastic = elastic;
            this.areaMin = areaMin;
            this.count = main.Count;

            dists = new float[count];
            for (int i = 0; i < count; i++)
            {
                dists[i] = Vector3.Distance(main.Nodes[i], vice.Nodes[i]);
            }
            /* 计算初始面积 */
            areas = new Vector2[count];
            areas[0] = Vector2.zero;
            for (int i = 1; i < count; i++)
            {
                Vector3 top_a = a.Nodes[i - 1];
                Vector3 bot_a = a.Nodes[i];
                Vector3 top_v = vice.Nodes[i - 1];
                Vector3 bot_v = vice.Nodes[i];

                /* area.x <top.v,top.a,bot.a 逆时针的顶点序>
                 * area.y <top.v,top.a,bot.v>
                 * 海伦公式
                 */
                float dist_av = (top_a - top_v).magnitude;
                float dist_aa = (top_a - bot_a).magnitude;
                float dist_vv = (top_v - bot_v).magnitude;
                float dist_dia_L = (top_v - bot_a).magnitude;//对角线
                float dist_dia_R = (top_a - bot_v).magnitude;
                /* 上三角,对角线和主轴a 形成的三角形 */
                float p_aa = (dist_dia_L + dist_aa + dist_av) / 2;
                float area_aa = Mathf.Sqrt(p_aa * (p_aa - dist_dia_L) * (p_aa - dist_aa) * (p_aa - dist_av));
                /* 下三角,对角线和副轴vice 形成的三角形 */
                float p_vv = (dist_dia_R + dist_vv + dist_av) / 2;
                float area_vv = Mathf.Sqrt(p_vv * (p_vv - dist_dia_R) * (p_vv - dist_vv) * (p_vv - dist_av));

                areas[i] = new Vector2(area_aa, area_vv);
            }
        }
        public void Simulate(float tick)
        {
            Vector3 dir;
            float dist;
            Vector3 elasticA, elasticVice;
            //面积的变化率+/-
            for (int i = 1; i < count; i++)
            {
                /* 算一下的面积的改变量 */
                Vector3 top_a = a.Nodes[i - 1];
                Vector3 bot_a = a.Nodes[i];
                Vector3 top_v = vice.Nodes[i - 1];
                Vector3 bot_v = vice.Nodes[i];

                /* area.x <top.v,top.a,bot.a 逆时针的顶点序>
                 * area.y <top.v,top.a,bot.v>
                 * 海伦公式
                 * a ----- v     a ---- v
                 * |    /           \   |
                 * |   /             \  |
                 * |  /               \ |
                 * a       v     a      v
                 */
                Vector3 vec_av = top_a - top_v;
                Vector3 vec_aa = bot_a - top_a;
                Vector3 vec_dia_L = top_v - bot_a;

                Vector3 vec_dia_R = bot_v - top_a;
                Vector3 vec_vv = bot_v - top_v;
                //Vector3 vec_av_ = bot_a - bot_v;
                float dist_av = vec_av.magnitude;
                float dist_aa = vec_aa.magnitude;
                float dist_vv = vec_vv.magnitude;
                float dist_dia_L = vec_dia_L.magnitude;//对角线
                float dist_dia_R = vec_dia_R.magnitude;
                /* 上三角,对角线和主轴a 形成的三角形 */
                float p_aa = (dist_dia_L + dist_aa + dist_av) / 2;
                float area_aa = Mathf.Sqrt(p_aa * (p_aa - dist_dia_L) * (p_aa - dist_aa) * (p_aa - dist_av));
                /* 下三角,对角线和副轴vice 形成的三角形 */
                float p_vv = (dist_dia_R + dist_vv + dist_av) / 2;
                float area_vv = Mathf.Sqrt(p_vv * (p_vv - dist_dia_R) * (p_vv - dist_vv) * (p_vv - dist_av));
                /* 面积的变化比率 */
                float k_aa = area_aa / areas[i].x;
                float k_vv = area_vv / areas[i].y;
                Vector3 delta_aa = Vector3.zero;
                /* 面积过小的问题,(面积过大，通过轴距elastic 进行约束) */
                if (k_aa < areaMin)
                {
                    /* 节点距 在Rope中有约束，这里只需要限定角度/高度 */
                    Vector3 planar = Vector3.Cross(vec_av, vec_aa);
                    Vector3 pen = Vector3.Cross(planar, vec_av).normalized;

                    float dot_pen = Vector3.Dot(pen, vec_aa / dist_aa);
                    float dot_av = Vector3.Dot(vec_aa / dist_aa, vec_av / dist_av);
                    //修正高度,保证最小面积，  计算在aa 上的投影长度
                    float proj_pen = dot_pen * dist_aa * areaMin / k_aa;
                    Vector3 dir_aa = pen * proj_pen + vec_av / dist_av * dist_aa * dot_av;
                    dir_aa = dir_aa.normalized * dist_aa;
                    delta_aa = dir_aa - vec_aa;
                    //Vector3 vec_aaX = Vector3.Lerp(pen, vec_aa, k_aa * k_aa).normalized * dist_aa;//xx
                    //delta_aa = vec_aaX - vec_aa;
                }
                Vector3 delta_vv = Vector3.zero;
                /* 下三角可以通过 elastic 约束 */
                if (k_vv < areaMin)
                {
                    vec_av = -vec_av;
                    Vector3 planar = Vector3.Cross(vec_av, vec_vv);
                    Vector3 pen = Vector3.Cross(planar, vec_av).normalized;

                    float dot_pen = Vector3.Dot(pen, vec_vv / dist_vv);
                    float dot_av = Vector3.Dot(vec_vv / dist_vv, vec_av / dist_av);
                    //修正高度,保证最小面积，  计算在aa 上的投影长度
                    float proj_pen = dot_pen * dist_vv * areaMin / k_vv;
                    Vector3 dir_vv = pen * proj_pen + vec_av / dist_av * dist_vv * dot_av;
                    dir_vv = dir_vv.normalized * dist_vv;
                    delta_vv = dir_vv - vec_vv;
                }

                dir = vice.Nodes[i] - a.Nodes[i];
                dist = dir.magnitude;
                dir = dir / dist;
                /* 
                 * 节点之间的拉伸/压缩 
                 */
                float deltaDist = dist - dists[i];
                elasticVice = -dir * deltaDist * elastic;
                elasticA = -elasticVice;

                //elasticA = elasticVice = Vector3.zero;
                /* 扭曲钳制 和 弹性伸缩 */
                a.Constrains[i] += (elasticA * tick + delta_aa);
                vice.Constrains[i] += (elasticVice * tick + delta_vv);
            }
        }
        //public void SimulateX(float tick)
        //{
        //    if (isFail) return;
        //    Vector3 preDir = (vice.Nodes[0] - a.Nodes[0]).normalized;
        //    Vector3 preAxis = (a.Nodes[1] - a.Nodes[0]).normalized;

        //    Vector3 dir;
        //    float dist;
        //    Vector3 twistA, twistVice;
        //    Vector3 elasticA, elasticVice;
        //    Vector3 backA, backVice;
        //    for (int i = 1; i < count; i++)
        //    {
        //        twistA = twistVice = Vector3.zero;
        //        elasticA = elasticVice = Vector3.zero;
        //        backA = backVice = Vector3.zero;

        //        dir = vice.Nodes[i] - a.Nodes[i];
        //        dist = dir.magnitude;
        //        dir = dir / dist;
        //        float dot_pre = Vector3.Dot(preDir, dir);

        //        /* 防止向后插入 不同深度节点之间 产生的平面 */
        //        Vector3 axis_a = (a.Nodes[i] - a.Nodes[i - 1]).normalized;
        //        Vector3 axis_v = (vice.Nodes[i] - vice.Nodes[i - 1]).normalized;

        //        Vector3 tan = Vector3.Cross(preAxis, dir);
        //        Vector3 planar = Vector3.Cross(dir, tan).normalized;//垂直于dir(因此 a-a,或者 vice-vice没有区别),与pre-axis 同向
        //        float dot_planar_a = Vector3.Dot(planar, axis_a);
        //        float dot_planar_v = Vector3.Dot(planar, axis_v);
        //        if (dot_planar_a < backCos)
        //        {
        //            tan = Vector3.Cross(axis_a, planar);
        //            Vector3 proj = Vector3.Cross(planar, tan).normalized;
        //            backA = (planar * backCos + proj * backSin - axis_a) * dist;
        //        }
        //        if (dot_planar_v < backSin)
        //        {
        //            tan = Vector3.Cross(axis_v, planar);
        //            Vector3 proj = Vector3.Cross(planar, tan).normalized;
        //            axis_v = planar * backCos + proj * backSin;
        //            backVice = (planar * backCos + proj * backSin - axis_v) * dist;
        //        }
        //        /* 节点产生了穿插？ 以父节点上的轴向为标准 */
        //        if (dot_pre < twistCos)
        //        {
        //            planar = Vector3.Cross(preDir, dir);
        //            tan = Vector3.Cross(planar, preDir).normalized;
        //            Vector3 dirRefined = preDir * twistCos + tan * twistSin;
        //            Vector3 delta = (dirRefined - dir) * dist / 2;
        //            twistVice = delta;
        //            twistA = -delta;
        //        }
        //    }
        //}
    }

}
