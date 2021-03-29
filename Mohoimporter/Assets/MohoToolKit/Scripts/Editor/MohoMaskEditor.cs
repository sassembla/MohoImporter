using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MohoMaskBase))]
public class MohoMaskEditor : Editor
{
    private static readonly string[] DisplayedMaskChannel = {"CHANNEL1","CHANNEL2","CHANNEL3"};
    private static readonly int[] MaskValue = { 16, 32, 48 };

    public override void OnInspectorGUI()
    {
        MohoMaskBase MMB = target as MohoMaskBase;
        bool allowSceneObjects = !EditorUtility.IsPersistent(target);

        MMB.StencilID = EditorGUILayout.IntPopup("StencilID", MMB.StencilID, DisplayedMaskChannel, MaskValue);
        if(MMB.MaskShader == null)
            MMB.MaskShader = Shader.Find("Moho/MaskShader");

        MMB.MaskShader = EditorGUILayout.ObjectField("Mask Shader",MMB.MaskShader,typeof(Shader), allowSceneObjects) as Shader;
        MMB.mohoBase = EditorGUILayout.ObjectField("Mask Base", MMB.mohoBase, typeof(Transform), allowSceneObjects) as Transform;

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Maskの影響に入るObject");

        if (MMB.mohoCildList == null | MMB.mohoChildPriority == null)
        {
            MMB.mohoCildList = new List<Transform>();
            MMB.mohoChildPriority = new List<int>();
        }
        else if (MMB.mohoCildList.Count != MMB.mohoChildPriority.Count)
        {
            MMB.mohoCildList = new List<Transform>();
            MMB.mohoChildPriority = new List<int>();
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("MaskChild Object");
        EditorGUILayout.LabelField("Priority");
        EditorGUILayout.EndHorizontal();

        for (var i = 0; i < MMB.mohoCildList.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            MMB.mohoCildList[i] = EditorGUILayout.ObjectField(MMB.mohoCildList[i] , typeof(Transform), allowSceneObjects) as Transform;
            if (MMB.mohoCildList[i] == null)
            {
                EditorGUILayout.LabelField("");
            }
            else
            {
                MMB.mohoChildPriority[i] = EditorGUILayout.IntField(MMB.mohoChildPriority[i]);
            }
            EditorGUILayout.EndHorizontal();
        }

        //Listのボタン
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("");
        if (GUILayout.Button("+"))
        {
            MMB.mohoCildList.Add(null);
            MMB.mohoChildPriority.Add(0);
        }

        EditorGUI.BeginDisabledGroup((MMB.mohoCildList.Count<1));
        if (GUILayout.Button("-"))
            {
                MMB.mohoCildList.RemoveAt(MMB.mohoCildList.Count - 1);
                MMB.mohoChildPriority.RemoveAt(MMB.mohoChildPriority.Count - 1);
            }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        if (GUILayout.Button("Setup Mask"))
        {
            MMB.SetUPData();
        }

        //base.OnInspectorGUI();
    }
}
