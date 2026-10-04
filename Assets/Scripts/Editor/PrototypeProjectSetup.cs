using HayChoriYPaty;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HayChoriYPaty.Editor
{
    /// <summary>One-time setup helper for the playable scene and portrait target. Safe to rerun.</summary>
    public static class PrototypeProjectSetup
    {
        [MenuItem("Hay Chori y Paty/Regenerate Main Prototype Scene")]
        public static void CreatePrototypeScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            GameObject cameraObject = GameObject.Find("Main Camera");
            Camera camera = cameraObject != null ? cameraObject.GetComponent<Camera>() : Camera.main;
            if (camera != null)
            {
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.backgroundColor = new Color(0.93f, 0.78f, 0.55f);
            }

            GameObject controller = new GameObject("Floresta Round Controller");
            controller.AddComponent<GameController>();
            controller.AddComponent<PrototypeView>();

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true) };
            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1920;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.productName = "Hay Chori y Paty";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.haychoriypaty.game");

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("Hay Chori y Paty: saved Assets/Scenes/Main.unity and configured portrait 1080×1920.");
        }
    }
}
