using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mg.Cloth
{
    [Serializable]
    public class TailAttribute:RopeAttribute
    {
        public AnimationCurve stiffCurve;       //硬度
        public float stiffness = 5.0f;
    }
}