using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class MohoStrokeConvert : EditorWindow
{
    [SerializeField]
    public Texture2D targetObj;


    string pathBase;
    string[] imageFilePaths;

    private Vector2 leftScrollPos = Vector2.zero;

    [MenuItem("MohoEditor/MohoStrokeConvert")]
    private static void Create()
    {
        GetWindow<MohoStrokeConvert>("MohoStrokeConvert");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("変換するImageを一つ選択してください");
        targetObj = EditorGUILayout.ObjectField("Image", targetObj, typeof(Texture2D), true) as Texture2D;

        if (GUILayout.Button("Check!"))
        {
            GetDirectryImage();
        }


        if (imageFilePaths != null)
        {
            leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);
            //リストを表示
            foreach (string path in imageFilePaths)
            {
                EditorGUILayout.LabelField(path);
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Convert"))
            {
                CreateAnimationImage();
            }
        }
    }

    private void GetDirectryImage()
    {
        if (targetObj == null)
            return;

        //        targetObj.GetInstanceID

        pathBase = AssetDatabase.GetAssetPath(targetObj.GetInstanceID());
        pathBase = System.IO.Path.GetDirectoryName(pathBase);

        imageFilePaths = Directory.GetFiles(pathBase, "*.png", SearchOption.AllDirectories);
    }

    /// <summary>
    /// https://qiita.com/r-ngtm/items/6cff25643a1a6ba82a6c
    /// </summary>
    private void CreateAnimationImage()
    {
        Texture2D tempTex = null;


        foreach (string path in imageFilePaths)
        {
            var Image1 = tempTex;
            var Image2 = ReadPng(path);

            if (Image1 != null)
            {
                for (var x = 0; x < Image2.width; x++)
                {
                    for (var y = 0; y < Image2.height; y++)
                    {
                        var col1 = Image1.GetPixel(x, y);
                        var col2 = Image2.GetPixel(x, y);
                        float value = (col2.a / imageFilePaths.Length) + col1.r;
                        Color colFix;
                        colFix.r = value;
                        colFix.g = value;
                        colFix.b = value;
                        colFix.a = 1.0f;

                        Image2.SetPixel(x, y, colFix);
                    }
                }
                tempTex = Image2;

            }
            else
            {
                for (var x = 0; x < Image2.width; x++)
                {
                    for (var y = 0; y < Image2.height; y++)
                    {
                        var col2 = Image2.GetPixel(x, y);
                        float value = col2.a / imageFilePaths.Length;
                        Color colFix;
                        colFix.r = value;
                        colFix.g = value;
                        colFix.b = value;
                        colFix.a = 1.0f;
                        Image2.SetPixel(x, y, colFix);
                    }
                }

                tempTex = Image2;
            }
        }

        //png形式で保存
        //http://ft-lab.ne.jp/cgi-bin-unity/wiki.cgi?page=unity%5Fscript%5Ftexture2d%5Fsave%5Fpng%5Ffile
        if (tempTex != null)
        {
            string fileName = pathBase + "/Marge.png";

            byte[] pngData = tempTex.EncodeToPNG();
            File.WriteAllBytes(fileName, pngData);
            Object.DestroyImmediate(tempTex);
            tempTex = AssetDatabase.LoadAssetAtPath(fileName, typeof(Texture2D)) as Texture2D;
        }
    }

    byte[] ReadPngFile(string path)
    {
        FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
        BinaryReader bin = new BinaryReader(fileStream);
        byte[] values = bin.ReadBytes((int)bin.BaseStream.Length);

        bin.Close();

        return values;
    }

    public Texture2D ReadPng(string path)
    {
        byte[] readBinary = ReadPngFile(path);

        int pos = 16; // 16バイトから開始

        int width = 0;
        for (int i = 0; i < 4; i++)
        {
            width = width * 256 + readBinary[pos++];
        }

        int height = 0;
        for (int i = 0; i < 4; i++)
        {
            height = height * 256 + readBinary[pos++];
        }

        Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        texture.LoadImage(readBinary);

        return texture;
    }

}
