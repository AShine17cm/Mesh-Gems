using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;
public class ATest : MonoBehaviour
{
    public bool test = false;

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

        }
    }
}
