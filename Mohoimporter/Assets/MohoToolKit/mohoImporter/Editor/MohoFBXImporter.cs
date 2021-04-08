using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.Reflection;

public class MohoFBXImporter : AssetPostprocessor
{
    private bool isMohoModel = false;
    private string ClipPath;

    void OnPreprocessModel()
    {
        isMohoModel = false;
        ClipPath = "";
        // resampleRotations only became part of Unity as of version 5.3.
        // If you're using an older version of Unity, comment out the following block of code.
        // Set resampleRotations to false to fix the "bouncy" handling of constant interpolation keyframes.
        try
        {
            var importer = assetImporter as ModelImporter;

            //AnimationCurveのreSampleはしないです　壊れるから
            importer.resampleCurves = false;

            //↓これでMaterialとTextureを外に生成します
            importer.materialLocation = ModelImporterMaterialLocation.External;

        }
        catch
        {
        }
    }

    void OnPostprocessGameObjectWithUserProperties(GameObject g, string[] names, System.Object[] values)
    {
        // Only operate on FBX files
        if (assetPath.IndexOf(".fbx") == -1)
        {
            return;
        }

        for (int i = 0; i < names.Length; i++)
        {
            // ファイル名で一致するやつがあったらmoho由来のfbxと見做している
            if (names[i] == "ASP_FBX")
            {
                isMohoModel = true; // at least some part of this comes from Anime Studio
                break;
            }
        }
    }

