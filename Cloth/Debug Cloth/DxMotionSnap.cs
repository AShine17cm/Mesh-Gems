using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/* 在移动一段距离后，迅速挺住
 * 用于捕捉一个状态*/
public class DxMotionSnap : MonoBehaviour
{
    public float spd = 10f;
    public float period = 0.3f;
    public Transform target;
    public Vector3 moveDir = Vector3.down;
    public bool test = false;
    float timer;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (test)
        {
            test = false;
            timer = period;
        }
        if (timer > 0)
        {
            timer -= Time.deltaTime;
            target.position = target.position + moveDir*spd * Time.deltaTime;
            if (timer <= 0)
            {
                Debug.Break();
            }
        }
    }
}
