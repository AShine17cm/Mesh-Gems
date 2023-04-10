using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mg.Cloth
{
    [Serializable]
    public class RopeAttribute
    {
        public AnimationCurve bendCurve;        //弯曲度 适合于保持形状
        public AnimationCurve adjacentCurve;    //相邻节点的角度控制
        public AnimationCurve dampCurve;        //衰减速度
        public int bendDegree = 80;
        public int adjacentDegree = 120;
        public float damping = 0.1f;            // 会乘以0.1f

        public float gravity = 10f;
        public float physicsRadius = 0.5f;      //物理碰撞半径
        public float physicsStasis = 1f;        //物理停滞时间

    }
}