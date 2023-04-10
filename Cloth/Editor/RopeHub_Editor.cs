using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace Mg.Cloth
{

    [CustomEditor(typeof(RopeHub))]
    public class RopeHub_Editor : Editor
    {
        RopeHub hub;
        private void OnEnable()
        {
            hub = target as RopeHub;
        }
        /* 用于初始化一些 曲线，参数值 */
        public override bool RequiresConstantRepaint()
        {
            hub.InitEditor();
            return base.RequiresConstantRepaint();
        }
    }
}