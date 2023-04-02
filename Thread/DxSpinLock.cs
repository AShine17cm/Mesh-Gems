using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Diagnostics;
using Debug = UnityEngine.Debug;


public class DxSpinLock : MonoBehaviour
{
    public bool testAct_12 = false;
    public bool testTask_AB = false;
    public Transform target;
    public float step = 0.2f;

    SpinLockDemo demo;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (testAct_12)
        {
            testAct_12 = false;
            if (demo == null) demo = new SpinLockDemo();
            demo.TestAct_12();
        }
        if (testTask_AB)
        {
            testTask_AB = false;
            if (demo == null) demo = new SpinLockDemo();
            demo.TestTask_AB();
        }
    }
    //private void FixedUpdate()
    //{
    //    int th_id = Thread.CurrentThread.ManagedThreadId;
    //    string th_name = Thread.CurrentThread.Name;
    //    Debug.Log("Thread id:" + th_id);
    //}

}
