using System;
using System.Collections.Generic;
using UnityEngine;

/*
 单轴-单层(嵌套子节点)，具有平行约束, 最大弯曲度(柔性)约束
 1-2-3-4
 1-2-3-4 (平行约束)
 1-
   2-
      3-
        4-
 */
[Serializable]
public class SoftBone
{
    public Transform pivot;
    public AnimationCurve massCurve;        //质量-控制速度
    public AnimationCurve softCurve;        //弯曲度
    public int maxDegree = 60;
    public float radius = 0.3f;
    public float physicsRadius = 0.5f;       //物理碰撞半径
    private float[] masses;                  //质量-弯曲速度
    private float[] dists;                   //单轴-距离约束
    private float[] softs;                   //弯曲-变形量 <Cos>  可改为各项异性

    private Transform[] bones;
    private Vector3[] nodes;            //节点的旧位置
    private Vector3[] initDirs;         //初始轴向 local-space
    int countNode;
    LayerMask mask;

    float maxAng;

    bool hasPair = false;
    SoftBone constrainPair;       //如果有的话
    float radiusB = 0;
    float diameterAB, sqrAB;
    public void InitEditor()
    {
        if (pivot == null && massCurve.keys.Length == 0)
        {
            massCurve = AnimationCurve.Constant(0, 1, 0.1f);
            softCurve = AnimationCurve.Constant(0, 1, 1.0f);
        }
    }
    public Vector3 ParallelNode(int idx)    //平行的节点
    {
        if (idx < nodes.Length)
            return nodes[idx];
        return nodes[nodes.Length - 1];
    }
    public void Init(LayerMask mask,SoftBone constrainPair)
    {
        /* 最大弯曲度 */
        maxDegree = Mathf.Clamp(maxDegree, 10, 359);
        maxAng = Mathf.Deg2Rad * maxDegree;

        this.mask = mask;
        /* 平行约束 */
        hasPair = (null != constrainPair);
        if (hasPair)
        {
            radiusB = constrainPair.radius;
            diameterAB = radius + radiusB;
            sqrAB = diameterAB * diameterAB;
        }

        countNode = pivot.childCount + 1;   //算上根节点
        masses = new float[countNode];
        dists = new float[countNode];
        softs = new float[countNode];

        bones = new Transform[countNode];
        nodes = new Vector3[countNode];
        initDirs = new Vector3[countNode];

        float step = 1f / (countNode - 1);
        float t;
        float preMass = 0;
        bones[0] = pivot;   //第一个节点
        initDirs[0] = Vector3.zero;
        dists[0] = 0;
        masses[0] = 0;
        softs[0] = 0;
        for (int i = 1; i < countNode; i++)
        {
            t = step * i;
            bones[i] = pivot.GetChild(i - 1);
            dists[i] = bones[i].localPosition.magnitude;
            initDirs[i] = bones[i].localPosition.normalized; //初始轴向-local
            preMass = preMass + massCurve.Evaluate(t);
            masses[i] = preMass;

            float soft = maxDegree * softCurve.Evaluate(t);
            softs[i] = Mathf.Cos(Mathf.Deg2Rad * maxDegree * soft);
        }
    }
    /* World Space
     * dirForce:    可以 local+World重力 混合
     * deltaMove:   根节点 的移动 
     * amtMimic:    根节点 带动的位移权重
     */
    void Refresh(Vector3 dirForce, float tick)
    {
        Vector3 pos = pivot.position;
        /* 根节点 */
        nodes[0] = pos;

        Simulate(dirForce, tick);

        //检测物理碰撞<Head-Tail> 和 末端的
        Vector3 tail = nodes[countNode - 1];
        if (Physics.Linecast(nodes[0], tail, mask)||Physics.Linecast(tail,tail+Vector3.down*physicsRadius))
        {
            SimulatePhysics();
        }

    }
    void Simulate(Vector3 dirForce,float tick)
    {
        Vector3 virPos;
        Vector3 virDir;
        Vector3 cpPos;     //钳制的位置 clamp-pos
        Vector3 init_wldDir;
        for (int i = 1; i < countNode; i++)
        {
            virPos = nodes[i];                                              //向后的虚位
            virDir = (virPos - nodes[i - 1]).normalized;                    //指向 上层节点
            virDir = (virDir + dirForce * (masses[i] * tick)).normalized;   //重力 下拉
            cpPos = nodes[i - 1] + virDir * dists[i];                       //距离钳制的 临时位置
            /* 平行约束 */
            if (hasPair)
            {
                Vector3 pairPos = constrainPair.ParallelNode(i);
                Vector3 vecBA = pairPos - cpPos;
                float sqr = vecBA.sqrMagnitude;
                if (sqr < sqrAB)
                {
                    float distBA = Mathf.Sqrt(sqr);
                    Vector3 dirBA = vecBA / distBA;
                    cpPos = pairPos + dirBA * diameterAB;   //平行约束

                    virDir = (cpPos - nodes[i - 1]).normalized;//修正 虚方向
                }
            }
            /* 弯曲约束 */
            init_wldDir= bones[i - 1].TransformDirection(initDirs[i]);
            float dot = Vector3.Dot(init_wldDir, virDir);
            if (dot < softs[i])
            {
                float ang= Mathf.Acos(dot);
                float sin = Mathf.Sin(ang);
                Vector3 planarDir = Vector3.Cross(init_wldDir, virDir);
                Vector3 init_wldDir_X = Vector3.Cross(planarDir, init_wldDir);
                init_wldDir_X.Normalize();
                virDir = init_wldDir * dot + init_wldDir_X * sin;
                cpPos = nodes[i - 1] + virDir * dists[i];
            }
            nodes[i] = cpPos;   //最终的位置
        }
    }
    /* 检测与地面的物理碰撞 强约束 */
    void SimulatePhysics()
    {
        Vector3 virPos;
        Vector3 virDir;
        Vector3 cpPos;     //钳制的位置 clamp-pos
        Vector3 init_wldDir;
        RaycastHit hit;
        for (int i = 1; i < countNode; i++)
        {
            virPos = nodes[i];                                              //向后的虚位
            /* 假设 前一个点在碰撞体外部 */
            if (Physics.CheckSphere(nodes[i], physicsRadius, mask))
            {

            }
            if(Physics.Linecast(nodes[i-1],nodes[i],out hit))
            {
                Vector3 nm = hit.normal;
            }
            virDir = (virPos - nodes[i - 1]).normalized;                    //指向 上层节点
            //virDir = (virDir + dirForce * (masses[i] * tick)).normalized;   //重力 下拉
            cpPos = nodes[i - 1] + virDir * dists[i];                       //距离钳制的 临时位置
            /* 平行约束 */
            if (hasPair)
            {
                Vector3 pairPos = constrainPair.ParallelNode(i);
                Vector3 vecBA = pairPos - cpPos;
                float sqr = vecBA.sqrMagnitude;
                if (sqr < sqrAB)
                {
                    float distBA = Mathf.Sqrt(sqr);
                    Vector3 dirBA = vecBA / distBA;
                    cpPos = pairPos + dirBA * diameterAB;   //平行约束

                    virDir = (cpPos - nodes[i - 1]).normalized;//修正 虚方向
                }
            }
            /* 弯曲约束 */
            init_wldDir = bones[i - 1].TransformDirection(initDirs[i]);
            float dot = Vector3.Dot(init_wldDir, virDir);
            if (dot < softs[i])
            {
                float ang = Mathf.Acos(dot);
                float sin = Mathf.Sin(ang);
                Vector3 planarDir = Vector3.Cross(init_wldDir, virDir);
                Vector3 init_wldDir_X = Vector3.Cross(planarDir, init_wldDir);
                init_wldDir_X.Normalize();
                virDir = init_wldDir * dot + init_wldDir_X * sin;
                cpPos = nodes[i - 1] + virDir * dists[i];
            }
            nodes[i] = cpPos;   //最终的位置
        }
    }
    // Update is called once per frame
    void Update()
    {

    }
}
