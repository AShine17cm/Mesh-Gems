using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mg.Cloth
{
    [Serializable]
    public class TailAttribute
    {
        public AnimationCurve bendCurve;        //弯曲度
        public AnimationCurve dampCurve;        //衰减速度
        public AnimationCurve stiffCurve;       //硬度
        public float damping = 0.1f;            // 会乘以0.1f
        public int bendDegree = 60;
        public float stiffness = 1.0f;
        [Range(0.01f, 10)] public float timeScale = 1f;
        public float gravity = 9.8f;
        public float radius = 0.3f;
        public float physicsRadius = 0.5f;      //物理碰撞半径
        public float physicsStasis = 1f;        //物理停滞时间

    }
}