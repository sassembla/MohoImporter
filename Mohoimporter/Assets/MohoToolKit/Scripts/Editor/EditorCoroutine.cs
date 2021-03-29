using System.Collections;
using UnityEditor;

public class EditorCoroutine
{
    // Editorで使えるCoroutineのStart関数
    public static void StartEditorCoroutine(IEnumerator cor)
    {
        EditorApplication.CallbackFunction coroutineAct = null;
        coroutineAct = () =>
        {
            if (!cor.MoveNext())
            {
                EditorApplication.update -= coroutineAct;// 取り除く
            }
        };

        EditorApplication.update += coroutineAct;
    }
}