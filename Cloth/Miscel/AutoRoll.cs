using System;
using System.Collections.Generic;
using UnityEngine;
using Mg.Util;
namespace Mg.Games
{
    //滚动 子节点
    public class AutoRoll : MonoBehaviour
    {
        float max = 80f;
        float degreeSpd = 180f;
        float vaultDegree = 1f;
        float tickBack = 0.05f;

        float min;
        float degree = 0;
        Vector3 lastFwd, lastRit;
        SampleFloat samDegree;

        Transform rollTr;
        Transform motorTr;
        public void Init(int countSam,float max,float degreeSpd,float vaultDegree,float tickBack)
        {
            this.max = max;
            this.degreeSpd = degreeSpd;
            this.vaultDegree = vaultDegree;
            this.tickBack = tickBack;

            rollTr = transform;
            min = -max;
            motorTr = rollTr.parent;
            degree = 0;
            samDegree = new SampleFloat(countSam);
        }
        public void Setup()
        {
            degree = 0;
            rollTr.localEulerAngles = Vector3.zero;
        }
        public void UpdateRoll(float tick)
        {
            Vector3 fwd = motorTr.forward;

            //Slant 做机身的角度 倾斜
            float dot = Vector3.Dot(fwd, lastFwd);              //倾斜    机体
            float deg = Mathf.Acos(dot) * Mathf.Rad2Deg;

            float dotSign = Vector3.Dot(fwd, lastRit);
            float sign = -Mathf.Sign(dotSign);

            if (deg > vaultDegree)
            {
                degree = degree + deg * (tick * degreeSpd) * sign;
                degree = Mathf.Clamp(degree, min, max);

                lastFwd = fwd;                                  //变动时  才更新last
                lastRit = motorTr.right;
            }
            else
            {
                degree = Mathf.Lerp(degree, 0, tickBack);       //还原 机体
            }
            samDegree.AddSample(degree);                        //采样 角度
            degree = samDegree.average;
            rollTr.localEulerAngles = new Vector3(0, 0, degree);//倾斜 机体
        }
    }
}