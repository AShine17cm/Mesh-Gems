using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

/*
    指定参与者的数量，超出会引发异常, 少了会死锁(因为用了Interlocked)?
    PostPhaseAct 只会执行一次(在最后一个执行的线程上?)，和参与者数量无关
    <异常> 会被所有的参与者都捕获到
    所有参与者对Barrier.SignalAndWait的调用次数 必须相同 phase-number
    如果其中一个参与者线程 被异常终结，导致Barrier.SignalAndWait 没有发出，会导致整个Barrier无法结束（死锁的假象）
 */
public class BarrierDemo 
{
    static string fmt_th = "T Id:<Color=Red>{0}</Color>  Name:<Color=red>{1}</Color> ";
    static object lockObj = new object();
    public int count;
    Barrier barrier;

    public Transform target;//只能在主线程 修改position
    public Vector3 pos;
    public float step;

    public void Init(Transform target,float step)
    {
        this.target = target;
        this.step = step;
        this.pos = target.position;

        barrier = new Barrier(3,PostPhaseAct);  //3个参与者
        barrier.AddParticipants(2);             //变成5个参与者
        barrier.RemoveParticipant();            //变成4个参与者

        /*4个 action 刚刚好*/
        Parallel.Invoke(BarrierAction_1, BarrierAction_1, BarrierAction_1,BarrierAction_2);
        /*3个 会死锁？*/
        /*5个 引发异常 */

        target.position = pos;      //只能在主线程设置

        barrier.Dispose();
    }
    /* 无论多少个参与者，只会执行一次(在最后一个执行的线程上?) */
    public void PostPhaseAct(Barrier barrier)
    {
        int th_id = Thread.CurrentThread.ManagedThreadId;
        string th_name = Thread.CurrentThread.Name;
        string msg1 = string.Format(fmt_th, th_id, th_name);
        string msg2 = string.Format("Post-Phase action: count={0},phase={1}", count, barrier.CurrentPhaseNumber);
        Debug.Log(msg1 + "  " + msg2);

        if (barrier.CurrentPhaseNumber == 2)
        {
            throw new System.Exception("哈哈哈-哈哈-!");
        }
    }
    public void BarrierAction_1()
    {
        int th_id = Thread.CurrentThread.ManagedThreadId;
        string th_name = Thread.CurrentThread.Name;

        /* 
         * 加锁才能保证执行结果
         * 不加锁,操作的是缓冲中的副本，(从缓冲输出，导致最后的结果变小)
         * 加锁 可以保证 必然以一个<依次>顺序在操作这个对象
         */
        lock (lockObj)
        {
            pos = pos + Vector3.right * step;
        }
        Interlocked.Increment(ref count);
        barrier.SignalAndWait();//post-phase  count+3, phase=0

        lock (lockObj)
        {
            pos = pos + Vector3.right * step;
        }
        Interlocked.Increment(ref count);
        barrier.SignalAndWait();//post-phase  count+3, phase=1

        /* 第三次，引发一个Exception, 所有的参与者都能捕捉到 */
        Interlocked.Increment(ref count);//+3
        try
        {
            barrier.SignalAndWait();
        }catch(BarrierPostPhaseException bppe)
        {
            string msg1 = string.Format(fmt_th, th_id, th_name);
            string msg2 = string.Format("Caught BarrierPostPhaseException:{0}", bppe.InnerException.Message);
            Debug.Log(msg1+"  "+msg2);
        }
        /* 第四次 */
        Interlocked.Increment(ref count);
        barrier.SignalAndWait();//post-phase  count=+3, phase=3
    }
    public void BarrierAction_2()
    {
        int th_id = Thread.CurrentThread.ManagedThreadId;
        string th_name = Thread.CurrentThread.Name;

        Interlocked.Increment(ref count);
        barrier.SignalAndWait();//post-phase  count+1, phase=0
        //Interlocked.Increment(ref count);
        barrier.SignalAndWait();//post-phase  count+0, phase=1

        /* 第三次，引发一个Exception, 所有的参与者都能捕捉到 */
        Interlocked.Increment(ref count);
        try
        {
            barrier.SignalAndWait();
        }
        catch (BarrierPostPhaseException bppe)
        {
            string msg1 = string.Format(fmt_th, th_id, th_name);
            string msg2 = string.Format("Caught BarrierPostPhaseException:{0}", bppe.InnerException.Message);
            Debug.Log(msg1 + "  " + msg2);
        }
        /* 第四次 */
        Interlocked.Increment(ref count);
        barrier.SignalAndWait();//post-phase  count+1, phase=3
    }
}
