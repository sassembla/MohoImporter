using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleChekcer : MonoBehaviour
{
    public ParticleSystem _PS;

    // Update is called once per frame
    void Update()
    {
        if(_PS != null)
            Debug.Log("ParticleCount >>" + _PS.particleCount);        
    }
}
