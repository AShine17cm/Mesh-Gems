using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

    /*
    * 如果当前未在等待、睡眠或联接状态中阻止此线程，则下次开始阻塞时，该线程将中断。
    * ThreadInterruptedException 在中断的线程中引发，但直到线程阻塞。
    * 如果线程从不阻塞，则永远不会引发异常，因此线程可能会完成，而不会中断。
    * 如果线程不捕获 Interrupt,则托管环境 捕获，并终止线程
    * ThreadAbortException  在线程上引发异常，并终止线程
    */
public class ThreadInterrupt
{
    static string fmtTint = "<Color=red>{0}</Color>{1}";
    /* 在线程上引发一个Interrupt异常 */
    public static void TestInterrupt()
    {
        StayAwake stayAwake = new StayAwake();
        Thread t_1 = new Thread(new ThreadStart(stayAwake.ThreadMethod));
        t_1.Name = "StayAwake";
        t_1.Start();

        //在线程 t_1上引发一个异常
        //当t_1当前处于blocked<sleep> 或者 将来处于 blocked<sleep>状态
        //将会 捕获此异常
        t_1.Interrupt();

        stayAwake.SleepSwitch = true;
        t_1.Join();
    }
    /*  */
    public static void TestInterruptAbort()
    {
        Thread t_sleeping = new Thread(SleepInfinitely);
        t_sleeping.Name = "TH_Sleeping_1";
        t_sleeping.Start();
        Thread.Sleep(2000);
        t_sleeping.Interrupt();

        Thread.Sleep(1000);

        Thread t_sleeping2 = new Thread(SleepInfinitely);
        t_sleeping2.Name = "TH_Sleeping_2";
        t_sleeping2.Start();
        Thread.Sleep(2000);
        t_sleeping2.Abort();
    }
    public static void SleepInfinitely()
    {
        int id = Thread.CurrentThread.ManagedThreadId;
        string name = Thread.CurrentThread.Name;
        Debug.Log(string.Format(fmtTint, "Thread Id:", id));
        Debug.Log(string.Format(fmtTint, "Name:", name));

        try
        {
            Thread.Sleep(Timeout.Infinite);
        }
        catch(ThreadInterruptedException e)
        {
            Debug.Log(string.Format(fmtTint, id, name) + "  Interrupt:" + e.StackTrace);
        }
        catch(ThreadAbortException e)
        {
            Debug.Log(string.Format(fmtTint, id, name) + "  Abort:" + e.StackTrace);
        }
        finally
        {
            Debug.Log(string.Format(fmtTint, id, name) + "  Finally");
        }
        Debug.Log(string.Format(fmtTint, id, name) + "  Finishing");
    }
}

public class StayAwake
{
    static string fmtTint = "<Color=red>{0}</Color>{1}";

    bool sleepSwicth = false;
    public bool SleepSwitch
    {
        set { sleepSwicth = value; }
    }

    public void ThreadMethod()
    {
        int id = Thread.CurrentThread.ManagedThreadId;
        string name = Thread.CurrentThread.Name;
        Debug.Log(string.Format(fmtTint, "Thread Id:", id));
        Debug.Log(string.Format(fmtTint, "Name:", name));


        while (!sleepSwicth)
        {
            /* 使用 SpinWait,展示 interrupt 作用在 running thread 上的效果 */
            Thread.SpinWait(10000000);
        }

        try
        {
            Debug.Log("Thread going to sleep:" + id + "  :" + name);
            /*进入 blocked<sleep>状态
             * 立刻被异常 ThreadInterruptedException woken up
             */
            Thread.Sleep(Timeout.Infinite);
        }
        catch (ThreadInterruptedException e)
        {
            Debug.Log("Thread:<Color=red>" + id + "   :" + name + "</Color> Cannot go to sleep" + e.StackTrace);
        }
    }
}
