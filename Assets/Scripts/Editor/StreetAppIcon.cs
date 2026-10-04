#if UNITY_EDITOR && UNITY_ANDROID
using System.IO;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

namespace HayChoriYPaty.Editor
{
    /// <summary>Reuse the exact presentation logo in adaptive launcher icons without repainting it.</summary>
    public sealed class StreetAppIcon : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 100;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            // Unity passes the unityLibrary root; resources belong to its sibling launcher.
            string launcher = Path.GetFullPath(Path.Combine(path, "../launcher"));
            string logo = Path.Combine(Application.dataPath, "Art/Street/Resources/street-logo.png");
            if (!Directory.Exists(launcher) || !File.Exists(logo))
                throw new BuildFailedException("Hay Chori y Paty app icon: launcher or presentation logo missing.");

            string resources = Path.Combine(launcher, "src/main/res");
            string artwork = Path.Combine(resources, "drawable-nodpi");
            string drawables = Path.Combine(resources, "drawable");
            string adaptive = Path.Combine(resources, "mipmap-anydpi-v26");
            string values = Path.Combine(resources, "values");
            Directory.CreateDirectory(artwork);
            Directory.CreateDirectory(drawables);
            Directory.CreateDirectory(adaptive);
            Directory.CreateDirectory(values);

            // Copy the original bytes. Native XML supplies padding/scale, never modifies the logo.
            File.Copy(logo, Path.Combine(artwork, "hay_chori_paty_logo.png"), true);
            File.WriteAllText(Path.Combine(drawables, "hay_chori_paty_foreground.xml"), ForegroundXml);
            File.WriteAllText(Path.Combine(values, "hay_chori_paty_icon.xml"), BackgroundXml);
            File.WriteAllText(Path.Combine(adaptive, "app_icon.xml"), AdaptiveXml);
        }

        // A 60% wide inset preserves the entire circular badge within the adaptive mask.
        // Height follows the original 1274x1235 aspect ratio instead of stretching to a square.
        private const string ForegroundXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<inset xmlns:android=""http://schemas.android.com/apk/res/android""
    android:insetLeft=""20%"" android:insetRight=""20%""
    android:insetTop=""20.92%"" android:insetBottom=""20.92%"">
    <bitmap android:src=""@drawable/hay_chori_paty_logo""
        android:gravity=""fill"" android:filter=""true"" />
</inset>
";
        private const string BackgroundXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<resources>
    <color name=""hay_chori_paty_icon_background"">#FFF1D0</color>
</resources>
";
        private const string AdaptiveXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<adaptive-icon xmlns:android=""http://schemas.android.com/apk/res/android"">
    <background android:drawable=""@color/hay_chori_paty_icon_background"" />
    <foreground android:drawable=""@drawable/hay_chori_paty_foreground"" />
</adaptive-icon>
";
    }
}
#endif
