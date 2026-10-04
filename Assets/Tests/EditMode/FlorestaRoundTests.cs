using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    // Runtime stays in Assembly-CSharp to preserve existing scene/script references.
    // Tests exercise its actual frame step, not a duplicate simulation.
    public sealed class FlorestaRoundTests
    {
        private GameObject root;
        private Component game;
        private Type controllerType;
        private Type orderType;
        private Type kindType;
        private object balance;
        private UnityEngine.Random.State randomState;
        private const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;

        [SetUp]
        public void SetUp()
        {
            randomState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(731);
            controllerType = Type.GetType("HayChoriYPaty.GameController, Assembly-CSharp", true);
            orderType = Type.GetType("HayChoriYPaty.CustomerOrder, Assembly-CSharp", true);
            kindType = Type.GetType("HayChoriYPaty.OrderKind, Assembly-CSharp", true);
            root = new GameObject("Floresta test (temporary)");
            game = root.AddComponent(controllerType);
            balance = Get("Balance");
            Tune("customersToWin", 99);
            Tune("arrivalIntervalSeconds", 1000f);
            Start();
            Set("nextArrival", 1000f);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Random.state = randomState;
        }

        private object Get(string name) { return controllerType.GetProperty(name).GetValue(game, null); }
        private void Set(string name, object value) { controllerType.GetField(name, Hidden).SetValue(game, value); }
        private void Tune(string name, object value) { balance.GetType().GetField(name).SetValue(balance, value); }
        private object Call(string name) { return controllerType.GetMethod(name).Invoke(game, null); }
        private void Start() { Call("StartRound"); }
        private string Phase { get { return Get("Phase").ToString(); } }
        private IList Queue { get { return (IList)Get("Guests"); } }
        private void Step(float seconds)
        {
            MethodInfo tick = controllerType.GetMethod("AdvanceRound", Hidden);
            int frames = Mathf.CeilToInt(seconds / 0.02f);
            for (int i = 0; i < frames; i++) tick.Invoke(game, new object[] { seconds / frames });
        }
        private object Guest(string kind, float patience = 100f)
        {
            object guest = Activator.CreateInstance(orderType, new object[] { Queue.Count + 1, Enum.Parse(kindType, kind), patience });
            Queue.Add(guest);
            return guest;
        }
        private string State(object guest) { return orderType.GetProperty("State").GetValue(guest, null).ToString(); }

        [Test]
        public void ReenableResetsRoundForNoDomainReloadEditorPlay()
        {
            Set("coins", 100); Call("TryHire"); Guest("Chori");
            controllerType.GetMethod("OnEnable", Hidden).Invoke(game, null);
            Assert.AreEqual("Ready", Phase); Assert.AreEqual(16, Get("Coins"));
            Assert.AreEqual(1, Get("StaffCount")); Assert.AreEqual(0, Queue.Count);
        }

        [Test]
        public void FoodAutomaticallyCooksSeasonsAndPaysExactlyOnce()
        {
            Tune("choriCookSeconds", 1f); Tune("condimentSeconds", 0.4f);
            object guest = Guest("ChoriAndCoca");
            int before = (int)Get("Coins");
            Step(0.1f); Assert.AreEqual("Cooking", State(guest));
            Step(0.95f); Assert.AreEqual("Seasoning", State(guest));
            Step(0.5f);
            Assert.AreEqual(0, Queue.Count); Assert.AreEqual(1, Get("Served"));
            Assert.AreEqual(before + (int)balance.GetType().GetField("coinsPerOrder").GetValue(balance), Get("Coins"));
            Step(3f); Assert.AreEqual(1, Get("Served"));
        }

        [Test]
        public void DrinkBypassesOccupiedGrillAndIsDelivered()
        {
            Tune("choriCookSeconds", 20f); Tune("condimentSeconds", 0.2f);
            Guest("Chori"); Guest("CocaOnly");
            Step(0.5f); Assert.AreEqual(1, Get("Served"));
            Assert.AreEqual(1, Get("CookingCount")); Assert.AreEqual(1, Queue.Count);
        }

        [Test]
        public void ParallelCookingHonorsInspectorCapacity()
        {
            Tune("grillCapacity", 2); Tune("choriCookSeconds", 10f);
            Guest("Chori"); Guest("Chori"); object third = Guest("Chori");
            Step(0.1f); Assert.AreEqual(2, Get("CookingCount")); Assert.AreEqual("Waiting", State(third));
        }

        [Test]
        public void DeparturesReduceCrowdAndClampDefeatAtZero()
        {
            Tune("crowdDeparturePenalty", 70f); Guest("Chori", 0.05f); Guest("Chori", 0.05f);
            Step(0.1f); Assert.AreEqual(2, Get("Departed")); Assert.AreEqual(0f, Get("CrowdPatience"));
            Assert.AreEqual("Lost", Phase); Assert.AreEqual(0, Get("Served"));
        }

        [Test]
        public void FullQueuePressureReducesSharedPatience()
        {
            Tune("crowdPressurePerSecond", 2f); Tune("choriCookSeconds", 30f);
            Guest("Chori"); Guest("Chori"); Guest("Chori");
            Step(1f); Assert.Less((float)Get("CrowdPatience"), 100f); Assert.AreEqual("Playing", Phase);
        }

        [Test]
        public void TimeoutLosesAndStoppedRoundDoesNotAdvance()
        {
            Tune("roundDurationSeconds", 1f); Step(1.1f);
            Assert.AreEqual("Lost", Phase); int coins = (int)Get("Coins");
            Step(3f); Assert.AreEqual(coins, Get("Coins"));
        }

        [Test]
        public void TargetWinsAndRestartClearsOutcomeAndEconomy()
        {
            Tune("customersToWin", 1); Tune("condimentSeconds", 0.1f); Guest("CocaOnly");
            Step(0.4f); Assert.AreEqual("Won", Phase);
            Start(); Assert.AreEqual("Playing", Phase); Assert.AreEqual(0, Get("Served"));
            Assert.AreEqual(0, Queue.Count); Assert.AreEqual(1, Get("StaffCount"));
            Assert.AreEqual(balance.GetType().GetField("startingCoins").GetValue(balance), Get("Coins"));
        }

        [Test]
        public void EarnedCoinsBuySecondWorkerOnceAndIncreaseRate()
        {
            Tune("condimentSeconds", 0.1f); Guest("CocaOnly"); Step(0.4f);
            int before = (int)Get("Coins"); float rate = (float)Get("WorkRate");
            Assert.IsTrue((bool)Call("TryHire")); Assert.AreEqual(before - 30, Get("Coins"));
            Assert.AreEqual(2, Get("StaffCount")); Assert.Greater((float)Get("WorkRate"), rate);
            Assert.IsFalse((bool)Call("TryHire"));
        }

        [Test]
        public void UpgradeDeductsFundsAndHonorsCap()
        {
            Set("coins", 120); float rate = (float)Get("WorkRate");
            for (int i = 0; i < 3; i++) Assert.IsTrue((bool)Call("TryUpgradeSpeed"));
            Assert.AreEqual(48, Get("Coins")); Assert.AreEqual(3, Get("SpeedUpgrades"));
            Assert.Greater((float)Get("WorkRate"), rate); Assert.IsFalse((bool)Call("TryUpgradeSpeed"));
        }

        [Test]
        public void UnaffordableOrFinishedPurchasesAreRejectedWithoutCharge()
        {
            Assert.IsFalse((bool)Call("TryHire")); Assert.IsFalse((bool)Call("TryUpgradeSpeed"));
            Assert.AreEqual(16, Get("Coins"));
            Tune("roundDurationSeconds", 0.1f); Step(0.2f); Set("coins", 100);
            Assert.IsFalse((bool)Call("TryHire")); Assert.IsFalse((bool)Call("TryUpgradeSpeed"));
            Assert.AreEqual(100, Get("Coins"));
        }

        [Test]
        public void StaffAndSpeedImproveMeasuredFoodThroughput()
        {
            Tune("choriCookSeconds", 2f); Tune("condimentSeconds", 0.1f);
            for (int i = 0; i < 6; i++) Guest("Chori");
            Step(8f); int baseline = (int)Get("Served");
            Start(); Set("nextArrival", 1000f); Set("coins", 150);
            Call("TryHire"); Call("TryUpgradeSpeed"); Call("TryUpgradeSpeed");
            for (int i = 0; i < 6; i++) Guest("Chori");
            Step(8f); Assert.Greater((int)Get("Served"), baseline);
        }

        [Test]
        public void DefaultRoundArrivesAndCompletesWithAutomaticPurchases()
        {
            Tune("customersToWin", 12); Tune("arrivalIntervalSeconds", 5.2f);
            Start(); bool sawQueue = false;
            for (int i = 0; i < 1500 && Phase == "Playing"; i++)
            {
                Step(0.1f); sawQueue |= Queue.Count > 0;
                Call("TryHire"); Call("TryUpgradeSpeed");
            }
            Assert.IsTrue(sawQueue); Assert.AreEqual("Won", Phase);
            Assert.AreEqual(12, Get("Served")); Assert.AreEqual(2, Get("StaffCount"));
            Assert.Greater((int)Get("SpeedUpgrades"), 0);
        }
    }
    public sealed class AndroidViewportTests
    {
        private static Rect Viewport(Vector2 size, Rect safe)
        {
            Type view = Type.GetType("HayChoriYPaty.PrototypeView, Assembly-CSharp", true);
            return (Rect)view.GetMethod("CanvasViewport", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { size, safe });
        }

        [Test]
        public void PortraitCanvasKeepsOriginalReferenceLayout()
        {
            Assert.AreEqual(new Rect(0, 0, 1080, 1920), Viewport(new Vector2(1080, 1920), new Rect(0, 0, 1080, 1920)));
        }

        [Test]
        public void TallPhoneExcludesAsymmetricCutoutAndNavigationInsets()
        {
            Rect v = Viewport(new Vector2(1080, 2400), new Rect(0, 80, 1080, 2220));
            Assert.AreEqual(new Rect(0, 250, 1080, 1920), v);
            Vector2 guiButton = v.position + new Vector2(395, 890) * (v.width / 540);
            Vector2 androidTouch = new Vector2(guiButton.x, 2400 - guiButton.y);
            Vector2 roundTrip = (new Vector2(androidTouch.x, 2400 - androidTouch.y) - v.position) / (v.width / 540);
            Assert.AreEqual(new Vector2(395, 890), roundTrip);
        }

        [Test]
        public void SideInsetsAndShortScreensKeepCanvasInsideSafeArea()
        {
            Rect v = Viewport(new Vector2(1200, 1600), new Rect(60, 40, 1080, 1500));
            Assert.That(v.width / v.height, Is.EqualTo(540f / 960f).Within(0.0001f));
            Assert.That(v.xMin, Is.GreaterThanOrEqualTo(60)); Assert.That(v.xMax, Is.LessThanOrEqualTo(1140));
            Assert.That(v.yMin, Is.GreaterThanOrEqualTo(60)); Assert.That(v.yMax, Is.LessThanOrEqualTo(1560));
        }

        [Test]
        public void InvalidSafeAreaFallsBackAndZeroScreenDoesNotDivideByZero()
        {
            Assert.AreEqual(new Rect(0, 0, 540, 960), Viewport(new Vector2(540, 960), new Rect()));
            Assert.AreEqual(new Rect(), Viewport(Vector2.zero, new Rect()));
        }
    }
}