    // moho fbx import後に通過する処理
    void OnPostprocessModel(GameObject g)
    {
        // Only operate on FBX files
        if (assetPath.IndexOf(".fbx") == -1)
        {
            return;
        }

        if (!isMohoModel)
        {
            //Debug.Log("*** Not Moho ***");
            return;
        }

        //AnimationControllerを作成
        var filePath = System.IO.Path.GetDirectoryName(assetPath);
        filePath = filePath + "/" + g.name + "_Controller.controller";

        AnimatorController animCont = null;
        var asset = AssetDatabase.LoadAssetAtPath(filePath, typeof(AnimatorController)) as AnimatorController;
        if (asset == null)
        {
            animCont = AnimatorController.CreateAnimatorControllerAtPath(filePath);
            AssetDatabase.Refresh();
        }
        else
        {
            animCont = asset;
        }

        //いったん作ったAnimatorControllerのリンクを破棄してAssetDatabaseから読み込みしなおし
        //animCont = null;
        //animCont = AssetDatabase.LoadAssetAtPath(filePath, typeof(AnimatorController)) as AnimatorController;

        //TODO:AnimationClipをSteteにセットしたいがNullになる未調査
        /*{
            Debug.Log("AnimationClip Path>>" + ClipPath);
            AnimationClip tempClip = AssetDatabase.LoadAssetAtPath(ClipPath, typeof(AnimationClip)) as AnimationClip;
            var makeStateMachine = animCont.layers[0].stateMachine;
            Debug.Log(makeStateMachine + "/" + animCont + "/[" + tempClip.name + "]");
            var animState = makeStateMachine.AddState("DefaultMotion");
            animState.motion = tempClip;
        }*/

        //Animatorはつけておきます
        Animator gAnim = g.AddComponent<Animator>();
        gAnim.runtimeAnimatorController = animCont;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Shader shader = Shader.Find("Moho/TransparentShader");
        if (shader == null)
            return;

        Renderer[] renderers = g.GetComponentsInChildren<Renderer>();
        int straightRenderOrder = 0;// shader.renderQueue;
        foreach (Renderer r in renderers)
        {
            int renderOrder = straightRenderOrder;
            if (r.name.Contains("|"))
            {
                string[] stringSeparators = new string[] { "|" };
                string[] parts = r.name.Split(stringSeparators, StringSplitOptions.None);
                int j;
                if (Int32.TryParse(parts[parts.Length - 1], out j))
                    renderOrder += j;
            }
            r.sharedMaterial.shader = shader; // apply an unlit shader
            //r.sharedMaterial.renderQueue = renderOrder; // set a fixed render order
            r.sharedMaterial.SetFloat("_CullMode", (float)UnityEngine.Rendering.CullMode.Off);
            r.sharedMaterial.SetFloat("_ZWrite", 0f);
            r.sharedMaterial.SetFloat("_OffsetFactor", -1f * renderOrder);
            r.sharedMaterial.SetFloat("_OffsetUnits", -1f * renderOrder);
            //straightRenderOrder++;


            Texture tex = r.sharedMaterial.GetTexture("_MainTex");
            if (tex == null)
            {
                Debug.LogError("r.sharedMaterial.GetTexture(_MainTex) が見つからなかった r:" + r);
                continue;
            }
            else
            {
                string texPath = AssetDatabase.GetAssetPath(tex);
                Debug.Log("    >>>>>>  " + texPath);

                var _p = AssetImporter.GetAtPath(texPath);


                TextureImporter _ti = _p as TextureImporter;
                _ti.mipmapEnabled = false;
                _ti.wrapMode = TextureWrapMode.Clamp;

                EditorUtility.SetDirty(_ti);
                //AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                //AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
            }
        }

        AssetDatabase.SaveAssets();
        //AssetDatabase.Refresh();

        //Import時に変なScaleが入っていた時はScale1に戻す
        //必要なら、Animationの方にScaleが入っているからこちらは見えるようにします
        Transform[] transforms = g.GetComponentsInChildren<Transform>();
        foreach (Transform T in transforms)
        {
            if (T.localScale.x < 0.00001f && T.localScale.y < 0.00001f && T.localScale.z < 0.00001f)
            {
                T.localScale = Vector3.one;
            }
        }

        Debug.Log("moho import処理完了待ち開始 assetPath:" + assetPath);// Assets/somewhere/x.fbx

        // 上で作成したいろいろなものがAssetDatabaseにセットされるので、その完了待ちをする。完了時処理とかもしこめる。
        var importedPathSource = assetPath.Split('/');
        var importedPathSourceWithoutFileName = new string[importedPathSource.Length - 1];
        for (var i = 0; i < importedPathSourceWithoutFileName.Length; i++)
        {
            importedPathSourceWithoutFileName[i] = importedPathSource[i];
        }
        var importedPath = string.Join("/", importedPathSourceWithoutFileName);

        // この時点で9ファイルある、これが10ファイルになる
        var fileCount = Directory.GetFiles(importedPath).Length;

        IEnumerator waitCor()
        {
            // assetImport処理が終わるまで待つ
            while (true)
            {
                var filePaths = Directory.GetFiles(importedPath);
                // foreach (var f in filePaths)
                // {
                //     Debug.Log("f:" + f);
                // }

                // TODO: 今のところは1ファイルでも増えてれば完了にしてる
                if (fileCount < filePaths.Length)
                {
                    break;
                }
                yield return null;
            }


            var targetFBX = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;

            var FBXPath = AssetDatabase.GetAssetPath(targetFBX);
            var path = Path.GetDirectoryName(AssetDatabase.GetAssetOrScenePath(targetFBX));
            var projectName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetOrScenePath(targetFBX));

            var animFilePath = path + "/" + projectName + "_anim/Mainline.anim";
            var jsonFilePath = path + "/" + projectName + ".json";

            var animData = AssetDatabase.LoadAssetAtPath(animFilePath, typeof(AnimationClip)) as AnimationClip;

            if (File.Exists(jsonFilePath) || animData != null)
            {
                var jsonData = File.ReadAllText(jsonFilePath);
                MohoSmartWarpImporter.Run(projectName, FBXPath, targetFBX, jsonData, path, animData);
            }
            else
            {
                //animファイルか、Jsonファイルがありません
                Debug.LogError("情報が足りていません、以下を確認ください");
                Debug.LogError("FBXをReimportする");
                Debug.LogError("jsonファイルを作成する");
            }

            Debug.Log("import処理完了 assetPath:" + assetPath);
        };
        EditorCoroutine.StartEditorCoroutine(waitCor());
    }

    void OnPostprocessAnimation(GameObject g, AnimationClip clip)
    {

        // Only operate on FBX files
        if (assetPath.IndexOf(".fbx") == -1)
        {
            return;
        }

        if (!isMohoModel)
        {
            //Debug.Log("*** Not Moho ***");
            return;
        }

        Debug.Log("AnimCreate!!");

        var filePath = System.IO.Path.GetDirectoryName(assetPath);
        string filepath = filePath + "/" + g.name + "_anim/";
        Debug.Log(">>" + filepath);

        if (!Directory.Exists(filepath))
        {
            Debug.Log("ディレクトリ作るよ！ >>" + g.name + "  Path >>" + System.IO.Path.GetDirectoryName(assetPath));
            var _nFolerId = AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(assetPath), g.name + "_anim");

            AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(_nFolerId));
            //Directory.CreateDirectory(filepath);
            AssetDatabase.Refresh();

            ////https://qiita.com/Rijicho_nl/items/e6b693d7b45339de358e
            //Type projectwindowtype = Assembly.Load("UnityEditor").GetType("UnityEditor.ProjectBrowser");
            //EditorWindow projectwindow = EditorWindow.GetWindow(projectwindowtype, false, "Project", false);

            ////AssetのInstanceIDを所得してフォルダーのIDを所得
            //var _obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GetAssetPath(g));
            //EditorGUIUtility.PingObject(_obj);



            //EditorApplication.update = ProjectWindowUtil
            //TODO:問題の本質はここじゃないかもだけど、
            //　　　いったんここで少し待ってディレクトリが見れるようにしたい。
        }

        
        string exportPath = filepath + "/" + clip.name + ".anim";
        string tempPath = filePath + "/temp.anim";

        var copyClip = UnityEngine.Object.Instantiate(clip);

        //AnimationClipをいじるならここ！
        ErrorChechAnimation(ref copyClip);

        AssetDatabase.CreateAsset(copyClip, exportPath);

        //AssetDatabase.CreateAsset(copyClip, tempPath);
        //ClipPath = exportPath;

        //File.Copy(tempPath, exportPath, true);
        //File.Delete(tempPath);　//<<これも動かないのが謎

        AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(exportPath));

        AssetDatabase.Refresh();
    }

    //mohoからExportされたAnimationの変なAnimationCurveを排除します
    void ErrorChechAnimation(ref AnimationClip clip)
    {
        EditorCurveBinding[] curveData = AnimationUtility.GetCurveBindings(clip);
        foreach (EditorCurveBinding curveBind in curveData)
        {
            if ((curveBind.propertyName == "m_Enabled" && curveBind.type == typeof(Renderer)) ||
               (curveBind.propertyName == "material._Color.a" && curveBind.type == typeof(MeshRenderer)))
            {
                //以下二つのキーは削除します
                //RendererをEnableにしているBinding
                //MaterialColorにアルファを入れているBinding
                AnimationUtility.SetEditorCurve(clip, curveBind, null);
            }
            else
            {
                if ((curveBind.propertyName == "m_LocalScale.x" ||
                    curveBind.propertyName == "m_LocalScale.y" ||
                    curveBind.propertyName == "m_LocalScale.z") &&
                    curveBind.type == typeof(Transform))
                {

                    //次はすべてのフレームで1e-06のキーしかないcurveを探します
                    bool isDestroyFlag = true;
                    var curve = AnimationUtility.GetEditorCurve(clip, curveBind);
                    for (int i = 0; i < curve.keys.Length; i++)
                    {
                        var key = curve.keys[i];
                        //Debug.Log(curveBind.path + "/" + curveBind.propertyName + ":[" + i + "]" + key.value);
                        //1e-06とは違う数値が入っていたら削除フラグを解除
                        //※1e-06はMohoのNear0の定義値っぽい
                        if (key.value != 1e-06f)
                            isDestroyFlag = false;
                    }
                    //削除フラグがTrueの場合、このcurveBindは削除します
                    if (isDestroyFlag)
                    {
                        AnimationUtility.SetEditorCurve(clip, curveBind, null);
                    }
                }
            }
        }

    }
}
