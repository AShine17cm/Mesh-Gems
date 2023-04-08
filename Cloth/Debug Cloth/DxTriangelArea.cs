using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* 测试 三角形面积 */
public class DxTriangelArea : MonoBehaviour
{
    public Transform topA, topV, botA, botV;
    public Vector2 area;
    public float areaMin = 0.7f;

    Vector3 planar;
    Vector3 pen;
    Vector3 new_aa;
    Vector3 delta_aa;

    Vector3 top_a;
    Vector3 bot_a;
    Vector3 top_v;
    Vector3 bot_v;
    void Start()
    {
        top_a = topA.position;
        bot_a = botA.position;
        top_v = topV.position;
        bot_v = botV.position;

        /* area.x <top.a,top.v,bot.a>
         * area.y <top.v,bot.v,bot.a>
         * 海伦公式
         */
        float dist_av = (top_a - top_v).magnitude;
        float dist_aa = (top_a - bot_a).magnitude;
        float dist_vv = (top_v - bot_v).magnitude;
        float dist_av_ = (bot_a - bot_v).magnitude;
        float dist_dia = (top_v - bot_a).magnitude;//对角线
        /* 上三角,对角线和主轴a 形成的三角形 */
        float p_aa = (dist_dia + dist_aa + dist_av) / 2;
        float area_aa = Mathf.Sqrt(p_aa * (p_aa - dist_dia) * (p_aa - dist_aa) * (p_aa - dist_av));
        /* 下三角,对角线和副轴vice 形成的三角形 */
        float p_vv = (dist_dia + dist_vv + dist_av_) / 2;
        float area_vv = Mathf.Sqrt(p_vv * (p_vv - dist_dia) * (p_vv - dist_vv) * (p_vv - dist_av_));

        area = new Vector2(area_aa, area_vv);
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 top_a = topA.position;
        Vector3 bot_a = botA.position;
        Vector3 top_v = topV.position;
        Vector3 bot_v = botV.position;

        TriangleArea(top_a, bot_a, top_v, bot_v, area);
    }
    void TriangleArea(Vector3 top_a,Vector3 bot_a,Vector3 top_v,Vector3 bot_v,Vector2 area)
    {
        /* area.x <top.v,top.a,bot.a 逆时针的顶点序>
         * area.y <bot.a,top.v,bot.v,顺时针的顶点序>
         * 海伦公式
         * a ----- v
         *      /
         *     /
         *    /
         * a ----- v
         */
        Vector3 vec_av = top_a - top_v;
        Vector3 vec_aa = bot_a - top_a;
        Vector3 vec_dia = top_v - bot_a;
        Vector3 vec_vv = bot_v - top_v;
        Vector3 vec_av_ = bot_a - bot_v;
        float dist_av = vec_av.magnitude;
        float dist_aa = vec_aa.magnitude;
        //dist_aa = (this.bot_a - this.top_a).magnitude;//保持aa的长度
        float dist_vv = vec_vv.magnitude;
        float dist_av_ = vec_av_.magnitude;
        float dist_dia = vec_dia.magnitude;//对角线
        /* 上三角,对角线和主轴a 形成的三角形 */
        float p_aa = (dist_dia + dist_aa + dist_av) / 2;
        float area_aa = Mathf.Sqrt(p_aa * (p_aa - dist_dia) * (p_aa - dist_aa) * (p_aa - dist_av));
        /* 下三角,对角线和副轴vice 形成的三角形 */
        float p_vv = (dist_dia + dist_vv + dist_av_) / 2;
        float area_vv = Mathf.Sqrt(p_vv * (p_vv - dist_dia) * (p_vv - dist_vv) * (p_vv - dist_av_));
        /* 面积的变化比率 */
        float k_aa = area_aa / area.x;
        float k_vv = area_vv / area.y;
        Vector3 delta_aa = Vector3.zero;
        /* 面积过小的问题,(面积过大，通过轴距elastic 进行约束) */
        if (k_aa < areaMin||true)
        {
            /* 节点距 在Rope中有约束，这里只需要限定角度/高度 */
            Vector3 planar = Vector3.Cross(vec_av, vec_aa);
            Vector3 pen = Vector3.Cross(planar, vec_av).normalized;
            float dot_pen = Vector3.Dot(pen, vec_aa / dist_aa);
            float dot_av = Vector3.Dot(vec_aa/dist_aa, vec_av/dist_av);
            //修正高度,保证最小面积，  计算在aa 上的投影长度
            float proj_pen = dot_pen * dist_aa *areaMin / k_aa;
            delta_aa = pen * proj_pen + vec_av/dist_av*dist_aa * dot_av - vec_aa;
            //画线的数据
            this.planar = planar;
            this.pen = pen;
            this.delta_aa = delta_aa;
            this.new_aa = pen * proj_pen + vec_av/dist_av*dist_aa*dot_av;
        }
    }

    private void OnDrawGizmos()
    {
        if (!topA || !topV || !botA || !botV) return;

        Vector3 top_a = topA.position;
        Vector3 top_v = topV.position;
        Vector3 bot_a = botA.position;
        Vector3 bot_v = botV.position;

        Gizmos.color = Color.grey;
        Gizmos.DrawLine(top_v, bot_a);

        Gizmos.DrawLine(top_a, top_v);
        Gizmos.DrawLine(top_a, bot_a);

        Gizmos.DrawLine(top_v, bot_v);
        Gizmos.DrawLine(bot_a, bot_v);

        Gizmos.color = Color.black;
        Gizmos.DrawLine(this.top_v,this.bot_a);
                       
        Gizmos.DrawLine(this.top_a,this.top_v);
        Gizmos.DrawLine(this.top_a,this.bot_a);
                        
        Gizmos.DrawLine(this.top_v,this.bot_v);
        Gizmos.DrawLine(this.bot_a,this.bot_v);


        Gizmos.color = Color.green;
        Gizmos.DrawLine(top_a, top_a + planar.normalized);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(top_a, top_a + pen);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(top_a, top_a + new_aa);
    }
}
