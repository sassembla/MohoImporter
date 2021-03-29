using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TwoPointEmitter : ScreenSideEmitter
{
    //ParticleCount分のParticleを扱います
    //このEmitのParticleは無くならないので、このカウント分がずっと生き残ります。

    public int ParticleCount;
    private int LiveCounter;
    public Vector3[] StartEmit;
    public Vector3[] EndEmit;
    public float[] SwingEmit;

    //Xの位置　Min/Max
    public Vector2 PosXRange;
    //Yの位置　Min/Max
    public Vector2 PosYRange;
    //落下するときの左右の振れ幅
    [Range(0.0f, 1.0f)]
    public float Distotion;
    [Range(0.0f,10.0f)]
    public float FallingSwing;

    //開始と終わりの座標を反転します
    public bool _isReverce;

    //ParticleSystemのLifeTimeを使うと演出しにくいので自前で用意します。
    public float LifeTime; //移動の時間
    public float MiddlelTime; //次の演出までの間
    [Range(0.0f, 0.1f)]
    public float MiddleTimeDistortion;
    public float LastTime; //最後の演出の時間

    private float remainingLifetime;

    //乱数テーブル作る
    //Particleをエミットする
    //生きているParticleを動かす

    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();
        InitTwoPoint();
    }

    public override void OnEnable()
    {
        base.OnEnable();
        InitTwoPoint();
    }

    void InitTwoPoint()
    {
        base._particleSystem.Clear();

        StartEmit = new Vector3[ParticleCount];
        EndEmit = new Vector3[ParticleCount];
        SwingEmit = new float[ParticleCount];

        LiveCounter = 0;

        for(var i = 0; i < ParticleCount; i++)
        {
            StartEmit[i] = Vector3.zero;
            EndEmit[i] = Vector3.zero;
            SwingEmit[i] = 0f;
        }

        if (!_isReverce)
        {
            SetMinMaxPos(ref StartEmit);
            SetScreenSidePos(ref EndEmit);
        } else
        {
            SetMinMaxPos(ref EndEmit);
            SetScreenSidePos(ref StartEmit);
        }

        SetSwingEmit(ref SwingEmit);

        ParticleEmit(ref _particleSystem, ParticleCount);
        base._particleSystem.Pause();


    }


    public override void ParticleEmit(ref ParticleSystem _ps, int _count)
    {
        //ParticleCount以上は作らない
        if (LiveCounter >= ParticleCount)
            return;

        for (var i = 0; i < _count; i++)
        {
            if (LiveCounter < ParticleCount)
            {
                base.emitParams.position = Vector3.zero;
                //一個生成
                _ps.Emit(base.emitParams, 1);
                LiveCounter++;
            }
        }
    }

    // Update is called once per frame
    public override void Update()
    {
        //base.Update();
        base.TimerCounter();
        if (base.MasterTime < LifeTime)
        {
            //移動のシーケンス
            ParticleMove();
            IntervalTimeCount = 0;
        } else if (base.MasterTime < LifeTime + MiddlelTime)
        {
            //次の演出までの間
            IntervalMove();

        }else if (base.MasterTime < LifeTime + MiddlelTime + LastTime)
        {
            //最後の演出
            base._particleSystem.Play();
        }
        
    }

    void ParticleMove()
    {
        if(MasterTime < LifeTime)
        {
            base.IntervalTime = 0f;
            remainingLifetime = MasterTime / LifeTime;

            ParticleSystem.Particle[] _particles = new ParticleSystem.Particle[ParticleCount];
            base._particleSystem.GetParticles(_particles);

            for (var i = 0; i < _particles.Length; i++)
            {
                //startLifetimeが0の時は、このParticleは存在していない
                if (_particles[i].startLifetime != 0)
                {
                    float lifetimeNow = base.MasterTime / LifeTime;
                    var SwingMask = (lifetimeNow - 0.5f)*2f;
                    SwingMask *= SwingMask;
                    SwingMask = 1.0f - SwingMask;
                    var tempSwingX = Mathf.Sin(Mathf.PI * lifetimeNow * SwingEmit[i]) * SwingMask;
                    tempSwingX *= Distotion;

                    //残りの生存時間/全体の生存時間
                    //float lifetimeNow = _particles[i].remainingLifetime / _particles[i].startLifetime;
                    //lifetimeNow = Mathf.SmoothStep(0.3f, 0.5f, lifetimeNow);

                    //Todo : smoothstepだとEasingできないので後で考える
                    Vector3 tempPos;
                    tempPos.x = Mathf.SmoothStep(StartEmit[i].x, EndEmit[i].x, remainingLifetime) + tempSwingX;
                    tempPos.y = Mathf.SmoothStep(StartEmit[i].y, EndEmit[i].y, remainingLifetime);
                    tempPos.z = base._ZDistance;

                    _particles[i].position = tempPos;
                }
            }

            base._particleSystem.SetParticles(_particles,_particles.Length);
        }
    }

    private float IntervalTimeCount = 0;

    void IntervalMove()
    {
        IntervalTimeCount += Time.deltaTime;
        base.IntervalTime = 0f;
        ParticleSystem.Particle[] _particles = new ParticleSystem.Particle[ParticleCount];
        base._particleSystem.GetParticles(_particles);

        var yMove = Mathf.Sin(Mathf.PI * IntervalTimeCount) * MiddleTimeDistortion;

        for (var i = 0; i < _particles.Length; i++)
        {
            //startLifetimeが0の時は、このParticleは存在していない
            if (_particles[i].startLifetime != 0)
            {
                //Todo : smoothstepだとEasingできないので後で考える
                Vector3 tempPos;
                tempPos.x = EndEmit[i].x;
                tempPos.y = EndEmit[i].y + yMove;
                tempPos.z = base._ZDistance;

                _particles[i].position = tempPos;
            }
        }
        base._particleSystem.SetParticles(_particles, _particles.Length);


    }

    void LastMove()
    {
        base.IntervalTime = 0f;

        var LastTimeInterbal = base.MasterTime - (LifeTime + MiddlelTime);        
        remainingLifetime = (LastTimeInterbal / LastTime);
        var yMove = Mathf.Sin(remainingLifetime * Mathf.PI);

        ParticleSystem.Particle[] _particles = new ParticleSystem.Particle[ParticleCount];
        base._particleSystem.GetParticles(_particles);

        for (var i = 0; i < _particles.Length; i++)
        {
            //startLifetimeが0の時は、このParticleは存在していない
            if (_particles[i].startLifetime != 0)
            {
                //Todo : smoothstepだとEasingできないので後で考える
                Vector3 tempPos;
                tempPos.x = EndEmit[i].x;
                tempPos.y = EndEmit[i].y + yMove;
                tempPos.z = base._ZDistance;

                _particles[i].position = tempPos;
            }
        }

        base._particleSystem.SetParticles(_particles, _particles.Length);
    }

    void SetMinMaxPos(ref Vector3[] posArray)
    {
        for(var i = 0; i < posArray.Length; i++)
        {
            Vector3 tempPos = Vector3.zero;
            tempPos.x = Random.Range(PosXRange.x, PosXRange.y) * base.ScreenWide;
            tempPos.y = Random.Range(PosYRange.x, PosYRange.y) * base.ScreenHeight;
            tempPos.z = _ZDistance;

            tempPos = base._camera.ScreenToWorldPoint(tempPos);
            posArray[i] = tempPos;
        }
    }

    void SetScreenSidePos(ref Vector3[] posArray)
    {
        for(var i = 0; i < posArray.Length; i++)
        {
            posArray[i] = base.GetPos();
        }
    }

    void SetSwingEmit(ref float[] swingArray)
    {
        for(var i = 0; i < swingArray.Length; i++)
        {
            var temp = Random.Range(0.0f, FallingSwing);
            swingArray[i] = temp;
        }

    }

}
