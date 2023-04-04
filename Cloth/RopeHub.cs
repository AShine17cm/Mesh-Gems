using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mg.Cloth;

public class RopeHub : MonoBehaviour
{
    public Rope[] links;
    public LayerMask physicsLayer;
    //用于在 Editor里动态刷新 两根曲线的参数效果
    public bool refreshParameters = false; 

    void Start()
    {
        if (null != links)
        {
            for (int i = 0; i < links.Length; i++)
            {
                links[i].InitRuntime(physicsLayer,null);
            }
        }
    }

    void Update()
    {
#if UNITY_EDITOR
        if (refreshParameters)
        {
            for (int i = 0; i < links.Length; i++)
            {
                links[i].Refresh_Damp_Bend( );
            }
        }
#endif
        for(int i = 0; i < links.Length; i++)
        {
            links[i].Refresh(Vector3.down, Time.deltaTime);
        }
    }
#if UNITY_EDITOR
    public void InitEditor()
    {
        if (null != links)
        {
            for (int i = 0; i < links.Length; i++)
            {
                links[i].InitEditor();
            }
        }
    }
    private void OnDrawGizmos()
    {
        for (int i = 0; i < links.Length; i++)
        {
            links[i].OnDrawGizoms();
        }
    }
#endif
}
