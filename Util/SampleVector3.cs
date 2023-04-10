using UnityEngine;
namespace Mg.Util
{
    public class SampleVector3
    {
        public int count;           //样本数量
        public int idx = 0;         //最新样本 指针
        public Vector3[] samples;   //样本库
        public Vector3 sum;         //矢量和
        public Vector3 ValNew;      //平均数
        public SampleVector3(int count,Vector3 initSample)
        {
            this.count = count;
            samples = new Vector3[count];
            Reset(initSample);
        }
        public void AddSample(Vector3 newSample)
        {
            sum -= samples[idx];
            sum += newSample;
            ValNew = sum / count;

            samples[idx] = newSample;
            idx = (idx + 1) % count;
        }
        public void Reset(Vector3 bySample)
        {
            idx = 0;
            sum = bySample*count;
            ValNew = bySample;
            for(int i=0;i<count;i++)
            {
                samples[i] = bySample;
            }
        }
    }
}

