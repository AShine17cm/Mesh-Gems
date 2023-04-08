using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mg.Cloth;

public class TailHub : MonoBehaviour
{
    public TailAttribute[] attributes;
    public Tail[] links;
    public ConstrainPair[] constrainPairs;  //如果costrain-pair 有重复-冲突，自己负责，代码不检查
    public LayerMask physicsLayer;
    ClothConstrain[] constrains;
    int countCP = 0;
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
            links[i].InitRuntime(attributes[idx], physicsLayer);
        }
        if (constrainPairs != null)
        {
            /* 为了代码的简洁，没有检查重复的约束 */
            countCP = constrainPairs.Length;
            constrains = new ClothConstrain[countCP];
            int counLink = links.Length;
            for (int i = 0; i < countCP; i++)
            {
                ConstrainPair cp = constrainPairs[i];
                if (cp.IsLegal(counLink))
                {
                    Rope main = links[cp.main];
                    Rope vice = links[cp.vice];
                    /* 节点数量不一致，也可以做，但是这里不做 */
                    if (main.Count == vice.Count)
                    {
                        ClothConstrain cc = new ClothConstrain(main, vice, cp.elastic, cp.areaMin);
                        constrains[i] = cc;
                    }
                }
            }
        }
    }

    void Update()
    {
        for (int i = 0; i < links.Length; i++)
        {
            links[i].ClearConstrains();
        }
        /* 先行计算平行约束，防止跨帧的 tick误差,( 也可以自行计算tick，控制验算时机/频率) */
        for (int i = 0; i < countCP; i++)
        {
            constrains[i]?.Simulate(Time.deltaTime);//相关位置可能不合法，为null
        }
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
                TailAttribute att = attributes[i];
                if (attributes[i].dampCurve.keys.Length == 0)
                {
                    att.dampCurve = AnimationCurve.Constant(0, 1, 1.0f);
                    att.bendCurve = AnimationCurve.Constant(0, 1, 1.0f);
                    att.adjacentCurve = AnimationCurve.Constant(0, 1, 1.0f);
                    att.timeScale = 1f;
                    att.gravity = 10f;
                    att.damping = 0.1f;
                    att.bendDegree = 80;
                    att.adjacentDegree = 90;
                    att.physicsRadius = 0.5f;

                    att.stiffCurve = AnimationCurve.Constant(0, 1, 1.0f);
                    att.stiffness = 5.0f;
                }
            }
        }
    }
    private void OnDrawGizmos()
    {
        if (attributes==null|| attributes.Length == 0) return;

        for (int i = 0; i < links.Length; i++)
        {
            int idx = Mathf.Clamp(links[i].attributeIdx, 0, attributes.Length - 1);
            links[i].OnDrawGizoms(attributes[idx]);
        }
    }
#endif
}
