using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoftBoneHub : MonoBehaviour
{
    public SoftBone[] links;


    public void InitEditor()
    {
        if (null!=links)
        {
            for(int i = 0; i < links.Length; i++)
            {
                links[i].InitEditor();
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
