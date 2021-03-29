using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenPointEmitter : ScreenSideEmitter
{
    public Transform[] targetEmit;

    public override void Start()
    {
        base.Start();
    }

    public override void OnEnable()
    {
        base.OnEnable();
    }

    public  override Vector3 GetPos()
    {
        //Emitが無ければ画面中心になります
        if (targetEmit.Length == 0)
        {
            Vector3 CenterPos;
            CenterPos.x = Screen.width * 0.5f;
            CenterPos.y = Screen.height * 0.5f;
            CenterPos.z = 0f;
            CenterPos = base._camera.ScreenToWorldPoint(CenterPos);
            CenterPos.x += base.GetNormalDistribution() * _DistScale;
            CenterPos.y += base.GetNormalDistribution() * _DistScale;
            return CenterPos;
        }

        Matrix4x4 V = base._camera.worldToCameraMatrix;
        Matrix4x4 P = base._camera.projectionMatrix;
        Matrix4x4 VP = P * V;
        //座標分布
        Vector3 emit = targetEmit[Random.Range(0,targetEmit.Length)].position;
        Vector4 tempPos;
        tempPos.x = emit.x;
        tempPos.y = emit.y;
        tempPos.z = emit.z;
        tempPos.w = 1.0f;

        Vector4 Pos = VP * tempPos;

        if(Pos.w == 0)
        {
            return Vector3.zero;
        }

        emit.x = ((Pos.x / Pos.w) + 1) * 0.5f * Screen.width;
        emit.y = ((Pos.y / Pos.w) + 1) * 0.5f * Screen.height;
        emit.z = ((Pos.z / Pos.w) + 1) * 0.5f * (base._camera.farClipPlane - base._camera.nearClipPlane);

        emit = base._camera.ScreenToWorldPoint(emit);

        emit.x += base.GetNormalDistribution() * _DistScale;
        emit.y += base.GetNormalDistribution() * _DistScale;

        return emit;
    }
}
