using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Mg.Cloth
{

    [CustomEditor(typeof(TailHub))]
    public class TailHub_Editor : Editor
    {
        TailHub hub;
        private void OnEnable()
        {
            hub = target as TailHub;
        }
        /* 用于初始化一些 曲线，参数值 */
        public override bool RequiresConstantRepaint()
        {
            hub.InitEditor();
            return base.RequiresConstantRepaint();
        }
    }
}