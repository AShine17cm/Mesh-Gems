using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SoftBoneHub))]
public class SoftBoneHub_Editor : Editor
{
    SoftBoneHub hub;
    private void OnEnable()
    {
        hub = target as SoftBoneHub;
    }
    public override bool RequiresConstantRepaint()
    {
        hub.InitEditor();
        return base.RequiresConstantRepaint();
    }
}
