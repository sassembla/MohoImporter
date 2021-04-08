using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using MiniJSON;
using ImporterUtilty;

//public class MeshPointData
//{
//    public int Frame;
//    public Vector3?[] MeshVector;//データが存在していない判定をしたいのでNull許容しておく
//}

public class AllObjectMesh
{
    public string Name;
    public Vector3 Translate;
    public Vector3 Scale;
    public MeshPointData[] _meshPointData;
    public List<int> _triangle;
    public List<Vector2> UV;
    public float FrameRate;
    public GameObject SmartWarpGameObject;
}

public class AllImageLayer
{
    public string Name;
    public string LinkedLayer;
    public string BoneLayer;
    public Vector3 Translate;
    public Vector3 Scale;
    public float FrameRate;
    public FloatData[] _floatAnimationData; //Opacityのアニメで使う
}

public class AllBoneTree
{
    public string LayerName;
    public List<Transform> BoneTree;
}

public class AllObjectAnimation
{
    public string ObjectName;
    public EditorCurveBinding[] BrendShapeData;
    public AnimationCurve[] curveShapeData;
}

public class MohoSmartWarpImporter : EditorWindow
{
    private const string BLEND_SHAPE_NAME = "frame_";


    [MenuItem("MohoEditor/MohoSmartWarpImporter")]
    private static void Create()
    {
        GetWindow<MohoSmartWarpImporter>("MohoSmartWarpImporter");
    }

    //public Object jsonFile;
    //public string filePath;
    // public string jsonData;
    //public Object animFile;
    //public string animfilePath;
    // public AnimationClip animData;
    public GameObject targetFBXForGUI;
    // private string projectName;

