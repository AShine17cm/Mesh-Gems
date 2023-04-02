using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Mg.Games
{
    //缆绳末端 物体
    public class SnakeHead : MonoBehaviour
    {
        public Transform trCargo;
        [Header("Cable相关")]public float scalCargo=1f;
        public float scalBase = 0f;                     //{Cargo,Base}{1,0}{0,1}
        //缆绳 拖拽的物体
        public void UpdateCable(CableFree cable)
        {
            trCargo.position = cable.Tail;
            //trCargo.up = -cable.TailDir;//cable Root在下
            trCargo.forward = -cable.TailDir;
            if(cable.doScale)
            {
                trCargo.localScale = Vector3.one
                    * (Mathf.Clamp(cable.scale + 0.05f, 0.05f, 1f)*scalCargo+scalBase);//最后一帧 doScale无法判断到
            }
        }
    }
}