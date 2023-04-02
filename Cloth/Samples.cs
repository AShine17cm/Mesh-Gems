using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mg.Util
{
    public class Sample<T> where T : struct
    {
        //T Add(T a, T b)
        //{
        //    return (dynamic)a + (dynamic)b;
        //}
    }
    public class SampleFloat
    {
        public int count;
        public int idx;
        public float[] samples;
        public float sum;
        public float average;

        public SampleFloat(int countSam)
        {
            count = countSam;
            samples = new float[count];
        }
        public void AddSample(float val)
        {
            sum -= samples[idx];
            sum += val;
            average = sum / count;
            samples[idx] = val;
        }
    }
}