using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HayChoriYPaty.Tests
{
    public sealed class StreetSceneBuildGuardTests
    {
        private static Type Guard => AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("FirebaseAndroidBuildGuard")).First(t => t != null);
        private static void Invoke(string method, params object[] args)
        {
            try { Guard.GetMethod(method, BindingFlags.Public | BindingFlags.Static).Invoke(null, args); }
            catch (TargetInvocationException error) { throw error.InnerException; }
        }
        [Test] public void RuntimeSceneScriptsResolveToCompiledClasses()
        {
            Assert.DoesNotThrow(() => Invoke("ValidateScriptBindings"));
        }
        [Test] public void ShippedMainContainsRequiredActiveComponents()
        {
            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath("Assets/Scenes/Main.unity");
            bool opened = !scene.IsValid() || !scene.isLoaded;
            try
            {
                if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Additive);
                Assert.DoesNotThrow(() => Invoke("ValidateMainSceneBindings", scene));
            }
            finally
            {
                if (opened && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
        [Test] public void SceneWithoutGameComponentsIsRejected()
        {
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var error = Assert.Catch<Exception>(() => Invoke("ValidateMainSceneBindings", scene));
                StringAssert.Contains("Main requires exactly one active", error.Message);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
    }
}
