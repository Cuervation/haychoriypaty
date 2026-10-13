using System;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HayChoriYPaty.Tests
{
    /// <summary>Exercises Mercado/Heladera through StreetView's pointer and dispatch paths.</summary>
    public sealed class StreetMarketViewTests
    {
        private const BindingFlags Pub = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags Priv = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags PubStatic = BindingFlags.Public | BindingFlags.Static;
        private const BindingFlags PrivStatic = BindingFlags.NonPublic | BindingFlags.Static;
        private GameObject root;
        private Component game, view;
        private Type gameType, viewType;
        private bool hadSave;
        private string previousSave;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            gameType = Type.GetType("HayChoriYPaty.StreetGame, Assembly-CSharp", true);
            string key = (string)gameType.GetField("ProgressKey", PubStatic).GetValue(null);
            hadSave = PlayerPrefs.HasKey(key);
            previousSave = hadSave ? PlayerPrefs.GetString(key) : null;
            object seed = Activator.CreateInstance(gameType.GetNestedType("SaveData", BindingFlags.Public));
            seed.GetType().GetField("version").SetValue(seed, 4);
            seed.GetType().GetField("unlockedLevel").SetValue(seed, 10);
            Type marketDataType = Type.GetType("HayChoriYPaty.StreetMarketData, Assembly-CSharp", true);
            object marketSeed = Activator.CreateInstance(marketDataType);
            marketDataType.GetField("balanceCents", Pub).SetValue(marketSeed, 50000L);
            seed.GetType().GetField("market").SetValue(seed, marketSeed);
            PlayerPrefs.SetString(key, JsonUtility.ToJson(seed));
            PlayerPrefs.Save();
            root = new GameObject("Street market UI test (temporary)");
            game = root.AddComponent(gameType);
            viewType = Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true);
            view = root.AddComponent(viewType);
            viewType.GetField("introActive", Priv).SetValue(view, false);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null) UnityEngine.Object.Destroy(root);
            yield return null; // StreetGame.OnDisable writes persistence; restore after it completes.
            string key = (string)gameType.GetField("ProgressKey", PubStatic).GetValue(null);
            if (hadSave) PlayerPrefs.SetString(key, previousSave); else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        private object GetGame(string name) => gameType.GetProperty(name, Pub).GetValue(game, null);
        private object Sim => GetGame("Sim");
        private object Market => GetGame("Market");
        private void Dispatch(int action) => viewType.GetMethod("DispatchAction", Priv).Invoke(view, new object[] { action });
        private Vector2 Pixel(float x, float y)
        {
            Rect viewport = (Rect)viewType.GetMethod("CanvasViewport", PrivStatic)
                .Invoke(null, new object[] { new Vector2(Screen.width, Screen.height), Screen.safeArea });
            float scale = viewport.width / 540f;
            float height = (float)viewType.GetMethod("CanvasLogicalHeight", PrivStatic).Invoke(null, new object[] { viewport });
            return new Vector2(viewport.x + x * scale, Screen.height - (viewport.y + y * scale));
        }
        private void Click(float x, float y)
        {
            Vector2 p = Pixel(x, y);
            MethodInfo pointer = viewType.GetMethod("HandlePointer", Priv);
            pointer.Invoke(view, new object[] { p, true, false });
            pointer.Invoke(view, new object[] { p, false, true });
        }

        [UnityTest]
        public IEnumerator SelectorTabsAndQuantitySelectionRequireExplicitDebouncedPurchase()
        {
            viewType.GetField("levelSelectActive", Priv).SetValue(view, true);
            Dispatch(161); // The selector's Mercado/Heladera button.
            Assert.IsTrue((bool)viewType.GetField("marketActive", Priv).GetValue(view));
            Assert.AreEqual(1, viewType.GetField("marketTab", Priv).GetValue(view));
            Click(140, 119); // Switch to Mercado through actual hit bounds.
            Assert.AreEqual(0, viewType.GetField("marketTab", Priv).GetValue(view));

            int before = (int)Market.GetType().GetMethod("Units", Pub).Invoke(Market, new object[] { 0 });
            Click(135, 417); // +1 Chorizo pack, no purchase yet.
            Assert.AreEqual(1, ((int[])viewType.GetField("marketPendingPacks", Priv).GetValue(view))[0]);
            Assert.AreEqual(before, (int)Market.GetType().GetMethod("Units", Pub).Invoke(Market, new object[] { 0 }));
            Click(290, 417); // Confirm twice inside 250 ms.
            Click(290, 417);
            Assert.AreEqual(before + 1, (int)Market.GetType().GetMethod("Units", Pub).Invoke(Market, new object[] { 0 }));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayIsStockGatedMinimumFiadoCanRestockAndRetryIsGone()
        {
            viewType.GetField("marketActive", Priv).SetValue(view, true);
            object data = Market.GetType().GetProperty("Data", Pub).GetValue(Market, null);
            Array.Clear((int[])data.GetType().GetField("stockUnits", Pub).GetValue(data), 0, 10);
            Assert.IsFalse((bool)GetGame("CanStartRound"));
            Dispatch(160);
            Assert.AreEqual("Ready", Sim.GetType().GetProperty("Phase", Pub).GetValue(Sim, null).ToString());
            for (int id = 0; id < 10; id++)
            {
                int missing = (int)Market.GetType().GetMethod("MissingPacks", Pub).Invoke(Market, new[] { Sim, (object)id });
                if (missing > 0) Assert.IsTrue((bool)gameType.GetMethod("BuyMissingIngredient", Pub).Invoke(game, new object[] { id }));
            }
            Assert.IsTrue((bool)GetGame("CanStartRound"));
            Dispatch(160);
            Assert.AreEqual("Playing", Sim.GetType().GetProperty("Phase", Pub).GetValue(Sim, null).ToString());
            Assert.IsFalse((bool)gameType.GetMethod("Retry", Pub).Invoke(game, null));
            yield return null;
        }

        [UnityTest]
        public IEnumerator DraggingIngredientListDoesNotConfirmAPurchase()
        {
            viewType.GetField("marketActive",Priv).SetValue(view,true);
            Dispatch(71); Dispatch(100);
            long op=(long)Market.GetType().GetProperty("NextPurchaseId",Pub).GetValue(Market,null);
            MethodInfo pointer=viewType.GetMethod("HandlePointer",Priv);
            pointer.Invoke(view,new object[]{Pixel(290,417),true,false});
            pointer.Invoke(view,new object[]{Pixel(290,257),false,false});
            pointer.Invoke(view,new object[]{Pixel(290,257),false,true});
            Assert.Greater((float)viewType.GetField("marketScroll",Priv).GetValue(view),0f);
            Assert.AreEqual(op,Market.GetType().GetProperty("NextPurchaseId",Pub).GetValue(Market,null));
            yield return null;
        }

        [UnityTest]
        public IEnumerator FirstLevelFridgeConsultationDoesNotUnlockPurchasing()
        {
            object save=gameType.GetField("save",Priv).GetValue(game);
            save.GetType().GetField("unlockedLevel").SetValue(save,0);
            viewType.GetField("levelSelectActive",Priv).SetValue(view,true);
            Dispatch(161); Assert.AreEqual(1,viewType.GetField("marketTab",Priv).GetValue(view));
            Dispatch(71); Assert.AreEqual(1,viewType.GetField("marketTab",Priv).GetValue(view));
            Assert.IsFalse((bool)gameType.GetMethod("TryBuyIngredient",Pub).Invoke(game,new object[]{0,1,false,0L}));
            Assert.AreEqual("Ready",Sim.GetType().GetProperty("Phase",Pub).GetValue(Sim,null).ToString());
            yield return null;
        }

        [UnityTest]
        public IEnumerator HiddenMarketPlayCallbackCannotStartRound()
        {
            viewType.GetField("marketActive", Priv).SetValue(view, false);
            Dispatch(160);
            Assert.AreEqual("Ready", Sim.GetType().GetProperty("Phase", Pub).GetValue(Sim, null).ToString());
            yield return null;
        }

        [UnityTest]
        public IEnumerator LastLevelDoesNotResolveANextMatchAction()
        {
            Assert.IsTrue((bool)gameType.GetMethod("SelectLevel", Pub).Invoke(game, new object[] { 10 }));
            Sim.GetType().GetProperty("Phase", Pub).SetValue(Sim,
                Enum.Parse(Sim.GetType().GetProperty("Phase", Pub).PropertyType, "Won"), null);
            yield return null;
            viewType.GetField("levelSelectActive", Priv).SetValue(view, false);
            Rect viewport = (Rect)viewType.GetMethod("CanvasViewport", PrivStatic)
                .Invoke(null, new object[] { new Vector2(Screen.width, Screen.height), Screen.safeArea });
            float height = (float)viewType.GetMethod("CanvasLogicalHeight", PrivStatic).Invoke(null, new object[] { viewport });
            float scale = height / 960f;
            viewType.GetField("logicalCanvasHeight", Priv).SetValue(view, height);
            viewType.GetField("layoutVerticalScale", Priv).SetValue(view, scale);
            Rect next = (Rect)viewType.GetMethod("VictoryExitBounds", Priv).Invoke(view, null);
            int hit = (int)viewType.GetMethod("HitAction", Priv).Invoke(view, new object[] { new Vector2(next.center.x, next.center.y / scale) });
            Assert.AreEqual(0, hit);
            Assert.IsFalse((bool)gameType.GetMethod("Retry", Pub).Invoke(game, null));
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator CaptureBothMarketTabsAtPortraitGameViewSizes()
        {
            viewType.GetField("marketActive", Priv).SetValue(view, true);
            viewType.GetField("levelSelectActive", Priv).SetValue(view, false);
            Assert.IsTrue((bool)gameType.GetMethod("SelectLevel", Pub).Invoke(game, new object[] { 4 }));
            object captureData=Market.GetType().GetProperty("Data",Pub).GetValue(Market,null);
            int[] captureStock=(int[])captureData.GetType().GetField("stockUnits").GetValue(captureData);
            captureStock[8]-=1; captureStock[9]-=2; // Representative saved partial-bottle stock; never edit the player's prior save.
            int[,] sizes = { { 540, 960 }, { 540, 1170 }, { 540, 1200 } };
            for (int i = 0; i < sizes.GetLength(0); i++)
            {
                yield return CaptureGameViewSize(sizes[i, 0], sizes[i, 1]);
            }
        }

        private static void SaveCurrentGameViewCapture(string name)
        {
            Texture2D capture = ScreenCapture.CaptureScreenshotAsTexture();
            Assert.IsNotNull(capture, "Unity did not return a GameView capture.");
            string[] resolution = name.Substring(name.LastIndexOf('-') + 1).Split('x');
            Assert.AreEqual(int.Parse(resolution[0]), capture.width);
            Assert.AreEqual(int.Parse(resolution[1]), capture.height);
            string path = Path.Combine(Path.GetTempPath(), "hay-chori-market-" + name + ".png");
            File.WriteAllBytes(path, capture.EncodeToPNG());
            Debug.Log("Street market visual capture saved to " + path + " (" + capture.width + "x" + capture.height + ")");
            UnityEngine.Object.Destroy(capture);
        }

        private IEnumerator CaptureGameViewSize(int width, int height)
        {
            Assembly editorAssembly = typeof(UnityEditor.EditorWindow).Assembly;
            Assert.IsNotNull(editorAssembly, "UnityEditor.GameView is unavailable; responsive capture cannot run.");
            Type gameViewType = editorAssembly.GetType("UnityEditor.GameView");
            Type windowType = editorAssembly.GetType("UnityEditor.EditorWindow");
            MethodInfo getWindow = windowType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.Name == "GetWindow" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(Type));
            Assert.IsNotNull(getWindow, "Could not resolve EditorWindow.GetWindow(Type).");
            object window = getWindow.Invoke(null, new object[] { gameViewType });
            PropertyInfo selected = gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo selectedField = gameViewType.GetField("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            int previous = selected != null ? Convert.ToInt32(selected.GetValue(window, null)) : Convert.ToInt32(selectedField.GetValue(window));

            Type sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
            object sizeRegistry = sizesType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).GetValue(null, null);
            Type groupType = editorAssembly.GetType("UnityEditor.GameViewSizeGroupType");
            object groupKind = sizesType.GetProperty("currentGroupType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(sizeRegistry, null);
            object group = sizesType.GetMethod("GetGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(sizeRegistry, new[] { groupKind });
            MethodInfo countMethod = group.GetType().GetMethod("GetTotalCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo getSize = group.GetType().GetMethod("GetGameViewSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            int count = Convert.ToInt32(countMethod.Invoke(group, null)), target = -1, addedIndex = -1;
            for (int i = 0; i < count; i++)
            {
                object size = getSize.Invoke(group, new object[] { i });
                int w = Convert.ToInt32(size.GetType().GetProperty("width").GetValue(size, null));
                int h = Convert.ToInt32(size.GetType().GetProperty("height").GetValue(size, null));
                PropertyInfo typeProperty=size.GetType().GetProperty("type");
                if (w == width && h == height && typeProperty != null && typeProperty.GetValue(size, null).ToString() == "FixedResolution") { target = i; break; }
            }
            if (target < 0)
            {
                Type sizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
                Type sizeKindType = editorAssembly.GetType("UnityEditor.GameViewSizeType");
                object sizeKind = Enum.Parse(sizeKindType, "FixedResolution");
                object newSize = Activator.CreateInstance(sizeType, new[] { sizeKind, (object)width, height, "Temporary " + width + "x" + height });
                MethodInfo builtInMethod=group.GetType().GetMethod("GetBuiltinCount",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
                int builtInCount=Convert.ToInt32(builtInMethod.Invoke(group,null));
                group.GetType().GetMethod("AddCustomSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(group, new[] { newSize });
                target = Convert.ToInt32(countMethod.Invoke(group, null)) - 1;
                addedIndex=target; // Unity 6000.6 RemoveCustomSize takes the total index.
            }
            try
            {
                if (selected != null) selected.SetValue(window, target, null); else selectedField.SetValue(window, target);
                yield return null; yield return null;
                Assert.AreEqual(width, Screen.width, "GameView width did not update to the requested fixed resolution.");
                Assert.AreEqual(height, Screen.height, "GameView height did not update to the requested fixed resolution.");
                viewType.GetField("marketScroll", Priv).SetValue(view, 0f);
                Click(140,119);
                Assert.AreEqual(0, viewType.GetField("marketTab", Priv).GetValue(view), "Market tab hit on requested ratio");
                Rect body=(Rect)viewType.GetMethod("MarketBodyBounds", Priv).Invoke(view,null);
                Assert.LessOrEqual(body.yMax, Screen.height);
                yield return null; yield return new WaitForEndOfFrame();
                SaveCurrentGameViewCapture($"market-{width}x{height}");
                Click(390,119);
                Assert.AreEqual(1, viewType.GetField("marketTab", Priv).GetValue(view), "Fridge tab hit on requested ratio");
                yield return null; yield return new WaitForEndOfFrame();
                SaveCurrentGameViewCapture($"heladera-{width}x{height}");
                float maxScroll=(float)viewType.GetMethod("MarketMaxScroll",Priv).Invoke(view,null);
                viewType.GetField("marketScroll",Priv).SetValue(view,maxScroll);
                yield return null; yield return new WaitForEndOfFrame();
                SaveCurrentGameViewCapture($"heladera-botellas-{width}x{height}");
            }
            finally
            {
                int restoreIndex = Mathf.Clamp(previous, 0, count - 1);
                if (selected != null) selected.SetValue(window, restoreIndex, null); else selectedField.SetValue(window, restoreIndex);
                if (addedIndex >= 0)
                    group.GetType().GetMethod("RemoveCustomSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(group, new object[] { addedIndex });
            }
            yield return null; // Let the restored GameView size reach Screen before the next capture.
        }
#endif
    }
}
