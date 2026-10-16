using System.IO;
using System.Xml;
using UnityEditor.Android;
using HayChoriYPaty.Cloud;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class FirebaseAndroidBuildGuard : IPreprocessBuildWithReport, IPostGenerateGradleAndroidProject
{
    public int callbackOrder => -1000;
    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android) return;
        const string file = "Assets/Resources/player-firebase-config.json";
        if (!File.Exists("Assets/google-services.json") || !File.Exists(file))
            throw new BuildFailedException("Real Firebase Android/Google configuration missing. Configure haychoriypaty before shipping the mandatory-login player.");
        var config = JsonUtility.FromJson<PlayerFirebaseConfig>(File.ReadAllText(file));
        if (config == null || config.projectId != FirebasePlayerSession.ProjectId || string.IsNullOrEmpty(config.webClientId) ||
            PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != "com.haychoriypaty.game")
            throw new BuildFailedException("Refusing build with a different Firebase project, Android ID or absent Google OAuth client.");
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
