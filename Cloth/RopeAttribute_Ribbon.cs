using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mg.Cloth
{
    [Serializable]
    public class RopeAttribute_Ribbon:RopeAttribute
    {
        public AnimationCurve tanBendCurve;
        public int tanBendDegree = 80;

    }
}