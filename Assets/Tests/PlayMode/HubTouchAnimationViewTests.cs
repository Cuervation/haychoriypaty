#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HayChoriYPaty.Tests
{
    public sealed class HubTouchAnimationViewTests
    {
        const BindingFlags I = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        const BindingFlags S = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy;
        GameObject root, cameraRoot;
        Component game, view;
        Type gt, vt;
        FieldInfo gate;
        bool oldGate;
        float oldTime;
        UnityEditor.EditorWindow window;
        PropertyInfo selectedSize;
        object group;
        int oldSize;
        readonly List<int> addedCustomIndices = new List<int>();
        static string Output => Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Acceptance/HubTouchAnimation20261106"));
        static Type T(string n) => Type.GetType("HayChoriYPaty." + n + ", Assembly-CSharp", true);
        static object Get(object o, string n) => o.GetType().GetField(n, I).GetValue(o);
        static void Set(object o, string n, object v) => o.GetType().GetField(n, I).SetValue(o, v);
        static object Call(object o, string n, params object[] args) => o.GetType().GetMethod(n, I).Invoke(o, args);
        bool Persist<TMatch>(string json, TMatch match) => true; // Never PlayerPrefs/cloud.
        void Hub()
        {
            foreach (string flag in new[] {"introActive", "settingsActive", "marketActive", "badgesActive", "rankingActive", "levelSelectActive"}) Set(view, flag, false);
            Set(view, "hubActive", true); Set(view, "pressedAction", 0);
        }
        void Portrait(int width, int height)
        {
            Assembly editor = typeof(UnityEditor.EditorWindow).Assembly;
            if (window == null)
            {
                window = UnityEditor.EditorWindow.GetWindow(editor.GetType("UnityEditor.GameView"));
                selectedSize = window.GetType().GetProperty("selectedSizeIndex", I); oldSize = (int)selectedSize.GetValue(window);
                Type sizes = editor.GetType("UnityEditor.GameViewSizes");
                object registry = sizes.GetProperty("instance", S).GetValue(null);
                group = sizes.GetMethod("GetGroup", I).Invoke(registry, new[] {sizes.GetProperty("currentGroupType", I).GetValue(registry)});
            }
            // Remove only this fixture's own leftover size from an interrupted earlier run.
            if (addedCustomIndices.Count == 0)
            {
                string[] labels = (string[])Call(group, "GetDisplayTexts");
                for (int i = labels.Length - 1; i >= 0; i--)
                    if (labels[i].Contains("Hub touch animation temporary QA")) Call(group, "RemoveCustomSize", i);
            }
            int absoluteIndex = (int)Call(group, "GetTotalCount");
            object size = Activator.CreateInstance(editor.GetType("UnityEditor.GameViewSize"), new object[]
                {Enum.Parse(editor.GetType("UnityEditor.GameViewSizeType"), "FixedResolution"), width, height, "Hub touch animation temporary QA"});
            Call(group, "AddCustomSize", size); addedCustomIndices.Add(absoluteIndex);
            selectedSize.SetValue(window, (int)Call(group, "GetTotalCount") - 1); window.Repaint();
        }
        [UnitySetUp] public IEnumerator SetUp()
        {
            oldTime = Time.timeScale; gt = T("StreetGame"); vt = T("StreetView");
            gate = gt.GetField("AllowEditorTestInitialization", S); oldGate = (bool)gate.GetValue(null); gate.SetValue(null, false);
            Assert.IsNull(Type.GetType("HayChoriYPaty.Cloud.FirebasePlayerSession, Assembly-CSharp", true).GetProperty("Instance", S).GetValue(null));
            Portrait(540, 1200); yield return null; yield return null;
            cameraRoot = new GameObject("Hub QA camera"); cameraRoot.tag = "MainCamera"; cameraRoot.AddComponent<Camera>().transform.position = new Vector3(0, 0, -10);
            root = new GameObject("Hub memory-only QA"); root.SetActive(false); game = root.AddComponent(gt); root.SetActive(true);
            object seed = Activator.CreateInstance(gt.GetNestedType("SaveData", BindingFlags.Public)); Set(seed, "version", 5); Set(seed, "unlockedLevel", 10);
            Type callback = typeof(Func<,,>).MakeGenericType(typeof(string), T("StreetMatchRecord"), typeof(bool));
            Delegate save = GetType().GetMethod("Persist", I).MakeGenericMethod(T("StreetMatchRecord")).CreateDelegate(callback, this);
            Assert.IsTrue((bool)Call(game, "ActivatePlayer", JsonUtility.ToJson(seed), save));
            view = root.AddComponent(vt); Hub();
            float deadline = Time.realtimeSinceStartup + 90f;
            while (!(bool)Get(view, "viewResourcesReady") && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue((bool)Get(view, "viewResourcesReady")); Hub(); Time.timeScale = 0f; yield return null;
        }
        IEnumerator Capture(string name)
        {
            yield return null; yield return new WaitForEndOfFrame();
            Texture2D capture = ScreenCapture.CaptureScreenshotAsTexture();
            Assert.AreEqual(Screen.width, capture.width); Assert.AreEqual(Screen.height, capture.height);
            Directory.CreateDirectory(Output); File.WriteAllBytes(Path.Combine(Output, name + ".png"), capture.EncodeToPNG());
            UnityEngine.Object.Destroy(capture);
        }
        Vector2 Pixel(Vector2 layout)
        {
            Rect vp = (Rect)vt.GetMethod("CanvasViewport", S).Invoke(null, new object[] {new Vector2(Screen.width, Screen.height), Screen.safeArea});
            return new Vector2(vp.x + layout.x * vp.width / 540f, Screen.height - vp.y - layout.y * vp.height / 960f);
        }
        [UnityTest] public IEnumerator FourCleanSignsAnimatePausedAndTheirStableTargetsNavigateOnRelease()
        {
            foreach (Vector2 size in new[] {new Vector2(540, 1200), new Vector2(1080, 1920)})
            {
                if (Screen.width != (int)size.x || Screen.height != (int)size.y) { Portrait((int)size.x, (int)size.y); yield return null; yield return null; }
                Hub(); Set(view, "hubSignageAnimationTimeForTesting", 0f);
                yield return Capture("hub-signs-phase-a-" + (int)size.x + "x" + (int)size.y);
                Set(view, "hubSignageAnimationTimeForTesting", .7f);
                yield return Capture("hub-signs-phase-b-" + (int)size.x + "x" + (int)size.y);
                if ((int)size.x == 540)
                    for (int frame = 0; frame < 24; frame++)
                    {
                        Set(view, "hubSignageAnimationTimeForTesting", frame * 2.8f / 24f);
                        yield return Capture("hub-loop-" + frame.ToString("D2"));
                    }
                MethodInfo scale = vt.GetMethod("HubSignagePulseScale", S);
                float start = Time.unscaledTime;
                Set(view, "hubSignageAnimationTimeForTesting", -1f);
                yield return new WaitForSecondsRealtime(.4f);
                Assert.Greater(Time.unscaledTime - start, .3f); Assert.AreEqual(0f, Time.timeScale);
                foreach (int action in new[] {200, 201, 202, 203})
                {
                    float a = (float)scale.Invoke(null, new object[] {action, 0f, false});
                    float b = (float)scale.Invoke(null, new object[] {action, .7f, false});
                    Assert.Greater(Mathf.Abs(a - b), .01f);
                    Hub(); Rect target = (Rect)Call(view, "HubArtworkActionBounds", action);
                    Assert.AreEqual(action, Call(view, "HitAction", target.center));
                    Vector2 pixel = Pixel(target.center); Call(view, "HandlePointer", pixel, true, false);
                    Assert.AreEqual(action, Get(view, "pressedAction")); Assert.IsTrue((bool)Get(view, "hubActive"));
                    if (action == 200) yield return Capture("hub-play-pressed-" + (int)size.x + "x" + (int)size.y);
                    Call(view, "HandlePointer", pixel, false, true);
                    if (action != 201) Assert.IsFalse((bool)Get(view, "hubActive"), "Action " + action);
                    string screen = action == 200 ? "levelSelectActive" : action == 201 ? "marketActive" : action == 202 ? "badgesActive" : "rankingActive";
                    Assert.IsTrue((bool)Get(view, screen), "Action " + action + " opens " + screen);
                }
            }
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            if (root != null) UnityEngine.Object.Destroy(root); if (cameraRoot != null) UnityEngine.Object.Destroy(cameraRoot);
            Time.timeScale = oldTime; yield return null;
            if (window != null) { selectedSize.SetValue(window, oldSize); window.Repaint(); }
            if (group != null) for (int n = addedCustomIndices.Count - 1; n >= 0; n--) Call(group, "RemoveCustomSize", addedCustomIndices[n]);
            addedCustomIndices.Clear(); if (gate != null) gate.SetValue(null, oldGate);
        }
    }
}
#endif
