using System;
using System.Collections.Generic;
using UnityEngine;
namespace Mg.Cloth
{
    /* 防止插入的一个接口 */
    public interface IConstrainGeo
    {
        Vector3 TestIntersect(Vector3 point);
        void TestIntersect(Rope rope,float tick);
    }
}