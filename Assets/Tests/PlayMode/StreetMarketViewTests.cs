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
    /// <summary>Exercises Mercado through StreetView's pointer and dispatch paths.</summary>
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
        public IEnumerator MarketEntryAndQuantitySelectionRequireExplicitDebouncedPurchase()
        {
            viewType.GetField("levelSelectActive", Priv).SetValue(view, true);
            Dispatch(161); // The selector's single Mercado button.
            Assert.IsTrue((bool)viewType.GetField("marketActive", Priv).GetValue(view));
            Assert.IsNull(viewType.GetField("marketTab", Priv), "There is no separate stock-screen tab.");
            Assert.AreEqual(0, (int)viewType.GetMethod("HitMarketAction", Priv).Invoke(view, new object[] { new Vector2(462, 35) }),
                "The old HELADERA header button is gone.");

            // The batch PlayMode host is landscape 640x480, unlike the portrait product layout.
            // Exercise the actual logical market geometry at a 9:16 height instead of letting the
            // test host clip the expanded row before the purchase control exists on screen.
            viewType.GetField("logicalCanvasHeight", Priv).SetValue(view, 960f);
            int before = (int)Market.GetType().GetMethod("Units", Pub).Invoke(Market, new object[] { 0 });
            Rect firstRow = (Rect)viewType.GetMethod("MarketRowBounds", Priv).Invoke(view, new object[] { 0 });
            MethodInfo control = viewType.GetMethod("MarketPurchaseControlBounds", PrivStatic);
            MethodInfo hitMarket = viewType.GetMethod("HitMarketAction", Priv);
            Rect plus = (Rect)control.Invoke(null, new object[] { firstRow, 100 });
            int plusAction = (int)hitMarket.Invoke(view, new object[] { plus.center });
            Assert.AreEqual(100, plusAction, "The visible + control changes pending quantity only.");
            Dispatch(plusAction);
            Assert.AreEqual(1, ((int[])viewType.GetField("marketPendingPacks", Priv).GetValue(view))[0]);
            Assert.AreEqual(before, (int)Market.GetType().GetMethod("Units", Pub).Invoke(Market, new object[] { 0 }));
            Rect buy = (Rect)control.Invoke(null, new object[] { firstRow, 140 });
            int buyAction = (int)hitMarket.Invoke(view, new object[] { buy.center });
            Assert.AreEqual(140, buyAction, "Buying requires a separate explicit confirmation.");
            Dispatch(buyAction);
            Dispatch(buyAction); // An immediate duplicate confirmation is debounced.
            Assert.AreEqual(before + 1, (int)Market.GetType().GetMethod("Units", Pub).Invoke(Market, new object[] { 0 }));
            yield return null;
        }

        [UnityTest]
        public IEnumerator MarketAllowsExtraStockAfterGoalAndPersistsItWithoutCredit()
        {
            Assert.IsTrue((bool)gameType.GetMethod("SelectLevel", Pub).Invoke(game, new object[] { 1 }));
            viewType.GetField("marketActive", Priv).SetValue(view, true);
            object data = Market.GetType().GetProperty("Data", Pub).GetValue(Market);
            int[] stock = (int[])data.GetType().GetField("stockUnits").GetValue(data);
            stock[0] = 0;
            int goal = ((int[])Market.GetType().GetMethod("RequiredUnits", Pub).Invoke(Market, new[] { Sim }))[0];
            Assert.AreEqual(200, goal);
            Dispatch(180);
            for (int i=0;i<20;i++) Dispatch(120);
            Assert.AreEqual(200, ((int[])viewType.GetField("marketPendingPacks", Priv).GetValue(view))[0], "No arbitrary99-pack ceiling.");
            Dispatch(140);
            Assert.AreEqual(goal, stock[0]);
            Assert.AreEqual(0, viewType.GetMethod("MarketExpandedId", Priv).Invoke(view, null), "Keep buying after covering the goal.");
            viewType.GetField("marketLastPurchaseTime", Priv).SetValue(view, -100f);
            Dispatch(120); Dispatch(140); Dispatch(140);
            Assert.AreEqual(goal + 10, stock[0], "Extra stock is bought once, not capped at the objective.");
            string key = (string)gameType.GetField("ProgressKey", PubStatic).GetValue(null);
            object saved = JsonUtility.FromJson(PlayerPrefs.GetString(key), gameType.GetNestedType("SaveData", BindingFlags.Public));
            object savedMarket = saved.GetType().GetField("market").GetValue(saved);
            Assert.AreEqual(goal+10, ((int[])savedMarket.GetType().GetField("stockUnits").GetValue(savedMarket))[0]);
            data.GetType().GetField("balanceCents").SetValue(data, 0L);
            viewType.GetField("marketLastPurchaseTime", Priv).SetValue(view, -100f);
            Dispatch(120); Dispatch(140);
            Assert.AreEqual(goal+10, stock[0], "Extra stock never bypasses the cash-only guard.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayIsStockGatedCashOnlyRestockAndRetryIsGone()
        {
            Assert.IsTrue((bool)gameType.GetMethod("SelectLevel", Pub).Invoke(game, new object[] { 1 }),
                "This test covers a later level; Level 1 has its separate bounded starter stock floor.");
            viewType.GetField("marketActive", Priv).SetValue(view, true);
            object data = Market.GetType().GetProperty("Data", Pub).GetValue(Market, null);
            data.GetType().GetField("balanceCents", Pub).SetValue(data, 150000L); // Level 2's Chori+Coca minimum costs $1,000.
            Array.Clear((int[])data.GetType().GetField("stockUnits", Pub).GetValue(data), 0, 10);
            Assert.IsFalse((bool)GetGame("CanStartRound"));
            Dispatch(160);
            Assert.AreEqual("Ready", Sim.GetType().GetProperty("Phase", Pub).GetValue(Sim, null).ToString());
            FieldInfo balanceField = data.GetType().GetField("balanceCents", Pub);
            long available = (long)balanceField.GetValue(data);
            balanceField.SetValue(data, 0L);
            for (int id = 0; id < 10; id++)
            {
                int missing = (int)Market.GetType().GetMethod("MissingPacks", Pub).Invoke(Market, new[] { Sim, (object)id });
                if (missing > 0) Assert.IsFalse((bool)gameType.GetMethod("BuyMissingIngredient", Pub).Invoke(game, new object[] { id }), "No cash means no purchase.");
            }
            Assert.AreEqual(0L, balanceField.GetValue(data));
            Assert.IsFalse((bool)GetGame("CanStartRound"));
            Assert.IsFalse((bool)viewType.GetMethod("MarketCanBuyMissing", Priv).Invoke(view, null));
            string blockedSummary = (string)viewType.GetMethod("MarketEconomyFooterSummary", Priv).Invoke(view, null);
            StringAssert.Contains("FALTANTES", blockedSummary);
            StringAssert.Contains("SALDO", blockedSummary);
            StringAssert.DoesNotContain("FIADO", blockedSummary);
            balanceField.SetValue(data, available);
            Assert.IsTrue((bool)viewType.GetMethod("MarketCanBuyMissing", Priv).Invoke(view, null));
            string affordableSummary = (string)viewType.GetMethod("MarketEconomyFooterSummary", Priv).Invoke(view, null);
            StringAssert.Contains("SALDO DESPUÉS", affordableSummary);
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

        [Test]
        public void MarketObjectiveCardsFitOneOrTwoRowsAndFollowLiveLevelData()
        {
            MethodInfo rowCount = viewType.GetMethod("MarketObjectiveRowCount", PrivStatic);
            MethodInfo rowItems = viewType.GetMethod("MarketObjectiveItemsInRow", PrivStatic);
            MethodInfo cardBounds = viewType.GetMethod("MarketObjectiveCardBounds", PrivStatic);
            MethodInfo bodyBounds = viewType.GetMethod("MarketBodyBounds", PrivStatic, null, new[] { typeof(int), typeof(float) }, null);
            MethodInfo footerBounds = viewType.GetMethod("MarketEconomyFooterBounds", PrivStatic);
            MethodInfo playBounds = viewType.GetMethod("MarketPlayBounds", Priv);
            FieldInfo logicalHeight = viewType.GetField("logicalCanvasHeight", Priv);
            float previousLogicalHeight = (float)logicalHeight.GetValue(view);
            int[] firstRowExpected = { 1, 2, 3, 2, 3, 3, 4 };
            float[] heights = { 960f, 1170f, 1200f }; // 16:9, 19.5:9, 20:9 at 540 logical width.
            for (int count = 1; count <= 7; count++)
            {
                int expectedRows = count <= 3 ? 1 : 2;
                Assert.AreEqual(expectedRows, rowCount.Invoke(null, new object[] { count }), "row count for " + count);
                Assert.AreEqual(firstRowExpected[count - 1], rowItems.Invoke(null, new object[] { count, 0 }), "first row for " + count);
                if (expectedRows == 2)
                    Assert.AreEqual(count - firstRowExpected[count - 1], rowItems.Invoke(null, new object[] { count, 1 }), "second row for " + count);

                Rect[] cards = new Rect[count];
                int secondRowCount = 0;
                for (int slot = 0; slot < count; slot++)
                {
                    cards[slot] = (Rect)cardBounds.Invoke(null, new object[] { slot, count });
                    Assert.Greater(cards[slot].width, 0f);
                    Assert.Greater(cards[slot].height, 0f);
                    Assert.GreaterOrEqual(cards[slot].xMin, 26f);
                    Assert.LessOrEqual(cards[slot].xMax, 514f);
                    if (count > 3 && cards[slot].y > cards[0].y) secondRowCount++;
                    for (int prior = 0; prior < slot; prior++)
                        Assert.IsFalse(cards[slot].Overlaps(cards[prior]), "overlap for " + count + " goals");
                }
                Assert.AreEqual(expectedRows == 1 ? 0 : count - firstRowExpected[count - 1], secondRowCount,
                    "the layout must use no more than two rows for " + count + " goals");

                for (int h = 0; h < heights.Length; h++)
                {
                    Rect body = (Rect)bodyBounds.Invoke(null, new object[] { count, heights[h] });
                    Rect footer = (Rect)footerBounds.Invoke(null, new object[] { heights[h] });
                    logicalHeight.SetValue(view, heights[h]);
                    Rect play = (Rect)playBounds.Invoke(view, null);
                    Assert.GreaterOrEqual(body.height, 140f, count + " goals at height " + heights[h]);
                    Assert.LessOrEqual(body.yMax + 26f, footer.y, "list/status/footer overlap at height " + heights[h]);
                    Assert.Less(footer.yMax, play.y, "fixed footer summary overlaps primary play action");
                    Assert.LessOrEqual(play.yMax, heights[h], "play action must remain on screen");
                    Assert.Less(cards[cards.Length - 1].yMax, body.y, "goal cards overlap ingredient list");
                }
            }
            logicalHeight.SetValue(view, previousLogicalHeight);

            MethodInfo select = gameType.GetMethod("SelectLevel", Pub);
            Type simType = Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true);
            FieldInfo names = simType.GetField("ProductNames", PubStatic);
            string[] productNames = (string[])names.GetValue(null);
            MethodInfo caption = viewType.GetMethod("MarketObjectiveCaption", PrivStatic);
            int[] levelIndices = { 1, 3, 4, 9 }; // Chicago, Ferro, first seven-goal level, Camioneros.
            int[] expectedCounts = { 2, 5, 7, 7 };
            for (int i = 0; i < levelIndices.Length; i++)
            {
                Assert.IsTrue((bool)select.Invoke(game, new object[] { levelIndices[i] }));
                object sim = Sim;
                Assert.AreEqual(expectedCounts[i], sim.GetType().GetProperty("ProductCount", Pub).GetValue(sim, null));
                int productCount = (int)sim.GetType().GetProperty("ProductCount", Pub).GetValue(sim, null);
                for (int slot = 0; slot < productCount; slot++)
                {
                    int product = (int)sim.GetType().GetMethod("GetAvailableProduct", Pub).Invoke(sim, new object[] { slot });
                    int goal = (int)sim.GetType().GetMethod("GetProductGoal", Pub).Invoke(sim, new object[] { product });
                    string fullName = productNames[product];
                    int split = fullName.IndexOf(' ');
                    string expectedName = split > 0 ? fullName.Substring(0, split) : fullName;
                    Assert.AreEqual(expectedName + " ×" + goal,
                        caption.Invoke(null, new object[] { sim, slot, productCount >= 5 }),
                        "card caption preserves the selected level's actual product order/goal");
                }
            }
        }

        [UnityTest]
        public IEnumerator DraggingIngredientListDoesNotConfirmAPurchase()
        {
            Assert.IsTrue((bool)gameType.GetMethod("SelectLevel", Pub).Invoke(game, new object[] { 9 })); // Seven-product list actually requires scrolling.
            viewType.GetField("marketActive",Priv).SetValue(view,true);
            Dispatch(100);
            long op=(long)Market.GetType().GetProperty("NextPurchaseId",Pub).GetValue(Market,null);
            MethodInfo pointer=viewType.GetMethod("HandlePointer",Priv);
            Rect body=(Rect)viewType.GetMethod("MarketBodyBounds",Priv).Invoke(view,null);
            pointer.Invoke(view,new object[]{Pixel(290,body.y+147f),true,false});
            pointer.Invoke(view,new object[]{Pixel(290,body.y-13f),false,false});
            pointer.Invoke(view,new object[]{Pixel(290,body.y-13f),false,true});
            Assert.Greater((float)viewType.GetField("marketScroll",Priv).GetValue(view),0f);
            Assert.AreEqual(op,Market.GetType().GetProperty("NextPurchaseId",Pub).GetValue(Market,null));
            yield return null;
        }

        [UnityTest]
        public IEnumerator FirstLevelMarketDoesNotUnlockPurchasing()
        {
            object save=gameType.GetField("save",Priv).GetValue(game);
            save.GetType().GetField("unlockedLevel").SetValue(save,0);
            viewType.GetField("levelSelectActive",Priv).SetValue(view,true);
            Dispatch(161);
            Assert.IsTrue((bool)viewType.GetField("marketActive",Priv).GetValue(view));
            Assert.IsFalse((bool)gameType.GetMethod("TryBuyIngredient",Pub).Invoke(game,new object[]{0,1,0L}));
            Assert.AreEqual("Ready",Sim.GetType().GetProperty("Phase",Pub).GetValue(Sim,null).ToString());
            yield return null;
        }

        [Test]
        public void MercadoShowsRequiredRowsAndExpandableUnlockedOtherIngredients()
        {
            viewType.GetField("logicalCanvasHeight", Priv).SetValue(view, 960f);
            Assert.IsTrue((bool)gameType.GetMethod("SelectLevel", Pub).Invoke(game, new object[] { 0 }));
            MethodInfo visible = viewType.GetMethod("MarketVisibleIngredients", Priv);
            CollectionAssert.AreEqual(new[] { 0, 1 }, (int[])visible.Invoke(view, null), "Level 1 initially shows only Chorizo and Pan común.");
            Assert.IsTrue((bool)viewType.GetField("marketOtherExpanded", Priv).GetValue(view) == false);
            Assert.AreEqual(0, ((int[])viewType.GetMethod("MarketIngredientIds", Priv).Invoke(view, new object[] { false })).Length,
                "All Boys only sells the two products required for its opening match.");
            viewType.GetField("marketActive", Priv).SetValue(view, true);
            Rect hiddenOthers = (Rect)viewType.GetMethod("MarketOthersBounds", Priv).Invoke(view, null);
            Assert.AreEqual(0, (int)viewType.GetMethod("HitMarketAction", Priv).Invoke(view, new object[] { hiddenOthers.center }),
                "There is no invisible hit target where the optional section would have been.");
            Dispatch(190); // A stale/direct action cannot open a section that is absent.
            Assert.IsFalse((bool)viewType.GetField("marketOtherExpanded", Priv).GetValue(view));
            CollectionAssert.AreEqual(new[] { 0, 1 }, (int[])visible.Invoke(view, null));
            Assert.AreEqual((float)viewType.GetMethod("MarketRequiredContentHeight", Priv).Invoke(view, null),
                (float)viewType.GetMethod("MarketContentHeight", Priv).Invoke(view, null),
                "Level 1 allocates no vertical space to the absent optional section.");
            viewType.GetField("marketExpandedIngredient", Priv).SetValue(view, 0);
            MethodInfo rowBounds = viewType.GetMethod("MarketRowBounds", Priv);
            Rect expanded = (Rect)rowBounds.Invoke(view, new object[] { 0 });
            Rect compact = (Rect)rowBounds.Invoke(view, new object[] { 1 });
            Assert.AreEqual(168f, expanded.height);
            Assert.AreEqual(104f, compact.height);
            Assert.AreEqual(expanded.yMax, compact.y);
            var controls = new Rect[5];
            int[] actions = { 80, 100, 120, 140, 170 };
            for (int i = 0; i < actions.Length; i++)
            {
                controls[i] = (Rect)viewType.GetMethod("MarketPurchaseControlBounds", PrivStatic).Invoke(null, new object[] { expanded, actions[i] });
                Assert.GreaterOrEqual(controls[i].width, 40f); Assert.GreaterOrEqual(controls[i].height, 44f);
                Assert.IsTrue(expanded.Contains(controls[i].min)); Assert.IsTrue(expanded.Contains(controls[i].max - Vector2.one));
                for (int j = 0; j < i; j++) Assert.IsFalse(controls[i].Overlaps(controls[j]));
            }
            long before = (long)Market.GetType().GetProperty("BalanceCents", Pub).GetValue(Market, null);
            viewType.GetField("marketActive", Priv).SetValue(view, true);
            Dispatch(181); // Expand bread, never purchase or change stock/balance.
            Assert.AreEqual(1, viewType.GetMethod("MarketExpandedId", Priv).Invoke(view, null));
            Assert.AreEqual(before, Market.GetType().GetProperty("BalanceCents", Pub).GetValue(Market, null));
            CollectionAssert.AreEqual(new[] { 0, 1 }, (int[])visible.Invoke(view, null));
            Assert.IsTrue((bool)gameType.GetMethod("SelectLevel", Pub).Invoke(game, new object[] { 1 }));
            CollectionAssert.AreEqual(new[] { 0, 1, 6 }, (int[])visible.Invoke(view, null), "Next level automatically includes Coca only because its current goals require it.");
            Assert.AreEqual(7, ((int[])viewType.GetMethod("MarketIngredientIds", Priv).Invoke(view, new object[] { false })).Length,
                "Later levels retain unlocked optional products.");
            Assert.IsFalse((bool)viewType.GetField("marketOtherExpanded", Priv).GetValue(view), "The optional section starts collapsed in later levels.");
            Rect levelTwoOthers = (Rect)viewType.GetMethod("MarketOthersBounds", Priv).Invoke(view, null);
            Assert.AreEqual(190, (int)viewType.GetMethod("HitMarketAction", Priv).Invoke(view, new object[] { levelTwoOthers.center }));
            Dispatch(190);
            int[] expandedList = (int[])visible.Invoke(view, null);
            Assert.AreEqual(10, expandedList.Length);
            Assert.AreEqual(10, expandedList.Distinct().Count());
            Dispatch(190); // Collapse the later-level optional section.
            object save = gameType.GetField("save", Priv).GetValue(game);
            save.GetType().GetField("unlockedLevel").SetValue(save, 1);
            viewType.GetField("marketOtherExpanded", Priv).SetValue(view, true);
            CollectionAssert.AreEqual(new[] { 0, 1, 6 }, (int[])visible.Invoke(view, null), "Still-locked products must not appear in the optional group.");
            viewType.GetField("marketExpandedIngredient", Priv).SetValue(view, 8);
            Assert.AreNotEqual(8, viewType.GetMethod("MarketExpandedId", Priv).Invoke(view, null), "Locked ingredients cannot expose purchase controls.");
            Assert.IsFalse((bool)gameType.GetMethod("TryBuyIngredient", Pub).Invoke(game, new object[] { 8, 1, 0L }));
            Assert.AreEqual(before, Market.GetType().GetProperty("BalanceCents", Pub).GetValue(Market, null));
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
        public IEnumerator CaptureRoundedBlueMarket()
        {
            if (Application.isBatchMode) Assert.Ignore("Requires interactive Game View.");
            Assert.IsTrue((bool)gameType.GetMethod("SelectLevel", Pub).Invoke(game, new object[] { 1 }));
            viewType.GetField("marketActive", Priv).SetValue(view, true);
            viewType.GetField("levelSelectActive", Priv).SetValue(view, false);
            viewType.GetMethod("MarketRevealIngredient", Priv).Invoke(view, new object[] { 0 });
            yield return null; yield return null; yield return new WaitForEndOfFrame();
            var capture = ScreenCapture.CaptureScreenshotAsTexture();
            try { File.WriteAllBytes("/private/tmp/hcyp-market-rounded.png", capture.EncodeToPNG()); }
            finally { UnityEngine.Object.Destroy(capture); }
        }

        [UnityTest]
        public IEnumerator CaptureUnifiedMarketLevelsAtPortraitGameViewSizes()
        {
            viewType.GetField("marketActive", Priv).SetValue(view, true);
            viewType.GetField("levelSelectActive", Priv).SetValue(view, false);
            int[] levels = { 1, 3, 9 };
            int[,] sizes = { { 540, 960 }, { 540, 1170 }, { 540, 1200 } };
            foreach (int level in levels)
            {
                object save = gameType.GetField("save", Priv).GetValue(game);
                save.GetType().GetField("unlockedLevel").SetValue(save, 10);
                Assert.IsTrue((bool)gameType.GetMethod("SelectLevel", Pub).Invoke(game, new object[] { level }));
                object data = Market.GetType().GetProperty("Data", Pub).GetValue(Market, null);
                int[] stock = (int[])data.GetType().GetField("stockUnits").GetValue(data);
                int[] needed = (int[])Market.GetType().GetMethod("RequiredUnits", Pub).Invoke(Market, new[] { Sim });
                int shortage = 6;
                for (int id = 0; id < stock.Length; id++) stock[id] = needed[id] > 0 ? needed[id] + 45 : 0;
                stock[shortage] = Mathf.Max(0, needed[shortage] - 55);
                int missingPacks = (int)Market.GetType().GetMethod("MissingPacks", Pub).Invoke(Market, new[] { Sim, (object)shortage });
                ((int[])viewType.GetField("marketPendingPacks", Priv).GetValue(view))[shortage] = missingPacks;
                viewType.GetField("marketExpandedIngredient", Priv).SetValue(view, -1);
                for (int size = 0; size < sizes.GetLength(0); size++)
                    yield return CaptureGameViewSize(sizes[size, 0], sizes[size, 1]);
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
                Rect body=(Rect)viewType.GetMethod("MarketBodyBounds", Priv).Invoke(view,null);
                Assert.LessOrEqual(body.yMax, Screen.height);
                yield return null; yield return new WaitForEndOfFrame();
                int level = (int)Sim.GetType().GetProperty("LevelIndex", Pub).GetValue(Sim, null) + 1;
                SaveCurrentGameViewCapture($"mercado-nivel-{level:00}-{width}x{height}");
                if (height == 1200)
                {
                    int[] others = (int[])viewType.GetMethod("MarketIngredientIds", Priv).Invoke(view, new object[] { false });
                    if (others.Length > 0)
                    {
                        viewType.GetField("marketOtherExpanded", Priv).SetValue(view, true);
                        yield return null; yield return new WaitForEndOfFrame();
                        SaveCurrentGameViewCapture($"mercado-otros-nivel-{level:00}-{width}x{height}");
                        viewType.GetField("marketScroll", Priv).SetValue(view,
                            viewType.GetMethod("MarketMaxScroll", Priv).Invoke(view, null));
                        yield return null; yield return new WaitForEndOfFrame();
                        SaveCurrentGameViewCapture($"mercado-otros-final-nivel-{level:00}-{width}x{height}");
                        viewType.GetField("marketScroll", Priv).SetValue(view, 0f);
                        viewType.GetField("marketOtherExpanded", Priv).SetValue(view, false);
                    }
                    viewType.GetMethod("MarketRevealIngredient", Priv).Invoke(view, new object[] { 6 });
                    yield return null; yield return new WaitForEndOfFrame();
                    SaveCurrentGameViewCapture($"mercado-compra-nivel-{level:00}-{width}x{height}");
                }
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
