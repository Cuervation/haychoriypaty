using System.IO;
using System.Xml;
using UnityEditor.Android;
using HayChoriYPaty.Cloud;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using HayChoriYPaty;

public sealed class FirebaseAndroidBuildGuard : IPreprocessBuildWithReport, IProcessSceneWithReport, IPostGenerateGradleAndroidProject
{
    public int callbackOrder => -1000;
    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android) return;
        ValidateScriptBindings();
        const string file = "Assets/Resources/player-firebase-config.json";
        if (!File.Exists("Assets/google-services.json") || !File.Exists(file))
            throw new BuildFailedException("Real Firebase Android/Google configuration missing. Configure haychoriypaty before shipping the mandatory-login player.");
        var config = JsonUtility.FromJson<PlayerFirebaseConfig>(File.ReadAllText(file));
        if (config == null || config.projectId != FirebasePlayerSession.ProjectId || string.IsNullOrEmpty(config.webClientId) ||
            PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != "com.haychoriypaty.game")
            throw new BuildFailedException("Refusing build with a different Firebase project, Android ID or absent Google OAuth client.");
    }
    // Compilation can succeed with a relocated Bee cache while MonoScript type bindings are empty.
    // Never let that become a "successful" APK with a generic login and no playable scene.
    public static void ValidateScriptBindings()
    {
        string[] paths = {
            "Assets/Scripts/Core/StreetGame.cs", "Assets/Scripts/UI/StreetView.cs",
            "Assets/Scripts/UI/StreetKitchenRenderer.cs", "Assets/Scripts/Core/GameController.cs",
            "Assets/Scripts/UI/PrototypeView.cs" };
        System.Type[] expected = { typeof(StreetGame), typeof(StreetView), typeof(StreetKitchenRenderer),
            typeof(GameController), typeof(PrototypeView) };
        for (int i = 0; i < paths.Length; i++)
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(paths[i]);
            if (script == null || script.GetClass() != expected[i])
                throw new BuildFailedException("Invalid scene script binding: " + paths[i] +
                    ". Rebuild the isolated project's compiler caches for its current path; preserve asset GUIDs. APK not safe to ship.");
        }
    }

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (report == null || report.summary.platform != BuildTarget.Android || scene.path != "Assets/Scenes/Main.unity") return;
        ValidateMainSceneBindings(scene);
    }

    public static void ValidateMainSceneBindings(Scene scene)
    {
        StreetGame game = null;
        StreetView view = null;
        StreetKitchenRenderer renderer = null;
        int games = 0, views = 0, renderers = 0;
        foreach (var root in scene.GetRootGameObjects())
        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
        foreach (var component in transform.GetComponents<Component>())
        {
            // GetMonoBehavioursWithMissingScriptCount can report zero for cached invalid bindings.
            if (component == null)
                throw new BuildFailedException("Main scene contains an unresolved component on " + transform.name + ". APK not safe to ship.");
            if (component is StreetGame foundGame) { game = foundGame; games++; }
            if (component is StreetView foundView) { view = foundView; views++; }
            if (component is StreetKitchenRenderer foundRenderer) { renderer = foundRenderer; renderers++; }
        }
        if (games != 1 || views != 1 || renderers != 1 || !game.isActiveAndEnabled || !view.isActiveAndEnabled || !renderer.isActiveAndEnabled)
            throw new BuildFailedException("Main requires exactly one active StreetGame, StreetView and StreetKitchenRenderer. APK not safe to ship.");
    }

    // Firebase Auth brings an Analytics Android dependency transitively; this game does not opt in.
    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string file = Path.Combine(path, "src/main/AndroidManifest.xml");
        var manifest = new XmlDocument(); manifest.Load(file);
        var application = manifest.SelectSingleNode("/manifest/application");
        if (application == null) throw new BuildFailedException("Android manifest has no application element");
        const string android = "http://schemas.android.com/apk/res/android";
        foreach (var pair in new[] {
            new[] { "firebase_analytics_collection_deactivated", "true" },
            new[] { "google_analytics_adid_collection_enabled", "false" } })
        {
            XmlElement metadata = null;
            foreach (XmlNode child in application.ChildNodes)
                if (child is XmlElement element && element.Name == "meta-data" && element.GetAttribute("name", android) == pair[0]) metadata = element;
            if (metadata == null) { metadata = manifest.CreateElement("meta-data"); application.AppendChild(metadata); }
            metadata.SetAttribute("name", android, pair[0]); metadata.SetAttribute("value", android, pair[1]);
        }
        manifest.Save(file);
    }

}
