using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ThreadMisc 
{

    public static void Proc()
    {
        /* CPU不能重新排序 位于Barrier两侧的内存访问代码 */
        // read(objectA.x)
        Thread.MemoryBarrier();
        //
        // read(objectA.y)

        /* read-write 操作，CPU不能移动到此操作之后 */
        int data=0;
        Thread.VolatileWrite(ref data,12);


        Thread.VolatileRead(ref data);
        /* read-write 操作，CPU不能移动到此操作之前*/


        /* 尝试主动让出CPU，由系统选择可以执行的Thread */
        bool isOK= Thread.Yield();
    }
}
