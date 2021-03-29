using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using MiniJSON;
using ImporterUtilty;

public class MohoColorAnimMarge : EditorWindow
{
    [MenuItem("MohoEditor/MohoColorAnimMarge")]
    private static void Create()
    {
        GetWindow<MohoColorAnimMarge>("MohoColorAnimMarge");
    }

    public Object jsonFile;
    public string filePath;
    public string jsonData;
    public Object animFile;
    public string animfilePath;
    public AnimationClip animData;
    public GameObject targetObject;

    private void OnGUI()
    {
        targetObject = EditorGUILayout.ObjectField("Set targetObject", targetObject, typeof(GameObject), true) as GameObject;
        jsonFile = EditorGUILayout.ObjectField("Set jsonFile", jsonFile, typeof(Object), true) as Object;
        animFile = EditorGUILayout.ObjectField("Set AnimationFile", animFile, typeof(Object), true) as Object;

        //if (GUILayout.Button("FileLoad"))
        //{
        //    filePath = AssetDatabase.GetAssetPath(jsonFile);
        //    jsonData = File.ReadAllText(filePath);
        //    var json = (IDictionary)Json.Deserialize(jsonData);
        //    ReadAnimationJson(ref json);
        //}

        if (GUILayout.Button("Animation Set"))
        {
            filePath = AssetDatabase.GetAssetPath(jsonFile);
            jsonData = File.ReadAllText(filePath);
            animfilePath = AssetDatabase.GetAssetPath(animFile);
            animData = AssetDatabase.LoadAssetAtPath(animfilePath,typeof(AnimationClip)) as AnimationClip;
            var json = (IDictionary)Json.Deserialize(jsonData);
            AnimationMarge(ref json);
        }
    }

    private void ReadAnimationJson(ref IDictionary json)
    {
        var animationList = (IList)json["AnimationList"];
        for(var i=0; i < animationList.Count; i++)
        {
            //Channel情報の復号
            var animationData = (IDictionary)animationList[i];
            var objectName = (string)animationData["ObjectName"];
            var keyLength = (long)animationData["KeyLength"];
            var animationKey = (IList)animationData["AnimationKey"];
            Debug.Log(i + " Object:" + objectName + " Keylength:" + keyLength);

            //keyframe情報の復号
            for(var j=0;j < animationKey.Count; j++)
            {
                var channel = (IDictionary)animationKey[j];
                var frame = (long)channel["Frame"];
                var value = (IDictionary)channel["Value"];
                Color valColor = Color.black;
                valColor.r = ObjectToFloat(value["r"]);
                valColor.g = ObjectToFloat(value["g"]);
                valColor.b = ObjectToFloat(value["b"]);
                valColor.a = ObjectToFloat(value["a"]);
                Debug.Log("    Frame:" + frame + " Color:" + valColor);
            }
        }
    }

    private void AnimationMarge(ref IDictionary json)
    {
        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(animData);
        List<string> pickUpPath = new List<string>();

        foreach (EditorCurveBinding bind in bindings)
        {
            //MohoのExport側で対応しました
            //出力後のデータは|数字がルートについているので削除しておきます。
            var tempPath = bind.path;
            //var count = tempPath.IndexOf("|");
            //if (count != -1)
            //{
            //    tempPath = tempPath.Substring(0, count);
            //}

            //bind.type がMeshRendererのものを対象にする場合は上書き
            //Pathがマッチした場合は、そのpathを対象にMeshRendererのTypeのClipを作成します。
            //マッチしなかったときは、新規でpathを組んでClipを作成します。
            //この場合正しいPathの所得がObjectを検索しないとできないので、要検討
            //いったんこのパターンは置いておいておく
            //JsonのChannelName Line Colorは今回無視します。　 Lineは本体と統合されてテクスチャになってしまうので

            //Debug.Log("BindingData   :" + bind.type + "  :" + bind.propertyName);

            //同じObjectを示すClipがないか、jsonデータとの比較
            var animationList = (IList)json["AnimationList"];
            for (var i = 0; i < animationList.Count; i++)
            {
                //Channel情報の復号
                var animationData = (IDictionary)animationList[i];
                var objectName = (string)animationData["ObjectName"];

                if (objectName == tempPath)
                {
                    //Debug.Log("Match!! >>" + tempPath + "  Propertyname:" + bind.type);
                    UniquePath(ref pickUpPath,tempPath);
                }
            }
        }

        //Debug表示
        /*
        Debug.Log("-------PickUp------");
        foreach (string data in pickUpPath)
        {
            Debug.Log(data);
        }
        */

        //Animationにjsonのkeyを埋め込んでいきます
        CreateCurve(ref pickUpPath, ref json);
    }

