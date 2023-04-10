using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mg.Cloth;

public class RopeHub : MonoBehaviour
{
    public RopeAttribute[] attributes;
    public Rope[] links;
    public ConstrainPair[] constrainPairs;  //如果costrain-pair 有重复-冲突，自己负责，代码不检查
    public LayerMask physicsLayer;
    public float timeScale = 1f;
    public float teleportVault = 5f;       //瞬移?
    public int maxFrameRate = 100;
    ClothConstrain[] constrains;
    IConstrainGeo[] geos;
    int countCP = 0;

    float tick;
    float timer;
    Transform tr;
    Vector3 lastPos;
    void Start()
    {
        tr = transform;
        lastPos = tr.position;
        tick = 1f*timeScale / Mathf.Max(1, maxFrameRate);
        timer = 0;
        geos = GetComponentsInChildren<IConstrainGeo>();

        for (int i = 0; i < attributes.Length; i++)
        {
            RopeAttribute att = attributes[i];
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
        /* 检查瞬移 */
        Vector3 pos = tr.position;
        if ((pos - lastPos).sqrMagnitude > teleportVault)
        {
            lastPos = pos;
            Vector3 delta = pos - lastPos;
            timer = 0;
            for (int i = 0; i < links.Length; i++)
            {
                links[i].Teleport(delta);
            }
        }
        /* 更新频率,不追帧 */
        timer += Time.deltaTime;
        if (timer < tick)
        {
            return;
        }
        timer = Mathf.Min(timer - tick, tick);

        for (int i = 0; i < links.Length; i++)
        {
            links[i].ClearConstrains();
        }
        /* 自定义的形体约束，比如球体 */
        if (null != geos)
        {
            for(int g = 0; g < geos.Length; g++)
            {
                for(int r = 0; r < links.Length; r++)
                {
                    geos[g].TestIntersect(links[r],tick);
                }
            }
        }
        /* 先行计算平行约束，防止跨帧的 tick误差,( 也可以自行计算tick，控制验算时机/频率) */
        for (int i = 0; i < countCP; i++)
        {
            constrains[i]?.Simulate(tick);//相关位置可能不合法，为null
        }

        for (int i = 0; i < links.Length; i++)
        {
            links[i].Refresh(Vector3.down, tick);
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
                    att.adjacentCurve = AnimationCurve.Constant(0, 1, 1.0f);
                    att.gravity = 10f;
                    att.damping = 0.1f;
                    att.bendDegree = 80;
                    att.adjacentDegree = 90;
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
            links[i].OnDrawGizoms(attributes[idx]);
        }
    }
#endif
}
