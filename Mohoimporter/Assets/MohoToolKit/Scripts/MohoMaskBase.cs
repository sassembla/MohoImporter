using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MohoMaskBase : MonoBehaviour
{
    //StencilMaskに使います
    const int Mask = 48; // 0011 0000


    //16 32 48の三つ使えることにしておきます
    // 16 : 0001 0000 CHANNEL1
    // 32 : 0010 0000 CHANNEL2
    // 48 : 0011 0000 CHANNEL3
    public int StencilID = 16;//0001 0000

    public Shader MaskShader;
    public Transform mohoBase;
    public List<Transform> mohoCildList;
    public List<int> mohoChildPriority;
    private Material _mat;

    public void SetUPData()
    {
        Init(mohoBase,0,true);
        InitChild();
    }

    private void InitChild()
    {
        int _count = 0;
        foreach(Transform child in mohoCildList)
        {
            if(child != null)
                Init(child.transform, mohoChildPriority[_count], false);
            _count++;
        }
    }

    private void Init(Transform obj,int Priority,bool isBase)
    {
        _mat = GetMaterial(obj);

        if (_mat != null && MaskShader != null)
        {
            _mat.shader = MaskShader;

            _mat.SetFloat("_MASK",1);
            _mat.SetInt("_Stencil", Mask);
            _mat.SetInt("_StencilMask", StencilID);

            if (isBase)
            {
                //Base 設定
                _mat.SetInt("_StencilComp", (int)UnityEngine.Rendering.CompareFunction.Always);
                _mat.SetInt("_StencilOp", (int)UnityEngine.Rendering.StencilOp.Replace);
                _mat.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                _mat.SetFloat("_ZWrite", 1);
            } else
            {
                //Child
                //mask child設定
                _mat.SetInt("_StencilComp", (int)UnityEngine.Rendering.CompareFunction.Equal);
                _mat.SetInt("_StencilOp", (int)UnityEngine.Rendering.StencilOp.Keep);
                _mat.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                _mat.SetFloat("_ZWrite", 1);
                //var Queue = (int)UnityEngine.Rendering.RenderQueue.Transparent + Priority;
                //_mat.renderQueue = Queue;
            }

        }
    }

    private Material GetMaterial(Transform obj)
    {
        Material tmp = null;

        MeshRenderer MR = obj.transform.GetComponent<MeshRenderer>();
        SkinnedMeshRenderer SMR;

        if (MR != null)
        {
            tmp = MR.sharedMaterial;
        }
        else
        {
            SMR = obj.transform.GetComponent<SkinnedMeshRenderer>();
            if (SMR != null)
                tmp = SMR.sharedMaterial;
        }

        return tmp;
    }
}
