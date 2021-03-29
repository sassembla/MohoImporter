using System.Collections.Generic;
using UnityEngine;

namespace MohoToolKit
{
    [System.Serializable]
    public class MohoColorChangeSet
    {
        public MohoAnimationColorChange.ColorCategory Category = MohoAnimationColorChange.ColorCategory.Color1;
        public GameObject[] MohoObject;
        public Material[] MohoMaterial;

        //実行時に呼ぶこと
        public void InitMaterial()
        {
            MohoMaterial = new Material[MohoObject.Length];

            for(var i = 0; i < MohoObject.Length; i++)
            {
                MeshRenderer msR = null;
                SkinnedMeshRenderer smR = null;

                msR = MohoObject[i].GetComponent<MeshRenderer>();
                if(msR == null)
                {
                    smR = MohoObject[i].GetComponent<SkinnedMeshRenderer>();
                }

                if (msR != null)
                {
                    MohoMaterial[i] = msR.sharedMaterial;
                }
                else if (smR != null)
                {
                    MohoMaterial[i] = smR.sharedMaterial;
                }
                else
                {
                    MohoMaterial[i] = null;
                }
            }
        }
    }

    [ExecuteInEditMode]
    public class MohoAnimationColorChange : MonoBehaviour
    {
        //SkinnedMeshもMeshも扱います
        public MohoColorChangeSet[] MohoChangeData;

        private MohoColorChangeSet[] MohoChangeDataBackup;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (MohoChangeData != MohoChangeDataBackup)
            {
                MohoChangeDataBackup = MohoChangeData;
                InitData();
            }
        }
#endif

        #region ColorProperty
        public enum ColorCategory
        {
            Color1,
            Color2,
            Color3,
            Color4,
            Color5,
            Color6
        }
        //Animatorからアクセスするために用意しておきます
        public Color _Col1;
        public Color _Col2;
        public Color _Col3;
        public Color _Col4;
        public Color _Col5;
        public Color _Col6;
        private Color[] ColList;
        private Color[] ColorBackup;

        private void SetColList()
        {
            if(ColList == null)
            {
                ColList = new Color[6];
            }

            ColList[0] = _Col1;
            ColList[1] = _Col2;
            ColList[2] = _Col3;
            ColList[3] = _Col4;
            ColList[4] = _Col5;
            ColList[5] = _Col6;

            if(ColorBackup == null)
            {
                ColorBackup = new Color[6];
                ColorBackupList();
            } else if (ColorBackup.Length == 0)
            {
                ColorBackup = new Color[6];
                ColorBackupList();
            }
        }

        private void ColorBackupList()
        {
            for(var i = 0; i < ColList.Length; i++)
            {
                ColorBackup[i] = ColList[i];
            }
        }
        #endregion

        //Sprites/DefaultのShaderのColorプロパティ
        private int colorID = Shader.PropertyToID("_Color");

        void Start()
        {
            //初期化
            InitData();

            //_Colを扱いやすくするためにリスト化
            SetColList();
        }


        private void InitData()
        {
            //Materialの収集
            foreach (MohoColorChangeSet dat in MohoChangeData)
            {
                dat.InitMaterial();
            }
        }

        void Update()
        {
            ChangeColor();
        }

        void ChangeColor()
        {
            int categoryType = 0;
            //Inspectorの色を配列にセット
            SetColList();

            for (var i = 0; i < MohoChangeData.Length; i++)
            {
                categoryType = (int)MohoChangeData[i].Category;
                //変化のある数値だけ処理します
                if (ColorBackup[categoryType] != ColList[categoryType])
                {
                    for (var l = 0; l < MohoChangeData[i].MohoObject.Length; l++)
                    {
                        MohoChangeData[i].MohoMaterial[l].SetColor(colorID, ColList[categoryType]);
                    }
                }
            }

            //今のフレームの状態を保持
            ColorBackupList();
        }
    }
}
