//#if UNITY_EDITOR
//using UnityEditor;
//using UnityEditor.SceneManagement;

//[InitializeOnLoad]
//public class PlayFromInitScene
//{
//    static PlayFromInitScene()
//    {
//        EditorApplication.playModeStateChanged += OnPlayModeChanged;
//    }

//    private static void OnPlayModeChanged(PlayModeStateChange state)
//    {
//        if (state == PlayModeStateChange.ExitingEditMode)
//        {
//            if (EditorBuildSettings.scenes.Length > 0)
//            {
//                EditorSceneManager.playModeStartScene =
//                    AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorBuildSettings.scenes[0].path);
//            }
//        }
//    }
//}
//#endif



#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class ResetPlayModeScene
{
    static ResetPlayModeScene()
    {
        // 강제로 시작 씬 설정을 해제하여 현재 열린 씬에서 시작하도록 만듭니다.
        EditorSceneManager.playModeStartScene = null;
        UnityEngine.Debug.Log("시작 씬 고정이 해제되었습니다. 이제 현재 씬에서 실행됩니다.");
    }
}
#endif