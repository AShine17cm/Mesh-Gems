using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Mg.Games
{
    //模拟 旋转
    [Serializable]
    public class MimicRotate
    {
        public Vector4[] samples;   //eulerY{x,y},eulerX{z,w}
        public float[] times;
        public float range = 1f;

        float[] periods;

        Transform trPivot;
        int countSam;
        int idx;
        float timer;

        public void Init(Transform trPivot)
        {
            countSam = samples.Length;
            this.trPivot = trPivot;
            periods = new float[countSam];
            Array.Copy(times, periods, countSam);
        }
        public void StartSimulate()
        {
            idx = 0;
            timer = 0;
        }
        public Vector3 Simulate(float tick)
        {
            timer += tick;
            if (timer > periods[idx])
            {
                //idx = (idx + 1) % countSam;
                idx = idx + 1;
                if(idx>=countSam)
                {
                    idx = 0;
                    for(int i=0;i<countSam;i++)
                    {
                        periods[i] = times[i] + Random.Range(0, range);
                    }
                }
                timer = timer - periods[idx];
            }
            float t = timer / periods[idx];
            t = (3f - t) * t * t;
            float valA = Mathf.Lerp(samples[idx].x, samples[idx].y, t) * Mathf.Deg2Rad;
            float valB = Mathf.Lerp(samples[idx].z, samples[idx].w, t) * Mathf.Deg2Rad;
            float cosA = Mathf.Cos(valA);
            float sinA = Mathf.Sin(valA);
            float sinB = Mathf.Sin(valB);

            Vector3 dir = trPivot.forward * cosA + trPivot.right * sinA + trPivot.up * sinB;
            dir.Normalize();
            return dir;
        }
    }
}