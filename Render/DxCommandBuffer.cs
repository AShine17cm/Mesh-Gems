using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/* 测试一下 Command Buffer */
public class DxCommandBuffer : MonoBehaviour
{
    public Camera camera;
    public GameObject target;
    public List<Transform> matrixs;

    Material mat;
    Mesh mesh;
    Matrix4x4[] mats;

    CommandBuffer cmd;
    void Start()
    {
        mats = new Matrix4x4[matrixs.Count];
        for(int i = 0; i < mats.Length; i++)
        {
            mats[i] = matrixs[i].localToWorldMatrix;
        }
        mesh = target.GetComponent<MeshFilter>().sharedMesh;
        mat = target.GetComponent<MeshRenderer>().sharedMaterial;
        cmd= CommandBufferPool.Get(mat.name + "_0");
        cmd.DrawMeshInstanced(mesh, 0, mat, 0, mats, mats.Length);
        camera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, cmd);
    }

    // Update is called once per frame
    void Update()
    {
        //Graphics.DrawMeshInstanced(mesh, 0, mat, mats, mats.Length);

    }
    private void OnPreCull()
    {
        //Graphics.DrawMeshInstanced(mesh, 0, mat, mats, mats.Length);
    }
   
    private void OnPreRender()
    {
        //Graphics.DrawMeshInstanced(mesh, 0, mat, mats, mats.Length);
    }
}