    void UniquePath(ref List<string> pickup,string path)
    {
        foreach(string data in pickup)
        {
            if(data == path)
            {
                return;
            }
        }

        pickup.Add(path);
    }

    void CreateCurve(ref List<string> pickUpPath, ref IDictionary json)
    {
        //targetObjectの中からpathのObjectを探す
        foreach(string pick in pickUpPath)
        {
            var target = targetObject.transform.Find(pick);
            System.Type type;

            //Materialを決める
            Material _mat;
            if (target != null)
            {
                //Objectが見つかった
                _mat = target.GetComponent<MeshRenderer>().sharedMaterial;
                type = typeof(MeshRenderer);
                if(_mat == null)
                {
                    _mat = target.GetComponent<SkinnedMeshRenderer>().sharedMaterial;
                    type = typeof(SkinnedMeshRenderer);
                }

            } else
            {
                //Objectが見つからなかった
                //Default設定
                _mat = new Material(Shader.Find("Legacy Shader/Diffuse"));//後でちゃんとしたShaderに直します
                type = typeof(MeshRenderer);
            }

            //Materialが決まったので、AnimationCurveをセットしていきます
            var animationList = (IList)json["AnimationList"];
            var rate = (float)(long)json["FrameRate"];
            for (var i = 0; i < animationList.Count; i++)
            {
                //Channel情報の復号
                var animationData = (IDictionary)animationList[i];
                var objectName = (string)animationData["ObjectName"];

                //二度手間だけどもう一回検索
                if (objectName == pick)
                {
                    var animationKey = (IList)animationData["AnimationKey"];
                    //Set animData in Binding
                    CreateColorBinding(ref animData, pick, type, ref animationKey,rate);
                }
            }
        }
    }

    void CreateColorBinding(ref AnimationClip animClip,string _path,System.Type _type, ref IList keyData,float rate)
    {
        EditorCurveBinding curveBindingR = new EditorCurveBinding();
        EditorCurveBinding curveBindingG = new EditorCurveBinding();
        EditorCurveBinding curveBindingB = new EditorCurveBinding();
        EditorCurveBinding curveBindingA = new EditorCurveBinding();
        curveBindingR.path = _path;
        curveBindingR.type = _type;
        curveBindingR.propertyName = "material._Color.r";

        curveBindingG.path = _path;
        curveBindingG.type = _type;
        curveBindingG.propertyName = "material._Color.g";

        curveBindingB.path = _path;
        curveBindingB.type = _type;
        curveBindingB.propertyName = "material._Color.b";

        curveBindingA.path = _path;
        curveBindingA.type = _type;
        curveBindingA.propertyName = "material._Color.a";

        AnimationCurve curveR = new AnimationCurve();
        AnimationCurve curveG = new AnimationCurve();
        AnimationCurve curveB = new AnimationCurve();
        AnimationCurve curveA = new AnimationCurve();

        //keyframe情報の復号
        for (var j = 0; j < keyData.Count; j++)
        {
            Keyframe keyR = new Keyframe();
            Keyframe keyG = new Keyframe();
            Keyframe keyB = new Keyframe();
            Keyframe keyA = new Keyframe();

            var channel = (IDictionary)keyData[j];

            keyR.time = keyG.time = keyB.time = keyA.time = (float)(long)channel["Frame"] / rate;

            var value = (IDictionary)channel["Value"];
            keyR.value = ObjectToFloat(value["r"]);
            keyG.value = ObjectToFloat(value["g"]);
            keyB.value = ObjectToFloat(value["b"]);
            keyA.value = ObjectToFloat(value["a"]);

            curveR.AddKey(keyR);
            curveG.AddKey(keyG);
            curveB.AddKey(keyB);
            curveA.AddKey(keyA);
        }

        AnimationUtility.SetEditorCurve(animClip, curveBindingR, curveR);
        AnimationUtility.SetEditorCurve(animClip, curveBindingG, curveG);
        AnimationUtility.SetEditorCurve(animClip, curveBindingB, curveB);
        AnimationUtility.SetEditorCurve(animClip, curveBindingA, curveA);

        //Dataの保存
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
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
}
