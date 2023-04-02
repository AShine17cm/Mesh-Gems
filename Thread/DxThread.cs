using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
public class DxThread : MonoBehaviour
{
    public bool testProc = false;
    public ThreadPara[] paras;

    public bool testInterrupt = false;
    public bool testIntAbo = false;
    [Space(5)]
    public bool testCritical = false;
    public bool tryInterruptCritical = false;
    [Space(5)]
    public bool testStatic = false;

    Thread t_critical;
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

            int th_id = Thread.CurrentThread.ManagedThreadId;
            string name = Thread.CurrentThread.Name;
            Debug.Log("Thread Id:<Color=green>" + th_id + "</Color> name:<Color=green> " + name + "</Color>");
            ThreadHub threadFunc = new ThreadHub();
            threadFunc.DoJob(paras);
        }
        if (testInterrupt)
        {
            testInterrupt = false;
            ThreadInterrupt.TestInterrupt();
        }
        if (testIntAbo)
        {
            testIntAbo = false;
            ThreadInterrupt.TestInterruptAbort();
        }
        /* - */
        if (testCritical)
        {
            testCritical = false;
            t_critical= ThreadCriticalRegion.TestCritical();
        }
        if (tryInterruptCritical)
        {
            tryInterruptCritical = false;
            if(null!= t_critical)
            {
                t_critical.Interrupt();
            }
        }

        /* static */
        if (testStatic)
        {
            testStatic = false;
            ThreadStaticDemo.TestStatic();
        }
    }

}
