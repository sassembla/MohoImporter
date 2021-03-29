using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MohoStrokeAnimationController : MonoBehaviour
{
    [SerializeField]
    private GameObject StrokeObject = null;
    private GameObject _strokeObjectBackUp = null;
    private Material _strokeMaterial;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_strokeObjectBackUp != StrokeObject)
        {
            _strokeObjectBackUp = StrokeObject;
            InitData();
        }
    }
#endif


    private int valueID = Shader.PropertyToID("_Value");

    [Range(0.0f,1.0f)]
    public float StrokeValue;
    private float _strokeValueBackup;

    // Start is called before the first frame update
    void Start()
    {
        InitData();
    }

    void InitData()
    {
        StrokeValue = 0;
        _strokeValueBackup = StrokeValue;

        if (StrokeObject != null)
        {
            _strokeMaterial = StrokeObject.GetComponent<MeshRenderer>().sharedMaterial;
            _strokeMaterial.SetFloat(valueID, StrokeValue);
        }
    }
    // Update is called once per frame
    void Update()
    {
        if(StrokeObject != null)
        {
            if(_strokeValueBackup != StrokeValue)
            {
                _strokeMaterial.SetFloat(valueID, StrokeValue);
                _strokeValueBackup = StrokeValue;
            }
        }
    }
}
