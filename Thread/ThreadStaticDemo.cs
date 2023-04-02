using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

/* 不能对 ThreadStatic 变量初始化，不然只会实例化出一个线程 */
public class ThreadStaticDemo
{
    static string fmtTint = "<Color=red>{0}</Color>{1}";
    public static void TestStatic()
    {

        Thread t = new Thread(ThreadData.ThreadProc);
        t.Name = "TH_STATIC_" + 0;
        t.Start();

        Thread t2 = new Thread(ThreadData.ThreadProc);
        t2.Name = "TH_STATIC_" + 2;
        t2.Start();

        Thread.Sleep(2000);
        t.Abort();
        t2.Abort();

        return;

        //List<Thread> pool = new List<Thread>(10);
        //for(int i = 0; i < 3; i++)
        //{
        //    Thread t = new Thread(ThreadData.ThreadProc);
        //    t.Name = "TH_STATIC_" + i;
        //    t.Start();
        //    pool.Add(t);
        //}
        //Thread.Sleep(2000);
        //for(int i = 0; i < pool.Count; i++)
        //{
        //    //LocalDataStoreSlot localData= Thread.GetNamedDataSlot("doWork");
            
        //    pool[i].Abort();
        //}
    }

    class ThreadData
    {
        [ThreadStatic]
        static int th_specificData;
        [ThreadStatic]
        static string th_name;
        [ThreadStatic]
        static bool doWork;
        //不能对 ThreadStatic 变量初始化，不然只会实例化出一个线程
        //static bool doWork = true;
        public static void ThreadProc()
        {
            int id = Thread.CurrentThread.ManagedThreadId;
            string name = Thread.CurrentThread.Name;
            th_specificData = id;
            th_name = name;

            doWork = true;
            int counter = 0;
            try
            {
                while (doWork)
                {
                    counter += 1;
                    Debug.Log(string.Format(fmtTint, id, name) + " | " + string.Format(fmtTint, th_specificData, th_name) + " | " + counter);
                    //Console.WriteLine("Id:" + th_specificData + " name:" + th_specificData + "  |  " + " xid:" + id + " xname:" + name+"  |  "+counter);
                    Thread.Sleep(200);
                }
            }
            catch(ThreadAbortException e)
            {
                Debug.Log(string.Format(fmtTint, id, name) + " | Abort");
            }
        }
    }
}
