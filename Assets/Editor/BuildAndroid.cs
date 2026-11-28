using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

public class BuildAndroid
{
    public static void Build()
    {
        string[] scenes = { "Assets/Scenes/Main.unity" };
        string buildPath = "/Users/celestino/HayChoriYPaty/Builds/HayChoriYPaty.apk";

        BuildReport report = BuildPipeline.BuildPlayer(
            scenes,
            buildPath,
            BuildTarget.Android,
            BuildOptions.None
        );

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Android build succeeded!");
        }
        else
        {
            Debug.LogError("Android build failed!");
        }
    }
}
