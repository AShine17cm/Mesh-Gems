using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Mg.Games
{
    //拖动 物体
    public class Tow : MonoBehaviour
    {
        public bool IsActive { get; set; }
        public Cable cable;
        [Header("Cable挂点")]public GameObject goCable;
        [Header("Local坐标")]public Vector3 dirMask;
        Vector3 dir;//World 坐标内
        Transform trCable;
        Transform trDyn;


        [Header("Cable相关")] public float scalCargo = 1f;
        Transform trCargo;
        bool isScaleDown;
        public void InitComponent(object actorArg)
        {
            dirMask.Normalize();
            trCable = goCable.transform;
            trDyn = trCable.parent;

            dir = trDyn.localToWorldMatrix.MultiplyVector(dirMask);
            cable.Init(goCable, dir);

        }
        public void SetupComponent()
        {
            //timer = 0;
            goCable.SetActive(true);
            trCargo.gameObject.SetActive(true);
        }
        void OnSignal(int signal)
        {
            switch (signal)
            {
                //case ActorSignal.DepartureDone:
                case 0:
                    cable.StartScaleup();
                    isScaleDown = false;
                    this.IsActive = true;
                    break;
                //case ActorSignal.LandStart:
                case 1:
                    isScaleDown = true;
                    cable.StartScaledown();
                       break;
            }
        }
        void UpdateTow()
        {
            if (!IsActive) return;
            this.dir = trDyn.localToWorldMatrix.MultiplyVector(dirMask);    //转换Force
            Vector3 delta =cable.Head- trCable.position;                  //7.18 与Cable中 delta=node[i]-conPos 一致

            float tick = 0.02f;
            if (isScaleDown) tick =tick* 8f;//加速缩小  DeadTime是0.5

            cable.UpdateLink(tick, dir,delta,0);

            trCargo.position = cable.Tail;
            trCargo.forward = cable.TailDir;
            if (cable.doScale)
            {
                trCargo.localScale = Vector3.one
                    * (Mathf.Clamp(cable.scale + 0.05f, 0.05f, 1f) * scalCargo);//最后一帧 doScale无法判断到
            }
        }
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (cable == null) return;

            cable.OnDrawGizmos();
        }
#endif
    }
}