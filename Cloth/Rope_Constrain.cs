using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mg.Cloth
{
    /* 平行约束相关的代码 主要是一些数据的读取 */
    public partial class Rope
    {
        public int Count
        {
            get { return countNode; }
        }
        public Vector3[] Constrains
        {
            get { return constrains; }
        }
        public Vector3[] Nodes
        {
            get { return nodes; }
        }
    }
}