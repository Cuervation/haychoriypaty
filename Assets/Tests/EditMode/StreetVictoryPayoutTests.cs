using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    /// <summary>Regressions for one-time migration and per-real-victory Mercado payouts.</summary>
    public sealed class StreetVictoryPayoutTests
    {
        private const BindingFlags Public = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags AnyInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        // Concrete content of Logs/Acceptance/EconomyAudit-20261015/phone-save-before.json.
        // Kept inline so tests don't read ignored, machine-local audit output at runtime.
        private const string PhoneSaveBefore = @"{
  ""version"":4,""unlockedLevel"":10,""price"":5.0,
  ""prices"":[5.0,5.0,10.0,12.0,5.0,12.0,7.0],""coins"":0,""staff"":1,""speed"":0,""workerLevel"":2,
  ""parrilleros"":1,""cocacoleros"":0,""premiumParrilleros"":0,""ferneteros"":0,""mozos"":0,
  ""market"":{
    ""balanceCents"":0,""nextPurchaseId"":12,""lastPurchaseId"":11,""cumulativePurchaseUnits"":1115,""cumulativePurchaseSpendCents"":178750,
    ""nextAttemptId"":14,""attemptId"":13,""settledAttemptId"":13,""attemptLevel"":1,""initializedUnlockedLevel"":10,
    ""stockUnits"":[0,0,245,245,150,140,0,165,120,120],
    ""attemptStock"":[200,200,245,245,150,140,200,165,120,120],
    ""highestRecognizedSalesByLevel"":[50000,100000,112500,172500,261000,294000,333250,366250,403500,455500,0],
    ""bonusPaidByLevel"":[true,true,true,true,true,true,true,true,true,true,false],
    ""attemptActive"":false,""kitGranted"":true,""legacyTransitionRepairApplied"":true,""legacyZeroBalanceRecoveryApplied"":true,
    ""lastResult"":{
      ""won"":true,""level"":1,""grossSalesCents"":200000,""leftoverCents"":128500,""halfSalesCents"":100000,
      ""recognizedSalesCents"":0,""bonusCents"":0,""progressionBonusCents"":0,""earnedCents"":0,""totalBalanceCents"":0,
      ""consumedUnits"":[200,200,0,0,0,0,200,0,0,0],""remainingUnits"":[0,0,245,245,150,140,0,165,120,120]
    }
  }
}";

        private static Type Runtime(string name) => Type.GetType("HayChoriYPaty." + name + ", Assembly-CSharp", true);
        private static object Get(object value, string name)
        {
            PropertyInfo property = value.GetType().GetProperty(name, Public);
            if (property != null) return property.GetValue(value, null);
            FieldInfo field = value.GetType().GetField(name, AnyInstance);
            if (field != null) return field.GetValue(value);
            throw new MissingMemberException(value.GetType().FullName, name);
        }
        private static void Put(object value, string name, object fieldValue) =>
            value.GetType().GetField(name, AnyInstance).SetValue(value, fieldValue);
        private static object Call(object value, string name, params object[] args)
        {
            foreach (MethodInfo method in value.GetType().GetMethods(Public))
            {
                if (method.Name != name || method.GetParameters().Length != args.Length) continue;
                ParameterInfo[] parameters = method.GetParameters();
                bool matches = true;
                for (int i = 0; i < args.Length; i++)
                    if (args[i] != null && !parameters[i].ParameterType.IsInstanceOfType(args[i])) { matches = false; break; }
                if (matches) return method.Invoke(value, args);
            }
            throw new MissingMethodException(value.GetType().FullName, name);
        }
        private static object NewMarket(object balance, object data) => Activator.CreateInstance(Runtime("StreetMarket"), new[] { data, balance });
        private static object Result(bool won, int level, long gross, long leftover, long recognized, long bonus, long progression)
        {
            object result = Activator.CreateInstance(Runtime("StreetMarketResult"));
            Put(result, "won", won); Put(result, "level", level); Put(result, "grossSalesCents", gross);
            Put(result, "leftoverCents", leftover); Put(result, "halfSalesCents", gross / 2);
            Put(result, "recognizedSalesCents", recognized); Put(result, "bonusCents", bonus);
            Put(result, "progressionBonusCents", progression); Put(result, "earnedCents", recognized + bonus + progression);
            return result;
        }
        private static object NewVictoryReceipt(long wallet, long recognized, long bonus, long progression)
        {
            object data = Activator.CreateInstance(Runtime("StreetMarketData"));
            Put(data, "balanceCents", wallet);
            Put(data, "attemptId", 13L); Put(data, "settledAttemptId", 13L);
            Put(data, "attemptLevel", 0); Put(data, "initializedUnlockedLevel", 10);
            Put(data, "kitGranted", true);
            Put(data, "cumulativePurchaseSpendCents", 178750L);
            Put(data, "stockUnits", new[] { 0, 0, 245, 245, 150, 140, 0, 165, 120, 120 });
            long[] recognizedBaseline = new long[11]; recognizedBaseline[0] = 100000L;
            Put(data, "highestRecognizedSalesByLevel", recognizedBaseline);
            bool[] paid = new bool[11]; for (int i = 0; i < 10; i++) paid[i] = true;
            Put(data, "bonusPaidByLevel", paid);
            Put(data, "legacyTransitionRepairApplied", true); Put(data, "legacyZeroBalanceRecoveryApplied", true);
            Put(data, "lastResult", Result(true, 0, 200000L, 128500L, recognized, bonus, progression));
            return data;
        }
        private static object NewBalance() => Activator.CreateInstance(Runtime("StreetBalance"));
        private static long Recover(object market) => (long)Call(market, "RecoverLegacyVictoryPayout");

        [Test]
        public void RecoversExactPersistedPhoneVictoryReceiptOnceAndPurchasesOnlyLevelThreeShortfall()
        {
            string key = (string)Runtime("StreetGame").GetField("ProgressKey").GetValue(null);
            bool hadSave = PlayerPrefs.HasKey(key); string oldSave = hadSave ? PlayerPrefs.GetString(key) : null;
            GameObject host = null;
            try
            {
                PlayerPrefs.SetString(key, PhoneSaveBefore); PlayerPrefs.Save();
                host = new GameObject("Phone victory receipt lifecycle recovery");
                Component game = host.AddComponent(Runtime("StreetGame"));
                game.GetType().GetMethod("OnEnable", Private).Invoke(game, null);
                DisablePresentationAudio(game);
                object market = Get(game, "Market");
                Assert.AreEqual(357000L, Get(market, "BalanceCents"), "50% of $2,000 gross plus twice the $1,285 leftover is $3,570.");
                object recoveredData = Get(market, "Data");
                Assert.AreEqual(1115L, Get(recoveredData, "cumulativePurchaseUnits"));
                Assert.AreEqual(178750L, Get(recoveredData, "cumulativePurchaseSpendCents"));
                Assert.AreEqual(12L, Get(recoveredData, "nextPurchaseId")); Assert.AreEqual(11L, Get(recoveredData, "lastPurchaseId"));
                Assert.AreEqual(14L, Get(recoveredData, "nextAttemptId"));
                Assert.AreEqual(13L, Get(recoveredData, "attemptId")); Assert.AreEqual(13L, Get(recoveredData, "settledAttemptId"));
                Assert.AreEqual(1, Get(recoveredData, "attemptLevel")); Assert.AreEqual(10, Get(recoveredData, "initializedUnlockedLevel"));
                CollectionAssert.AreEqual(new[] { 0, 0, 245, 245, 150, 140, 0, 165, 120, 120 }, (int[])Get(recoveredData, "stockUnits"));
                CollectionAssert.AreEqual(new[] { 200, 200, 245, 245, 150, 140, 200, 165, 120, 120 }, (int[])Get(recoveredData, "attemptStock"));
                object receipt = Get(market, "LastResult");
                Assert.AreEqual(100000L, Get(receipt, "recognizedSalesCents"));
                Assert.AreEqual(257000L, Get(receipt, "bonusCents"));
                Assert.AreEqual(357000L, Get(receipt, "earnedCents"));

                Assert.IsTrue((bool)Call(game, "SelectLevel", 2), "The phone save's unlocked level 3 remains accessible through normal navigation.");
                object levelThree = Get(game, "Sim");
                Assert.AreEqual(37500L, Call(market, "MinimumPurchaseCostCents", levelThree));
                long purchaseUnitsBefore = (long)Get(recoveredData, "cumulativePurchaseUnits");
                long purchaseSpendBefore = (long)Get(recoveredData, "cumulativePurchaseSpendCents");
                for (int ingredient = 0; ingredient < 10; ingredient++)
                {
                    int missingPacks = (int)Call(market, "MissingPacks", levelThree, ingredient);
                    if (missingPacks > 0)
                        Assert.IsTrue((bool)Call(game, "TryBuyIngredient", ingredient, missingPacks, (long)Get(market, "NextPurchaseId")),
                            "Exact Level 3 missing packs must be purchasable from the recovered receipt.");
                }
                Assert.IsTrue((bool)Call(market, "HasRequiredStock", levelThree));
                Assert.AreEqual(319500L, Get(market, "BalanceCents"), "Only the $375.00 non-sandwich Level 3 stock shortfall is spent.");
                Assert.AreEqual(37500L, (long)Get(recoveredData, "cumulativePurchaseSpendCents") - purchaseSpendBefore);
                Assert.AreEqual(150L, (long)Get(recoveredData, "cumulativePurchaseUnits") - purchaseUnitsBefore);
                Assert.AreEqual(15L, Get(recoveredData, "nextPurchaseId")); Assert.AreEqual(14L, Get(recoveredData, "lastPurchaseId"));
                int[] stockAfterLevelThree = (int[])((int[])Get(recoveredData, "stockUnits")).Clone();

                UnityEngine.Object.DestroyImmediate(host); host = null;
                host = new GameObject("Reload recovered phone receipt");
                game = host.AddComponent(Runtime("StreetGame"));
                game.GetType().GetMethod("OnEnable", Private).Invoke(game, null);
                DisablePresentationAudio(game);
                market = Get(game, "Market"); object persisted = Get(market, "Data");
                Assert.AreEqual(319500L, Get(market, "BalanceCents"), "The lifecycle must not grant the migrated receipt twice after stock purchases.");
                CollectionAssert.AreEqual(stockAfterLevelThree, (int[])Get(persisted, "stockUnits"));
                Assert.AreEqual(1625L, Get(persisted, "cumulativePurchaseUnits"));
                Assert.AreEqual(261250L, Get(persisted, "cumulativePurchaseSpendCents"));
                Assert.AreEqual(15L, Get(persisted, "nextPurchaseId")); Assert.AreEqual(14L, Get(persisted, "lastPurchaseId"));
                Assert.AreEqual(14L, Get(persisted, "nextAttemptId"));
                Assert.AreEqual(13L, Get(persisted, "attemptId")); Assert.AreEqual(13L, Get(persisted, "settledAttemptId"));
                Assert.AreEqual(1, Get(persisted, "attemptLevel")); Assert.AreEqual(10, Get(persisted, "initializedUnlockedLevel"));
                Assert.AreEqual(1, Get(persisted, "victoryPayoutVersion"));
            }
            finally
            {
                if (host != null) UnityEngine.Object.DestroyImmediate(host);
                if (hadSave) PlayerPrefs.SetString(key, oldSave); else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        // This separate audit starts from legitimate, already-unlocked phone progress; it is not evidence that a fresh campaign reached Level 11.
        [TestCase(1.2f, TestName = "ExistingPhoneProgress_LevelElevenPaidPlay_SpeedFactor1_2")]
        [TestCase(1.5f, TestName = "ExistingPhoneProgress_LevelElevenPaidPlay_SpeedFactor1_5")]
        public void ExistingPhoneProgressCanAuditLevelElevenWithOnlyRecoveredAndEarnedCash(float speedValueMultiplier)
        {
            string key = (string)Runtime("StreetGame").GetField("ProgressKey").GetValue(null);
            bool hadSave = PlayerPrefs.HasKey(key); string oldSave = hadSave ? PlayerPrefs.GetString(key) : null;
            GameObject host = null;
            try
            {
                PlayerPrefs.SetString(key, PhoneSaveBefore); PlayerPrefs.Save();
                host = new GameObject("Existing phone save real Level 11 economy audit");
                Component game = host.AddComponent(Runtime("StreetGame"));
                game.GetType().GetMethod("OnEnable", Private).Invoke(game, null);
                DisablePresentationAudio(game);
                object market = Get(game, "Market");
                Assert.AreEqual(10, Get(game, "UnlockedLevel"), "Use the phone save's actual unlocked progress; do not grant a new unlock.");
                Assert.AreEqual(357000L, Get(market, "BalanceCents"), "Only the receipt migration's authorized payout funds this audit.");
                Assert.IsTrue((bool)Call(game, "SelectLevel", 10), "Select existing unlocked Level 11 via normal navigation.");
                object sim = Get(game, "Sim");
                long beforeRestock = (long)Get(market, "BalanceCents");
                long spendBefore = (long)Get(Get(market, "Data"), "cumulativePurchaseSpendCents");
                long requiredCost = (long)Call(market, "MinimumPurchaseCostCents", sim);
                Assert.AreEqual(186500L, requiredCost, "Buy the exact authentic Level 11 shortfall from the actual catalog.");
                for (int ingredient = 0; ingredient < 10; ingredient++)
                {
                    int missing = (int)Call(market, "MissingPacks", sim, ingredient);
                    if (missing > 0)
                        Assert.IsTrue((bool)Call(game, "TryBuyIngredient", ingredient, missing, (long)Get(market, "NextPurchaseId")),
                            "Purchase only exact missing Level 11 packs; no extra stock or grant.");
                }
                Assert.IsTrue((bool)Call(market, "HasRequiredStock", sim));
                Assert.AreEqual(requiredCost, (long)Get(Get(market, "Data"), "cumulativePurchaseSpendCents") - spendBefore);
                Assert.AreEqual(beforeRestock - requiredCost, Get(market, "BalanceCents"));
                Assert.AreEqual(170500L, Get(market, "BalanceCents"), "The actual phone receipt leaves $1,705 after the $1,865 stock purchase.");
                Assert.IsTrue((bool)Call(game, "TryStartRound"), "The paid and stocked Level 11 must start normally.");

                int openingCoins = (int)Get(sim, "Coins");
                int hireSpend = 0, speedSpend = 0;
                for (int tick = 0; tick < 1600 && Get(sim, "Phase").ToString() == "Playing"; tick++)
                {
                    Call(sim, "Step", .25f);
                    if ((tick & 1) != 0) continue;
                    Tuple<int, int> spends = StreetEconomyCampaignAuditTests.ApplyAffordableFiveRolePolicy(game, sim, 3f, speedValueMultiplier);
                    hireSpend += spends.Item1; speedSpend += spends.Item2;
                }

                string phase = Get(sim, "Phase").ToString();
                Assert.AreEqual(openingCoins + (int)Get(sim, "CoinsEarned") - hireSpend - speedSpend, Get(sim, "Coins"),
                    "The Level 11 attempt must conserve only real opening cash, sales, paid hires, and upgrades.");
                TestContext.WriteLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "Existing phone-save Level 11: {0}, elapsed={1:F1}s, delivered={2}, cash={3}, hires=${4}, speed=${5}, team={6}/{7}/{8}/{9}/{10}, workRate={11:F2}, speedFactor={12:F1}",
                    phase, Get(sim, "Elapsed"), Get(sim, "Delivered"), Get(sim, "Coins"), hireSpend, speedSpend,
                    Get(sim, "ParrilleroCount"), Get(sim, "ParrilleroPremiumCount"), Get(sim, "CocacoleroCount"),
                    Get(sim, "FerneteroCount"), Get(sim, "MozoCount"), Get(sim, "WorkRate"), speedValueMultiplier));
                for (int product = 0; product < 7; product++)
                    if ((bool)Call(sim, "IsProductAvailable", product))
                        TestContext.WriteLine("  product " + product + ": sold=" + Call(sim, "GetProductDelivered", product) +
                            ", remaining=" + Call(sim, "GetProductRemaining", product));

                Assert.IsTrue((bool)Call(game, "ProcessRoundResult"), "Settle this real terminal gameplay result through StreetGame.");
                if ((bool)Get(game, "HasPendingChulengo")) Assert.IsTrue((bool)Call(game, "TryCollectVictoryReward"), "Explicit normal reward claim preserves legacy payout assertions.");
                Assert.AreEqual("Won", Get(sim, "Phase").ToString(),
                    "This is an audit of existing phone progress, not proof that a fresh campaign reached Level 11; preserve a real loss as a failure.");
            }
            finally
            {
                if (host != null) UnityEngine.Object.DestroyImmediate(host);
                if (hadSave) PlayerPrefs.SetString(key, oldSave); else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        [TestCase(100000L, 0L, 0L, 257000L, TestName = "RecoveryAddsOnlyMissingDoubleLeftover")]
        [TestCase(0L, 257000L, 1000L, 100000L, TestName = "RecoveryPreservesEarlierProgressionReward")]
        public void PartialLegacyReceiptAddsOnlyItsMissingVictoryComponent(long recognized, long bonus, long progression, long missing)
        {
            object balance = NewBalance(), data = NewVictoryReceipt(progression, recognized, bonus, progression), market = NewMarket(balance, data);
            Assert.AreEqual(missing, Recover(market));
            Assert.AreEqual(progression + missing, Get(market, "BalanceCents"));
            object receipt = Get(Get(market, "Data"), "lastResult");
            Assert.AreEqual(100000L, Get(receipt, "recognizedSalesCents"));
            Assert.AreEqual(257000L, Get(receipt, "bonusCents"));
            Assert.AreEqual(progression, Get(receipt, "progressionBonusCents"));
            Assert.AreEqual(357000L + progression, Get(receipt, "earnedCents"));
        }

        [Test]
        public void FullyPaidLegacyReceiptIsMarkedWithoutDoubleCredit()
        {
            object balance = NewBalance(), data = NewVictoryReceipt(357000L, 100000L, 257000L, 0L), market = NewMarket(balance, data);
            Assert.AreEqual(0L, Recover(market));
            Assert.AreEqual(357000L, Get(market, "BalanceCents"));
            Assert.AreEqual(1, Get(Get(market, "Data"), "victoryPayoutVersion"));
        }

        [Test]
        public void ActiveAttemptDefersRecoveryWithoutChangingReceiptVersion()
        {
            object balance = NewBalance(), data = NewVictoryReceipt(0, 0, 0, 0);
            Put(data, "attemptActive", true);
            object market = NewMarket(balance, data);
            Assert.AreEqual(0L, Recover(market));
            Assert.AreEqual(0L, Get(market, "BalanceCents")); Assert.AreEqual(0, Get(data, "victoryPayoutVersion"));
        }

        [Test]
        public void LossAndInvalidReceiptNeverCreateVictoryMoney()
        {
            object balance = NewBalance(), loss = NewVictoryReceipt(0, 0, 0, 0);
            Put(loss, "lastResult", Result(false, 0, 200000L, 128500L, 0, 0, 0));
            object lossMarket = NewMarket(balance, loss);
            Assert.AreEqual(0L, Recover(lossMarket)); Assert.AreEqual(0L, Get(lossMarket, "BalanceCents"));
            Assert.AreEqual(1, Get(loss, "victoryPayoutVersion"), "A settled non-victory is not retried as a legacy victory.");

            object invalid = NewVictoryReceipt(0, 0, 0, 0);
            Put(invalid, "settledAttemptId", 12L);
            object invalidMarket = NewMarket(balance, invalid);
            Assert.AreEqual(0L, Recover(invalidMarket)); Assert.AreEqual(0L, Get(invalidMarket, "BalanceCents"));
            Assert.AreEqual(0, Get(invalid, "victoryPayoutVersion"), "An invalid receipt remains available for diagnosis, not marked migrated.");
        }

        [Test]
        public void OverflowingRecoveryIsRejectedWithoutPartialMutation()
        {
            object balance = NewBalance(), data = NewVictoryReceipt(long.MaxValue - 1L, 0, 0, 0), market = NewMarket(balance, data);
            object before = JsonUtility.ToJson(data);
            Assert.AreEqual(0L, Recover(market));
            Assert.AreEqual(before, JsonUtility.ToJson(data), "Balance and receipt must remain byte-for-byte unchanged on overflow.");
            Assert.AreEqual(0, Get(data, "victoryPayoutVersion"));
        }

        [Test]
        public void TwoRealLevelOneVictoriesEachPayFullFormulaAndSettlementIsAttemptIdempotent()
        {
            string key = (string)Runtime("StreetGame").GetField("ProgressKey").GetValue(null);
            bool hadSave = PlayerPrefs.HasKey(key); string oldSave = hadSave ? PlayerPrefs.GetString(key) : null;
            GameObject host = null;
            try
            {
                PlayerPrefs.DeleteKey(key); PlayerPrefs.Save();
                host = new GameObject("Real repeated Level 1 payout regression");
                Component game = host.AddComponent(Runtime("StreetGame"));
                game.GetType().GetMethod("OnEnable", Private).Invoke(game, null);
                DisablePresentationAudio(game);
                var playerPolicy = new StreetSimulationTests(); playerPolicy.SetUp();
                MethodInfo applyPolicy = typeof(StreetSimulationTests).GetMethod("ApplyStrategyPurchases", Private);
                Assert.IsNotNull(applyPolicy);

                long firstAttempt = RunLevelOneVictory(game, playerPolicy, applyPolicy);
                object market = Get(game, "Market");
                object firstSim = Get(game, "Sim");
                object firstResult = Get(market, "LastResult");
                AssertFullPayout(firstResult);
                long cashAfterFirst = (long)Get(market, "BalanceCents");
                Assert.IsNotNull(Call(market, "SettleAttempt", Get(game, "Sim")), "Repeated settlement should return the existing receipt.");
                Assert.AreEqual(cashAfterFirst, Get(market, "BalanceCents"));

                Assert.IsTrue((bool)Call(game, "SelectLevel", 0), "Unlocked Level 1 can be replayed without resetting the campaign save.");
                Assert.IsTrue((bool)Call(game, "TryStartRound"));
                long cashBeforeStaleSettlement = (long)Get(market, "BalanceCents");
                Assert.IsNotNull(Call(market, "SettleAttempt", firstSim), "A stale completed simulation may only return the existing receipt.");
                Assert.AreEqual(cashBeforeStaleSettlement, Get(market, "BalanceCents"), "A stale victory callback cannot settle or pay the active newer attempt.");
                Assert.IsTrue((bool)Get(Get(market, "Data"), "attemptActive"), "The current replay remains active after a stale callback.");
                long secondAttempt = RunLevelOneVictory(game, playerPolicy, applyPolicy);
                Assert.Greater(secondAttempt, firstAttempt);
                object secondResult = Get(Get(game, "Market"), "LastResult");
                AssertFullPayout(secondResult);
                Assert.GreaterOrEqual((long)Get(Get(game, "Market"), "BalanceCents"), cashAfterFirst,
                    "A previously paid level's replay must still post its own per-attempt reward.");
            }
            finally
            {
                if (host != null) UnityEngine.Object.DestroyImmediate(host);
                if (hadSave) PlayerPrefs.SetString(key, oldSave); else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        private static long RunLevelOneVictory(object game, StreetSimulationTests strategy, MethodInfo applyPolicy)
        {
            object sim = Get(game, "Sim"), market = Get(game, "Market");
            if (Get(sim, "Phase").ToString() == "Ready") Assert.IsTrue((bool)Call(game, "TryStartRound"));
            long attempt = (long)Get(Get(market, "Data"), "attemptId");
            for (int tick = 0; tick < 1300 && Get(sim, "Phase").ToString() == "Playing"; tick++)
            {
                Call(sim, "Step", .25f);
                applyPolicy.Invoke(strategy, new[] { sim, "D" });
            }
            TestContext.WriteLine("Played Level 1: " + Get(sim, "Phase") + ", sales=" + Get(sim, "Delivered") +
                ", time=" + ((float)Get(sim, "Elapsed")).ToString("F1") + "s");
            Assert.AreEqual("Won", Get(sim, "Phase").ToString(), "A real D-policy playthrough must win without forcing phase, quota or time.");
            Assert.IsTrue((bool)Call(game, "ProcessRoundResult"));
                if ((bool)Get(game, "HasPendingChulengo")) Assert.IsTrue((bool)Call(game, "TryCollectVictoryReward"), "Explicit normal reward claim preserves legacy payout assertions.");
            Assert.AreEqual(attempt, Get(Get(Get(game, "Market"), "Data"), "settledAttemptId"));
            return attempt;
        }

        private static void DisablePresentationAudio(Component game)
        {
            Behaviour audioCue = game.GetComponent(Runtime("OrderDeliverySound")) as Behaviour;
            if (audioCue != null)
            {
                Call(audioCue, "Initialize", (object)null); // Explicitly detach; EditMode doesn't reliably dispatch OnDisable.
                audioCue.enabled = false; // Real economy simulation, no audio playback in EditMode.
            }
        }

        private static void AssertFullPayout(object result)
        {
            long half = (long)Get(result, "grossSalesCents") / 2L;
            long doubleLeftover = (long)Get(result, "leftoverCents") * 2L;
            Assert.AreEqual(half, Get(result, "recognizedSalesCents"));
            Assert.AreEqual(doubleLeftover, Get(result, "bonusCents"));
            Assert.AreEqual(half + doubleLeftover + (long)Get(result, "progressionBonusCents"), Get(result, "earnedCents"));
        }
    }
}
