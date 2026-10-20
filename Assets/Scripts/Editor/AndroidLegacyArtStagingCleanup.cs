#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HayChoriYPaty.Editor
{
    /// <summary>Remove only obsolete generated copies, never source art or live player data.</summary>
    public sealed class AndroidLegacyArtStagingCleanup : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.Android)
                RemoveArchivedCopies(Path.GetFullPath(Path.Combine(Application.dataPath, "..")));
        }

        public static void RemoveArchivedCopies(string projectRoot)
        {
            string gradle = Path.Combine(projectRoot, "Library/Bee/Android/Prj/IL2CPP/Gradle");
            string[] folders = {
                "unityLibrary/src/main/assets/bin/Data",
                "unityLibrary/build/intermediates/assets/debug/mergeDebugAssets/bin/Data",
                "launcher/build/intermediates/assets/debug/mergeDebugAssets/bin/Data"
            };
            string[] archivedTextureGuids = {
                "ba31aede125064fd8857b2227d07992e", "00ac52b88645b4b9d943757fe4beefcf",
                "4d07362ad5b74f948a14cb751a35874c", "262c70b1a7b14d359409cefc28e1de95"
            };
            bool removed = false;
            foreach (string folder in folders)
                foreach (string guid in archivedTextureGuids)
                {
                    string generatedFile = Path.Combine(gradle, folder, guid);
                    if (File.Exists(generatedFile)) { File.Delete(generatedFile); removed = true; }
                }
            if (!removed) return;
            // Incremental APK updates can leave holes after deleting entries. Force a fresh
            // package only when legacy copies were actually removed; native code is retained.
            foreach (string variant in new[] { "Debug", "Release" })
            {
                string packageCache = Path.Combine(gradle, "launcher/build/intermediates/incremental/package" + variant);
                if (Directory.Exists(packageCache)) Directory.Delete(packageCache, true);
                string apkFolder = Path.Combine(gradle, "launcher/build/outputs/apk/" + variant.ToLowerInvariant());
                if (Directory.Exists(apkFolder))
                    foreach (string apk in Directory.GetFiles(apkFolder, "launcher-*.apk")) File.Delete(apk);
            }
            Debug.Log("Removed archived Android art staging copies; incremental APK packaging reset.");
        }
    }
}
#endif
