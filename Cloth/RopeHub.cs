using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mg.Cloth;

public class RopeHub : MonoBehaviour
{
    public RopeAttribute[] attributes;

    public Rope[] links;
    public LayerMask physicsLayer;
    //用于在 Editor里动态刷新 两根曲线的参数效果
    public bool refreshParameters = false;

    void Start()
    {
        for (int i = 0; i < attributes.Length; i++)
        {
            RopeAttribute att = attributes[i];
            att.timeScale = Mathf.Max(att.timeScale, 0.001f);
            att.bendDegree = Mathf.Clamp(att.bendDegree, 10, 359);
        }
        /* 初始化 单轴 */
        for (int i = 0; i < links.Length; i++)
        {
            int idx = Mathf.Clamp(links[i].attributeIdx, 0, attributes.Length - 1);
            int pairIdx = links[i].pairIdx;
            Rope pair = null;
            if (pairIdx > 0 && pairIdx < links.Length)
            {
                pair = links[pairIdx];
            }
            links[i].InitRuntime(attributes[idx], physicsLayer,pair);
        }
    }

    void Update()
    {
#if UNITY_EDITOR
        if (refreshParameters)
        {
            for (int i = 0; i < links.Length; i++)
            {
                int idx = Mathf.Clamp(links[i].attributeIdx, 0, attributes.Length - 1);
                links[i].Refresh_Damp_Bend(attributes[idx]);
            }
        }
#endif
        for (int i = 0; i < links.Length; i++)
        {
            links[i].Refresh(Vector3.down, Time.deltaTime);
        }
    }
#if UNITY_EDITOR
    public void InitEditor()
    {
        if (null != attributes)
        {
            for (int i = 0; i < attributes.Length; i++)
            {
                RopeAttribute att = attributes[i];
                if (attributes[i].dampCurve.keys.Length == 0)
                {
                    att.dampCurve = AnimationCurve.Constant(0, 1, 1.0f);
                    att.bendCurve = AnimationCurve.Constant(0, 1, 1.0f);
                    att.timeScale = 1f;
                    att.gravity = 10f;
                    att.damping = 0.1f;
                    att.bendDegree = 80;
                    att.radius = 0.3f;
                    att.physicsRadius = 0.5f;
                }
            }
        }
    }
    private void OnDrawGizmos()
    {
        if (attributes.Length == 0) return;

        for (int i = 0; i < links.Length; i++)
        {
            int idx = Mathf.Clamp(links[i].attributeIdx, 0, attributes.Length - 1);
            int pairIdx = links[i].pairIdx;
            Rope pair = null;
            if (pairIdx > 0 && pairIdx < links.Length)
            {
                pair = links[pairIdx];
            }
            links[i].OnDrawGizoms(attributes[idx],pair);
        }
    }
#endif
}
