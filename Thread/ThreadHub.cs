using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading;
using System.Diagnostics;
//using Console = System.Console;
using Console = UnityEngine.Debug;
/*
    前台线程 所有前台线程总结，Runtime 会自动中介后台线程
    前台线程-主线程，调用Thread构造函数
    后台线程 在程序运行时执行，但是不能阻止程序终结的线程，如监视文件更改，或传入套接字连接
    后台线程-ThreadPool 中的线程，非托管代码 进入 托管执行环境的线程
 */
public class ThreadHub
{
    static object lockObj = new object();
    static int counter = 0;
    public static void ThreadProc()
    {
        int th_id = Thread.CurrentThread.ManagedThreadId;
        string name = Thread.CurrentThread.Name;
        LogThread();
        for (int i = 0; i < 10; i++)
        {
            Console.Log("Thread Id:<Color=red>" + th_id + "</Color> name:<Color=red> " + name + "</Color> :<Color=Purple>" + i + "</Color>");
            // Yield the rest of the time slice.
            Thread.Sleep(0);
        }
    }

    public static void ThreadProc_Para(object parameter)
    {
        int interval;
        try
        {
            ThreadPara para = parameter as ThreadPara;
            interval = para.val;
        }
        catch (InvalidCastException) { interval = 5000; }

        ThreadPara tp = parameter as ThreadPara;

        int th_id = Thread.CurrentThread.ManagedThreadId;
        string name = Thread.CurrentThread.Name;
        LogThread();
        Stopwatch sw = new Stopwatch();
        sw.Start();
        int i = -1;
        do
        {
            tp.val = i + 1314;
            i += 1;
            Console.Log("Thread Id:<Color=green>" + th_id + "</Color> name:<Color=green> " + name + "</Color> :<Color=Purple>" + i + "</Color>");
            // Yield the rest of the time slice.
            Thread.Sleep(100);
        }
        while (sw.ElapsedMilliseconds < interval);

        sw.Stop();
    }
    public static void ThreadProc_Pooled(object para)
    {
        LogThread();
    }
    public static void LogThread()
    {
        lock (lockObj)//临界资源
        {
            int id = Thread.CurrentThread.ManagedThreadId;
            string name = Thread.CurrentThread.Name;
            bool isPooled = Thread.CurrentThread.IsThreadPoolThread;
            bool isBackGround = Thread.CurrentThread.IsBackground;
            int hash = Thread.CurrentThread.GetHashCode();
            /* State掩码，可以组合 */
            System.Threading.ThreadState state = Thread.CurrentThread.ThreadState;

            string fmtTint = "<Color=red>{0}</Color>{1}";
            counter += 1;
            Console.Log(string.Format(fmtTint, "Counter:", counter));
            Console.Log(string.Format(fmtTint, "Thread Id:", id));
            Console.Log(string.Format(fmtTint, "Hash Code:", hash));
            Console.Log(string.Format(fmtTint, "Thread Name:", name));
            Console.Log(string.Format(fmtTint, "Is Pooled:", isPooled));
            Console.Log(string.Format(fmtTint, "Is Background:", isBackGround));
        }
    }
    public void DoJob(ThreadPara[] paras)
    {
        List<Thread> pool = new List<Thread>(1024);
        // The constructor for the Thread class requires a ThreadStart
        // delegate that represents the method to be executed on the
        // thread.  C# simplifies the creation of this delegate.
        Thread t = new Thread(new ThreadStart(ThreadProc));
        Thread t2 = new Thread(ThreadProc);
        Thread t_para = new Thread(new ParameterizedThreadStart(ThreadProc_Para));
        Thread t_para2 = new Thread(ThreadProc_Para);

        pool.Add(t);
        pool.Add(t2);
        pool.Add(t_para);
        pool.Add(t_para2);

        /* Thread Pool Worker,Pooled,Background */
        ThreadPool.QueueUserWorkItem(ThreadProc_Pooled);

        int count = paras.Length;
        /* 是否启动线程 */
        for (int i = 0; i < count; i++)
        {
            pool[i].Name = paras[i].threadName;
            if (paras[i].isEnable)
            {
                if (paras[i].hasPara)
                    pool[i].Start(paras[i]);
                else
                    pool[i].Start();
            }
        }
        // Start ThreadProc.  Note that on a uniprocessor, the new
        // thread does not get any processor time until the main thread
        // is preempted or yields.  Uncomment the Thread.Sleep that
        // follows t.Start() to see the difference.
        Thread.Sleep(1000);
        //Thread.CurrentThread.Abort();     //异常中止
        //Thread.CurrentThread.Suspend();
        //bool isOK= Thread.Yield();//尝试让出当前处理器
        //Thread.CurrentThread.Interrupt();

        Console.Log("Main Thread Call Pool.Join:");
        //主线程阻塞，等待Pool中的线程完成
        for (int i = 0; i < count; i++)
        {
            if (paras[i].isEnable)
            {
                if (pool[i].IsAlive)
                {
                    pool[i].Join();
                    Console.Log("Main Thread <Color=red>" + pool[i].Name + "</Color> has joined");
                }
            }
        }

        int th_id = Thread.CurrentThread.ManagedThreadId;
        string name = Thread.CurrentThread.Name;
        Console.Log("Thread Id:<Color=green>" + th_id + "</Color> name:<Color=green> " + name + "</Color>");
    }


    
}
