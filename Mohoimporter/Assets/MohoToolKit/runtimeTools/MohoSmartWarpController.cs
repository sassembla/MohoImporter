using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ImporterUtilty;

public class MohoSmartWarpController : MonoBehaviour
{
    public MeshPointData[] MeshAnimationPoint;
    public GameObject SmartWarpObject;

    private Mesh swMesh;

    public int from;
    public int to;

    [Range(0.0f,1.0f)]
    public float value = 0;

    private void Start()
    {
        swMesh = SmartWarpObject.GetComponent<MeshFilter>().mesh;
    }

    private void Update()
    {
        SetPointData();
    }

    private void SetPointData()
    {
        //数値が入っていないときははじきます
        if (MeshAnimationPoint == null)
            return;

        List<Vector3> vertices = new List<Vector3>();
        swMesh.GetVertices(vertices);

        //数が合わないことはないと思うけど
        if (vertices.Count != MeshAnimationPoint[from].MeshVector.Length)
            return;

        for(var i=0;i< MeshAnimationPoint[from].MeshVector.Length; i++)
        {
            Vector3 fromVec = (Vector3)MeshAnimationPoint[from].MeshVector[i];
            Vector3 toVec = (Vector3)MeshAnimationPoint[to].MeshVector[i];
            Vector3 fixedVec = Vector3.zero;
            fixedVec.x = Mathf.SmoothStep(fromVec.x,toVec.x, value);
            fixedVec.y = Mathf.SmoothStep(fromVec.y, toVec.y, value);
            fixedVec.z = 0.0f;

            vertices[i] = fixedVec;
        }

        //数値の書き戻し
        swMesh.SetVertices(vertices);
    }
}
