using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ImporterUtilty
{
    [System.Serializable]
    public class MeshPointData
    {
        [SerializeField]
        public int Frame;
        [SerializeField]
        public Vector3?[] MeshVector;//データが存在していない判定をしたいのでNull許容しておく
    }

    public class FloatData
    {
        [SerializeField]
        public int[] Frame;
        [SerializeField]
        public float[] MeshFloat;//データが存在していない判定をしたいのでNull許容しておく
    }
}
