using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* 将一个点集 连线成互不相交的多边形 */
public class MonotonePolygon
{
    public List<Vector2> inPoints=new List<Vector2>(128);   //原始点
    public List<Vector2> outPoints=new List<Vector2>(128);  //封闭的,不相交的 点连线

    public List<int> left=new List<int>(128);               //左侧的连线
    public List<int> right=new List<int>(128);              //右侧的连线

   public  MonotonePolygon(List<Vector2> rawPoints)
    {
        inPoints.AddRange(rawPoints);
    }
   public  void Process()
    {
        /* 根据y大小做冒泡排序  */
        int toIndex = inPoints.Count - 1;
        while (true)
        {
            for (int p = 0; p < toIndex; p++)
            {
                Vector2 a = inPoints[p];
                Vector2 b = inPoints[p + 1];
                if (a.y < b.y)
                {
                    inPoints[p] = b;
                    inPoints[p + 1] = a;
                }
            }
            toIndex -= 1;
            if (toIndex <= 0)
            {
                break;
            }
        }
        /* 做成一个Y的单调多边形 */
        int count = inPoints.Count;
        List<bool> states = new List<bool>(128);
        for (int i = 0; i < count; i++)
        {
            states.Add(false);
        }

        left.Add(0);
        states[0] = true;
        //count = count - 1; //用于调试
        /* 捡出左侧的单边 */
        for (int i = 0; i < count;)
        {
            //Vector2 pt = inPoints[i];
            Vector2 a = inPoints[i + 1];
            Vector2 b = inPoints[i + 2];
            int nextI = -1;             //下一个起始点
            if (a.x <= b.x)
            {
                left.Add(i + 1);
                states[i + 1] = true;
                nextI = i + 1;
            }
            else
            {
                left.Add(i + 2);
                states[i + 2] = true;
                nextI = i + 2;
            }
            if (i + 2 == count - 1) break;
            if (i + 3 == count - 1)
            {
                left.Add(i + 3);
                states[i + 3] = true;
                break;
            }
            i = nextI;
        }
        /* 倒序相加，首尾相接  */
        right.Add(count - 1); //right侧的边 包括<Y-min,Y-max>
        for (int i = states.Count - 1; i >= 0; i -= 1)
        {
            if (!states[i])
            {
                right.Add(i);
            }
        }
        right.Add(0);

        for (int i = 0; i < left.Count; i++)
        {
            outPoints.Add(inPoints[left[i]]);
        }
        for (int i = 0; i < right.Count; i++)
        {
            if (i == 0 || i == right.Count - 1) continue;//排除<Y-min,Y-max>
            outPoints.Add(inPoints[right[i]]);
        }
    }
}
