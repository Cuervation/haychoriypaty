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
            foreach (string flag in new[] {"introActive", "settingsActive", "marketActive", "badgesActive", "rankingActive", "levelSelectActive", "profileActive"}) Set(view, flag, false);
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
        // 2026-11-25: the ranking screen with a made-up server answer (podium, list, own row outside the Top 10, apodo form, no connection).
        [UnityTest, Explicit("Captures the ranking screen states to Logs/Acceptance/Ranking-20261125")]
        public IEnumerator RankingScreenStatesCapture()
        {
            if (Application.isBatchMode) Assert.Ignore("Requires interactive Game View.");
            Portrait(575, 1280); yield return null; yield return null;
            Type snapshotType = Type.GetType("HayChoriYPaty.Cloud.LeaderboardSnapshot, Assembly-CSharp", true);
            Type entryType = Type.GetType("HayChoriYPaty.Cloud.LeaderboardEntry, Assembly-CSharp", true);
            Type stateType = vt.GetNestedType("RankingState", BindingFlags.NonPublic);
            Func<string, string, long, long, object> entry = (uid, nick, points, won) =>
            {
                object e = Activator.CreateInstance(entryType);
                Set(e, "uid", uid); Set(e, "nickname", nick); Set(e, "points", points); Set(e, "wonLevels", won); return e;
            };
            string[] nicks = { "Xiangxiu", "Chori_Rey", "La Bondiola", "Ñandú FC", "Paty Doble", "El Negro", "Hincha 10", "Fernet&Co", "Doña Coca", "Vacío Total" };
            object snapshot = Activator.CreateInstance(snapshotType);
            IList top = (IList)Get(snapshot, "top");
            for (int i = 0; i < nicks.Length; i++) top.Add(entry("u" + i, nicks[i], 2030 - i * 140, 14 - i / 2));
            Set(snapshot, "me", entry("me", "Cuervation", 393, 4)); Set(snapshot, "myRank", 1779L);
            Set(view, "rankingSnapshot", snapshot);
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Acceptance/Ranking-20261125"));
            Directory.CreateDirectory(folder);
            foreach (var shot in new[] { ("ready", 2), ("nickname", 3), ("offline", 4) })
            {
                foreach (string flag in new[] { "introActive", "settingsActive", "marketActive", "badgesActive", "levelSelectActive", "hubActive" }) Set(view, flag, false);
                Set(view, "rankingActive", true); Set(view, "rankingState", Enum.ToObject(stateType, shot.Item2));
                yield return null; yield return new WaitForEndOfFrame();
                Texture2D capture = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(folder, "ranking-" + shot.Item1 + ".png"), capture.EncodeToPNG());
                UnityEngine.Object.Destroy(capture);
            }
            // Few players (the real server today): the player alone at Nº 1, empty podium places and free list rows.
            object sparse = Activator.CreateInstance(snapshotType);
            object alone = entry("me", "cuervo", 1655, 12);
            ((IList)Get(sparse, "top")).Add(alone); Set(sparse, "me", alone); Set(sparse, "myRank", 1L);
            Set(view, "rankingSnapshot", sparse); Set(view, "rankingState", Enum.ToObject(stateType, 2));
            yield return null; yield return new WaitForEndOfFrame();
            Texture2D sparseCapture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(folder, "ranking-ready-sparse.png"), sparseCapture.EncodeToPNG());
            UnityEngine.Object.Destroy(sparseCapture);
            // Shortest supported portrait (16:9): the board must still fit without overlaps.
            Portrait(1080, 1920); yield return null; yield return null;
            Set(view, "rankingSnapshot", snapshot); Set(view, "rankingState", Enum.ToObject(stateType, 2));
            yield return null; yield return new WaitForEndOfFrame();
            Texture2D shortCapture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(folder, "ranking-ready-1080x1920.png"), shortCapture.EncodeToPNG());
            UnityEngine.Object.Destroy(shortCapture);
        }
        // 2026-11-26: the badges trophy vitrina with mixed progress, at the top, half-way and the end of the scroll.
        [UnityTest, Explicit("Captures the badges vitrina to Logs/Acceptance/Badges-20261126")]
        public IEnumerator BadgesVitrinaCapture()
        {
            if (Application.isBatchMode) Assert.Ignore("Requires interactive Game View.");
            Portrait(575, 1280); yield return null; yield return null;
            object save = gt.GetField("save", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
            object statistics = save.GetType().GetField("statistics").GetValue(save);
            statistics.GetType().GetField("deliveredByProduct").SetValue(statistics, new long[] { 5400, 2600, 820, 120, 3100, 0, 510 });
            statistics.GetType().GetMethod("Normalize").Invoke(statistics, null);
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Acceptance/Badges-20261126"));
            Directory.CreateDirectory(folder);
            foreach (string flag in new[] { "introActive", "settingsActive", "marketActive", "rankingActive", "levelSelectActive", "hubActive" }) Set(view, flag, false);
            Set(view, "badgesActive", true);
            for (int frame = 0; frame < 4; frame++) yield return null; // the trophy art is sampled in Update
            float max = (float)vt.GetMethod("BadgesMaxScroll", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { (float)Get(view, "logicalCanvasHeight") });
            foreach (var shot in new[] { ("top", 0f), ("middle", .5f), ("end", 1f) })
            {
                Set(view, "badgesScroll", max * shot.Item2);
                yield return null; yield return new WaitForEndOfFrame();
                Texture2D capture = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(folder, "badges-" + shot.Item1 + ".png"), capture.EncodeToPNG());
                UnityEngine.Object.Destroy(capture);
            }
            Portrait(1080, 1920); yield return null; yield return null;
            Set(view, "badgesScroll", 0f);
            yield return null; yield return new WaitForEndOfFrame();
            Texture2D shortCapture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(folder, "badges-top-1080x1920.png"), shortCapture.EncodeToPNG());
            UnityEngine.Object.Destroy(shortCapture);
        }
        // 2026-11-26: the hub profile card and the profile screen (preview data: there is no Firebase session in the editor).
        [UnityTest, Explicit("Captures the hub profile card and profile screen to Logs/Acceptance/Profile-20261126")]
        public IEnumerator ProfileScreensCapture()
        {
            if (Application.isBatchMode) Assert.Ignore("Requires interactive Game View.");
            object save = gt.GetField("save", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
            object statistics = save.GetType().GetField("statistics").GetValue(save);
            statistics.GetType().GetField("deliveredByProduct").SetValue(statistics, new long[] { 5400, 2600, 820, 120, 3100, 0, 510 });
            Set(statistics, "attemptsFinished", 184L); Set(statistics, "wins", 97L); Set(statistics, "customersAttended", 9321L);
            statistics.GetType().GetMethod("Normalize").Invoke(statistics, null);
            Set(view, "profilePreviewNickname", "Cuervation"); Set(view, "profilePreviewEmail", "hincha.ejemplo@gmail.com"); Set(view, "profilePreviewSignedIn", true);
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Acceptance/Profile-20261126"));
            Directory.CreateDirectory(folder);
            IEnumerator Shot(string name)
            {
                yield return null; yield return new WaitForEndOfFrame();
                Texture2D capture = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(folder, name + ".png"), capture.EncodeToPNG());
                UnityEngine.Object.Destroy(capture);
            }
            foreach (var size in new[] { new Vector2Int(575, 1280), new Vector2Int(1080, 1920) })
            {
                Portrait(size.x, size.y); yield return null; yield return null;
                Hub(); yield return Shot("hub-card-" + size.x + "x" + size.y);
                Call(view, "DispatchAction", 240);
                Assert.IsTrue((bool)Get(view, "profileActive"), "tapping the card opens the profile");
                yield return Shot("profile-" + size.x + "x" + size.y);
                Call(view, "DispatchAction", 242);
                Assert.IsTrue((bool)Get(view, "profileEditing"));
                Set(view, "profileNicknameInput", "Cuervation 10");
                yield return Shot("profile-editing-" + size.x + "x" + size.y);
                Call(view, "DispatchAction", 244);
                Call(view, "DispatchAction", 245);
                Assert.IsTrue((bool)Get(view, "profileConfirmSignOut"));
                yield return Shot("profile-signout-confirm-" + size.x + "x" + size.y);
                Call(view, "DispatchAction", 247);
                Call(view, "DispatchAction", 241);
                Assert.IsTrue((bool)Get(view, "hubActive"));
            }
            Set(view, "profilePreviewSignedIn", false); Set(view, "profilePreviewNickname", null); Set(view, "profilePreviewEmail", null);
            Call(view, "DispatchAction", 240);
            yield return Shot("profile-signed-out-1080x1920");
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
