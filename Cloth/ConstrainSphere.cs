using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mg.Cloth
{
    [Serializable]
    public class Sphere
    {
        public Transform center;
        public float radius = 1f;
        public float strength = 1f;
    }
    public class SphereConstrain : MonoBehaviour, IConstrainGeo
    {
        public List<Sphere> spheres;
        /* 插入量，没有加权tick */
        public Vector3 TestIntersect(Vector3 point)
        {
            Vector3 delta = Vector3.zero;
            Sphere sphere;
            for (int s = 0; s < spheres.Count; s++)
            {
                sphere = spheres[s];
                if (null == sphere.center || 0.01f > sphere.radius) continue;

                Vector3 vec = point - sphere.center.position;
                float sqr = vec.sqrMagnitude;
                if (sqr < sphere.radius * sphere.radius)
                {
                    float dist = Mathf.Max(0.001f, Mathf.Sqrt(sqr));
                    delta = delta + vec * (sphere.radius / dist - 1) * sphere.strength;
                }
            }
            return delta;
        }
        /* 有加权 tick和strength */
        public void TestIntersect(Rope rope, float tick)
        {
            int countNode = rope.Count;

            for (int c = 0; c < countNode; c++)
            {
                Vector3 p = rope.Nodes[c];
                Vector3 delta = TestIntersect(p) * tick;
                rope.Constrains[c] += delta;
            }
        }
    }
}
