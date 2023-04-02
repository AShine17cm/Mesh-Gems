using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Diagnostics;
using Debug = UnityEngine.Debug;


public class DxBarrier : MonoBehaviour
{
    public bool testProc = false;
    public Transform target;
    public float step = 0.2f;
    BarrierDemo demo;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (testProc)
        {
            testProc = false;
            demo = new BarrierDemo();
            demo.Init(target,step);
        }
        //int th_id = Thread.CurrentThread.ManagedThreadId;
        //string th_name = Thread.CurrentThread.Name;
        //Debug.Log("Thread id u:" + th_id);
    }

}
