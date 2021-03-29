using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenSideEmitter : MonoBehaviour
{
    public ParticleSystem _particleSystem;
    private ParticleSystem.Particle[] _particleParts;

    private GameObject[] ParticleObject;

    [HideInInspector]
    public float ScreenWide;
    [HideInInspector]
    public float ScreenHeight;

    [HideInInspector]
    public Camera _camera;

    [Range(0f, 1f)]
    public float _DistScale;

    [Range(0f, 10f)]
    public float _ZDistance;

    //Durationの数(このScriptの生存時間
    private float Duration;
    //南回繰り返すか
    public int EmitCount = 1;
    private int _emitCount = 0;
    //00時間おきに
    public float EmitTime = 0f;
    //何個放出
    public int EmitManyPieces = 100;

    [HideInInspector]
    public float MasterTime;
    [HideInInspector]
    public float IntervalTime;

    public virtual void Start()
    {
        //Screenのパラメータはユーザーのカメラ次第でよく変わるので一回所得して使いまわす。
        ScreenWide = Screen.width;
        ScreenHeight = Screen.height;


        InitParticleEmitter();
    }
    public virtual void OnEnable()
    {
        InitParticleEmitter();
    }

    void InitParticleEmitter()
    {
        Duration = _particleSystem.main.duration;
        _emitCount = EmitCount;

        //座標系の計算に使うので所得しておく
        _camera = Camera.main;
        //emit用のテンポラリを生成
        emitParams = new ParticleSystem.EmitParams();

        _particleSystem.Clear();
        _particleSystem.Play();

        MasterTime = 0f;
        IntervalTime = 0f;
    }

    [HideInInspector]
    public ParticleSystem.EmitParams emitParams;
    public virtual void ParticleEmit(ref ParticleSystem _ps,int _count)
    {
        for(var i = 0; i < _count; i++)
        {
            emitParams.position = GetPos();
            //一個生成
            _ps.Emit(emitParams,1);
        }
    }

    public virtual void Update()
    {
        TimerCounter();
        ParticleEmitUpdate();
    }

    public void TimerCounter()
    {
        MasterTime += Time.deltaTime;
        IntervalTime += Time.deltaTime;
    }

    private void ParticleEmitUpdate()
    {
        if (IntervalTime >= EmitTime && _emitCount > 0 && MasterTime < Duration)
        {
            ParticleEmit(ref _particleSystem, EmitManyPieces);
            _emitCount--;
            IntervalTime = 0f;
        }
    }

    //座標決め
    public virtual Vector3 GetPos()
    {
        //座標分布
        var rnd1 = Random.Range(0f, 1f);
        var rnd2 = Random.Range(0, 2);

        //正規分布
        Vector3 RndPos;
        if (Random.Range(0f, 1f) >= 0.5f)
        {
            RndPos.x = rnd1 * ScreenWide;
            RndPos.y = rnd2 * ScreenHeight;
            RndPos.z = _ZDistance;
        } else
        {
            RndPos.x = rnd2 * ScreenWide;
            RndPos.y = rnd1 * ScreenHeight;
            RndPos.z = _ZDistance;
        }

        RndPos = _camera.ScreenToWorldPoint(RndPos);
        RndPos.x += GetNormalDistribution() * _DistScale;
        RndPos.y += GetNormalDistribution() * _DistScale;

        return RndPos;
    }

    //正規分布
    float normalCos = float.MaxValue;
    public float GetNormalDistribution()
    {
        float ret;
        if(normalCos != float.MaxValue)
        {
            ret = normalCos;
            normalCos = float.MaxValue;
        } else
        {
            var x = Random.value;
            var y = Random.value;
            var t0 = Mathf.Sqrt(-2f * Mathf.Log(x));
            var t1 = Mathf.PI * 2f * y;
            normalCos = t0 * Mathf.Cos(t1);
            ret = t0 * Mathf.Sin(t1);
        }

        return ret;
    }

}
