using System;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    public sealed class TowerDefenseGameModeTests
    {
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;
        private Type configType, routeType, geometryType, modeType, balanceType;
        private ScriptableObject config;
        private object balance, mode;
        private object capturedDelivery;

        [SetUp]
        public void SetUp()
        {
            configType = RuntimeType("TowerDefenseLevelConfig");
            routeType = RuntimeType("TowerDefenseRouteGeometryConfig");
            geometryType = RuntimeType("TowerDefenseRouteGeometry");
            modeType = RuntimeType("TowerDefenseGameMode");
            balanceType = RuntimeType("StreetBalance");
            config = ScriptableObject.CreateInstance(configType);
            // Synthetic domain fixtures use the legacy-sized baseline unless a test
            // explicitly loads the live per-level Resources configuration.
            Set(config, "totalFanCount", 60);
            Set(config, "fanSpeed", .1575f);
            balance = Activator.CreateInstance(balanceType);
        }

        [TearDown]
        public void TearDown()
        {
            Invoke(mode, "Stop");
            if (config != null) UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void BeginsOwnCamionerosLevelUsingOnlyExistingLevelBalance()
        {
            Set(config, "totalFanCount", 120);
            Set(config, "fanSpeed", .20475f);
            mode = CreateMode(0.02f);
            Invoke(mode, "Begin");

            Assert.AreEqual("tower-defense", Get<string>(mode, "ModeId"));
            Assert.AreEqual("tower-defense.camioneros.plaza-de-mayo.level-1", Get<string>(mode, "LevelId"));
            Assert.AreEqual(500, Get<int>(mode, "Coins"));
            Assert.AreEqual(120, Get<int>(mode, "TotalFanCount"));
            Assert.AreEqual(0, Get<int>(mode, "SpawnedFans"));
            Assert.AreEqual(0, Get<int>(mode, "ServedFans"));
            Assert.IsFalse(Get<bool>(mode, "HasWon"));
            Assert.IsFalse(Get<bool>(mode, "HasLost"));
            Assert.IsTrue(Get<bool>(RuntimeType("TowerDefenseGameMode"), "OwnsRuntime", null));
            Assert.IsTrue(Get<bool>(RuntimeType("TowerDefenseGameMode"), "CampaignStepSuspended", null));
            CollectionAssert.AreEqual(new[] { 0, 4 }, (int[])Get<object>(mode, "AvailableProducts"));
        }

        [Test]
        public void CamionerosStationsShareOneLineAndCenterOnTheirCrewSlotsInBothAreas()
        {
            object route = Get<object>(config, "Route");
            routeType.GetField("SegmentLengths", PublicInstance).SetValue(route, new[] { 8f, 8f, 8f });
            routeType.GetField("CorridorWidth", PublicInstance).SetValue(route, 1.92f);
            routeType.GetField("HorizontalCorridorWidth", PublicInstance).SetValue(route, 1.536f);
            routeType.GetField("RowSpacing", PublicInstance).SetValue(route, 6f);
            routeType.GetField("PlayableBounds", PublicInstance).SetValue(route, new Rect(-6f, -6.96f, 12f, 13.92f));
            mode = CreateMode(.02f);

            object parrillero = Enum.Parse(RuntimeType("StreetWorkerRole"), "Parrillero");
            object cocacolero = Enum.Parse(RuntimeType("StreetWorkerRole"), "Cocacolero");
            Vector2 areaOneTable = (Vector2)Invoke(mode, "StationPosition", new[] { parrillero, (object)0 });
            Vector2 areaOneTableVisual = (Vector2)Invoke(mode, "StationVisualPosition", new[] { parrillero, (object)0 });
            Vector2 areaOneBarrel = (Vector2)Invoke(mode, "StationPosition", new[] { cocacolero, (object)0 });
            Vector2 areaOneBarrelVisual = (Vector2)Invoke(mode, "StationVisualPosition", new[] { cocacolero, (object)0 });
            Assert.AreNotEqual(areaOneTable.y, areaOneBarrel.y,
                "The non-rendered service anchors remain at their existing role-specific points.");
            Assert.AreEqual(areaOneTableVisual.y, areaOneBarrelVisual.y, .0001f,
                "Both stations render on one shared depth line in area 1.");
            Assert.Less(areaOneTableVisual.x, areaOneBarrelVisual.x,
                "The Parrillero table stays to the left of the Coca barrel.");

            Vector2 areaTwoTable = (Vector2)Invoke(mode, "StationPosition", new[] { parrillero, (object)1 });
            Vector2 areaTwoBarrel = (Vector2)Invoke(mode, "StationPosition", new[] { cocacolero, (object)1 });
            Vector2 areaTwoTableVisual = (Vector2)Invoke(mode, "StationVisualPosition", new[] { parrillero, (object)1 });
            Vector2 areaTwoBarrelVisual = (Vector2)Invoke(mode, "StationVisualPosition", new[] { cocacolero, (object)1 });
            Assert.AreNotEqual(areaTwoTable.y, areaTwoBarrel.y,
                "The non-rendered area-2 service anchors remain role-specific.");
            float scale = Get<float>(mode, "WorldUnitsPerLogicalPixel");
            Assert.AreEqual(areaTwoTableVisual.y, areaTwoBarrelVisual.y, .0001f,
                "Both stations render on one shared depth line in area 2.");
            Assert.Less(areaTwoTableVisual.x, areaTwoBarrelVisual.x,
                "The Parrillero table stays to the left of the Coca barrel.");
            float tableWidth = (float)RuntimeType("StreetKitchenLayout").GetField("TableWidth", PublicStatic).GetRawConstantValue();
            float barrelWidth = (float)RuntimeType("StreetKitchenLayout").GetField("BarrelWidth", PublicStatic).GetRawConstantValue();
            float clearGap = areaTwoBarrelVisual.x - areaTwoTableVisual.x - (tableWidth + barrelWidth) * scale * .5f;
            Assert.GreaterOrEqual(clearGap, 23.5f * scale - .0001f,
                "Both stations remain individually separated after aligning each with its full crew.");
            Assert.AreEqual(Mathf.Abs(areaOneBarrelVisual.x - areaOneTableVisual.x) / scale,
                Get<float>(mode, "CurrentStationSpacingLogical"), .001f,
                "The exposed station spacing follows the actual presentation anchors.");
            AssertStationVisualInsideArea(0, areaOneTableVisual, tableWidth, scale);
            AssertStationVisualInsideArea(0, areaOneBarrelVisual, barrelWidth, scale);
            AssertStationVisualInsideArea(1, areaTwoTableVisual, tableWidth, scale);
            AssertStationVisualInsideArea(1, areaTwoBarrelVisual, barrelWidth, scale);

            int losRedondosCampaignLevelId = (int)RuntimeType("TowerDefenseLevelConfig")
                .GetField("LosRedondosCampaignLevelId", PublicStatic).GetRawConstantValue();
            Set(config, "sourceCampaignLevelId", losRedondosCampaignLevelId);
            Assert.AreEqual(areaTwoTable, Invoke(mode, "StationVisualPosition", new[] { parrillero, (object)1 }),
                "This visual adjustment is limited to Camioneros.");
        }

        [Test]
        public void LosRedondosIdentityUsesCamionerosEconomyWithoutReadingTraditionalIdTenBalance()
        {
            mode = CreateMode(.02f);
            int camionerosHire = (int)Invoke(mode, "HireCost", new[] { Enum.Parse(RuntimeType("StreetWorkerRole"), "Parrillero") });
            int camionerosSpeed = Get<int>(mode, "SpeedUpgradeCost");
            Assert.AreEqual(9, Get<int>(config, "EconomicBalanceCampaignLevelId"));

            Invoke(mode, "Stop");
            configType.GetField("sourceCampaignLevelId", PrivateInstance).SetValue(config, 10);
            configType.GetField("levelId", PrivateInstance).SetValue(config, "tower-defense.los-redondos.tandil.level-1");
            configType.GetField("displayName", PrivateInstance).SetValue(config, "Los Redondos / Tandil");
            configType.GetField("backgroundResource", PrivateInstance).SetValue(config, "street-background-losredondos-v1");
            Set(config, "totalFanCount", 60);
            Set(config, "fanSpeed", .1575f);
            mode = CreateMode(.02f);

            Assert.AreEqual(10, Get<int>(config, "SourceCampaignLevelId"));
            Assert.AreEqual(9, Get<int>(config, "EconomicBalanceCampaignLevelId"));
            Assert.AreEqual("tower-defense.los-redondos.tandil.level-1", Get<string>(mode, "LevelId"));
            Assert.AreEqual(camionerosHire,
                (int)Invoke(mode, "HireCost", new[] { Enum.Parse(RuntimeType("StreetWorkerRole"), "Parrillero") }));
            Assert.AreEqual(camionerosSpeed, Get<int>(mode, "SpeedUpgradeCost"));
            Assert.AreEqual(60, Get<int>(mode, "TotalFanCount"));
        }

        [Test]
        public void SharedEconomyProfilePreservesCurrentIdNineCurvesAndIgnoresLegacyRowsNineAndTen()
        {
            Type profile = RuntimeType("TowerDefenseEconomyProfile");
            Type simulation = RuntimeType("StreetSimulation");
            Type roleType = RuntimeType("StreetWorkerRole");
            MethodInfo profileHire = profile.GetMethod("CalculateHireCost", PublicStatic);
            MethodInfo profileSpeed = profile.GetMethod("CalculateSpeedCost", PublicStatic);
            MethodInfo legacyHire = simulation.GetMethod("CalculateHireCost", PublicStatic);
            MethodInfo legacySpeed = simulation.GetMethod("CalculateSpeedCost", PublicStatic);
            Assert.AreEqual(500, profile.GetField("StartingCoins", PublicStatic).GetRawConstantValue());

            foreach (string roleName in new[] { "Parrillero", "Cocacolero" })
            {
                object role = Enum.Parse(roleType, roleName);
                for (int workers = 0; workers <= 8; workers++)
                {
                    int formerLevelNineCost = (int)legacyHire.Invoke(null, new[] { balance, (object)9, role, (object)workers, (object)0L });
                    int frozenCost = (int)profileHire.Invoke(null, new[] { role, (object)workers, (object)0L });
                    Assert.AreEqual(formerLevelNineCost, frozenCost, roleName + " curve at worker count " + workers);
                }
            }
            for (int speed = 0; speed < 9; speed++)
                Assert.AreEqual((int)legacySpeed.Invoke(null, new object[] { balance, 9, speed }),
                    (int)profileSpeed.Invoke(null, new object[] { speed }), "Speed tier " + speed);

            object exposedSnapshot = profile.GetMethod("CreateBalanceSnapshot", PublicStatic).Invoke(null, null);
            int frozenHireAtFour = (int)profileHire.Invoke(null, new[] { Enum.Parse(roleType, "Parrillero"), (object)4, (object)0L });
            int frozenSpeedAtFour = (int)profileSpeed.Invoke(null, new object[] { 4 });
            Array snapshotRows = (Array)balanceType.GetField("levelBalances", PublicInstance).GetValue(exposedSnapshot);
            object snapshotRow = snapshotRows.GetValue(9);
            int[] snapshotBases = (int[])snapshotRow.GetType().GetField("hireBaseCostsByRole", PublicInstance).GetValue(snapshotRow);
            snapshotBases[0] = 99999;
            snapshotRow.GetType().GetField("speedCostMultiplier", PublicInstance).SetValue(snapshotRow, 999f);
            Assert.AreEqual(frozenHireAtFour,
                (int)profileHire.Invoke(null, new[] { Enum.Parse(roleType, "Parrillero"), (object)4, (object)0L }),
                "Mutating the public snapshot must not mutate the hot-path cached profile.");
            Assert.AreEqual(frozenSpeedAtFour, (int)profileSpeed.Invoke(null, new object[] { 4 }));

            mode = CreateMode(.02f);
            Invoke(mode, "Begin");
            object baselineHire = Invoke(mode, "HireCost", new[] { Enum.Parse(roleType, "Parrillero") });
            int baselineSpeed = Get<int>(mode, "SpeedUpgradeCost");
            Assert.AreEqual(500, Get<int>(mode, "Coins"));

            Array rows = (Array)balanceType.GetField("levelBalances", PublicInstance).GetValue(balance);
            foreach (int levelId in new[] { 9, 10 })
            {
                object row = rows.GetValue(levelId);
                int[] bases = (int[])row.GetType().GetField("hireBaseCostsByRole", PublicInstance).GetValue(row);
                for (int i = 0; i < bases.Length; i++) bases[i] = 999;
                row.GetType().GetField("speedCostMultiplier", PublicInstance).SetValue(row, 99f);
                row.GetType().GetField("startingCoins", PublicInstance).SetValue(row, 99999);
            }

            Assert.AreEqual(baselineHire, Invoke(mode, "HireCost", new[] { Enum.Parse(roleType, "Parrillero") }));
            Assert.AreEqual(baselineSpeed, Get<int>(mode, "SpeedUpgradeCost"));
            Assert.AreEqual(500, profile.GetField("StartingCoins", PublicStatic).GetRawConstantValue());
        }

        [Test]
        public void FanWaveIsFiniteAndAcceptsOnlyChoriAndCocaOrders()
        {
            mode = CreateMode(.02f);
            Invoke(mode, "Begin");
            Assert.IsFalse((bool)Invoke(mode, "TrySpawnFan", new[] { new[] { 1 }, new[] { 1 } }), "Paty is not an order in this mode.");
            for (int i = 0; i < 60; i++)
                Assert.IsTrue((bool)Invoke(mode, "TrySpawnFan", new[] { new[] { i % 2 == 0 ? 0 : 4 }, new[] { 1 } }));
            Assert.AreEqual(60, Get<int>(mode, "SpawnedFans"));
            Assert.AreEqual(60, ((System.Collections.IList)Get<object>(mode, "Fans")).Count);
            Assert.IsFalse((bool)Invoke(mode, "TrySpawnFan", new[] { new[] { 0 }, new[] { 1 } }), "No sixty-first customer may spawn.");
        }

        [Test]
        public void SerializedDefenseLevelsKeepTheirConfiguredFanEntrySpacing()
        {
            MethodInfo load = null;
            foreach (MethodInfo candidate in typeof(Resources).GetMethods(PublicStatic))
            {
                ParameterInfo[] parameters = candidate.GetParameters();
                if (candidate.Name == "Load" && candidate.IsGenericMethodDefinition && parameters.Length == 1 &&
                    parameters[0].ParameterType == typeof(string))
                {
                    load = candidate.MakeGenericMethod(configType);
                    break;
                }
            }
            Assert.IsNotNull(load, "Unity generic Resources.Load<T>(string) must be available.");

            object camioneros = load.Invoke(null, new object[] { "TowerDefense/Camioneros-PlazaDeMayo" });
            object losRedondos = load.Invoke(null, new object[] { "TowerDefense/LosRedondos-Tandil" });
            Assert.IsNotNull(camioneros);
            Assert.IsNotNull(losRedondos);
            Assert.AreEqual(96f, Get<float>(camioneros, "MinimumEntrySpacingLogical"),
                "Camioneros needs a visible gap larger than the 78 px fan frame.");
            Assert.AreEqual(58f, Get<float>(losRedondos, "MinimumEntrySpacingLogical"),
                "This request changes only campaign position 3; Los Redondos keeps its existing cadence.");
        }

        [Test]
        public void LiveCamionerosConfigSpawns120FansThirtyPercentFasterAndRedondosKeepsIts60FanBaseline()
        {
            object camioneros = Resources.Load("TowerDefense/Camioneros-PlazaDeMayo");
            object redondos = Resources.Load("TowerDefense/LosRedondos-Tandil");
            Assert.IsNotNull(camioneros);
            Assert.IsNotNull(redondos);
            Assert.AreEqual(120, Get<int>(camioneros, "TotalFanCount"));
            Assert.AreEqual(60, Get<int>(redondos, "TotalFanCount"));
            Assert.That(Get<float>(camioneros, "FanSpeed"), Is.EqualTo(.1575f * 1.3f).Within(.00001f));
            Assert.That(Get<float>(redondos, "FanSpeed"), Is.EqualTo(.1575f).Within(.00001f));

            mode = CreateMode(camioneros, .02f);
            Invoke(mode, "Begin");
            Assert.AreEqual(120, Get<int>(mode, "TotalFanCount"));
            float campaignSpeed = (float)balanceType.GetField("customerSpeed", PublicInstance).GetValue(balance);
            Assert.IsTrue((bool)Invoke(mode, "TrySpawnFan", new[] {new[] {0}, new[] {1}}));
            object camionerosFollower = Get<object>(((System.Collections.IList)Get<object>(mode, "Fans"))[0], "RouteFollower");
            Assert.That(Get<float>(camionerosFollower, "Speed"),
                Is.EqualTo(campaignSpeed * .1575f * 1.3f * .02f).Within(.0001f));
            for (int i = 1; i < 120; i++)
                Assert.IsTrue((bool)Invoke(mode, "TrySpawnFan", new[] {new[] {0}, new[] {1}}));
            Assert.AreEqual(120, Get<int>(mode, "SpawnedFans"));
            Assert.IsFalse((bool)Invoke(mode, "TrySpawnFan", new[] {new[] {0}, new[] {1}}),
                "Camioneros must reject fan number 121.");

            Invoke(mode, "Stop");
            mode = CreateMode(redondos, .02f);
            Invoke(mode, "Begin");
            Assert.AreEqual(60, Get<int>(mode, "TotalFanCount"));
            Assert.IsTrue((bool)Invoke(mode, "TrySpawnFan", new[] {new[] {4}, new[] {1}}));
            object redondosFollower = Get<object>(((System.Collections.IList)Get<object>(mode, "Fans"))[0], "RouteFollower");
            Assert.That(Get<float>(redondosFollower, "Speed"), Is.EqualTo(campaignSpeed * .1575f * .02f).Within(.0001f));
            for (int i = 1; i < 60; i++)
                Assert.IsTrue((bool)Invoke(mode, "TrySpawnFan", new[] {new[] {4}, new[] {1}}));
            Assert.AreEqual(60, Get<int>(mode, "SpawnedFans"));
            Assert.IsFalse((bool)Invoke(mode, "TrySpawnFan", new[] {new[] {4}, new[] {1}}),
                "Los Redondos must remain capped at 60.");
        }

        [Test]
        public void CamionerosFansEnterGraduallyInsteadOfAppearingAsABatch()
        {
            Set(config, "minimumEntrySpacingLogical", 96f);
            mode = CreateMode(.02f);
            Invoke(mode, "Begin");

            Invoke(mode, "Step", new object[] { 2f });
            Assert.AreEqual(1, Get<int>(mode, "SpawnedFans"),
                "At 2 seconds the first fan has not yet opened the 96 px entry gap for another fan.");

            var fans = (System.Collections.IList)Get<object>(mode, "Fans");
            object follower = Get<object>(fans[0], "RouteFollower");
            float minimumWorldGap = Get<float>(mode, "WorldUnitsPerLogicalPixel") * Get<float>(config, "MinimumEntrySpacingLogical");
            float remainingGap = Mathf.Max(0f, minimumWorldGap - Get<float>(follower, "Progress"));
            float speed = Get<float>(follower, "Speed");
            Assert.Greater(speed, 0f);
            float stepSeconds = remainingGap / speed + .1f;
            while (Get<float>(follower, "Progress") < minimumWorldGap && stepSeconds > 0f)
            {
                float step = Mathf.Min(1f, stepSeconds);
                Invoke(mode, "Step", new object[] { step });
                stepSeconds -= step;
            }
            Assert.GreaterOrEqual(Get<float>(follower, "Progress"), minimumWorldGap,
                "The first fan must clear the configured route gap before the next enters.");
            Assert.AreEqual(2, Get<int>(mode, "SpawnedFans"), "A second fan enters only after the first clears the minimum gap.");

            Invoke(mode, "Step", new object[] { 1f });
            Assert.AreEqual(2, Get<int>(mode, "SpawnedFans"),
                "The next fan must also clear the entrance; one open interval cannot release a batch.");
        }

        [Test]
        public void CamionerosFansMoveThirtyPercentFasterThanThePreviousDefenseSpeed()
        {
            const float previousMultiplier = 0.1575f;
            const float speedIncrease = 1.30f;
            const float logicalPixelScale = 0.02f;
            Set(config, "fanSpeed", previousMultiplier * speedIncrease);
            Assert.That(Get<float>(config, "FanSpeed"), Is.EqualTo(previousMultiplier * speedIncrease).Within(0.0001f));

            mode = CreateMode(logicalPixelScale);
            Invoke(mode, "Begin");
            Assert.IsTrue((bool)Invoke(mode, "TrySpawnFan", new[] { new[] { 0 }, new[] { 1 } }));

            object follower = Get<object>(((System.Collections.IList)Get<object>(mode, "Fans"))[0], "RouteFollower");
            float campaignSpeed = (float)balanceType.GetField("customerSpeed", PublicInstance).GetValue(balance);
            float expectedSpeed = campaignSpeed * previousMultiplier * speedIncrease * logicalPixelScale;
            Assert.That(Get<float>(follower, "Speed"), Is.EqualTo(expectedSpeed).Within(0.0001f),
                "Only the Tower Defense multiplier should scale the campaign customer speed.");
        }

        [Test]
        public void DeliversOnePhysicalSandwichExactlyOnceAndCompletesCorrectFan()
        {
            object kitchenBalance = balanceType.GetField("kitchen", PublicInstance).GetValue(balance);
            kitchenBalance.GetType().GetField("activeCookSeconds", PublicInstance).SetValue(kitchenBalance, 0.01f);
            mode = CreateMode(0.02f);
            Invoke(mode, "Begin");
            Hire("Parrillero", 0);
            capturedDelivery = null;
            EventInfo deliveryEvent = modeType.GetEvent("DeliveryCompleted", PublicInstance);
            Type deliveryType = deliveryEvent.EventHandlerType.GetGenericArguments()[0];
            ParameterExpression deliveryParameter = Expression.Parameter(deliveryType, "delivery");
            MethodInfo captureMethod = typeof(TowerDefenseGameModeTests)
                .GetMethod(nameof(CaptureDelivery), PrivateInstance);
            Delegate deliveryHandler = Expression.Lambda(deliveryEvent.EventHandlerType,
                Expression.Call(Expression.Constant(this), captureMethod,
                    Expression.Convert(deliveryParameter, typeof(object))), deliveryParameter).Compile();
            deliveryEvent.AddEventHandler(mode, deliveryHandler);

            Assert.IsTrue((bool)Invoke(mode, "TrySpawnFan", new[] { new[] { 0 }, new[] { 1 } }));
            // Fill the finite budget with requests that this cook cannot service.
            for (int i = 0; i < 29; i++)
                Assert.IsTrue((bool)Invoke(mode, "TrySpawnFan", new[] {new[] {4}, new[] {1}}));
            Invoke(mode, "Step", new[] {(object).6f});

            Assert.AreEqual(1, Get<int>(mode, "TotalDelivered"));
            Assert.AreEqual(1, Get<int[]>(mode, "DeliveredByProduct")[0]);
            Assert.AreEqual(1, Get<int>(mode, "ServedFans"));
            Assert.AreEqual(0, Get<int>(mode, "EscapedFans"));
            Assert.AreEqual(1, Get<int>(mode, "CustomersAttended"));
            Assert.AreEqual(1, Get<int>(mode, "OrdersCompleted"));
            Assert.AreEqual(5, Get<int>(mode, "CoinsEarned"));
            Vector2 expectedVisualSource = (Vector2)Invoke(mode, "StationVisualPosition", new object[]
                { Enum.Parse(RuntimeType("StreetWorkerRole"), "Parrillero"), 0 }) -
                Vector2.up * (float)modeType.GetField("StationRenderYOffsetLogical", PublicStatic).GetRawConstantValue() *
                Get<float>(mode, "WorldUnitsPerLogicalPixel");
            Assert.IsNotNull(capturedDelivery, "The physical delivery event should expose its presentation source.");
            Vector2 receivedSource = (Vector2)deliveryType.GetField("Source", PublicInstance).GetValue(capturedDelivery);
            Assert.AreEqual(expectedVisualSource, receivedSource,
                "The physical handoff cue fallback originates at the moved visual table, not its unchanged base anchor.");
        }

        [Test]
        public void OneCocacoleroWaitsForPreviousHandoffBeforeDeliveringToNextFan()
        {
            mode = CreateMode(.02f);
            Invoke(mode, "Begin");
            float waitingRow = RouteDistanceTo(0) + RowLength(0) * .5f;
            object firstFan = SpawnAt(4, waitingRow);
            object secondFan = SpawnAt(4, waitingRow);
            foreach (object fan in new[] {firstFan, secondFan})
            {
                object follower = Get<object>(fan, "RouteFollower");
                follower.GetType().GetProperty("Speed", PublicInstance).SetValue(follower, 0f, null);
            }
            Set(mode, "stopSpawning", true);
            Hire("Cocacolero", 0);

            Invoke(mode, "Step", new object[] {.05f});
            Assert.AreEqual(1, Get<int>(mode, "TotalDelivered"));
            float firstDeliveryAt = Get<float>(mode, "Elapsed");

            for (int i = 0; i < 10; i++) Invoke(mode, "Step", new object[] {.05f});
            Assert.AreEqual(1, Get<int>(mode, "TotalDelivered"),
                "The same Cocacolero cannot begin another Coca handoff while the first cue is still active.");
            Assert.AreEqual(0, Get<int>(firstFan, "PendingUnits"));
            Assert.AreEqual(1, Get<int>(secondFan, "PendingUnits"));

            for (int i = 0; i < 5 && Get<int>(mode, "TotalDelivered") < 2; i++)
                Invoke(mode, "Step", new object[] {.05f});
            Assert.AreEqual(2, Get<int>(mode, "TotalDelivered"), "The next fan must still receive Coca after the worker becomes available.");
            Assert.AreEqual(0, Get<int>(firstFan, "PendingUnits"));
            Assert.AreEqual(0, Get<int>(secondFan, "PendingUnits"));
            Assert.That(Get<float>(mode, "Elapsed") - firstDeliveryAt,
                Is.GreaterThanOrEqualTo((float)modeType.GetField("DeliveryCueDurationSeconds", PublicStatic).GetRawConstantValue()));
        }

        [Test]
        public void ChoriAndCocaOrderBubbleLinesDisappearAfterTheirPhysicalDeliveries()
        {
            object kitchenBalance = balanceType.GetField("kitchen", PublicInstance).GetValue(balance);
            kitchenBalance.GetType().GetField("activeCookSeconds", PublicInstance).SetValue(kitchenBalance, 0.01f);
            mode = CreateMode(0.02f);
            Set(mode, "serviceRange", 1000f);
            Invoke(mode, "Begin");
            Hire("Parrillero", 0);
            Hire("Cocacolero", 1);
            Assert.IsTrue((bool)Invoke(mode, "TrySpawnFan", new[] { new[] { 0, 4 }, new[] { 1, 1 } }));

            System.Collections.IList fans = (System.Collections.IList)Get<object>(mode, "Fans");
            object fan = fans[0];
            MethodInfo visibleLineCount = RuntimeType("StreetView").GetMethod(
                "TowerDefenseVisiblePendingLineCount", PrivateInstance | BindingFlags.Static);
            Assert.IsNotNull(visibleLineCount, "The same pending-order filter drives the fan speech bubbles.");
            Assert.AreEqual(2, visibleLineCount.Invoke(null, new[] { fan }));
            Type streetViewType = RuntimeType("StreetView");
            MethodInfo bubbleBounds = streetViewType.GetMethod("OrderBubbleBounds", PrivateInstance | BindingFlags.Static);
            MethodInfo lineBounds = streetViewType.GetMethod("OrderLineBounds", PrivateInstance | BindingFlags.Static);
            MethodInfo iconBounds = streetViewType.GetMethod("OrderIconBounds", PrivateInstance | BindingFlags.Static);
            Rect bubble = (Rect)bubbleBounds.Invoke(null, new object[] { Vector2.zero, 2, 20f });
            Rect secondLine = (Rect)lineBounds.Invoke(null, new object[] { bubble, 1, 2, 1f });
            Rect cocaIcon = (Rect)iconBounds.Invoke(null, new object[] { secondLine, 1f });
            Assert.That(bubble.height, Is.EqualTo(74f).Within(.01f), "TD keeps both existing full-size order rows.");
            Assert.That(cocaIcon.height, Is.GreaterThanOrEqualTo(23f), "The Coca icon must remain visible at the standard 26px order-icon size.");

            object follower = Get<object>(fan, "RouteFollower");
            Invoke(follower, "Advance", new object[] {Get<float>(follower, "TotalDistance") * .5f / Get<float>(follower, "Speed")});
            for (int i = 0; i < 100 && (int)visibleLineCount.Invoke(null, new[] { fan }) > 0; i++)
            {
                CallPrivate("AdvanceActiveCooks", .1f, 1f);
                CallPrivate("DeliverWithinServiceRanges");
            }

            int[] delivered = Get<int[]>(mode, "DeliveredByProduct");
            Assert.AreEqual(1, delivered[0], "The Parrillero/Mozo handoff delivers the requested chori.");
            Assert.AreEqual(1, delivered[4], "The Cocacolero handoff delivers the requested Coca.");
            Assert.AreEqual(0, visibleLineCount.Invoke(null, new[] { fan }),
                "A product icon is no longer rendered after its remaining order quantity reaches zero.");
        }

        [Test]
        public void TDStartsEmptyIgnoresRosterAndTicketCountsAndPlacesWorkersInChosenZone()
        {
            int[] roster = { 2, 2, 0, 0, 1 };
            long[] ticketWorkers = { 1, 1, 0, 0, 0 };
            mode = CreateIntegratedMode(0.02f, roster, ticketWorkers, 500);
            Assert.AreEqual(0, Invoke(mode, "WorkersCount", new[] { Enum.Parse(RuntimeType("StreetWorkerRole"), "Parrillero") }));
            Assert.AreEqual(0, Invoke(mode, "WorkersCount", new[] { Enum.Parse(RuntimeType("StreetWorkerRole"), "Cocacolero") }));
            Assert.AreEqual(0, Invoke(mode, "WorkersInZone", new[] { Enum.Parse(RuntimeType("StreetWorkerRole"), "Parrillero"), (object)0 }));

            object parrillero = Enum.Parse(RuntimeType("StreetWorkerRole"), "Parrillero");
            int cost = (int)Invoke(mode, "HireCost", new[] { parrillero });
            Type simulationType = RuntimeType("StreetSimulation");
            int expected = (int)simulationType.GetMethod("CalculateHireCost", PublicStatic)
                .Invoke(null, new[] { balance, (object)9, parrillero, 0, (object)0L });
            Assert.AreEqual(expected, cost, "TD ignores both campaign staff and ticket-worker discounts.");

            Invoke(mode, "Begin");
            Assert.IsTrue((bool)Invoke(mode, "CanPlaceWorker", new[] { parrillero, (object)1 }));
            Assert.IsTrue((bool)Invoke(mode, "TryHire", new[] { parrillero, (object)1 }));
            Assert.AreEqual(1, Invoke(mode, "WorkersInZone", new[] { parrillero, (object)1 }));
            Assert.AreEqual(0, Invoke(mode, "WorkersInZone", new[] { parrillero, (object)0 }));
            Assert.AreEqual(1L, Get<long[]>(mode, "HiresByRole")[0]);
            Assert.AreEqual((long)cost * 100L, Get<long>(mode, "HireSpendCents"));
            int speedCost = Get<int>(mode, "SpeedUpgradeCost");
            Assert.IsTrue((bool)Invoke(mode, "TryUpgradeSpeed"));
            Assert.AreEqual((long)speedCost * 100L, Get<long>(mode, "SpeedSpendCents"));
        }

        [Test]
        public void PlacementCapsAreIndependentByWorkerTypeAndAreaAndRejectedPlacementsDoNotSpend()
        {
            mode = CreateMode(0.02f);
            Invoke(mode, "Begin");
            object grill = Enum.Parse(RuntimeType("StreetWorkerRole"), "Parrillero");
            object coca = Enum.Parse(RuntimeType("StreetWorkerRole"), "Cocacolero");
            int parrilleroCost = (int)Invoke(mode, "HireCost", new[] { grill });
            Set(mode, "coins", 1000000);
            for (int zone = 0; zone < 2; zone++)
                for (int i = 0; i < 3; i++)
                    Assert.IsTrue((bool)Invoke(mode, "TryHire", new[] { grill, (object)zone }));
            int balanceAfterFull = Get<int>(mode, "Coins");
            long hiresAfterFull = Get<long[]>(mode, "HiresByRole")[0];
            Assert.IsFalse((bool)Invoke(mode, "CanPlaceWorker", new[] { grill, (object)0 }));
            Assert.IsFalse((bool)Invoke(mode, "TryHire", new[] { grill, (object)0 }));
            Assert.AreEqual(balanceAfterFull, Get<int>(mode, "Coins"));
            Assert.AreEqual(hiresAfterFull, Get<long[]>(mode, "HiresByRole")[0]);
            Assert.AreEqual(3, Invoke(mode, "WorkersInZone", new[] { grill, (object)0 }));
            Assert.AreEqual(3, Invoke(mode, "WorkersInZone", new[] { grill, (object)1 }));
            Assert.IsTrue((bool)Invoke(mode, "TryHire", new[] { coca, (object)0 }), "A different role has an independent area cap.");
            Assert.IsFalse((bool)Invoke(mode, "TryHire", new[] { grill, (object)2 }), "Invalid areas are rejected.");
            Assert.AreEqual(balanceAfterFull - (int)Invoke(mode, "HireCost", new[] { coca }), Get<int>(mode, "Coins"));
            Assert.Greater(parrilleroCost, 0);
        }

        [Test]
        public void ReflowPreservesEveryWorkersChosenAreaAndRebuildsPerAreaOffsets()
        {
            mode = CreateMode(0.02f);
            Invoke(mode, "Begin");
            object grill = Enum.Parse(RuntimeType("StreetWorkerRole"), "Parrillero");
            object coca = Enum.Parse(RuntimeType("StreetWorkerRole"), "Cocacolero");
            Set(mode, "coins", 1000000);
            Assert.IsTrue((bool)Invoke(mode, "TryHire", new[] { grill, (object)1 }));
            Assert.IsTrue((bool)Invoke(mode, "TryHire", new[] { grill, (object)1 }));
            Assert.IsTrue((bool)Invoke(mode, "TryHire", new[] { coca, (object)0 }));
            System.Collections.IList workers = (System.Collections.IList)Get<object>(mode, "Workers");
            Vector2 oldPosition = Get<Vector2>(workers[0], "Position");

            object route = Get<object>(config, "Route");
            object geometry = geometryType.GetMethod("Generate", PublicStatic).Invoke(null, new[] { route });
            Vector2 first = (Vector2)configType.GetMethod("WorkZonePosition", PublicInstance).Invoke(config, new[] { geometry, (object)0 });
            Vector2 second = (Vector2)configType.GetMethod("WorkZonePosition", PublicInstance).Invoke(config, new[] { geometry, (object)1 });
            Invoke(mode, "Reflow", new[] { route, 0.03f, first + Vector2.right * 10f, second + Vector2.left * 10f });

            Assert.AreEqual(1, Get<int>(workers[0], "ServiceZone"));
            Assert.AreEqual(1, Get<int>(workers[1], "ServiceZone"));
            Assert.AreEqual(0, Get<int>(workers[2], "ServiceZone"));
            Assert.AreNotEqual(oldPosition, Get<Vector2>(workers[0], "Position"), "Reflow rebuilds the visual placement at the new service-zone geometry.");
            Assert.AreEqual(2, Invoke(mode, "WorkersInZone", new[] { grill, (object)1 }));
        }

        [Test]
        public void UnaffordableRoleInvalidRoleAndPausedPlacementLeaveCoinsAndStatsUntouched()
        {
            mode = CreateMode(0.02f);
            Invoke(mode, "Begin");
            object grill = Enum.Parse(RuntimeType("StreetWorkerRole"), "Parrillero");
            object premium = Enum.Parse(RuntimeType("StreetWorkerRole"), "ParrilleroPremium");
            Set(mode, "coins", 0);
            int before = Get<int>(mode, "Coins");
            long[] hiresBefore = Get<long[]>(mode, "HiresByRole");
            Assert.IsFalse((bool)Invoke(mode, "CanPlaceWorker", new[] { grill, (object)0 }));
            Assert.IsFalse((bool)Invoke(mode, "TryHire", new[] { grill, (object)0 }));
            Assert.IsFalse((bool)Invoke(mode, "TryHire", new[] { premium, (object)0 }));
            Assert.AreEqual(before, Get<int>(mode, "Coins"));
            CollectionAssert.AreEqual(hiresBefore, Get<long[]>(mode, "HiresByRole"));
            Set(mode, "coins", 1000000);
            Invoke(mode, "Pause");
            Assert.IsFalse((bool)Invoke(mode, "CanPlaceWorker", new[] { grill, (object)0 }));
            Assert.IsFalse((bool)Invoke(mode, "TryHire", new[] { grill, (object)0 }));
            Assert.AreEqual(0, Invoke(mode, "WorkersInZone", new[] { grill, (object)0 }));
        }

        [Test]
        public void AnyUnservedFanLosingAtExitEndsRunImmediatelyWithoutPenalty()
        {
            Set(config, "startingWorkersByRole", new[] { 0, 0, 0, 0, 0 });
            mode = CreateMode(0.02f);
            Invoke(mode, "Begin");
            Assert.IsTrue((bool)Invoke(mode, "TrySpawnFan", new[] { new[] { 4 }, new[] { 1 } }));
            Assert.IsTrue((bool)Invoke(mode, "TrySpawnFan", new[] {new[] {0}, new[] {1}}));
            var fans = (System.Collections.IList)Get<object>(mode, "Fans");
            object follower = Get<object>(fans[0], "RouteFollower");
            float remainingDistance = Get<float>(follower, "TotalDistance") - Get<float>(follower, "Progress");
            Invoke(follower, "Advance", new object[] { (remainingDistance + .1f) / Get<float>(follower, "Speed") });
            Invoke(mode, "Step", new[] { (object).05f });

            Assert.AreEqual(1, Get<int>(mode, "EscapedFans"));
            Assert.AreEqual(0, Get<int>(mode, "ServedFans"));
            Assert.IsTrue(Get<bool>(mode, "HasLost"));
            Assert.IsFalse(Get<bool>(mode, "HasWon"));
            Assert.AreEqual("Finished", Get<object>(mode, "Phase").ToString());
            Assert.AreEqual(0, Get<int>(mode, "TotalDelivered"));
            Assert.AreEqual(1, fans.Count, "Defeat is immediate while the other supporter is still on the route.");
            Assert.AreEqual(500, Get<int>(mode, "Coins"));
        }

        [Test]
        public void SixtyActualCompleteOrdersFadeOutThenWinWithoutQuotaOrReplacementFan()
        {
            mode = CreateMode(.02f); Invoke(mode, "Begin");
            Assert.IsFalse(Get<bool>(mode, "HasWon"));
            CallPrivate("UpdateCompletionState");
            Assert.IsFalse(Get<bool>(mode, "HasWon"), "An initially empty scene is not a victory.");
            Hire("Cocacolero", 0);
            for (int n = 0; n < 60; n++)
                Assert.IsTrue((bool)Invoke(mode, "TrySpawnFan", new[] {new[] {4}, new[] {2}}));
            var fans = (System.Collections.IList)Get<object>(mode, "Fans");
            object first = fans[0];
            CallPrivate("DeliverWithinServiceRanges");
            Assert.AreEqual(1, Get<int>(first, "PendingUnits"));
            Assert.IsFalse(Get<bool>(first, "IsDeparting"));
            for (int n = 0; n < 119; n++)
            {
                ClearWorkerDeliveryCooldowns();
                CallPrivate("DeliverWithinServiceRanges");
            }
            foreach (object fan in fans)
            {
                Assert.IsTrue(Get<bool>(fan, "IsDeparting"));
                Assert.AreEqual(0, Get<int>(fan, "PendingUnits"));
            }
            Assert.IsFalse(Get<bool>(mode, "HasWon"), "Wait for visible departure, not just logical completion.");
            Assert.AreEqual(120, Get<int>(mode, "TotalDelivered"));
            Invoke(mode, "Step", new object[] {.5f});
            Assert.AreEqual(60, Get<int>(mode, "ServedFans"));
            Assert.AreEqual(60, Get<int>(mode, "SpawnedFans"));
            Assert.AreEqual(0, fans.Count);
            Assert.IsTrue(Get<bool>(mode, "HasWon")); Assert.IsFalse(Get<bool>(mode, "HasLost"));
            Assert.AreEqual("Finished", Get<object>(mode, "Phase").ToString());
            Assert.IsFalse((bool)Invoke(mode, "TrySpawnFan", new[] {new[] {4}, new[] {1}}));
            int coins = Get<int>(mode, "Coins");
            Invoke(mode, "Step", new object[] {1f});
            Assert.AreEqual(coins, Get<int>(mode, "Coins"));
            Assert.AreEqual(120, Get<int>(mode, "TotalDelivered"));
        }

        [Test]
        public void NoDeadlineCanEndDefenseAndAutomaticWaveNeverExceedsSixty()
        {
            mode = CreateMode(.02f); Invoke(mode, "Begin");
            Assert.IsTrue((bool)Invoke(mode, "TrySpawnFan", new[] {new[] {4}, new[] {1}}));
            var fans = (System.Collections.IList)Get<object>(mode, "Fans");
            object first = Get<object>(fans[0], "RouteFollower");
            first.GetType().GetProperty("Speed").SetValue(first, 0f);
            for (int n = 0; n < 401; n++) Invoke(mode, "Step", new object[] {1f});
            Assert.Greater(Get<float>(mode, "Elapsed"), 380f);
            Assert.AreEqual("Playing", Get<object>(mode, "Phase").ToString());
            Assert.IsFalse(Get<bool>(mode, "HasWon")); Assert.IsFalse(Get<bool>(mode, "HasLost"));
            for (int n = 1; n < 60; n++)
            {
                foreach (object fan in fans)
                {
                    object follower = Get<object>(fan, "RouteFollower");
                    follower.GetType().GetProperty("Speed").SetValue(follower, 1f);
                    Invoke(follower, "Advance", new object[] {Mathf.Max(0f, 2f - Get<float>(follower, "Progress"))});
                    follower.GetType().GetProperty("Speed").SetValue(follower, 0f);
                }
                Invoke(mode, "Step", new object[] {.05f});
            }
            Assert.AreEqual(60, Get<int>(mode, "SpawnedFans"));
            foreach (object fan in fans)
                foreach (object line in (System.Collections.IEnumerable)Get<object>(fan, "Order"))
                {
                    Assert.Contains(Get<int>(line, "Product"), new[] {0, 4});
                    Assert.That(Get<int>(line, "Remaining"), Is.InRange(1, 4));
                }
            Invoke(mode, "Step", new object[] {1f});
            Assert.AreEqual(60, Get<int>(mode, "SpawnedFans"));
        }

        [TestCase(0, 0)]
        [TestCase(4, 0)]
        [TestCase(0, 1)]
        [TestCase(4, 1)]
        public void AdjacentDeliveryPrioritizesExitProgressAndNeverMovesWorker(int product, int zone)
        {
            mode = CreateMode(.02f);
            Invoke(mode, "Begin");
            Hire(product == 4 ? "Cocacolero" : "Parrillero", zone);
            var workers = (System.Collections.IList)Get<object>(mode, "Workers");
            object worker = workers[0];
            Vector2 position = Get<Vector2>(worker, "Position");
            // First-created fan is physically nearer the station, but less urgent.
            float upperMid = RouteDistanceTo(zone * RowStride) + RowLength(zone) * .5f;
            float lowerEnd = RouteDistanceTo((zone + 1) * RowStride + 1) - .1f;
            object behind = SpawnAt(product, upperMid);
            object ahead = SpawnAt(product, lowerEnd);
            object nonRequester = SpawnAt(product == 4 ? 0 : 4, lowerEnd + .04f);
            object outside = SpawnAt(product, zone == 0 ? RouteDistanceTo(2 * RowStride) + RowLength(2) * .5f : RowLength(0) * .5f);
            ReadyServing(product, Get<int>(worker, "Id"));
            int coins = Get<int>(mode, "Coins");
            CallPrivate("DeliverWithinServiceRanges");
            Assert.AreEqual(0, Get<int>(ahead, "PendingUnits"), "Most advanced adjacent requester must receive first.");
            Assert.AreEqual(1, Get<int>(behind, "PendingUnits"), "Only one serving per worker/pass, regardless of spawn order.");
            Assert.AreEqual(1, Get<int>(outside, "PendingUnits"), "Cannot serve a different area's route.");
            Assert.AreEqual(1, Get<int>(nonRequester, "PendingUnits"), "Do not substitute another product.");
            Assert.AreEqual(1, Get<int>(mode, "TotalDelivered"));
            int price = Mathf.RoundToInt((float)RuntimeType("StreetSimulation").GetMethod("OfficialProductPrice", PublicStatic).Invoke(null, new object[] {product}));
            Assert.AreEqual(coins + price, Get<int>(mode, "Coins"));
            Assert.AreEqual(position, Get<Vector2>(worker, "Position"));
            Assert.AreEqual(zone, Get<int>(worker, "ServiceZone"));
            Assert.IsFalse(Get<bool>(worker, "IsMoving"));
            Assert.AreSame(behind, ((System.Collections.IList)Get<object>(mode, "Fans"))[0], "Target sorting must not reorder live fans.");
        }

        [Test]
        public void ThreeParrillerosInOneAreaKeepDistinctHandoffSpaceAndServeInParallel()
        {
            mode = CreateMode(.02f); Invoke(mode, "Begin");
            Hire("Parrillero", 0); Hire("Parrillero", 0); Hire("Parrillero", 0);
            var workers = (System.Collections.IList)Get<object>(mode, "Workers");
            Assert.AreEqual(3, workers.Count);
            for (int i = 0; i < workers.Count; i++)
                for (int j = i + 1; j < workers.Count; j++)
                {
                    Vector2 a = Get<Vector2>(workers[i], "Position"), b = Get<Vector2>(workers[j], "Position");
                    Assert.That(Mathf.Abs(a.x - b.x) / .02f, Is.GreaterThanOrEqualTo(72f),
                        "Camioneros crews are deliberately compact (CamionerosCrewVisualOverlapLogical): at least 72 logical pixels apart.");
                }

            float adjacentRowMidpoint = RouteDistanceTo(0) + RowLength(0) * .5f;
            object[] fans = new object[3];
            for (int i = 0; i < fans.Length; i++) fans[i] = SpawnAt(0, adjacentRowMidpoint);
            for (int i = 0; i < workers.Count; i++) ReadyServing(0, Get<int>(workers[i], "Id"));

            CallPrivate("DeliverWithinServiceRanges");

            Assert.AreEqual(3, Get<int>(mode, "TotalDelivered"), "Each ready Parrillero may complete one handoff in the same pass.");
            for (int i = 0; i < fans.Length; i++) Assert.AreEqual(0, Get<int>(fans[i], "PendingUnits"));
            for (int i = 0; i < workers.Count; i++)
            {
                Assert.Greater(Get<float>(workers[i], "ServeAnimationSeconds"), 0f,
                    "Every worker that delivered must enter its own visible handoff animation.");
                Assert.AreEqual(0, Get<int>(workers[i], "ServiceZone"));
                Assert.IsFalse(Get<bool>(workers[i], "IsMoving"), "Workers stay in their selected area; the handoff pose, not route-walking, is animated.");
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        public void CamionerosCrewGroupsShareOneLineAndStayCenteredBehindTheirStations(int zone)
        {
            object route = Get<object>(config, "Route");
            routeType.GetField("SegmentLengths", PublicInstance).SetValue(route, new[] {8f, 8f, 8f});
            routeType.GetField("RowSpacing", PublicInstance).SetValue(route, 5.88f);
            routeType.GetField("CorridorWidth", PublicInstance).SetValue(route, 1.92f);
            routeType.GetField("HorizontalCorridorWidth", PublicInstance).SetValue(route, 1.68f);
            routeType.GetField("PlayableBounds", PublicInstance).SetValue(route, new Rect(-6f, -8f, 12f, 16f));

            mode = CreateMode(.02f);
            Invoke(mode, "Begin");
            Set(mode, "coins", 10000);
            for (int i = 0; i < 3; i++) Hire("Parrillero", zone);
            for (int i = 0; i < 3; i++) Hire("Cocacolero", zone);

            var workers = (System.Collections.IList)Get<object>(mode, "Workers");
            Assert.AreEqual(6, workers.Count);
            float pixel = Get<float>(mode, "WorldUnitsPerLogicalPixel");
            float clearHeight = (5.88f - 1.68f) / pixel;
            float workerHeight = (float)modeType.GetField("WorkerVisualHeightLogical", PublicStatic).GetRawConstantValue();
            float fenceClearance = (float)modeType.GetField("WorkerFenceClearanceLogical", PublicStatic).GetRawConstantValue();
            float sharedRowY = Get<Vector2>(workers[0], "Position").y;

            float pivot = (float)modeType.GetField("WorkerVisualPivotY", PublicStatic).GetRawConstantValue();
            Type streetViewType = RuntimeType("StreetView");
            Rect[] parrilleroPoses = (Rect[])streetViewType.GetProperty("WorkerFoodPoseBounds",
                BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, null);
            Rect[] cocacoleroPoses = (Rect[])streetViewType.GetMethod("WorkerDrinkPoseBounds",
                BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] {true});
            float pWidth = MaxPoseWidthLogical(parrilleroPoses, workerHeight, "street-parrillero");
            float cWidth = MaxPoseWidthLogical(cocacoleroPoses, workerHeight,
                "street-cocacolero-levels3-5-v1", true);
            float pSpacing = pWidth - 10f;
            float cSpacing = cWidth - 10f;
            Vector2 bounds = (Vector2)CallPrivate("WorkAreaHorizontalBounds", zone);
            Vector2 center = (Vector2)Invoke(mode, "WorkZone", new object[] {zone});
            var envelopesByRole = new Rect[2][] { new Rect[3], new Rect[3] };
            object parrilleroRole = Enum.Parse(RuntimeType("StreetWorkerRole"), "Parrillero");
            object cocacoleroRole = Enum.Parse(RuntimeType("StreetWorkerRole"), "Cocacolero");
            Vector2 tablePosition = (Vector2)Invoke(mode, "StationVisualPosition", new[] { parrilleroRole, (object)zone });
            Vector2 barrelPosition = (Vector2)Invoke(mode, "StationVisualPosition", new[] { cocacoleroRole, (object)zone });
            for (int i = 0; i < workers.Count; i++)
            {
                bool parrillero = Get<object>(workers[i], "Role").ToString() == "Parrillero";
                Vector2 position = Get<Vector2>(workers[i], "Position");
                Rect envelope = WorkerVisualEnvelope(position, parrillero ? pWidth : cWidth,
                    workerHeight, pivot, pixel);
                int roleIndex = parrillero ? 0 : 1;
                int slotIndex = i % 3;
                envelopesByRole[roleIndex][slotIndex] = envelope;
                Assert.GreaterOrEqual(envelope.xMin, bounds.x - .0001f);
                Assert.LessOrEqual(envelope.xMax, bounds.y + .0001f);
                Assert.GreaterOrEqual(envelope.yMin,
                    center.y - clearHeight * pixel * .5f + fenceClearance * pixel - .0001f);
                Assert.LessOrEqual(envelope.yMax,
                    center.y + clearHeight * pixel * .5f - fenceClearance * pixel + .0001f);
                Assert.AreEqual(sharedRowY, position.y, .0001f,
                    "Both teams share the same horizontal/depth line in this work area.");
            }

            Assert.AreEqual(tablePosition.x, Get<Vector2>(workers[0], "Position").x, .0001f,
                "The first Parrillero stays in the center slot.");
            Assert.AreEqual(tablePosition.x - pSpacing * pixel, Get<Vector2>(workers[1], "Position").x, .0001f,
                "The second Parrillero takes the left slot.");
            Assert.AreEqual(tablePosition.x + pSpacing * pixel, Get<Vector2>(workers[2], "Position").x, .0001f,
                "The third Parrillero takes the right slot.");
            Assert.AreEqual(barrelPosition.x, Get<Vector2>(workers[3], "Position").x, .0001f,
                "The first Cocacolero stays in the center slot.");
            Assert.AreEqual(barrelPosition.x - cSpacing * pixel, Get<Vector2>(workers[4], "Position").x, .0001f,
                "The second Cocacolero takes the left slot.");
            Assert.AreEqual(barrelPosition.x + cSpacing * pixel, Get<Vector2>(workers[5], "Position").x, .0001f,
                "The third Cocacolero takes the right slot.");
            Assert.AreEqual(tablePosition.y, barrelPosition.y, .0001f,
                "The table and barrel share one depth line behind both crews.");
            Assert.AreEqual(-10f * pixel, envelopesByRole[0][0].xMin - envelopesByRole[0][1].xMax, .0001f,
                "The left and center Parrillero bounds overlap by ten logical pixels.");
            Assert.AreEqual(-10f * pixel, envelopesByRole[0][2].xMin - envelopesByRole[0][0].xMax, .0001f,
                "The center and right Parrillero bounds overlap by ten logical pixels.");
            Assert.AreEqual(-10f * pixel, envelopesByRole[1][0].xMin - envelopesByRole[1][1].xMax, .0001f,
                "The left and center Cocacolero bounds overlap by ten logical pixels.");
            Assert.AreEqual(-10f * pixel, envelopesByRole[1][2].xMin - envelopesByRole[1][0].xMax, .0001f,
                "The center and right Cocacolero bounds overlap by ten logical pixels.");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void CamionerosFirstHireStaysInCenterAndLaterHiresFlankIt(int zone)
        {
            object route = Get<object>(config, "Route");
            routeType.GetField("SegmentLengths", PublicInstance).SetValue(route, new[] {8f, 8f, 8f});
            routeType.GetField("RowSpacing", PublicInstance).SetValue(route, 5.88f);
            routeType.GetField("CorridorWidth", PublicInstance).SetValue(route, 1.92f);
            routeType.GetField("HorizontalCorridorWidth", PublicInstance).SetValue(route, 1.68f);
            routeType.GetField("PlayableBounds", PublicInstance).SetValue(route, new Rect(-6f, -8f, 12f, 16f));

            mode = CreateMode(.02f);
            Invoke(mode, "Begin");
            Set(mode, "coins", 10000);
            string[] roles = { "Parrillero", "Cocacolero" };
            Rect[] parrilleroPoses = (Rect[])RuntimeType("StreetView").GetProperty("WorkerFoodPoseBounds",
                BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, null);
            Type streetViewType = RuntimeType("StreetView");
            Rect[] cocacoleroPoses = (Rect[])streetViewType.GetMethod("WorkerDrinkPoseBounds",
                BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] {true});
            float targetHeight = (float)modeType.GetField("WorkerVisualHeightLogical", PublicStatic).GetRawConstantValue();
            float[] spacings = { MaxPoseWidthLogical(parrilleroPoses, targetHeight, "street-parrillero") - 10f,
                MaxPoseWidthLogical(cocacoleroPoses, targetHeight, "street-cocacolero-levels3-5-v1", true) - 10f };
            for (int roleIndex = 0; roleIndex < roles.Length; roleIndex++)
            {
                object role = Enum.Parse(RuntimeType("StreetWorkerRole"), roles[roleIndex]);
                for (int hireCount = 1; hireCount <= 3; hireCount++)
                {
                    Hire(roles[roleIndex], zone);
                    var crew = new System.Collections.ArrayList();
                    System.Collections.IList allWorkers = (System.Collections.IList)Get<object>(mode, "Workers");
                    for (int i = 0; i < allWorkers.Count; i++)
                        if (Get<object>(allWorkers[i], "Role").ToString() == roles[roleIndex] &&
                            Get<int>(allWorkers[i], "ServiceZone") == zone) crew.Add(allWorkers[i]);
                    Assert.AreEqual(hireCount, crew.Count);
                    float centerX = ((Vector2)Invoke(mode, "StationVisualPosition", new[] { role, (object)zone })).x;
                    float scale = Get<float>(mode, "WorldUnitsPerLogicalPixel");
                    for (int hireIndex = 0; hireIndex < hireCount; hireIndex++)
                    {
                        float slotOffset = hireIndex == 0 ? 0f : hireIndex == 1
                            ? -spacings[roleIndex] : spacings[roleIndex];
                        Assert.AreEqual(centerX + slotOffset * scale,
                            Get<Vector2>(crew[hireIndex], "Position").x, .0001f,
                            hireIndex == 0
                                ? "The first hire remains centered as the crew grows."
                                : "Subsequent hires take the left slot, then the right slot.");
                    }
                }
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        public void TallPortraitStagesBothCamionerosCrewsOnOneLineBehindTheirStations(int zone)
        {
            object route = Get<object>(config, "Route");
            routeType.GetField("SegmentLengths", PublicInstance).SetValue(route, new[] {8f, 8f, 8f});
            routeType.GetField("RowSpacing", PublicInstance).SetValue(route, 6f);
            routeType.GetField("CorridorWidth", PublicInstance).SetValue(route, 1.92f);
            routeType.GetField("HorizontalCorridorWidth", PublicInstance).SetValue(route, 1.536f);
            routeType.GetField("PlayableBounds", PublicInstance).SetValue(route, new Rect(-6f, -8f, 12f, 16f));

            mode = CreateMode(.02f);
            object geometry = geometryType.GetMethod("Generate", PublicStatic).Invoke(null, new[] {route});
            Assert.IsTrue(Get<bool>(geometry, "IsValid"));
            Invoke(mode, "Begin");
            Set(mode, "coins", 10000);
            for (int i = 0; i < 3; i++) Hire("Parrillero", zone);
            for (int i = 0; i < 3; i++) Hire("Cocacolero", zone);
            var hiredWorkers = (System.Collections.IList)Get<object>(mode, "Workers");
            Assert.AreEqual(6, hiredWorkers.Count, "The target-area roster must permit the full 3+3 staff cap.");
            Vector2 workCenter = (Vector2)Invoke(mode, "WorkZone", new object[] {zone});
            object parrilleroRole = Enum.Parse(RuntimeType("StreetWorkerRole"), "Parrillero");
            object cocacoleroRole = Enum.Parse(RuntimeType("StreetWorkerRole"), "Cocacolero");
            float workerHeight = (float)modeType.GetField("WorkerVisualHeightLogical", PublicStatic).GetRawConstantValue();
            float pivotY = (float)RuntimeType("TowerDefenseGameMode")
                .GetField("WorkerVisualPivotY", PublicStatic).GetRawConstantValue();
            float fenceClearance = (float)modeType.GetField("WorkerFenceClearanceLogical", PublicStatic).GetRawConstantValue();
            float parrilleroVisualWidth = (float)modeType.GetField("ParrilleroVisualWidthLogical", PublicStatic).GetRawConstantValue();
            float cocacoleroVisualWidth = (float)modeType.GetField("CocacoleroVisualWidthLogical", PublicStatic).GetRawConstantValue();
            const float pixelScale = .02f;
            float parrilleroSpacing;
            float cocacoleroSpacing;
            float rowSpacing = (float)routeType.GetField("RowSpacing", PublicInstance).GetValue(route);
            float horizontalCorridor = (float)routeType.GetField("HorizontalCorridorWidth", PublicInstance).GetValue(route);
            float halfClearHeight = (rowSpacing - horizontalCorridor) / (2f * pixelScale);
            float visualMinY = -workerHeight * pivotY;
            float visualMaxY = workerHeight * (1f - pivotY);
            Type streetViewType = RuntimeType("StreetView");
            Rect[] parrilleroPoses = (Rect[])streetViewType.GetProperty("WorkerFoodPoseBounds",
                BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, null);
            Rect[] cocacoleroPoses = (Rect[])streetViewType.GetMethod("WorkerDrinkPoseBounds",
                BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] {true});
            float parrilleroWidth = MaxPoseWidthLogical(parrilleroPoses, workerHeight, "street-parrillero");
            float cocacoleroWidth = MaxPoseWidthLogical(cocacoleroPoses, workerHeight,
                "street-cocacolero-levels3-5-v1", true);
            parrilleroSpacing = parrilleroWidth - 10f;
            cocacoleroSpacing = cocacoleroWidth - 10f;
            Assert.LessOrEqual(parrilleroWidth, parrilleroVisualWidth,
                "The position envelope must include all four visible Parrillero poses.");
            Assert.LessOrEqual(cocacoleroWidth, cocacoleroVisualWidth,
                "The position envelope must include all four visible Cocacolero poses.");
            var workerEnvelopes = new Rect[2][] {new Rect[3], new Rect[3]};
            var standingEnvelopes = new Rect[2][] {new Rect[3], new Rect[3]};
            Vector2 tableAnchor = (Vector2)Invoke(mode, "StationVisualPosition", new[] {parrilleroRole, (object)zone});
            Vector2 barrelAnchor = (Vector2)Invoke(mode, "StationVisualPosition", new[] {cocacoleroRole, (object)zone});
            Type kitchenLayout = RuntimeType("StreetKitchenLayout");
            float tableWidth = (float)kitchenLayout.GetField("TableWidth", PublicStatic).GetRawConstantValue();
            float tableHeight = (float)kitchenLayout.GetField("SandwichTableHeight", PublicStatic).GetRawConstantValue();
            float barrelWidth = (float)kitchenLayout.GetField("BarrelWidth", PublicStatic).GetRawConstantValue();
            float barrelHeight = (float)kitchenLayout.GetField("BarrelHeight", PublicStatic).GetRawConstantValue();
            float stationRenderOffset = (float)modeType.GetField("StationRenderYOffsetLogical", PublicStatic).GetRawConstantValue();
            Vector2 horizontalBounds = (Vector2)modeType.GetMethod("WorkAreaHorizontalBounds", PrivateInstance)
                .Invoke(mode, new object[] {zone});
            float tableHalfGroup = parrilleroSpacing + parrilleroVisualWidth * .5f;
            float barrelHalfGroup = cocacoleroSpacing + cocacoleroVisualWidth * .5f;
            float expectedParrilleroCenterX = horizontalBounds.x +
                (fenceClearance + tableHalfGroup) * pixelScale;
            float expectedCocacoleroCenterX = horizontalBounds.y -
                (fenceClearance + barrelHalfGroup) * pixelScale;
            Assert.AreEqual(expectedParrilleroCenterX, tableAnchor.x, .0001f,
                "The table is centered on its crew while the complete trio clears the outer fence.");
            Assert.AreEqual(expectedCocacoleroCenterX, barrelAnchor.x, .0001f,
                "The barrel is centered on its crew while the complete trio clears the outer fence.");
            Assert.AreEqual((expectedCocacoleroCenterX - expectedParrilleroCenterX) / pixelScale,
                Get<float>(mode, "CurrentStationSpacingLogical"), .001f,
                "The spacing property reports the actual side-by-side visual anchors.");
            Vector2 table = tableAnchor - Vector2.up * stationRenderOffset * pixelScale;
            Vector2 barrel = barrelAnchor - Vector2.up * stationRenderOffset * pixelScale;
            Assert.AreEqual(workCenter.y, table.y, .0001f,
                "The table render center uses the middle of the fenced area.");
            Assert.AreEqual(table.y, barrel.y, .0001f,
                "The table and barrel render on the same depth line.");
            Rect tableEnvelope = Rect.MinMaxRect(table.x - tableWidth * pixelScale * .5f,
                table.y - tableHeight * pixelScale * .5f,
                table.x + tableWidth * pixelScale * .5f,
                table.y + tableHeight * pixelScale * .5f);
            Rect barrelEnvelope = Rect.MinMaxRect(barrel.x - barrelWidth * pixelScale * .5f,
                barrel.y - barrelHeight * pixelScale * .5f,
                barrel.x + barrelWidth * pixelScale * .5f,
                barrel.y + barrelHeight * pixelScale * .5f);
            Assert.GreaterOrEqual(tableEnvelope.xMin, horizontalBounds.x - .0001f,
                "The complete table footprint stays inside the selected fenced area.");
            Assert.LessOrEqual(tableEnvelope.xMax, horizontalBounds.y + .0001f,
                "The complete table footprint stays inside the selected fenced area.");
            Assert.GreaterOrEqual(barrelEnvelope.xMin, horizontalBounds.x - .0001f,
                "The complete Coca barrel footprint stays inside the selected fenced area.");
            Assert.LessOrEqual(barrelEnvelope.xMax, horizontalBounds.y + .0001f,
                "The complete Coca barrel footprint stays inside the selected fenced area.");
            Assert.GreaterOrEqual(tableEnvelope.yMin, workCenter.y - halfClearHeight * pixelScale - .0001f,
                "The full-size table does not cross the lower horizontal fence.");
            Assert.LessOrEqual(tableEnvelope.yMax, workCenter.y + halfClearHeight * pixelScale + .0001f,
                "The full-size table does not cross the upper horizontal fence.");
            Assert.GreaterOrEqual(barrelEnvelope.yMin, workCenter.y - halfClearHeight * pixelScale - .0001f,
                "The full-size barrel does not cross the lower horizontal fence.");
            Assert.LessOrEqual(barrelEnvelope.yMax, workCenter.y + halfClearHeight * pixelScale + .0001f,
                "The full-size barrel does not cross the upper horizontal fence.");
            Assert.GreaterOrEqual(barrel.x - table.x - (tableWidth + barrelWidth) * pixelScale * .5f,
                0f, "The two full-size stations remain separated on their shared depth line.");
            float workerRowOffset = (float)modeType.GetField("CamionerosWorkerRowOffsetLogical", PrivateStatic).GetRawConstantValue();
            float expectedRowAnchor = Mathf.Clamp(workerRowOffset,
                -halfClearHeight + fenceClearance - visualMinY,
                halfClearHeight - fenceClearance - visualMaxY);
            float expectedWorkerY = workCenter.y + expectedRowAnchor * pixelScale;
            for (int i = 0; i < 3; i++)
            {
                object parrilleroState = hiredWorkers[i];
                object cocacoleroState = hiredWorkers[i + 3];
                Assert.AreEqual("Parrillero", Get<object>(parrilleroState, "Role").ToString());
                Assert.AreEqual("Cocacolero", Get<object>(cocacoleroState, "Role").ToString());
                Assert.AreEqual(zone, Get<int>(parrilleroState, "ServiceZone"));
                Assert.AreEqual(zone, Get<int>(cocacoleroState, "ServiceZone"));
                Vector2 parrilleroPosition = Get<Vector2>(parrilleroState, "Position");
                Vector2 cocacoleroPosition = Get<Vector2>(cocacoleroState, "Position");
                float parrilleroSide = i == 0 ? 0f : i == 1 ? -parrilleroSpacing : parrilleroSpacing;
                float cocacoleroSide = i == 0 ? 0f : i == 1 ? -cocacoleroSpacing : cocacoleroSpacing;
                Assert.AreEqual(expectedParrilleroCenterX + parrilleroSide * pixelScale,
                    parrilleroPosition.x, .0001f);
                Assert.AreEqual(expectedCocacoleroCenterX + cocacoleroSide * pixelScale,
                    cocacoleroPosition.x, .0001f);
                Assert.AreEqual(expectedWorkerY, parrilleroPosition.y, .0001f,
                    "All Parrilleros share the common worker line; the first hire stays centered.");
                Assert.AreEqual(expectedWorkerY, cocacoleroPosition.y, .0001f,
                    "All Cocacoleros share the same line as the Parrilleros; the first hire stays centered.");
                Rect parrilleroEnvelope = WorkerVisualEnvelope(parrilleroPosition, parrilleroVisualWidth,
                    workerHeight, pivotY, pixelScale);
                Rect cocacoleroEnvelope = WorkerVisualEnvelope(cocacoleroPosition, cocacoleroVisualWidth,
                    workerHeight, pivotY, pixelScale);
                workerEnvelopes[0][i] = parrilleroEnvelope;
                workerEnvelopes[1][i] = cocacoleroEnvelope;
                standingEnvelopes[0][i] = WorkerVisualEnvelope(parrilleroPosition, parrilleroWidth,
                    workerHeight, pivotY, pixelScale);
                standingEnvelopes[1][i] = WorkerVisualEnvelope(cocacoleroPosition, cocacoleroWidth,
                    workerHeight, pivotY, pixelScale);
            }

            float innerTurnFenceX = FindInnerTurnFenceX(geometry, zone, workCenter.y);
            for (int roleIndex = 0; roleIndex < workerEnvelopes.Length; roleIndex++)
            for (int i = 0; i < workerEnvelopes[roleIndex].Length; i++)
            {
                Rect worker = workerEnvelopes[roleIndex][i];
                Assert.GreaterOrEqual(worker.xMin, horizontalBounds.x - .0001f,
                    "The complete sprite stays inside the outer edge of its selected zone.");
                Assert.LessOrEqual(worker.xMax, horizontalBounds.y + .0001f,
                    "The complete sprite stays inside the inner U-turn edge of its selected zone.");
                Assert.GreaterOrEqual(worker.yMin, workCenter.y - halfClearHeight * pixelScale + fenceClearance * pixelScale - .0001f,
                    "The complete worker sprite must clear the lower horizontal fence.");
                Assert.LessOrEqual(worker.yMax, workCenter.y + halfClearHeight * pixelScale - fenceClearance * pixelScale + .0001f,
                    "The complete worker sprite must clear the upper horizontal fence.");
                if (zone == 0)
                    Assert.LessOrEqual(worker.xMax, innerTurnFenceX - fenceClearance * pixelScale + .0001f,
                        "The complete worker sprite must stay inside the right U-turn fence.");
                else
                    Assert.GreaterOrEqual(worker.xMin, innerTurnFenceX + fenceClearance * pixelScale - .0001f,
                        "The complete worker sprite must stay inside the left U-turn fence.");
            }

            for (int roleIndex = 0; roleIndex < standingEnvelopes.Length; roleIndex++)
            {
                Assert.AreEqual(-10f * pixelScale, standingEnvelopes[roleIndex][0].xMin - standingEnvelopes[roleIndex][1].xMax,
                    .0001f, "The left and center standing workers overlap by ten logical pixels.");
                Assert.AreEqual(-10f * pixelScale, standingEnvelopes[roleIndex][2].xMin - standingEnvelopes[roleIndex][0].xMax,
                    .0001f, "The center and right standing workers overlap by ten logical pixels.");
            }

        }

        private static float MaxPoseWidthLogical(Rect[] poses, float targetHeight, string textureResource,
            bool matchedCocacolero = false)
        {
            Assert.IsNotNull(poses);
            Assert.GreaterOrEqual(poses.Length, 4);
            Texture2D texture = Resources.Load<Texture2D>(textureResource);
            Assert.IsNotNull(texture, "The production worker texture is available for SpriteMeshType.Tight measurement.");
            Vector2 importScale = matchedCocacolero
                ? new Vector2(texture.width / 1247f, texture.height / 1261f) : Vector2.one;
            float pivotY = (float)RuntimeType("TowerDefenseGameMode")
                .GetField("WorkerVisualPivotY", PublicStatic).GetRawConstantValue();
            float width = 0f;
            for (int i = 0; i < 4; i++)
            {
                Rect pose = poses[i];
                Rect scaledPose = new Rect(pose.x * importScale.x, pose.y * importScale.y,
                    pose.width * importScale.x, pose.height * importScale.y);
                Rect spriteRect = new Rect(scaledPose.x, texture.height - scaledPose.yMax,
                    scaledPose.width, scaledPose.height);
                Sprite sprite = Sprite.Create(texture, spriteRect, new Vector2(.5f, pivotY), 100f, 0,
                    SpriteMeshType.Tight);
                width = Mathf.Max(width, targetHeight * sprite.bounds.size.x /
                    Mathf.Max(.001f, sprite.bounds.size.y));
                UnityEngine.Object.DestroyImmediate(sprite);
            }
            return width;
        }

        private static Rect WorkerVisualEnvelope(Vector2 position, float widthLogical, float heightLogical,
            float pivotY, float pixelScale)
        {
            float width = widthLogical * pixelScale;
            float height = heightLogical * pixelScale;
            return Rect.MinMaxRect(position.x - width * .5f,
                position.y - height * pivotY,
                position.x + width * .5f,
                position.y + height * (1f - pivotY));
        }

        private static float FindInnerTurnFenceX(object geometry, int zone, float worldY)
        {
            Vector2[][] boundaries = {
                Get<Vector2[]>(geometry, "LeftBoundary"),
                Get<Vector2[]>(geometry, "RightBoundary")
            };
            float innerX = zone == 0 ? float.PositiveInfinity : float.NegativeInfinity;
            for (int boundary = 0; boundary < boundaries.Length; boundary++)
            {
                Vector2[] points = boundaries[boundary];
                for (int i = 1; i < points.Length; i++)
                {
                    Vector2 a = points[i - 1], b = points[i];
                    if (Mathf.Abs(a.x - b.x) > .0001f || worldY < Mathf.Min(a.y, b.y) - .0001f ||
                        worldY > Mathf.Max(a.y, b.y) + .0001f) continue;
                    innerX = zone == 0 ? Mathf.Min(innerX, a.x) : Mathf.Max(innerX, a.x);
                }
            }
            Assert.IsFalse(float.IsInfinity(innerX), "The target service area must intersect its actual vertical fence rails.");
            return innerX;
        }

        [Test]
        public void SharedReadyChoriGoesToMostAdvancedEligibleFanAcrossBothAreas()
        {
            mode = CreateMode(.02f); Invoke(mode, "Begin");
            Hire("Parrillero", 0); Hire("Parrillero", 1);
            var workers = (System.Collections.IList)Get<object>(mode, "Workers");
            object behind = SpawnAt(0, RowLength(0) * .5f);
            object ahead = SpawnAt(0, RouteDistanceTo(2 * RowStride) + RowLength(2) * .75f);
            Assert.AreEqual(false, CallPrivate("IsAdjacentToArea", ahead, 0));
            Assert.AreEqual(true, CallPrivate("IsAdjacentToArea", ahead, 1));
            Assert.AreEqual(false, CallPrivate("IsAdjacentToArea", behind, 1));
            ReadyServing(0, Get<int>(workers[0], "Id"));
            object kitchen = Get<object>(mode, "Kitchen");
            Assert.AreEqual(1, Invoke(kitchen, "TableCount", new object[] {0}));
            int coins = Get<int>(mode, "Coins");
            CallPrivate("DeliverWithinServiceRanges");
            Assert.AreEqual(0, Get<int>(ahead, "PendingUnits"), "Shared physical stock must not be stolen by a less-urgent area just because its cook was hired first.");
            Assert.AreEqual(1, Get<int>(behind, "PendingUnits"));
            Assert.AreEqual(coins + 5, Get<int>(mode, "Coins"));
            Assert.AreEqual(1, Get<int>(mode, "TotalDelivered"));
            Assert.Greater(Get<float>(workers[1], "ServeAnimationSeconds"), 0f);
            Assert.AreEqual(0f, Get<float>(workers[0], "ServeAnimationSeconds"));
            CallPrivate("DeliverWithinServiceRanges");
            Assert.AreEqual(1, Get<int>(mode, "TotalDelivered"), "No second serving exists; a second pass cannot fabricate one.");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void SharedAreaIncludesBothBoundingRowsAndOwnTurnButNotOtherTurnOrThirdRow(int zone)
        {
            mode = CreateMode(.02f); Invoke(mode, "Begin");
            for (int row = 0; row < 3; row++)
            {
                object fan = SpawnAt(4, RouteDistanceTo(row * RowStride) + RowLength(row) * .5f);
                Assert.AreEqual(row == zone || row == zone + 1,
                    CallPrivate("IsAdjacentToArea", fan, zone), "row " + row);
            }
            for (int turn = 0; turn < 2; turn++)
            {
                object fan = SpawnAt(4, RouteDistanceTo(turn * RowStride + 1) + TurnLength(turn) * .5f);
                Assert.AreEqual(turn == zone, CallPrivate("IsAdjacentToArea", fan, zone), "turn " + turn);
            }
        }

        private object SpawnAt(int product, float distance)
        {
            Assert.IsTrue((bool)Invoke(mode, "TrySpawnFan", new object[] {new[] {product}, new[] {1}}));
            var fans = (System.Collections.IList)Get<object>(mode, "Fans");
            object fan = fans[fans.Count - 1], follower = Get<object>(fan, "RouteFollower");
            Invoke(follower, "Advance", new object[] {distance / Get<float>(follower, "Speed")});
            return fan;
        }

        private int RowStride => (int)routeType.GetField("TurnSegments").GetValue(Get<object>(config, "Route")) + 1;
        private float RowLength(int row) => RouteDistanceTo(row * RowStride + 1) - RouteDistanceTo(row * RowStride);
        private float TurnLength(int turn) => RouteDistanceTo((turn + 1) * RowStride) - RouteDistanceTo(turn * RowStride + 1);

        private float RouteDistanceTo(int point)
        {
            Vector2[] points = Get<Vector2[]>(Get<object>(mode, "RouteGeometry"), "Centerline");
            float distance = 0;
            for (int i = 1; i <= point; i++) distance += Vector2.Distance(points[i - 1], points[i]);
            return distance;
        }

        private void ReadyServing(int product, int workerId)
        {
            if (product == 4) return; // Existing Coca barrel starts with physical servings.
            object kitchen = Get<object>(mode, "Kitchen");
            MethodInfo reserve = kitchen.GetType().GetMethod("ReserveCook", new[] {typeof(int), typeof(int)});
            object unit = reserve.Invoke(kitchen, new object[] {product, workerId});
            Assert.IsNotNull(unit);
            int id = Get<int>(unit, "Id");
            Assert.IsTrue((bool)Invoke(kitchen, "AdvanceCook", new object[] {id, workerId, 100f, 1f}));
            Assert.IsTrue((bool)Invoke(kitchen, "TakeCooked", new object[] {id, workerId}));
            Assert.IsTrue((bool)Invoke(kitchen, "DepositCooked", new object[] {id, workerId}));
        }

        private object CallPrivate(string method, params object[] args) =>
            modeType.GetMethod(method, PrivateInstance).Invoke(mode, args);

        private void AssertStationVisualInsideArea(int zone, Vector2 position, float width, float scale)
        {
            Vector2 bounds = (Vector2)CallPrivate("WorkAreaHorizontalBounds", zone);
            float clearance = (float)modeType.GetField("StationOuterClearanceLogical", PublicStatic).GetRawConstantValue() * scale;
            Assert.GreaterOrEqual(position.x - width * scale * .5f, bounds.x + clearance - .0001f,
                "The visual station stays inside its fenced area's left edge.");
            Assert.LessOrEqual(position.x + width * scale * .5f, bounds.y - clearance + .0001f,
                "The visual station stays inside its fenced area's right edge.");
        }

        private object CreateMode(float pixelScale) => CreateMode(config, pixelScale);

        private object CreateMode(object levelConfig, float pixelScale)
        {
            object route = Get<object>(levelConfig, "Route");
            object geometry = geometryType.GetMethod("Generate", PublicStatic).Invoke(null, new[] { route });
            Assert.IsTrue(Get<bool>(geometry, "IsValid"), string.Join("; ", Get<string[]>(Get<object>(geometry, "Validation"), "Errors")));
            Vector2 first = (Vector2)configType.GetMethod("WorkZonePosition", PublicInstance).Invoke(levelConfig, new[] { geometry, (object)0 });
            Vector2 second = (Vector2)configType.GetMethod("WorkZonePosition", PublicInstance).Invoke(levelConfig, new[] { geometry, (object)1 });
            try
            {
                return Activator.CreateInstance(modeType, new object[] { levelConfig, balance, route, pixelScale, first, second });
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException;
            }
        }

        private object CreateIntegratedMode(float pixelScale, int[] workerCounts, long[] ticketWorkers, int startingCoins)
        {
            object route = Get<object>(config, "Route");
            object geometry = geometryType.GetMethod("Generate", PublicStatic).Invoke(null, new[] { route });
            Assert.IsTrue(Get<bool>(geometry, "IsValid"));
            Vector2 first = (Vector2)configType.GetMethod("WorkZonePosition", PublicInstance).Invoke(config, new[] { geometry, (object)0 });
            Vector2 second = (Vector2)configType.GetMethod("WorkZonePosition", PublicInstance).Invoke(config, new[] { geometry, (object)1 });
            try
            {
                return Activator.CreateInstance(modeType, new object[]
                { config, balance, route, pixelScale, first, second, workerCounts, ticketWorkers, startingCoins });
            }
            catch (TargetInvocationException exception) { throw exception.InnerException; }
        }

        private static Type RuntimeType(string shortName) => Type.GetType("HayChoriYPaty." + shortName + ", Assembly-CSharp", true);

        private void CaptureDelivery(object delivery) => capturedDelivery = delivery;

        private void Hire(string roleName, int zone)
        {
            object role = Enum.Parse(RuntimeType("StreetWorkerRole"), roleName);
            Assert.IsTrue((bool)Invoke(mode, "TryHire", new[] { role, (object)zone }), "Expected successful placement: " + roleName + " in area " + zone + ".");
        }

        private void ClearWorkerDeliveryCooldowns()
        {
            var workers = (System.Collections.IList)Get<object>(mode, "Workers");
            for (int i = 0; i < workers.Count; i++)
            {
                FieldInfo cooldown = workers[i].GetType().GetField("<DeliveryCooldownSeconds>k__BackingField", PrivateInstance);
                Assert.IsNotNull(cooldown, "Expected the delivery cooldown backing field on worker state.");
                cooldown.SetValue(workers[i], 0f);
            }
        }

        private static object Invoke(object target, string methodName, object[] arguments = null)
        {
            if (target == null) return null;
            MethodInfo method = target.GetType().GetMethod(methodName, PublicInstance);
            Assert.IsNotNull(method, "Expected public method " + methodName + ".");
            try { return method.Invoke(target, arguments); }
            catch (TargetInvocationException exception) { throw exception.InnerException; }
        }

        private static T Get<T>(object target, string name, object[] unused = null)
        {
            PropertyInfo property = target is Type ? ((Type)target).GetProperty(name, PublicStatic) : target.GetType().GetProperty(name, PublicInstance);
            Assert.IsNotNull(property, "Expected public property " + name + ".");
            return (T)property.GetValue(target is Type ? null : target, null);
        }

        private static void Set(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, PrivateInstance);
            Assert.IsNotNull(field, "Expected private serialized field " + name + ".");
            field.SetValue(target, value);
        }
    }
}
