using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Mg.Games
{
    //机器蝎子的触手
    public class Snake : MonoBehaviour
    {
        public bool IsActive { get; set; }

        public CableFree cableFree;
        public MimicRotate mimic;
        public float amtMimic = 1f;
        [Header("拖拽物体")] public GameObject goItem;
        [Header("Cable挂点")] public GameObject goCable;
        [Header("Local坐标")] public Vector3 dirMask;
        [Header("虚拟速度")] public float spd = 2f;

        SnakeHead item;
        MeshRenderer mr;
        //Vector3 dir;//World 坐标内
        Transform trCable;
        Transform trDyn;

        //
        public void InitComponent(object actorArg)
        {
            InitSnake();
            mimic.Init(trDyn);
        }
        void InitSnake()
        {
            dirMask.Normalize();
            trCable = goCable.transform;
            trDyn = trCable.parent;
            item = goItem.GetComponent<SnakeHead>();

            Vector3 dir = trDyn.localToWorldMatrix.MultiplyVector(dirMask);
            mr = cableFree.Init(goCable, dir);//goCable无旋转
            cableFree.onScaleUp = this.OnScaleUp;
        }
        public void SetupComponent()
        {
            this.IsActive = true;
            mr.enabled = true;
        }
        public void StopComponent() 
        {
            this.IsActive = false;
            mr.enabled = false;
        }

        void OnScaleUp()
        {
               mimic.StartSimulate();
        }
        void OnSignal(int signal)
        {
 
            switch (signal)
            {
                //case ActorSignal.DepartureDone:
                case 0:
                    cableFree.StartScaleup();//-->Role Start
                    this.IsActive = true;
                    break;
                //case ActorSignal.LandStart: //ActorCrew 转发
                case 1:
                    cableFree.StartScaledown();
                    break;
            }
        }
        public void UpdateTow()
        {
            float tick = 0.02f;

            Vector3 vec = mimic.Simulate(tick);
            Vector3 delta = vec * (spd * tick);
            Vector3 rit = Vector3.Cross(trDyn.up, vec);
            Vector3 up = Vector3.Cross(vec, rit);
            Vector3 dir = rit * dirMask.x + up * dirMask.y + vec * dirMask.z;
            dir.Normalize();

            cableFree.UpdateLink(tick, dir, delta, amtMimic);
            item.UpdateCable(cableFree);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (cableFree == null) return;
            cableFree.OnDrawGizmos();
        }
#endif
    }
}