using System;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;
using Debug = UnityEngine.Debug;

/* 
 * SpinLock 预期等待时间很短(不应该 拥有另一个Spin-Lock, 不应该调用未知函数：比如虚函数，不应该分配内存)
 * 适合于 操作-粒度很小，数量很大
 * enableThreadOwnerTracking 构造函数中的此值，决定其他线程能解锁 被另一个线程拥有的Spin-Lock
 * */
public class SpinLockDemo
{
    SpinLock s1 = new SpinLock(true);
    StringBuilder sb = new StringBuilder();

    ManualResetEventSlim mre1 = new ManualResetEventSlim();/* 可以很好的同步两个工作者 */
    ManualResetEventSlim mre2 = new ManualResetEventSlim();
    /*  */
    void Action_1()
    {
        bool gotLock = false;
        for(int i = 0; i < 10000; i++)
        {
            gotLock = false;/* 必须先设定为false */
            try
            {
                s1.Enter(ref gotLock);
                sb.Append((i % 10).ToString());
            }
            finally
            {
                if (gotLock) s1.Exit();
            }
        }
    }
    void Action_2()
    {
        bool gotLock = false;
        for (int i = 0; i < 300; i++)
        {
            gotLock = false;/* 必须先设定为false */
            try
            {
                s1.Enter(ref gotLock);
                sb.Append((i % 10).ToString());
            }
            finally
            {
                if (gotLock) s1.Exit();
            }
        }
    }
    public void TestAct_12()
    {
        Parallel.Invoke(Action_1, Action_1, Action_2);
        Debug.Log("sb.Length:" + sb.Length);
    }

    void TaskA()
    {
        bool gotLock = false;
        try
        {
            s1.Enter(ref gotLock);
            mre1.Set();     //通知  电话线的另一端
            mre2.Wait();    //等待  通知
        }
        finally
        {
            if (gotLock) s1.Exit();
        }
    }
    void TaskB()
    {
        mre1.Wait();
        Debug.Log("B: is Held:" + s1.IsHeld+
            " By Current Thread:"+s1.IsHeldByCurrentThread+
            " Is ThreadOwnerTrackingEnabled:"+s1.IsThreadOwnerTrackingEnabled);

        /* 释放非当前线程拥有的锁，导致异常 */
        try
        {
            s1.Exit();
            Debug.Log("B: release lock");
        }
        catch(Exception e)
        {
            Debug.Log("B: lock.Exit resulted Exception:" + e.Message);
        }
        mre2.Set();
    }
    public void TestTask_AB()
    {
        Task taskA = Task.Factory.StartNew(TaskA);
        Task taskB = Task.Factory.StartNew(TaskB);
        Task.WaitAll(taskA, taskB);
        mre1.Dispose();
        mre2.Dispose();
    }
}
