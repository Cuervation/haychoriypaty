using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Starts the build on the next Editor update, outside an MCP execution/reload lock.</summary>
[InitializeOnLoad]
public static class FirebaseAndroidBuildLauncher
{
    private const string Pending = "HayChoriYPaty.FirebaseBuildPending";
    private static string StatusPath => Path.GetFullPath("Logs/firebase-android-build-" + PlayerSettings.bundleVersion + ".json");
    [Serializable] private sealed class Result
    {
        public string status;
        public int errors, warnings;
        public ulong bytes;
        public double seconds;
    }
    static FirebaseAndroidBuildLauncher()
    {
        if (SessionState.GetBool(Pending, false)) EditorApplication.update += BuildWhenReady;
    }
    [MenuItem("Hay Chori y Paty/Build Firebase Android Development")]
    public static void Queue()
    {
        if (EditorApplication.isPlaying || BuildPipeline.isBuildingPlayer) throw new InvalidOperationException("Stop play/build before queuing APK");
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android) throw new InvalidOperationException("Select Android target first");
        if (EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Preserve unsaved scene before building");
        SessionState.SetBool(Pending, true);
        Write(new Result { status = "scheduled" });
        EditorApplication.update -= BuildWhenReady;
        EditorApplication.update += BuildWhenReady;
    }
    private static void BuildWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) return;
        EditorApplication.update -= BuildWhenReady;
        SessionState.SetBool(Pending, false);
        try
        {
            if (EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Scene changed after queuing build");
            Write(new Result { status = "building" });
            string output = Path.GetFullPath("Builds/Android/HayChoriYPaty-" + PlayerSettings.bundleVersion + "-Firebase.apk");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { "Assets/Scenes/Main.unity" }, locationPathName = output,
                target = BuildTarget.Android, options = BuildOptions.Development
            });
            Write(new Result { status = report.summary.result.ToString(), errors = report.summary.totalErrors,
                warnings = report.summary.totalWarnings, bytes = report.summary.totalSize,
                seconds = report.summary.totalTime.TotalSeconds });
        }
        catch (Exception error)
        {
            Write(new Result { status = "exception", errors = 1 });
            Debug.LogException(error);
        }
    }
    private static void Write(Result result)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StatusPath));
        File.WriteAllText(StatusPath, JsonUtility.ToJson(result));
    }
}
