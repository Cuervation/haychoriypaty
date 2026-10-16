using System.IO;
using HayChoriYPaty.Cloud;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class FirebaseAndroidBuildGuard : IPreprocessBuildWithReport
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
}
