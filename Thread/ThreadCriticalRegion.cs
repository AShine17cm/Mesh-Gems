using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
/* 例如:
    在持有锁的时候，分配内存失败，导致线程终止，其它等待锁的线程 陷入死锁
 */
public class ThreadCriticalRegion
{
    static string fmtTint = "<Color=red>{0}</Color>{1}";
    public static object lockObj = new object();
    public static Thread TestCritical()
    {
        CriticalJob criticalJob = new CriticalJob();
        Thread t_critical = new Thread(criticalJob.DoJob);
        t_critical.Name = "TH_Critical";
        t_critical.Start();

        //Thread.Sleep(2000);
        //t_critical.Join();

        return t_critical;
    }


    public class CriticalJob
    {
        public bool stopWork = false;
        public bool StopWork
        {
            set { stopWork = value; }
        }

        public void DoJob()
        {
            int id = Thread.CurrentThread.ManagedThreadId;
            string name = Thread.CurrentThread.Name;
            lock (lockObj)
            {
                int counter = 0;
                Thread.BeginCriticalRegion();
                try
                {
                    while (!stopWork)
                    {
                        counter += 1;
                        Debug.Log(string.Format(fmtTint, id, name) + " >" + counter);
                        Thread.Sleep(100);
                    }
                }
                catch(ThreadInterruptedException e)
                {
                    Debug.Log(string.Format(fmtTint, id, name) + " > Interrupted");
                }

                int stopCounter = 10;
                while (stopCounter > 0)
                {
                    stopCounter -= 1;
                    Debug.Log(string.Format(fmtTint, id, name) + " <Color=green>>x></Color>" + stopCounter);
                    Thread.Sleep(100);
                }

                Thread.EndCriticalRegion();
            }
        }
    }
}
