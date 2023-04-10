using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Mg.Util
{
    /* 有权重分布 */
    public class SampleFloat_W
    {
        public int count;           //样本数量
        public int idx = 0;         //最新样本 指针
        public float[] samples;   //样本库
        public float ValNew;      //平均数
        public float[] weights;
        public float totalWeight;
        public SampleFloat_W(int count, float initSample, int weightKind = 0)
        {
            this.count = count;
            samples = new float[count];
            weights = new float[count];
            float step = 1f / count;
            weights[0] = 1;
            totalWeight = 1;
            for (int i = 1; i < count; i++)
            {
                float t = step * (count - i);
                if (weightKind == 0)     //不同的权重
                {
                    weights[i] = t;
                    totalWeight += t;
                }
                else if (weightKind == 1)
                {
                    weights[i] = t * t;
                    totalWeight += (t * t);
                }
            }
            Reset(initSample);
        }
        public void AddSample(float newSample)
        {
            samples[idx] = newSample;
            idx = (idx + 1) % count;
            /* 可以优化一下,记录最旧的数值，然后删除 */
            float sum = 0;
            int idx_0 = (idx + 1) % count;
            int at;
            for (int i = 0; i < count; i++)
            {
                at = (idx_0 + i) % count;
                /* weights 从 0计数，sample 从 at 计数 */
                sum += samples[at] * weights[i];
            }
            ValNew = sum / totalWeight;
        }
        public void Offset(float offset)
        {
            for (int i = 0; i < count; i++)
            {
                samples[i] += offset;
            }
            ValNew += offset;
        }
        public void Reset(float bySample)
        {
            idx = 0;
            ValNew = bySample;
            for (int i = 0; i < count; i++)
            {
                samples[i] = bySample;
            }
        }
    }
}