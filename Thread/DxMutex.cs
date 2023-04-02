using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Diagnostics;
using Debug = UnityEngine.Debug;


public class DxMutex : MonoBehaviour
{
    public bool testAct = false;
    public bool testTask_AB = false;
    public Transform target;
    public float step = 0.2f;
    public List<MutexDemo.Job> jobs = new List<MutexDemo.Job>(128);
    MutexDemo demo;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (testAct)
        {
            testAct = false;
            if (demo == null) demo = new MutexDemo();
            demo.Test(jobs);
        }
        if (testTask_AB)
        {
            testTask_AB = false;
            if (demo == null) demo = new MutexDemo();
            demo.Test(jobs);
        }
    }
    //private void FixedUpdate()
    //{
    //    int th_id = Thread.CurrentThread.ManagedThreadId;
    //    string th_name = Thread.CurrentThread.Name;
    //    Debug.Log("Thread id:" + th_id);
    //}

}
