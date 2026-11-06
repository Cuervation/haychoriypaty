using System;
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
        private Type configType, routeType, geometryType, modeType, balanceType;
        private ScriptableObject config;
        private object balance, mode;

        [SetUp]
        public void SetUp()
        {
            configType = RuntimeType("TowerDefenseLevelConfig");
            routeType = RuntimeType("TowerDefenseRouteGeometryConfig");
            geometryType = RuntimeType("TowerDefenseRouteGeometry");
            modeType = RuntimeType("TowerDefenseGameMode");
            balanceType = RuntimeType("StreetBalance");
            config = ScriptableObject.CreateInstance(configType);
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
            mode = CreateMode(0.02f);
            Invoke(mode, "Begin");

            Assert.AreEqual("tower-defense", Get<string>(mode, "ModeId"));
            Assert.AreEqual("tower-defense.camioneros.plaza-de-mayo.level-1", Get<string>(mode, "LevelId"));
            Assert.AreEqual(500, Get<int>(mode, "Coins"));
            Assert.AreEqual(10, Get<int>(mode, "TotalFanCount"));
            Assert.AreEqual(0, Get<int>(mode, "SpawnedFans"));
            Assert.AreEqual(0, Get<int>(mode, "ServedFans"));
            Assert.IsFalse(Get<bool>(mode, "HasWon"));
            Assert.IsFalse(Get<bool>(mode, "HasLost"));
            Assert.IsTrue(Get<bool>(RuntimeType("TowerDefenseGameMode"), "OwnsRuntime", null));
            Assert.IsTrue(Get<bool>(RuntimeType("TowerDefenseGameMode"), "CampaignStepSuspended", null));
            CollectionAssert.AreEqual(new[] { 0, 4 }, (int[])Get<object>(mode, "AvailableProducts"));
        }

        [Test]
        public void FanWaveIsFiniteAndAcceptsOnlyChoriAndCocaOrders()
        {
            mode = CreateMode(.02f);
            Invoke(mode, "Begin");
            Assert.IsFalse((bool)Invoke(mode, "TrySpawnFan", new[] { new[] { 1 }, new[] { 1 } }), "Paty is not an order in this mode.");
            for (int i = 0; i < 10; i++)
                Assert.IsTrue((bool)Invoke(mode, "TrySpawnFan", new[] { new[] { i % 2 == 0 ? 0 : 4 }, new[] { 1 } }));
            Assert.AreEqual(10, Get<int>(mode, "SpawnedFans"));
            Assert.AreEqual(10, ((System.Collections.IList)Get<object>(mode, "Fans")).Count);
            Assert.IsFalse((bool)Invoke(mode, "TrySpawnFan", new[] { new[] { 0 }, new[] { 1 } }), "No eleventh customer may spawn.");
        }

        [Test]
        public void DeliversOnePhysicalSandwichExactlyOnceAndCompletesCorrectFan()
        {
            object kitchenBalance = balanceType.GetField("kitchen", PublicInstance).GetValue(balance);
            kitchenBalance.GetType().GetField("activeCookSeconds", PublicInstance).SetValue(kitchenBalance, 0.01f);
            mode = CreateMode(0.02f);
            Invoke(mode, "Begin");
            Hire("Parrillero", 0);

            Assert.IsTrue((bool)Invoke(mode, "TrySpawnFan", new[] { new[] { 0 }, new[] { 1 } }));
            // Fill the finite budget with requests that this cook cannot service.
            for (int i = 0; i < 9; i++)
                Assert.IsTrue((bool)Invoke(mode, "TrySpawnFan", new[] {new[] {4}, new[] {1}}));
            Invoke(mode, "Step", new[] {(object).6f});

            Assert.AreEqual(1, Get<int>(mode, "TotalDelivered"));
            Assert.AreEqual(1, Get<int[]>(mode, "DeliveredByProduct")[0]);
            Assert.AreEqual(1, Get<int>(mode, "ServedFans"));
            Assert.AreEqual(0, Get<int>(mode, "EscapedFans"));
            Assert.AreEqual(1, Get<int>(mode, "CustomersAttended"));
            Assert.AreEqual(1, Get<int>(mode, "OrdersCompleted"));
            Assert.AreEqual(5, Get<int>(mode, "CoinsEarned"));
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
        public void TenActualCompleteOrdersFadeOutThenWinWithoutQuotaOrReplacementFan()
        {
            mode = CreateMode(.02f); Invoke(mode, "Begin");
            Assert.IsFalse(Get<bool>(mode, "HasWon"));
            CallPrivate("UpdateCompletionState");
            Assert.IsFalse(Get<bool>(mode, "HasWon"), "An initially empty scene is not a victory.");
            Hire("Cocacolero", 0);
            for (int n = 0; n < 10; n++)
                Assert.IsTrue((bool)Invoke(mode, "TrySpawnFan", new[] {new[] {4}, new[] {2}}));
            var fans = (System.Collections.IList)Get<object>(mode, "Fans");
            object first = fans[0];
            CallPrivate("DeliverWithinServiceRanges");
            Assert.AreEqual(1, Get<int>(first, "PendingUnits"));
            Assert.IsFalse(Get<bool>(first, "IsDeparting"));
            for (int n = 0; n < 19; n++) CallPrivate("DeliverWithinServiceRanges");
            foreach (object fan in fans)
            {
                Assert.IsTrue(Get<bool>(fan, "IsDeparting"));
                Assert.AreEqual(0, Get<int>(fan, "PendingUnits"));
            }
            Assert.IsFalse(Get<bool>(mode, "HasWon"), "Wait for visible departure, not just logical completion.");
            Assert.AreEqual(20, Get<int>(mode, "TotalDelivered"));
            Invoke(mode, "Step", new object[] {.5f});
            Assert.AreEqual(10, Get<int>(mode, "ServedFans"));
            Assert.AreEqual(10, Get<int>(mode, "SpawnedFans"));
            Assert.AreEqual(0, fans.Count);
            Assert.IsTrue(Get<bool>(mode, "HasWon")); Assert.IsFalse(Get<bool>(mode, "HasLost"));
            Assert.AreEqual("Finished", Get<object>(mode, "Phase").ToString());
            Assert.IsFalse((bool)Invoke(mode, "TrySpawnFan", new[] {new[] {4}, new[] {1}}));
            int coins = Get<int>(mode, "Coins");
            Invoke(mode, "Step", new object[] {1f});
            Assert.AreEqual(coins, Get<int>(mode, "Coins"));
            Assert.AreEqual(20, Get<int>(mode, "TotalDelivered"));
        }

        [Test]
        public void NoDeadlineCanEndDefenseAndAutomaticWaveNeverExceedsTen()
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
            for (int n = 1; n < 10; n++)
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
            Assert.AreEqual(10, Get<int>(mode, "SpawnedFans"));
            foreach (object fan in fans)
                foreach (object line in (System.Collections.IEnumerable)Get<object>(fan, "Order"))
                {
                    Assert.Contains(Get<int>(line, "Product"), new[] {0, 4});
                    Assert.That(Get<int>(line, "Remaining"), Is.InRange(1, 4));
                }
            Invoke(mode, "Step", new object[] {1f});
            Assert.AreEqual(10, Get<int>(mode, "SpawnedFans"));
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

        private object CreateMode(float pixelScale)
        {
            object route = Get<object>(config, "Route");
            object geometry = geometryType.GetMethod("Generate", PublicStatic).Invoke(null, new[] { route });
            Assert.IsTrue(Get<bool>(geometry, "IsValid"), string.Join("; ", Get<string[]>(Get<object>(geometry, "Validation"), "Errors")));
            Vector2 first = (Vector2)configType.GetMethod("WorkZonePosition", PublicInstance).Invoke(config, new[] { geometry, (object)0 });
            Vector2 second = (Vector2)configType.GetMethod("WorkZonePosition", PublicInstance).Invoke(config, new[] { geometry, (object)1 });
            try
            {
                return Activator.CreateInstance(modeType, new[] { config, balance, route, (object)pixelScale, first, second });
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

        private void Hire(string roleName, int zone)
        {
            object role = Enum.Parse(RuntimeType("StreetWorkerRole"), roleName);
            Assert.IsTrue((bool)Invoke(mode, "TryHire", new[] { role, (object)zone }), "Expected successful placement: " + roleName + " in area " + zone + ".");
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
