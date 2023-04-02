using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using Debug = UnityEngine.Debug;
/*
    Mutex使用了比Monitor更多的资源(local 和 named system 2个级别)
    可用于进程间的同步，跨应用程序的作用域
    Mutex.WaitOne 阻塞调用者，直到获取/超时
    线程只能释放自己拥有的Mutex.ReleaseMutex
    线程可以请求一个已经拥有的Mutex,而不会阻塞自己，但是释放的次数必须和请求/拥有的次数一样多
 */
public class MutexDemo
{
    static string fmt_th = "T Id:<Color=Red>{0}</Color>  Name:<Color=red>{1}</Color> ";
    private static Mutex mut;
    public int counter = 0;
    public List<Job> jobs;
    [Serializable]
    public class Job
    {
        public int id;
        public string name;
        public int data;
    }
    public void Test(List<Job> jobs)
    {
        mut = new Mutex(true);  //先占有 mutex
        this.jobs = jobs;
        List<Thread> pools = new List<Thread>(128);
        for (int i = 0; i < 3; i++)
        {
            Thread th = new Thread(new ThreadStart(Proc));
            pools.Add(th);
            th.Start();
        }

        Thread.Sleep(1000);
        /* 在主线程上先预备一个数据 */
        Job job_1 = new Job() {data= -1314,id= Thread.CurrentThread.ManagedThreadId,name= "Main Thread" };
        jobs.Add(job_1);
        Debug.Log("Main Thread has job done!");
        mut.ReleaseMutex();

        Thread.Sleep(3000);
        for(int i = 0; i < pools.Count; i++)
        {
            if(pools[i].IsAlive)
                pools[i].Join();
        }
    }
    public void Proc()
    {
        int th_id = Thread.CurrentThread.ManagedThreadId;
        string th_name = Thread.CurrentThread.Name;
        for (int i = 0; i < 10; i++)
        {
            mut.WaitOne();
            string str = string.Format(fmt_th, th_id, th_name);
            Debug.Log(str + " 进入了 protected area");

            /*安全区的工作*/
            counter += 1;
            Job job = new Job() { data = counter, id = th_id, name = th_name };
            jobs.Add(job);

            Thread.Sleep(100);

            Debug.Log(str + " 离开了 protetced area");
            mut.ReleaseMutex();
        }
    }
    /*
     创建一个有名称的mutex, 在操作系统中，<同名称>的mutex只存在一个,不论是那个进程/线程 导致这个命名的mutex被创建的
     */
    public void TestSysemMutex(string mutexName)
    {
        //https://learn.microsoft.com/en-us/dotnet/api/system.threading.mutex.-ctor?view=net-7.0
        Mutex sys_mut = new Mutex(false, mutexName);
    }

}
