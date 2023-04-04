using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCrossDir : MonoBehaviour
{
    public Transform trA;
    public Transform trB;
    public Transform trC;

    public Transform trD;
    public int toDegree=30;
    public float degree;

    public Vector3 posX;
    [Header("两个向量叉积的长度")]
    public float magPlanar;
    public float magX;
    public float dotBC;
    public float sqrSum;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Vector3 posA = trA.position;
        Vector3 posB = trB.position;
        Vector3 posC = trC.position;

        Vector3 dirBA = (posB - posA).normalized;
        Vector3 dirCA = (posC - posA).normalized;
        Vector3 planarDir = Vector3.Cross(dirBA, dirCA);    //ABC 平面
        Vector3 dirBA_X = Vector3.Cross(planarDir,dirBA);  //BA的垂线
        //dirBA_X.Normalize();
        magPlanar = planarDir.magnitude;
        magX = dirBA_X.magnitude;
        dotBC = Vector3.Dot(dirBA, dirCA);

        sqrSum = magPlanar * magPlanar + dotBC * dotBC;

        /* 正统的角度计算 */
        float angX = Mathf.Deg2Rad * toDegree;
        float cos = Mathf.Cos(angX);
        float sin = Mathf.Sin(angX);
        Vector3 dirX = dirBA * cos + dirBA_X * sin;
        posX = posA + dirX * 3;

        /* 角度的比例 插值 */
        float dot = Vector3.Dot(dirBA, dirCA);
        float ang = Mathf.Acos(dot);
        degree = Mathf.Rad2Deg * ang;

        float k = toDegree / degree;
        dirX = dirBA * (1 - k) + dirCA * k;
        dirX.Normalize();

        Vector3 posD = posA + dirX * 3;
        trD.position = posD;
    }
    private void OnDrawGizmos()
    {
        Vector3 posA = trA.position;
        Vector3 posB = trB.position;
        Vector3 posC = trC.position;
        Vector3 posD = trD.position;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(posA, posB);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(posA, posC);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(posA, posD);

        Gizmos.color = Color.white;
        Gizmos.DrawLine(posA, posX);
    }
}