    private void OnGUI()
    {
        var targetFBX = EditorGUILayout.ObjectField("Set targetObject", targetFBXForGUI, typeof(GameObject), true) as GameObject;

        if (targetFBX != null)
        {
            // nullではないのでGUIへとセット
            targetFBXForGUI = targetFBX;

            string FBXPath = AssetDatabase.GetAssetPath(targetFBX);
            string path = Path.GetDirectoryName(AssetDatabase.GetAssetOrScenePath(targetFBX));
            var projectName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetOrScenePath(targetFBX));

            string animFilePath = path + "/" + projectName + "_anim/Mainline.anim";
            string jsonFilePath = path + "/" + projectName + ".json";

            var animData = AssetDatabase.LoadAssetAtPath(animFilePath, typeof(AnimationClip)) as AnimationClip;

            if (File.Exists(jsonFilePath) || animData != null)
            {
                var jsonData = File.ReadAllText(jsonFilePath);

                if (GUILayout.Button("SmartWarpImport"))
                {
                    Run(projectName, FBXPath, targetFBX, jsonData, path, animData);
                }
            }
            else
            {
                //animファイルか、Jsonファイルがありません
                EditorGUILayout.LabelField("情報が足りていません、以下を確認ください");
                EditorGUILayout.LabelField("FBXをReimportする");
                EditorGUILayout.LabelField("jsonファイルを作成する");
            }
        }
        else
        {
            EditorGUILayout.LabelField("MohoのFBXファイルをセットしてください");
        }
    }

    // 1メソッドでprefab化するやつ
    public static void Run(string projectName, string FBXPath, GameObject targetFBX, string jsonData, string path, AnimationClip animData)
    {
        var _obj = AssetDatabase.LoadAssetAtPath(FBXPath, typeof(GameObject)) as GameObject;
        //var targetObject = PrefabUtility.InstantiatePrefab(_obj) as GameObject;
        var targetObject = GameObject.Instantiate(_obj) as GameObject;

        var json = (IDictionary)Json.Deserialize(jsonData);

        //https://robamemo.hatenablog.com/entry/2019/10/17/150758
        //var runner = new MohoSmartWarpImporter();
        var runner = ScriptableObject.CreateInstance("MohoSmartWarpImporter") as MohoSmartWarpImporter;

        //jsonの復号
        runner.ReadAnimationJson(ref json);

        //データ作成準備
        //１）すでに、targetObjectに存在しているSkinnedMeshObjectを探す
        //２）すでに、targetObjectに存在しているMeshRenderObjectを探す
        //SkinnedMeshObjectはSmartWarpObjectではないので処理から除外しますが、Boneアニメーション関係の処置は機能追加が必要になるかも
        runner.DataConfirmation(targetObject);

        //1)のSkinnedMeshObjectからBoneツリーを抜き出しておく
        runner.SaveExistingBoneTree();

        //jsonのデータからBoneツリー構築
        runner.CreateBoneTree(projectName, targetObject, ref json);

        //SmartWarpObjectを作成していきます
        runner.CreateSmartWarpObject(projectName, targetFBX, targetObject);

        //Animationの再構築を行います
        runner.AnimationMarge(targetObject, ref animData);

        //Animationの登録
        runner.SetAnimationController(projectName, targetObject, path, ref animData);

        //AnimationClipの０フレームを削る
        runner.ZeroFrameDelete(ref animData);

        //TODO:MeshのZではなくアニメーションデータに入れ込む必要がある。
        //MeshRendererなら本体のPositionのアニメをいじる
        //SKinnedMeshRendererならそのParentのPositionのアニメをいじる
        //それぞれアニメが存在していなかったら、アニメではなくObjectのPositionをいじる

        //最後の処理として、描画順を処理する
        runner.CameraZinMeshVertexPosition(targetFBX, targetObject ,ref animData);

        string prefabPath = path + "/" + projectName + ".prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(targetObject, prefabPath, InteractionMode.AutomatedAction);
        AssetDatabase.SaveAssets();

        DestroyImmediate(targetObject);

        //非表示Objectを削除します
        runner.DeleteHiddenObject(prefabPath);
    }

    //個別のデータ
    public MeshPointData[] PData;
    public MeshPointData[] FixedPData;

    //すべてのデータ
    public AllObjectMesh[] AllObjectData;
    public AllImageLayer[] AllImageData;

    private void ReadAnimationJson(ref IDictionary json)
    {
        var FrameRate = (long)json["FrameRate"];
        var StartFrame = (long)json["StartFrame"];
        var EndFrame = (long)json["EndFrame"];

        //Debug.Log(FrameRate + "  :" + StartFrame + "/" + EndFrame);

        var animationList = (IList)json["AnimationList"];

        //すべてのSmartWarpObjectの頂点データを格納しておきます
        AllObjectData = new AllObjectMesh[animationList.Count];
        AllImageData = new AllImageLayer[animationList.Count];

        int AllObjectIndex = 0;
        int AllImageIndex = 0;

        for (var i = 0; i < animationList.Count; i++)
        {
            //Channel情報の復号
            var animationData = (IDictionary)animationList[i];
            var objectName = (string)animationData["ObjectName"];
            var LayerType = (string)animationData["LayerType"];
            //Debug.Log("ObjectName:" + objectName);

            //レイヤーそれぞれの原点からの差分位置情報
            Vector3 TranslationVec = Vector3.zero;
            var Translation = (IDictionary)animationData["Translation"];
            TranslationVec.x = ObjectToFloat(Translation["x"]);
            TranslationVec.y = ObjectToFloat(Translation["y"]);
            TranslationVec.z = ObjectToFloat(Translation["z"]);

            //レイヤーそれぞれのスケール値を所得
            Vector3 scaleVec = Vector3.zero;
            var ScaleFactor = (IDictionary)animationData["LayerScale"];
            scaleVec.x = ObjectToFloat(ScaleFactor["x"]);
            scaleVec.y = ObjectToFloat(ScaleFactor["y"]);
            scaleVec.z = ObjectToFloat(ScaleFactor["z"]);

            if (LayerType == "IMAGE")
            {
                //LayerType == IMAGE
                AllImageLayer AIL = new AllImageLayer();
                AIL.Name = objectName;
                var tempString = (string)animationData["LinkedLayer"];
                if (tempString != null)
                {
                    AIL.LinkedLayer = tempString;
                }
                else
                {
                    AIL.LinkedLayer = "NULL";
                }

                tempString = (string)animationData["BoneLayer"];
                if (tempString != null)
                {
                    AIL.BoneLayer = tempString;
                }
                else
                {
                    AIL.BoneLayer = "NULL";
                }

                //この辺でアニメデータを抜き出して格納しておく
                tempString = (string)animationData["ChannelName"];
                AIL._floatAnimationData = null;

                if (tempString == "Layer Opacity")
                {

                    var OpacityCount = (IList)animationData["OpacityAnimation"];
                    var floatAnimationData = new FloatData[OpacityCount.Count];
                    //Debug.Log("anime lookup >> " + objectName);

                    for (var _Op = 0; _Op < OpacityCount.Count; _Op++)
                    {
                        var FloatAnimData = (IDictionary)OpacityCount[_Op];
                        var KeyLength = (long)FloatAnimData["KeyLength"];
                        var dataDuration = (long)FloatAnimData["Duration"];
                        var AnimationKey = (IList)FloatAnimData["AnimationKey"];

                        FloatData tempData = new FloatData();

                        //Debug.Log("Animation Key Count!!! >>>>> " + AnimationKey.Count);

                        int[] _Frame = new int[KeyLength];
                        float[] _MeshFloat = new float[KeyLength];


                        for (var _Op2=0;_Op2 < AnimationKey.Count; _Op2++)
                        {
                            //var inData = (IDictionary)AnimationKey[_Op2];
                            var inKey = (IDictionary)AnimationKey[_Op2];
                            _Frame[_Op2] = (int)ObjectToFloat(inKey["Frame"]);
                            _MeshFloat[_Op2] = ObjectToFloat(inKey["value"]);

                            //Debug.Log("anime lookup >> " + objectName + " / " + KeyLength);
                            //Debug.Log("Json Anime Key" + _Frame[_Op2] + " : " + _MeshFloat[_Op2]);
                        }

                        tempData.Frame = _Frame;
                        tempData.MeshFloat = _MeshFloat;

                        floatAnimationData[_Op] = tempData;
                    }
                    AIL._floatAnimationData = floatAnimationData;
                }
                AIL.FrameRate = FrameRate;
                AIL.Translate = TranslationVec;
                AIL.Scale = scaleVec;
                AllImageData[AllImageIndex] = AIL;
                AllImageIndex++;
            }
            else if (LayerType == "VECTOR")
            {
                //LayerType == VECTOR
                var PointCount = (IList)animationData["PointAnimation"];

                //for(var j=0;j < PointCount.Count; j++)
                //{
                //    var PointAnimData = (IDictionary)PointCount[j];
                //    var PointIndex = (long)PointAnimData["PointIndex"];
                //    var PointAnimationKey = (IList)PointAnimData["AnimationKey"];
                //    Debug.Log("PointIndex :" +  PointIndex + "   KeyLength:" + PointAnimationKey.Count);
                //}

                //一回Frame数を調べて
                int pointDuration = 0;
                for (var j = 0; j < PointCount.Count; j++)
                {
                    var PointAnimData = (IDictionary)PointCount[j];
                    var dataDuration = (long)PointAnimData["Duration"];
                    if (pointDuration < dataDuration) pointDuration = (int)dataDuration;
                }

                //Frameは0からDurationまでなので一個多い
                pointDuration++;

                //頂点データを格納する場所を初期化
                PData = new MeshPointData[pointDuration];
                var dataCount = 0;
                foreach (MeshPointData data in PData)
                {
                    MeshPointData tmpData = new MeshPointData();

                    tmpData.Frame = dataCount;
                    tmpData.MeshVector = new Vector3?[PointCount.Count];
                    for (var s = 0; s < tmpData.MeshVector.Length; s++)
                    {
                        tmpData.MeshVector[s] = null;
                    }

                    PData[dataCount] = tmpData;
                    dataCount++;
                }

                //PDataに、存在している値を格納
                for (var j = 0; j < PointCount.Count; j++)
                {
                    var PointAnimData = (IDictionary)PointCount[j];
                    var PointAnimationKey = (IList)PointAnimData["AnimationKey"];
                    var PointIndex = (int)(long)PointAnimData["PointIndex"];

                    //作業用
                    Vector3 tmpData = Vector3.zero;
                    for (var keyframe = 0; keyframe < PointAnimationKey.Count; keyframe++)
                    {
                        var channel = (IDictionary)PointAnimationKey[keyframe];
                        var thisFrame = (int)(long)channel["Frame"];
                        var valueData = (IList)channel["value"];

                        //dataの格納
                        Vector3 value = Vector3.zero;
                        value.x = ObjectToFloat(valueData[0]);// (float)(double)valueData[0];
                        value.y = ObjectToFloat(valueData[1]);// (float)(double)valueData[1];
                        //Debug.Log("PointIndex :" + PointIndex + "     KeyFrame: " + thisFrame + " Value:" + value);
                        PData[thisFrame].MeshVector[j] = value;
                    }
                }

                //Debug.Log("--データ抽出終了--");

                //データを整形
                List<MeshPointData> fixedPointData = new List<MeshPointData>();
                for (var j = 0; j < PData.Length; j++)
                {
                    bool isUse = false;
                    for (var s = 0; s < PData[j].MeshVector.Length; s++)
                    {
                        if (PData[j].MeshVector[s] != null)
                            isUse = true;
                    }

                    //index jのデータは使う
                    if (isUse)
                    {
                        fixedPointData.Add(PData[j]);
                    }
                }

                //データFix
                FixedPData = new MeshPointData[fixedPointData.Count];
                for (var l = 0; l < FixedPData.Length; l++)
                {
                    FixedPData[l] = fixedPointData[l];
                    //Debug.Log("frames :" + fixedPointData[l].Frame);

                    if (l > 0)
                    {
                        for (var s = 0; s < FixedPData[l].MeshVector.Length; s++)
                        {
                            for (var count = l; count >= 0; count--)
                            {
                                if (FixedPData[count].MeshVector[s] != null)
                                {
                                    FixedPData[l].MeshVector[s] = FixedPData[count].MeshVector[s];
                                    break;
                                }
                            }
                        }
                    }
                }


                AllObjectMesh AOM = new AllObjectMesh();
                AOM._meshPointData = FixedPData;
                //AOM._triangle = new List<int>();
                AOM.Name = objectName;
                AOM.Translate = TranslationVec;
                AOM.Scale = scaleVec;
                AOM.FrameRate = (float)(long)json["FrameRate"];

                //Triangleを構成する頂点の数を所得
                var triangleCountData = (IList)animationData["TriangleCount"];
                int triangleLastCount = System.Convert.ToInt32(triangleCountData[triangleCountData.Count - 1]);

                //Triangle情報抜き出し
                List<int> triangleList = new List<int>();
                var triangleData = (IList)animationData["Triangle"];

                //この数値が３の時は削る必要はない、3以上の時は削ることにするごみに見えるから。
                //この数値が3以上のときはSmartWarpのメッシュの作りに問題がある。
                //TODO:ミスしていることを伝える必要はあるかも
                if(triangleLastCount == 3)
                {
                    triangleLastCount = 0;
                }

                for (var l = 0; l < triangleData.Count - triangleLastCount; l++)
                {
                    var tmp = System.Convert.ToInt32(triangleData[l]);
                    triangleList.Add(tmp);
                }

                AOM._triangle = triangleList;
                AOM.UV = new List<Vector2>();

                AllObjectData[AllObjectIndex] = AOM;
                AllObjectIndex++;
            }
        }

        /*
        for(var i = 0; i < AllObjectData.Length; i++)
        {
            var data = AllObjectData[i];
            if(data != null)
            {
                Debug.Log("No :" + i + " " + data.Name);
            } else
            {
                Debug.Log("No : " + i + " NULL");
            }
        }
        */
    }

    public List<MeshRenderer> AllBaseMRData;
    public List<SkinnedMeshRenderer> AllBaseSMRData;

    //MeshRendererとSkinnedMeshRendererを探すぞ
    private void DataConfirmation(GameObject targetObject)
    {
        AllBaseMRData = new List<MeshRenderer>();
        AllBaseSMRData = new List<SkinnedMeshRenderer>();

        AllSearchRenderer(targetObject.transform);

        //Debug
        /*
        for(var i = 0; i < AllBaseMRData.Count; i++)
        {
            Debug.Log("MeshRenderer   :" + AllBaseMRData[i].name);
        }

        for(var i = 0; i < AllBaseSMRData.Count; i++)
        {
            Debug.Log("SkinnedMeshRenderer   :" + AllBaseSMRData[i].name);
        }
        */
    }
    private void AllSearchRenderer(Transform Object)
    {
        var _children = Object.GetComponentInChildren<Transform>();
        if (_children.childCount == 0) return;

        foreach (Transform obj in _children)
        {
            MeshRenderer MR = obj.GetComponent<MeshRenderer>();
            SkinnedMeshRenderer SMR = obj.GetComponent<SkinnedMeshRenderer>();

            if (MR != null)
            {
                AllBaseMRData.Add(MR);
            }
            if (SMR != null)
            {
                //Debug.Log(SMR.name + " RootBone >> " + SMR.rootBone.name);
                AllBaseSMRData.Add(SMR);
            }
            if (obj.childCount != 0)
                AllSearchRenderer(obj);
        }
    }

    //Treeを保持していたObjectはAllBaseSMRDataに入ってるExistTreeとはIndexで同一
    public AllBoneTree[] ExistTree;//Tree本体
    //既存のBoneTreeが存在しているなら保存しておく
    private void SaveExistingBoneTree()
    {
        //SkinnedMeshRendererが0の時は既存のツリーはないので処理しない
        if (AllBaseSMRData.Count == 0)
            return;

        ExistTree = new AllBoneTree[AllBaseSMRData.Count];

        for (var i = 0; i < AllBaseSMRData.Count; i++)
        {
            AllBoneTree tempABT = new AllBoneTree();
            tempABT.BoneTree = new List<Transform>();

            var _bones = AllBaseSMRData[i].bones;
            foreach (Transform obj in _bones)
            {
                tempABT.BoneTree.Add(obj);
            }

            tempABT.LayerName = "";
            var root = tempABT.BoneTree[0];
            if (root != null)
            {
                if (root.parent != null)
                {
                    tempABT.LayerName = root.parent.transform.name;
                }
            }
            ExistTree[i] = tempABT;
        }

        //Debug
        /*
        for(var i = 0; i < ExistTree.Length; i++)
        {
            for(var l = 0; l < ExistTree[i].BoneTree.Count; l++)
            {
                Debug.Log("[" + i + "][" + l + "]  :" + ExistTree[i].BoneTree[l].name);
            }
        }
        */
    }

    public AllBoneTree[] ABT;
    private void CreateBoneTree(string projectName, GameObject targetObject, ref IDictionary json)
    {
        var boneTreeList = (IList)json["Bonetree"];
        if (boneTreeList != null)
        {
            if (boneTreeList.Count > 0)
            {
                ABT = new AllBoneTree[boneTreeList.Count];

                for (var i = 0; i < boneTreeList.Count; i++)
                {
                    var tempABT = new AllBoneTree();
                    //Channel情報の復号
                    var boneData = (IDictionary)boneTreeList[i];
                    var rootBoneName = (string)boneData["LayerName"];
                    var boneList = (IList)boneData["BoneArray"];
                    tempABT.BoneTree = new List<Transform>();

                    //LayerNameを保存しておく
                    tempABT.LayerName = rootBoneName;

                    for (var l = 0; l < boneList.Count; l++)
                    {
                        var boneArrayData = (IDictionary)boneList[l];
                        var boneName = (string)boneArrayData["BoneName"];
                        //BoneのParent情報とっておいたけど今は使わないかも
                        //var boneParentIndex = (long)boneArrayData["ParentIndex"];

                        tempABT.BoneTree.Add(SearchBone(targetObject.transform, boneName));
                    }
                    ABT[i] = tempABT;

                    //Debug
                    /*
                    for (var s = 0; s < tempABT.BoneTree.Count; s++)
                    {
                        Debug.Log(tempABT.BoneTree[s]);
                    }
                    */
                }

                return;
            }
        }
 
        {
            //Boneが存在していないパターン
            //存在していないときは仮Boneを生成してそれをセットしておきます
            GameObject RootObj = targetObject.transform.root.gameObject;
            GameObject Bone = new GameObject();
            Bone.name = "Bone_" + projectName;
            Bone.transform.parent = RootObj.transform;

            ABT = new AllBoneTree[1];
            var tempABT = new AllBoneTree();
            tempABT.BoneTree = new List<Transform>();
            tempABT.BoneTree.Add(Bone.transform);
            tempABT.LayerName = "NEW";
            ABT[0] = tempABT;
        }
    }

    private Transform SearchBone(Transform Object, string _name)
    {
        var _children = Object.GetComponentInChildren<Transform>();
        //みつからないのでnullを返します
        if (_children.childCount == 0) { return null; }
        Transform temp = null;

        foreach (Transform obj in _children)
        {
            if (obj.name == _name)
            {
                return obj;
            }
            else
            {
                temp = SearchBone(obj, _name);
                if (temp != null) break;
            }
        }
        return temp;
    }

    private void CreateMesh(ref Mesh _mesh, int _index, int imageIndex, GameObject _target, Vector3 _scale)
    {
        var vertices = new List<Vector3>();
        Vector3 translateVec = Vector3.zero;

        translateVec = AllImageData[imageIndex].Translate;
        translateVec = translateVec - AllObjectData[_index].Translate;
        //xは反転しておく
        translateVec.x *= -1.0f;

        //Debug.Log("Trancelate :" + translateVec.x + "," + translateVec.y + "," + translateVec.z);

        //BaseMeshの作製
        for (var i = 0; i < AllObjectData[_index]._meshPointData[0].MeshVector.Length; i++)
        {
            Vector3 data = (Vector3)AllObjectData[_index]._meshPointData[0].MeshVector[i];
            //xは反転しておく
            data.x *= -1.0f;
            data -= translateVec;

            data.x *= _scale.x;
            data.y *= _scale.y;
            data.z *= _scale.z;

            vertices.Add(data);
        }
        //Debug.Log("_meshPointData Count :" + AllObjectData[_index]._meshPointData.Length);
        _mesh.SetVertices(vertices);
        _mesh.SetTriangles(AllObjectData[_index]._triangle, 0);

        List<Vector2> uvs = new List<Vector2>();
        GetUVList(ref uvs, ref vertices, _target);
        _mesh.SetUVs(0, uvs);

        //BlendShapeの作製
        //Todo :移動量としてしか使わないので、データ整形のときにNullだったものはそのままVector3.Zeroに置き換えてしまってもよかったかも
        Vector3[] blendShapeVert = new Vector3[vertices.Count];
        for (var s = 0; s < AllObjectData[_index]._meshPointData.Length; s++)
        {
            for (var i = 0; i < AllObjectData[_index]._meshPointData[0].MeshVector.Length; i++)
            {
                if (s == 0)
                {
                    blendShapeVert[i] = ((Vector3)AllObjectData[_index]._meshPointData[s].MeshVector[i]) - ((Vector3)AllObjectData[_index]._meshPointData[s].MeshVector[i]);
                }
                else
                {
                    blendShapeVert[i] = ((Vector3)AllObjectData[_index]._meshPointData[s].MeshVector[i]) - ((Vector3)AllObjectData[_index]._meshPointData[s - 1].MeshVector[i]);
                }
                blendShapeVert[i].x *= -1.0f;

                blendShapeVert[i].x *= _scale.x;
                blendShapeVert[i].y *= _scale.y;
                blendShapeVert[i].z *= _scale.z;
            }
            _mesh.AddBlendShapeFrame(BLEND_SHAPE_NAME + s, 100f, blendShapeVert, null, null);
        }

        _mesh.RecalculateNormals();
        _mesh.RecalculateTangents();
    }

    [SerializeField]
    public Mesh _mesh;
    //今回のスクリプトで作成したObjectのリストを作っておきます
    public List<Transform> BuildedMeshList;
    //Objectが参照していたImageObjectのPathをマージのために記録しておきます
    public List<string> BuildedObjectTargetPath;
    //ObjectのBindingPath作成用リスト
    public List<string> BuildedMeshPathList;
    //jsonのIndexを記録しておく
    public List<string> BuildedMeshIndexObject;

    private void CreateSmartWarpObject(string projectName, GameObject targetFBX, GameObject targetObject)
    {

        BuildedMeshList = new List<Transform>();
        BuildedMeshPathList = new List<string>();
        BuildedObjectTargetPath = new List<string>();
        BuildedMeshIndexObject = new List<string>();

        for (var i = 0; i < AllObjectData.Length; i++)
        {
            if (AllObjectData[i] == null)
                continue;

            string _name = AllObjectData[i].Name;
            _name = _name.Replace("|", "_");

            var filename = Path.GetDirectoryName(AssetDatabase.GetAssetOrScenePath(targetFBX)) + "/" + _name + ".asset";
            Mesh data = AssetDatabase.LoadAssetAtPath(filename, typeof(Mesh)) as Mesh;
            if (data != null)
            {
                Debug.Log("同じ名前のMeshが存在しているので削除しておきます　>>" + _name);
                AssetDatabase.DeleteAsset(filename);
                AssetDatabase.Refresh();
            }
        }

        for (var i = 0; i < AllImageData.Length; i++)
        {
            if (AllImageData[i] == null)
                continue;

            //Debug.Log(AllImageData[i].Name + " / " + AllImageData[i].LinkedLayer);

            //LinkedLayerがNullの場合SmartWarpと連動していないので処理しない
            if (AllImageData[i].LinkedLayer == "NULL")
                continue;

            int? ObjectIndex = GetSmartWarpIndex(AllImageData[i].LinkedLayer);
            Transform ExistImage = GetExistObject(AllImageData[i].Name);

            //Debug.Log((int)ObjectIndex + " / " + ExistImage);

            //if (ObjectIndex == null)
            //{
            //    Debug.Log("NULL : " + ExistImage);
            //}
            //else
            //{
            //    Debug.Log((int)ObjectIndex + " : " + ExistImage);
            //}

            //OjectIndexがNullの時はjsonに必要な情報が含まれてない
            //ExistImageがNullの時はFBX側に情報が入っていない
            if (ObjectIndex == null || ExistImage == null)
                continue;

            //ベースObjectが見つかった
            //Debug.Log(AllImageData[i].Name + " >> " + AllObjectData[(int)ObjectIndex].Name + "   ExistObj : " + ExistImage);
            CreateSmartWarpObjectCore(projectName, targetFBX, targetObject, i, ExistImage, (int)ObjectIndex);

            GameObject go = GetAllChilren(targetObject.transform, "Moho Camera");
            go.SetActive(false);
        }
    }

    private void CreateSmartWarpObjectCore(string projectName, GameObject targetFBX, GameObject targetObject, int _ImageIndex, Transform _Base, int _ObejctIndex)
    {
        GameObject RootObj = targetObject.transform.root.gameObject;

        //Boneを調べて　作成する
        //ImageがターゲットにしているLayer名を持つBoneTreeを検索
        int? LayerIndex = null;
        for (var i = 0; i < ABT.Length; i++)
        {
            if (ABT[i].LayerName == AllImageData[_ImageIndex].BoneLayer)
            {
                LayerIndex = i;
                break;
            }
        }

        int BoneIndex = 0;
        GameObject Bone;
        if (LayerIndex != null)
        {
            Bone = SearchBone((int)LayerIndex, _Base).gameObject;
            for (var i = 0; i < ABT[(int)LayerIndex].BoneTree.Count; i++)
            {
                if (ABT[(int)LayerIndex].BoneTree[i].name == Bone.name)
                {
                    BoneIndex = i;
                    break;
                }
            }
        } else
        {
            LayerIndex = 0;
            //Bone = new GameObject();
            //Bone.name = "Bone_" + projectName;
            //Bone.transform.parent = RootObj.transform;
        }

        //Materialの入れ物を準備
        Material newMeshMaterial;

        //Debug.Log(AllImageData[_ImageIndex].Name + " >> Bone:" +  Bone.name + "  >>" + AllObjectData[_ObejctIndex].Name);

        //Materialを所得
        newMeshMaterial = _Base.GetComponent<MeshRenderer>().sharedMaterial;

        GameObject SmoothWarp = new GameObject();
        SmoothWarp.name = _Base.name + "_append";
        SmoothWarp.transform.parent = _Base.parent;
        SmoothWarp.transform.localPosition = _Base.localPosition;
        SmoothWarp.transform.localScale = Vector3.one;
        Vector3 existScale = AllObjectData[_ObejctIndex].Scale;
        SmoothWarp.transform.localRotation = _Base.localRotation;

        var _skinMesh = SmoothWarp.AddComponent<SkinnedMeshRenderer>();

        string meshName = AllObjectData[_ObejctIndex].Name;
        meshName = meshName.Replace("|", "_") + "_" + _Base.name.Replace("|", "_");
        string filename = Path.GetDirectoryName(AssetDatabase.GetAssetOrScenePath(targetFBX)) + "/meshdata/" + meshName + ".asset";
        string dictPath = Path.GetDirectoryName(AssetDatabase.GetAssetOrScenePath(targetFBX)) + "/meshdata/";

        if (!Directory.Exists(dictPath))
            Directory.CreateDirectory(dictPath);

        //Mesh data = AssetDatabase.LoadAssetAtPath(filename, typeof(Mesh)) as Mesh;

        _mesh = new Mesh();
        _mesh.name = meshName;

        CreateMesh(ref _mesh, _ObejctIndex, _ImageIndex, _Base.gameObject, existScale);
        _skinMesh.sharedMaterial = newMeshMaterial;
        _skinMesh.rootBone = RootObj.transform;

        Transform[] _bones = ABT[(int)LayerIndex].BoneTree.ToArray();
        Matrix4x4[] _bindPose = new Matrix4x4[_bones.Length];

        for (var i = 0; i < _bindPose.Length; i++)
        {
            _bindPose[i] = _bones[i].transform.worldToLocalMatrix * _skinMesh.localToWorldMatrix;
        }
        BoneWeight[] _weights = new BoneWeight[AllObjectData[_ObejctIndex]._meshPointData[0].MeshVector.Length];
        for (var j = 0; j < _weights.Length; j++)
        {
            _weights[j].boneIndex0 = BoneIndex;
            _weights[j].weight0 = 1.0f;
        }

        _mesh.boneWeights = _weights;

        //bindPoseをMeshにセット
        _mesh.bindposes = _bindPose;

        //SkinnedMeshRendererに設定をセット
        _skinMesh.bones = _bones;

        AssetDatabase.CreateAsset(_mesh, filename);
        AssetDatabase.SaveAssets();

        _mesh = AssetDatabase.LoadAssetAtPath(filename, typeof(Mesh)) as Mesh;

        _skinMesh.sharedMesh = _mesh;

        //作成したObjectのリンクを記録しておきます
        AllObjectData[_ObejctIndex].SmartWarpGameObject = _skinMesh.gameObject;


        //Debug.Log("今作成しました！！　>>> " + _skinMesh.name);
        //作成したSkinMesh
        BuildedMeshList.Add(_skinMesh.transform);
        //作成したSkinMeshのPath
        BuildedMeshPathList.Add(GetBindingPath(targetObject, _skinMesh.transform));
        //SkinMeshと紐づくJsonObjectのIndex
        BuildedMeshIndexObject.Add(AllObjectData[_ObejctIndex].Name);
        //作成したSkinMeshが参照するFBXのImageのPath
        BuildedObjectTargetPath.Add(GetBindingPath(targetObject, _Base));

        //対象は消しておきます
        _Base.gameObject.SetActive(false);
    }

    //今あるObjectの階層の中からBoneを探し出す
    private Transform SearchBone(int _TreeID, Transform _BaseObject)
    {
        bool _IsLooked = false;

        Transform _parent = _BaseObject.parent;
        if (_parent == null)
            return null;

        foreach (Transform target in ABT[_TreeID].BoneTree)
        {
            if (target.name == _parent.name)
            {
                _IsLooked = true;
                return _parent;
            }
        }
        if (_IsLooked == false)
            _parent = SearchBone(_TreeID, _parent);

        return _parent;
    }

    //_nameと同じ名前を持つAllObjectDataをPickUPします
    //存在しないときはnull返す
    private int? GetSmartWarpIndex(string _name)
    {
        for (var i = 0; i < AllObjectData.Length; i++)
        {
            if (AllObjectData[i] == null)
                break;

            //Debug.Log(AllObjectData[i].Name + " <> " + _name);
            if (AllObjectData[i].Name == _name)
                return (int?)i;
        }

        return null;
    }

    //BaseデータからTransformを検索します
    private Transform GetExistObject(string _name)
    {
        for (var i = 0; i < AllBaseMRData.Count; i++)
        {
            //   Debug.Log("Searched ... " + _name + " <> " + AllBaseMRData[i].name);
            if (AllBaseMRData[i].name == _name)
                return AllBaseMRData[i].transform;
        }

        return null;
    }

    private void GetUVList(ref List<Vector2> _uvs, ref List<Vector3> _verticsNewMesh, GameObject _target)
    {
        Mesh _meshBase = new Mesh();
        _meshBase = _target.GetComponent<MeshFilter>().sharedMesh;

        var verticesBase = new List<Vector3>();
        _meshBase.GetVertices(verticesBase);
        List<Vector2> baseUv = new List<Vector2>();
        _meshBase.GetUVs(0, baseUv);

        //Debug.Log(targetObject.name + " in VerticesCount:" + verticesBase.Count);


        int[] meshPointUVIndex = new int[_verticsNewMesh.Count];

        for (var l = 0; l < _verticsNewMesh.Count; l++)
        {

            Vector3 checkJsonVec = _verticsNewMesh[l];
            //Debug.Log($"JsonVecNo:{l:0} Vertices:({checkJsonVec.x:0.0000}, {checkJsonVec.y:0.0000}, {checkJsonVec.z:0.0000})");

            int index = 0;
            float distance = 0;

            //全件検索
            for (var s = 0; s < verticesBase.Count; s++)
            {
                if (s == 0)
                {
                    index = s;
                    distance = Vector2.Distance(verticesBase[s], checkJsonVec);
                }
                else
                {
                    var distanceSub = Vector2.Distance(verticesBase[s], checkJsonVec);
                    if (distance >= distanceSub)
                    {
                        index = s;
                        distance = distanceSub;
                    }
                }
                //Debug.Log($"BaseVecNo:{i:0} Vertices:({verticesBase[i].x:0.0000}, {verticesBase[i].y:0.0000}, {verticesBase[i].z:0.0000})");
            }

            _uvs.Add(baseUv[index]);
            //Debug.Log(
            //    "(" + checkJsonVec.x + ", " + checkJsonVec.y + ", " + checkJsonVec.z + "} is Near! :("
            //    + verticesBase[index].x + ", " + verticesBase[index].y + ", " + verticesBase[index].z + ") is index:" + index
            //    + " distance :" + distance + "    UV:" + baseUv[index].x + "," + baseUv[index].y);
        }
    }

    private void AnimationMarge(GameObject targetObject, ref AnimationClip _anim)
    {
        List<EditorCurveBinding> alphaBind = new List<EditorCurveBinding>();
        List<AnimationCurve> alphaCurve = new List<AnimationCurve>();

        //ImageのOpacityのアニメーションはここで格納しておけば、Pathの張り直しは後工程がやってくれるはず
        foreach (AllImageLayer AIL in AllImageData)
        {
            if (AIL == null)
                continue;

            //アニメが存在していなかったらスキップ
            if (AIL._floatAnimationData == null)
                continue;

            //Debug.Log("Alpha Animation Create!!" + AIL.Name + "  :" + AIL._floatAnimationData.Length);

            for (var animNo = 0; animNo < AIL._floatAnimationData.Length; animNo++)
            {
                if (AIL._floatAnimationData[animNo] == null)
                    continue;

                //Debug.Log("Animation List count :" + AIL._floatAnimationData[animNo].Frame.Length);

                if (AIL._floatAnimationData[animNo].Frame.Length > 0)
                {
                    GameObject _baseObj;
                    _baseObj = GetAllChilren(targetObject.transform, AIL.Name);
                    //Debug.Log("この名前のObjectを探しているよ！" + AIL.Name + " : " + _baseObj);

                    string _AILpath = GetBindingPath(targetObject, _baseObj.transform);
                    //Debug.Log("Pathを探しているよ！" + AIL.Name + " : " + _AILpath);

                    EditorCurveBinding _AILBinding_R = new EditorCurveBinding();
                    EditorCurveBinding _AILBinding_G = new EditorCurveBinding();
                    EditorCurveBinding _AILBinding_B = new EditorCurveBinding();
                    EditorCurveBinding _AILBinding_A = new EditorCurveBinding();

                    AnimationCurve _AILCurve = new AnimationCurve();
                    Material _AILmat = _baseObj.GetComponent<MeshRenderer>().sharedMaterial;

                    Color _baseColor = _AILmat.color;

                    _AILBinding_R.path = _AILpath;
                    _AILBinding_G.path = _AILpath;
                    _AILBinding_B.path = _AILpath;
                    _AILBinding_A.path = _AILpath;

                    _AILBinding_R.type = typeof(MeshRenderer);
                    _AILBinding_G.type = typeof(MeshRenderer);
                    _AILBinding_B.type = typeof(MeshRenderer);
                    _AILBinding_A.type = typeof(MeshRenderer);

                    _AILBinding_R.propertyName = "material._Color.r";
                    _AILBinding_G.propertyName = "material._Color.g";
                    _AILBinding_B.propertyName = "material._Color.b";
                    _AILBinding_A.propertyName = "material._Color.a";

                    AnimationCurve curveR = new AnimationCurve();
                    AnimationCurve curveG = new AnimationCurve();
                    AnimationCurve curveB = new AnimationCurve();
                    AnimationCurve curveA = new AnimationCurve();

                    for (var _key = 0; _key < AIL._floatAnimationData[animNo].Frame.Length; _key++)
                    {
                        Keyframe keyR = new Keyframe();
                        Keyframe keyG = new Keyframe();
                        Keyframe keyB = new Keyframe();
                        Keyframe keyA = new Keyframe();

                        keyR.time = keyG.time = keyB.time = keyA.time = (float)AIL._floatAnimationData[animNo].Frame[_key] / (float)AIL.FrameRate;
                        float _alpha = (float)AIL._floatAnimationData[animNo].MeshFloat[_key];
                        //Debug.Log((float)AIL._floatAnimationData[animNo].Frame[_key] + "/" + (float)AIL.FrameRate);
                        //Debug.Log("[" + keyR.time + "] KeyValue :" + _baseColor + "/" + _alpha);

                        keyR.value = _baseColor.a;
                        keyG.value = _baseColor.g;
                        keyB.value = _baseColor.b;
                        keyA.value = _alpha;

                        curveR.AddKey(keyR);
                        curveG.AddKey(keyG);
                        curveB.AddKey(keyB);
                        curveA.AddKey(keyA);
                    }

                    alphaBind.Add(_AILBinding_R);
                    alphaBind.Add(_AILBinding_G);
                    alphaBind.Add(_AILBinding_B);
                    alphaBind.Add(_AILBinding_A);
                    alphaCurve.Add(curveR);
                    alphaCurve.Add(curveG);
                    alphaCurve.Add(curveB);
                    alphaCurve.Add(curveA);                  
                }
            }
        }

        //_animファイルからCurveを取り出し
        EditorCurveBinding[] _binding = AnimationUtility.GetCurveBindings(_anim);
        //AnimationもCurve抜き出し
        AnimationCurve[] _curve = new AnimationCurve[_binding.Length];
        for (var i = 0; i < _binding.Length; i++)
        {
            _curve[i] = AnimationUtility.GetEditorCurve(_anim, _binding[i]);
        }

        //SmartWarpObjectからアニメーションカーブを生成し保持
        List<AllObjectAnimation> AOA = new List<AllObjectAnimation>();

        foreach (AllObjectMesh _mesh in AllObjectData)
        {
            //Nullだったら以降スキップ
            if (_mesh == null)
                break;

            //その他ベクターで引っかかるイメージレイヤー
            if (_mesh.Name == "NULL")
                continue;

            //SmartWarpObjectじゃなく、おそらくベクター画像レイヤー
            if (_mesh.SmartWarpGameObject == null)
                continue;

            AllObjectAnimation _tempAoa = new AllObjectAnimation();

            _tempAoa.ObjectName = _mesh.Name;
            _tempAoa.BrendShapeData = new EditorCurveBinding[_mesh._meshPointData.Length];
            _tempAoa.curveShapeData = new AnimationCurve[_mesh._meshPointData.Length];

            float[] FrameData = new float[_mesh._meshPointData.Length];

            for (var s = 0; s < _mesh._meshPointData.Length; s++)
            {
                EditorCurveBinding _temp = new EditorCurveBinding();

                //FrameRateを求める
                FrameData[s] = (float)_mesh._meshPointData[s].Frame;

                //これでAllObjectData[l].SmartWarpGameObject の今回のアニメーションは
                //Framesの時間にｌ番目のBrendShapeをONにすることと決まる
                //あとはこの情報をAnimFileに書き込んでいくだけ

                //Path > AllObjectData[l].Name

                //Debug.Log(_mesh.Name);
                //Debug.Log(_mesh.SmartWarpGameObject.gameObject.name);
                _temp.path = _mesh.SmartWarpGameObject.gameObject.name;
                _temp.type = typeof(SkinnedMeshRenderer);
                _temp.propertyName = "blendShape." + BLEND_SHAPE_NAME + s.ToString();

                _tempAoa.BrendShapeData[s] = _temp;
                _tempAoa.curveShapeData[s] = new AnimationCurve();
            }

            for (var j = 0; j < _mesh._meshPointData.Length; j++)
            {
                //FrameRateを求める
                //var Frames = (float)AllObjectData[l]._meshPointData[j].Frame / AllObjectData[l].FrameRate;
                var isFrame = (float)_mesh._meshPointData[j].Frame;


                for (var k = 0; k < _mesh._meshPointData.Length; k++)
                {
                    Keyframe keyData = new Keyframe();


                    if (isFrame >= FrameData[k])
                    {
                        //Debug.Log(j + " : " + isFrame + " <> " + FrameData[k] + " is 100%");
                        keyData.time = isFrame / _mesh.FrameRate; ;
                        keyData.value = 100f;
                    }
                    else
                    {
                        //Debug.Log(j + " : " + isFrame + " <> " + FrameData[k] + "is No!");
                        keyData.time = isFrame / _mesh.FrameRate; ;
                        keyData.value = 0f;
                    }

                    _tempAoa.curveShapeData[k].AddKey(keyData);
                }
            }

            AOA.Add(_tempAoa);
        }

        //Debug.Log(_anim.name + " in Curve List ------------------");

        /*
        //既存のAnimationファイルの中のバインディングのPath一覧
        foreach (EditorCurveBinding ecb in _binding)
        {
            //Debug.Log("Extended Obj :" + ecb.path);
        }

        //今回作成したObjectのバインディング用Path一覧
        foreach (string _path in BuildedMeshPathList)
        {
            //Debug.Log("Created Obj :" + _path);
        }
        //AnimationUtility.GetAnimatableBindings()
        */

        //Bindingの張り直し
        int countBind = 0;
        foreach (EditorCurveBinding ecb in _binding)
        {
            //ecb.path とおなじpathを持つBuildedObjectTargetPathを探す
            int countIndex = 0;
            foreach (string Extended in BuildedObjectTargetPath)
            {
                //Debug.Log(ecb.path + " <> " + Extended);
                if (ecb.path == Extended)
                {
                    //Debug.Log("<<MATCH!!>>");
                    //countIndexのBindingを入れ替える
                    //ここはPathの文字列さえがあってればいいので見つけたらどんどん変えてしまう

                    //TODO:ここでMeshをSkinnedMeshに変えられるものは変えてしまいたい。
                    _binding[countBind].path = BuildedMeshPathList[countIndex];

                    if(_binding[countBind].type == typeof(MeshRenderer))
                    {
                        _binding[countBind].type = typeof(SkinnedMeshRenderer);
                    }

                    countIndex++;
                }
                else
                {
                    countIndex++;
                }
            }
            countBind++;
        }


        List<EditorCurveBinding> VisivleBind = new List<EditorCurveBinding>();
        List<AnimationCurve> VisivleCurve = new List<AnimationCurve>();

        //VisibleAnimationがあるのか確認します
        for (var i = 0; i < _binding.Length; i++)
        {
            /*
            if(_binding[i].path.Contains("Moho Camera"))
            {
                for(var l = 0; l < _curve[i].keys.Length; l++)
                {
                    Debug.Log(_binding[i].propertyName + " : " + _binding[i].type + " : " + _curve[i].keys[l].value);
                }
            }
            */

            if (_binding[i].propertyName.Contains("m_LocalScale.x"))
            {
                bool _IsOne = false;//１があるかフラグ
                bool _IsLow = false;//1e-06があるかフラグ
                for (var l = 0; l < _curve[i].keys.Length; l++)
                {
                    float temp = _curve[i].keys[l].value;
                    if (temp == 1.0f) _IsOne = true;
                    if (temp == 1e-06f) _IsLow = true;
                }

                if (_IsOne && _IsLow)
                {
                    //index iはVisibleアニメ確定
                    //Debug.Log(i + "はVisibleアニメだね" + _binding[i].path);

                    EditorCurveBinding tempBind = _binding[i];
                    tempBind.propertyName = "m_IsActive";
                    tempBind.type = typeof(GameObject);
                    AnimationCurve tempCurve = _curve[i];
                    for (var s = 0; s < tempCurve.keys.Length; s++)
                    {
                        Keyframe tempKey = tempCurve.keys[s];
                        if (tempKey.value == 1E-06f)
                        {
                            tempKey.value = 0f;
                        }
                        tempCurve.keys[s] = tempKey;
                    }

                    VisivleBind.Add(tempBind);
                    VisivleCurve.Add(tempCurve);
                }
            }
        }

        _anim.ClearCurves();
        //既存のデータを改造
        for (var i = 0; i < _binding.Length; i++)
        {
            AnimationUtility.SetEditorCurve(_anim, _binding[i], _curve[i]);
        }

        //既存のデータから、Visibleのアニメ追加
        for (var i = 0; i < VisivleBind.Count; i++)
        {
            AnimationUtility.SetEditorCurve(_anim, VisivleBind[i], VisivleCurve[i]);
        }

        
        //アルファアニメを追加します
        for(var i = 0; i < alphaBind.Count; i++)
        {
            AnimationUtility.SetEditorCurve(_anim, alphaBind[i], alphaCurve[i]);
        }
        

        
        //既存のデータに新規追加
        for (var i = 0; i < BuildedMeshList.Count; i++)
        {
            string MeshName = BuildedMeshList[i].name;
            string ObjectIndex = BuildedMeshIndexObject[i];
            for (var j = 0; j < AOA.Count; j++)
            {
                //Debug.Log(MeshName + " : " + ObjectIndex + " <> " + " AOA[" + j + "] : " + AOA[j].ObjectName);
                //二つがイコールなら、Bind情報を入れ替えて登録する
                if (ObjectIndex == AOA[j].ObjectName)
                {
                    EditorCurveBinding[] tempBind = AOA[j].BrendShapeData;
                    AnimationCurve[] tempCurve = AOA[j].curveShapeData;

                    for (var l = 0; l < tempBind.Length; l++)
                    {
                        EditorCurveBinding _bind = tempBind[l];
                        _bind.path = GetBindingPath(targetObject, BuildedMeshList[i]);


                        //Debug.Log("Bind Information >>>  " + _bind.path + " / " + _bind.propertyName + " / " + _bind.type);
                        //Debug.Log("Bind Information >>>  " + tempCurve[l].length);
                        AnimationUtility.SetEditorCurve(_anim, _bind, tempCurve[l]);
                    }
                }
            }
        }

        //Dataの保存
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

    }

    private void ZeroFrameDelete(ref AnimationClip _anim)
    {
        //Debug.Log("アニメのフレームレート　　>>" + _anim.frameRate);
        //_animファイルからCurveを取り出し
        EditorCurveBinding[] _binding = AnimationUtility.GetCurveBindings(_anim);
        //AnimationもCurve抜き出し
        AnimationCurve[] _curve = new AnimationCurve[_binding.Length];
        for (var i = 0; i < _binding.Length; i++)
        {
            _curve[i] = AnimationUtility.GetEditorCurve(_anim, _binding[i]);
        }

        //_animの中身をまっさらに
        _anim.ClearCurves();

        //ここで0フレーム目を削る
        for (var i = 0; i < _curve.Length; i++)
        {
            Keyframe[] _keys = _curve[i].keys;
            List<Keyframe> _cpKeys = new List<Keyframe>();
            //Keyframe _tmpKey = new Keyframe();
            
            //編集しやすいように、一旦Listにコピー
            for (var l = 0; l < _keys.Length; l++)
            {
                _cpKeys.Add(_keys[l]);
            }

            //BlendShapeデータは削除しない
            if (_binding[i].path.Contains("blendShape.") == false)
            {
                if (_cpKeys.Count > 2f)
                {
                    //Time0に数値のはいっていないカーブは何も処理しない
                    if (_cpKeys[0].time == 0f)
                    {
                        //最初のフレームがTime0のカーブ

                        if (_cpKeys.Count == 1)
                        {
                            //フレームが一個しかない場合
                            //0フレームを1フレームに変更して記録する
                            //_tmpKey = _cpKeys[0];
                            //_tmpKey.time = 1f / _anim.frameRate;

                            //_cpKeys[0] = _tmpKey;//入れ替え
                            _cpKeys.RemoveAt(0);

                        }
                        else
                        {
                            //フレームが二個以上ある場合
                            if (_keys[1].time == 1f)
                            {
                                //1フレーム目にKeyがあるので、そっちを優先して0を削除
                                _cpKeys.RemoveAt(0);
                            }
                            else
                            {
                                //1フレーム目にKeyが無いので、0フレームを1フレームに
                                //_tmpKey = _cpKeys[0];
                                //_tmpKey.time = 1f / _anim.frameRate;
                                //_cpKeys[0] = _tmpKey;
                                _cpKeys.RemoveAt(0);
                            }
                        }
                    }
                }
            }

            //ListをArrayに変換
            Keyframe[] _cpTemp = new Keyframe[_cpKeys.Count];
            for(var l = 0; l < _cpTemp.Length; l++)
            {
                Keyframe _temp = _cpKeys[l];
                //TODO:何か一括処理いれるならここでいれられる

                _cpTemp[l] = _temp;
            }

            _curve[i].keys = _cpTemp;
        }

        //AnimationCurveを書き戻す
        for (var i = 0; i < _binding.Length; i++)
        {
            AnimationUtility.SetEditorCurve(_anim, _binding[i], _curve[i]);
        }

        //Dataの保存
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    //AnimationControllerにAnimationClipを登録
    private void SetAnimationController(string projectName, GameObject targetObject, string path, ref AnimationClip _anim)
    {
        string controllerPath = path + "/" + projectName + "_Controller.controller";
        var _cont = AssetDatabase.LoadAssetAtPath(controllerPath, typeof(AnimatorController)) as AnimatorController;

        var makeStateMachine = _cont.layers[0].stateMachine;
        var animState = makeStateMachine.AddState("DefaultMotion");
        animState.motion = _anim;
    }

    //_objのAnimationCurve用のBindingPathを作成します
    private string GetBindingPath(GameObject targetObject, Transform _obj)
    {
        var path = _obj.name;
        bool _isEndPath = false;

        Transform targetObj = _obj.parent;

        do
        {
            var _parent = targetObj;
            if (_parent != null && _parent != targetObject.transform)
            {
                path = _parent.name + "/" + path;
                targetObj = targetObj.parent;
            }
            else
            {
                _isEndPath = true;
            }
        } while (!_isEndPath);

        return path;
    }

    //MiniJson のlongとdoubleの復号回避
    private float ObjectToFloat(object _value)
    {
        try
        {
            return (float)(double)_value;
        }
        catch
        {
            return (float)(long)_value;
        }
    }

    //階層の中から_NameのObjectを探します
    private GameObject GetAllChilren(Transform target, string _Name)
    {
        var children = target.gameObject.GetComponentsInChildren<Transform>(true);
        foreach(Transform _child in children)
        {
            if(_child.name == _Name)
            {
                return _child.gameObject;
            }
        }

        return null;
    }


    private void CameraZinMeshVertexPosition(GameObject targetFBX, GameObject targetObject, ref AnimationClip _anim)
    {
        var children = targetObject.transform.GetComponentsInChildren<Transform>(true);
        var vertices = new List<Vector3>();

        ////_animファイルからCurveを取り出し
        EditorCurveBinding[] _binding = AnimationUtility.GetCurveBindings(_anim);
        //AnimationもCurve抜き出し
        AnimationCurve[] _curve = new AnimationCurve[_binding.Length];
        for (var i = 0; i < _binding.Length; i++)
        {
            _curve[i] = AnimationUtility.GetEditorCurve(_anim, _binding[i]);
        }

        //_animの中身をまっさらに
        _anim.ClearCurves();

        foreach (Transform Obj in children)
        {
            //見えないObjectは処理しない
            if (!Obj.gameObject.activeSelf)
            {
                continue;
            }

            var MR = Obj.GetComponent<MeshRenderer>();
            var SMR = Obj.GetComponent<SkinnedMeshRenderer>();
            float Offset;
            Material tempMat = null;
            GameObject CurrentObject = null;

            //Offsetの所得
            if (MR != null)
            {
                //MeshRendererなので自分自身のアニメをいじる
                tempMat = MR.sharedMaterial;
                CurrentObject = Obj.gameObject;
                Offset = tempMat.GetFloat("_OffsetFactor") * -0.01f;

                Vector3 tempVec = CurrentObject.transform.position;
                tempVec.z += Offset;
                CurrentObject.transform.position = tempVec;
            }
            else if (SMR != null)
            {
                //SkinnedMeshRendererなので自分自身のアニメをいじる
                tempMat = SMR.sharedMaterial;
                CurrentObject = Obj.gameObject;
                Offset = tempMat.GetFloat("_OffsetFactor") * -0.01f;

                Mesh _mesh = SMR.sharedMesh;
                Transform[] _bones = SMR.bones;
                Matrix4x4[] _bindPose = _mesh.bindposes;

                Vector3 tempVec = CurrentObject.transform.position;
                tempVec.z += Offset;
                CurrentObject.transform.position = tempVec;

                for (var i = 0; i < _bindPose.Length; i++)
                {
                    _bindPose[i] = _bones[i].transform.worldToLocalMatrix * SMR.localToWorldMatrix;
                }
                _mesh.bindposes = _bindPose;
                SMR.sharedMesh = _mesh;
            }

            //処理開始
            if (tempMat != null && CurrentObject != null)
            {
                Offset = CurrentObject.transform.position.z;
                string CurrentPath = GetBindingPath(targetObject, CurrentObject.transform);

                for (var i = 0; i < _binding.Length; i++)
                {
                    if (_binding[i].type == typeof(Transform) && _binding[i].propertyName == "m_LocalPosition.z")
                    {
                        //Debug.Log("修正します　:" + _binding[i].path + "  <>   " + CurrentPath);

                        //Pathが見つかった
                        if (_binding[i].path == CurrentPath)
                        {
                            //PositionのCurveを取り出して改造します
                            Keyframe[] _keys = _curve[i].keys;

                            for (var cIndex = 0; cIndex < _keys.Length; cIndex++)
                            {
                                Keyframe kFrame = _keys[cIndex];
                                kFrame.value = Offset;//kFrame.value + Offset;
                                _keys[cIndex] = kFrame;
                            }

                            _curve[i].keys = _keys;
                        }
                    }
                }

            }
        }

        //AnimationCurveを書き戻す
        for (var i = 0; i < _binding.Length; i++)
        {
            AnimationUtility.SetEditorCurve(_anim, _binding[i], _curve[i]);
        }

        //Dataの保存
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void DeleteHiddenObject(string _path)
    {
        var instance = PrefabUtility.LoadPrefabContents(_path);
        var count = instance.transform.childCount;

        var data = instance.transform.GetComponentsInChildren<Transform>(true);

        for (var i = 0; i < data.Length; i++)
        {
            var Obj = data[i];
            //見えないObjectは処理しない
            if (!Obj.gameObject.activeSelf)
            {
                //Debug.Log(">>>>>   " + Obj.name + " is Delete!!");
                GameObject.DestroyImmediate(Obj.gameObject);
            }
        }
        PrefabUtility.SaveAsPrefabAsset(instance, _path);
        PrefabUtility.UnloadPrefabContents(instance);
    }

}
