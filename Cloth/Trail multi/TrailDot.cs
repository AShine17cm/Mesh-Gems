using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct TrailDot
{
    public Vector3 pt;
    public Vector3 dir;
    public TrailDot(Vector3 pt, Vector3 dir)
    {
        this.pt = pt;
        this.dir = dir;
    }
}
