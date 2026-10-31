using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    // Runtime stays in Assembly-CSharp; these focused domain tests bind by reflection.
    public sealed class StreetActiveCooksTests
    {
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private Type productionType, balanceType, simulationType, simulationBalanceType, roleType, stateType, workerType, workerAnimationType, marketType, marketDataType, sceneLayoutType, workstationLayoutType;

        [SetUp]
        public void SetUp()
        {
            productionType = Type.GetType("HayChoriYPaty.StreetKitchenProduction, Assembly-CSharp", true);
            balanceType = Type.GetType("HayChoriYPaty.StreetKitchenBalance, Assembly-CSharp", true);
            simulationType = Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true);
            simulationBalanceType = Type.GetType("HayChoriYPaty.StreetBalance, Assembly-CSharp", true);
            roleType = Type.GetType("HayChoriYPaty.StreetWorkerRole, Assembly-CSharp", true);
            stateType = Type.GetType("HayChoriYPaty.StreetWorkerState, Assembly-CSharp", true);
            workerType = Type.GetType("HayChoriYPaty.StreetWorker, Assembly-CSharp", true);
            workerAnimationType = Type.GetType("HayChoriYPaty.StreetWorkerAnimation, Assembly-CSharp", true);
            marketType = Type.GetType("HayChoriYPaty.StreetMarket, Assembly-CSharp", true);
            marketDataType = Type.GetType("HayChoriYPaty.StreetMarketData, Assembly-CSharp", true);
            sceneLayoutType = Type.GetType("HayChoriYPaty.StreetSceneLayout, Assembly-CSharp", true);
            workstationLayoutType = Type.GetType("HayChoriYPaty.StreetWorkstationLayout, Assembly-CSharp", true);
        }

        [TestCase(.8f)]
        [TestCase(1f)]
        [TestCase(1.25f)]
        public void CookingProgressRingTracksAlphaTrimmedHairThroughEveryCookPose(float verticalScale)
        {
            Type viewType = Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true);
            MethodInfo centerMethod = viewType.GetMethod("CookingProgressRingCenterY", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(centerMethod);
            Type projectionType = Type.GetType("HayChoriYPaty.StreetWorkerProjection, Assembly-CSharp", true);
            MethodInfo hairHeightMethod = projectionType.GetMethod("CookingPoseVisibleHairHeight", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo rootOffsetMethod = projectionType.GetMethod("CookingPoseFootInset", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(hairHeightMethod);
            Assert.IsNotNull(rootOffsetMethod);
            const float ringOuterRadius = 11.5f; // Existing 10-point radius plus half of the 3-point marker.
            float previousHairTop = 0f, previousRingTop = 0f;

            foreach (float workerFeetY in new[] { 270f, 412f, 560f })
            {
                for (int frame = 0; frame < 4; frame++)
                {
                    float visibleHairHeight = Convert.ToSingle(hairHeightMethod.Invoke(null, new object[] { frame }));
                    float spriteRootOffset = Convert.ToSingle(rootOffsetMethod.Invoke(null, new object[] { frame }));
                    float ringCenterY = Convert.ToSingle(centerMethod.Invoke(null,
                        new object[] { workerFeetY, verticalScale, visibleHairHeight, spriteRootOffset }));
                    float hairTopY = workerFeetY * verticalScale + spriteRootOffset - visibleHairHeight;
                    float ringBottomY = ringCenterY + ringOuterRadius;
                    Assert.That(hairTopY - ringBottomY, Is.EqualTo(4f).Within(.001f),
                        "The ring must stay exactly four logical pixels above the alpha-visible hair in each animation frame.");
                    if (frame > 0)
                    {
                        Assert.That(ringCenterY - previousRingTop, Is.EqualTo(hairTopY - previousHairTop).Within(.001f),
                            "The ring must follow the same head-top displacement across cooking-pose transitions.");
                    }
                    previousHairTop = hairTopY;
                    previousRingTop = ringCenterY;
                }
            }
        }

        [Test]
        public void CookingProgressRingUsesPureWhiteForNormalAndExactBlueForPremium()
        {
            Type viewType = Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true);
            MethodInfo colorMethod = viewType.GetMethod("CookingRingProgressColor", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(colorMethod);
            Color normal = (Color)colorMethod.Invoke(null, new[] { EnumValue(roleType, "Parrillero") });
            Color premium = (Color)colorMethod.Invoke(null, new[] { EnumValue(roleType, "ParrilleroPremium") });
            Assert.AreEqual(Color.white, normal);
            Assert.AreEqual(new Color32(0x16, 0x8B, 0xFF, 0xFF), (Color32)premium);
        }

        private object Make(int[] products, bool active, Action<object> configure = null)
        {
            object balance = Activator.CreateInstance(balanceType);
            configure?.Invoke(balance);
            return Activator.CreateInstance(productionType, new object[] { balance, products, null, active });
        }

        private static object Get(object value, string name) => value.GetType().GetProperty(name, PublicInstance).GetValue(value);
        private static object GetField(object value, string name) => value.GetType().GetField(name, PublicInstance).GetValue(value);
        private static object GetInternalField(object value, string name) =>
            value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(value);
        private static object Call(object value, string name, params object[] args)
        {
            MethodInfo[] candidates = value.GetType().GetMethods(PublicInstance);
            foreach (MethodInfo candidate in candidates)
            {
                if (candidate.Name != name) continue;
                ParameterInfo[] parameters = candidate.GetParameters();
                if (parameters.Length != args.Length) continue;
                bool matches = true;
                for (int i = 0; i < parameters.Length; i++)
                    if (args[i] != null && !parameters[i].ParameterType.IsInstanceOfType(args[i])) { matches = false; break; }
                if (matches) return candidate.Invoke(value, args);
            }
            throw new MissingMethodException(value.GetType().FullName, name);
        }
        private static float LayoutConstant(Type type, string name) =>
            Convert.ToSingle(type.GetField(name, BindingFlags.Public | BindingFlags.Static).GetRawConstantValue());

        private static IList Units(object kitchen) => (IList)Get(kitchen, "Units");
        private static IList Workers(object sim) => (IList)Get(sim, "Workers");
        private static object EnumValue(Type type, string name) => Enum.Parse(type, name);
        private static void SetWorker(object worker, string name, object value)
        {
            PropertyInfo property = worker.GetType().GetProperty(name, PublicInstance);
            property.GetSetMethod(true).Invoke(worker, new[] { value });
        }
        private object AnimationCall(string name, params object[] args)
        {
            foreach (MethodInfo method in workerAnimationType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (method.Name != name || method.GetParameters().Length != args.Length) continue;
                return method.Invoke(null, args);
            }
            throw new MissingMethodException("StreetWorkerAnimation." + name);
        }

        [Test]
        public void CookingToolFramesFollowRealProgressAndRepeatWithWorkerPhase()
        {
            Assert.AreEqual(new[] { 0, 1, 2, 3, 0, 1, 2, 3 }, new[] { .02f, .14f, .26f, .39f, .52f, .64f, .76f, .89f }
                .Select(progress => (int)AnimationCall("CookingFrameForProgress", progress, 0)).ToArray());
            Assert.AreEqual(2, AnimationCall("CookingFrameForProgress", .02f, 2),
                "Concurrent cooks start at different hand-tool phases so their arm motion does not synchronize.");

            object worker = Activator.CreateInstance(workerType);
            SetWorker(worker, "Role", EnumValue(roleType, "ParrilleroPremium"));
            SetWorker(worker, "State", EnumValue(stateType, "Cooking"));
            SetWorker(worker, "FacingVector", Vector2.right);
            SetWorker(worker, "CookingProgress", .26f);
            SetWorker(worker, "CookingCycleProgress", .26f);
            object pose = AnimationCall("Sample", worker, false);
            Assert.IsTrue((bool)GetField(pose, "Cooking"));
            Assert.IsTrue((bool)GetField(pose, "WorkingAtGrill"));
            Assert.AreEqual("Down", GetField(pose, "Facing").ToString(),
                "Both normal and Premium workers turn toward the grill while cooking.");
            Assert.AreEqual(2, GetField(pose, "CookingFrame"));
        }

        [Test]
        public void SpecificIdleGrillPoseIsNotAppliedToCooksAwayFromTheirStation()
        {
            object worker = Activator.CreateInstance(workerType);
            SetWorker(worker, "Role", EnumValue(roleType, "Parrillero"));
            SetWorker(worker, "State", EnumValue(stateType, "Idle"));
            SetWorker(worker, "FacingVector", Vector2.right);

            object away = AnimationCall("Sample", worker, false);
            object atGrill = AnimationCall("Sample", worker, true);
            Assert.IsFalse((bool)GetField(away, "WorkingAtGrill"));
            Assert.AreEqual("Right", GetField(away, "Facing").ToString());
            Assert.IsTrue((bool)GetField(atGrill, "WorkingAtGrill"));
            Assert.AreEqual("Down", GetField(atGrill, "Facing").ToString());
        }

        private object MakeActiveSimulation(int level, int startingCoins, int[] goals = null)
        {
            object balance = MakeActiveBalance(level, startingCoins, goals);
            return Activator.CreateInstance(simulationType, new object[] { balance, level, 5f, 0, 1, 0 });
        }

        private object MakeActiveBalance(int level, int startingCoins, int[] goals = null)
        {
            object balance = Activator.CreateInstance(simulationBalanceType);
            simulationBalanceType.GetField("maxCustomers", PublicInstance).SetValue(balance, 9);
            simulationBalanceType.GetField("customerArrivalSeconds", PublicInstance).SetValue(balance, 10000f);
            simulationBalanceType.GetField("workerSpeed", PublicInstance).SetValue(balance, 900f);
            simulationBalanceType.GetField("pickupSeconds", PublicInstance).SetValue(balance, .05f);
            Array levels = (Array)simulationBalanceType.GetField("levelBalances", PublicInstance).GetValue(balance);
            object row = levels.GetValue(level);
            row.GetType().GetField("startingCoins", PublicInstance).SetValue(row, startingCoins);
            row.GetType().GetField("goalsByProductId", PublicInstance).SetValue(row,
                goals == null ? new[] { 1000, 1000, 1000, 1000, 1000, 1000, 1000 } : (int[])goals.Clone());
            float[] durations = (float[])simulationBalanceType.GetField("levelDurations", PublicInstance).GetValue(balance);
            durations[level] = 1000f;
            return balance;
        }

        private static void SetArrivalFarFuture(object sim)
        {
            sim.GetType().GetField("arrival", PrivateInstance).SetValue(sim, 10000f);
        }

        private static void Step(object sim, int ticks)
        {
            for (int i = 0; i < ticks; i++) Call(sim, "Step", .05f);
        }

        private void SeedCookedUnits(object kitchen, int product, int count, int firstWorkerId)
        {
            for (int i = 0; i < count; i++)
            {
                int workerId = firstWorkerId + i;
                object unit = Call(kitchen, "ReserveCook", product, workerId);
                Assert.IsNotNull(unit, "Could not seed product " + product + " unit " + i + ".");
                int id = (int)Get(unit, "Id");
                Assert.IsTrue((bool)Call(kitchen, "AdvanceCook", id, workerId, 2f, 1f));
                Assert.IsTrue((bool)Call(kitchen, "TakeCooked", id, workerId));
                Assert.IsTrue((bool)Call(kitchen, "DepositCooked", id, workerId));
            }
        }

        private List<int> WorkerProductsForRole(object sim, string roleName)
        {
            var products = new List<int>();
            foreach (object worker in Workers(sim))
                if (Get(worker, "Role").ToString() == roleName) products.Add((int)Get(worker, "Product"));
            return products;
        }

        private List<int> CollectRealCookAssignments(object sim, string roleName, int count, int maxTicks = 3000)
        {
            var products = new List<int>();
            var seenUnits = new HashSet<int>();
            for (int tick = 0; tick < maxTicks && products.Count < count; tick++)
            {
                Step(sim, 1);
                foreach (object worker in Workers(sim))
                {
                    if (Get(worker, "Role").ToString() != roleName) continue;
                    int itemId = (int)Get(worker, "CookingItemId");
                    if (itemId > 0 && seenUnits.Add(itemId))
                        products.Add((int)Get(worker, "Product"));
                    if (products.Count >= count) break;
                }
            }
            return products;
        }

        private List<int> CaptureVisualProductChanges(object sim, string roleName, int ticks)
        {
            var products = new List<int>();
            var previousByWorker = new Dictionary<int, int>();
            for (int tick = 0; tick < ticks; tick++)
            {
                Step(sim, 1);
                foreach (object worker in Workers(sim))
                {
                    if (Get(worker, "Role").ToString() != roleName || !(bool)Get(worker, "VisualCooking")) continue;
                    int id = (int)Get(worker, "Id");
                    int product = (int)Get(worker, "VisualCookingProduct");
                    if (!previousByWorker.TryGetValue(id, out int previous) || previous != product)
                    {
                        products.Add(product);
                        previousByWorker[id] = product;
                    }
                }
            }
            return products;
        }

        [Test]
        public void SpeedUpgradesGiveCooksSixPercentAndOtherWorkersTenPercent()
        {
            object sim = MakeActiveSimulation(5, 1000000);
            Call(sim, "StartRound");
            Assert.AreEqual(1f, Get(sim, "WorkRate"));
            Assert.AreEqual(1f, Get(sim, "CookWorkRate"));

            Assert.IsTrue((bool)Call(sim, "TryUpgradeSpeed"));
            Assert.That((float)Get(sim, "WorkRate"), Is.EqualTo(1.10f).Within(.0001f));
            Assert.That((float)Get(sim, "CookWorkRate"), Is.EqualTo(1.06f).Within(.0001f));
            MethodInfo workRateFor = simulationType.GetMethod("WorkRateFor", PrivateInstance);
            foreach (string role in new[] { "Parrillero", "ParrilleroPremium" })
                Assert.That((float)workRateFor.Invoke(sim, new[] { EnumValue(roleType, role) }), Is.EqualTo(1.06f).Within(.0001f));

            Assert.IsTrue((bool)Call(sim, "TryUpgradeSpeed"));
            Assert.That((float)Get(sim, "CookWorkRate"), Is.EqualTo(1.12f).Within(.0001f));
            Assert.That((float)Get(sim, "WorkRate"), Is.EqualTo(1.20f).Within(.0001f));
            for (int i = 2; i < 9; i++) Assert.IsTrue((bool)Call(sim, "TryUpgradeSpeed"));
            Assert.That((float)Get(sim, "WorkRate"), Is.EqualTo(1.90f).Within(.0001f));
            Assert.That((float)Get(sim, "CookWorkRate"), Is.EqualTo(1.54f).Within(.0001f));

            foreach (string role in new[] { "Parrillero", "ParrilleroPremium" })
                Assert.That((float)workRateFor.Invoke(sim, new[] { EnumValue(roleType, role) }), Is.EqualTo(1.54f).Within(.0001f));
            foreach (string role in new[] { "Cocacolero", "Fernetero", "Mozo" })
                Assert.That((float)workRateFor.Invoke(sim, new[] { EnumValue(roleType, role) }), Is.EqualTo(1.90f).Within(.0001f));
        }

        [Test]
        public void CookAssignmentsAlternateGloballyForOneThroughFiveWorkers()
        {
            int[] expectedNormal = { 0, 1, 0, 1, 0 };
            int[] expectedPremium = { 2, 3, 2, 3, 2 };
            for (int workerCount = 1; workerCount <= 5; workerCount++)
            {
                object normal = MakeActiveSimulation(5, 100000);
                Call(normal, "StartRound");
                SetArrivalFarFuture(normal);
                for (int i = 1; i < workerCount; i++)
                    Assert.IsTrue((bool)Call(normal, "TryHire", EnumValue(roleType, "Parrillero")));
                CollectionAssert.AreEqual(expectedNormal,
                    CollectRealCookAssignments(normal, "Parrillero", expectedNormal.Length),
                    "The shared Normal sequence must stay Chori/Paty with " + workerCount + " workers.");
                Assert.AreEqual(0, Get(normal, "Delivered"), "Background cooking never creates a sale.");

                object premium = MakeActiveSimulation(5, 100000);
                Call(premium, "StartRound");
                SetArrivalFarFuture(premium);
                for (int i = 0; i < workerCount; i++)
                    Assert.IsTrue((bool)Call(premium, "TryHire", EnumValue(roleType, "ParrilleroPremium")));
                CollectionAssert.AreEqual(expectedPremium,
                    CollectRealCookAssignments(premium, "ParrilleroPremium", expectedPremium.Length),
                    "The shared Premium sequence must stay Bondiola/Vacío with " + workerCount + " workers.");
                Assert.AreEqual(0, Get(premium, "Delivered"));
            }
        }

        [Test]
        public void CookAlternationAndProductAvailabilityHoldAcrossAllActiveLevels()
        {
            int[] expectedNormal = { 0, 1, 0, 1 };
            int[] expectedPremium = { 2, 3, 2, 3 };
            for (int level = 5; level < 11; level++)
            {
                object normal = MakeActiveSimulation(level, 100000);
                Assert.IsTrue((bool)Get(normal, "UsesActiveCooks"), "Racing through Los Redondos must use active cooks.");
                for (int product = 0; product < 4; product++)
                    Assert.IsTrue((bool)Call(normal, "IsProductAvailable", product),
                        "All four grill products are enabled in level " + (level + 1) + ".");
                Call(normal, "StartRound");
                SetArrivalFarFuture(normal);
                CollectionAssert.AreEqual(expectedNormal, CollectRealCookAssignments(normal, "Parrillero", 4),
                    "Normal schedule changed at level " + (level + 1) + ".");

                object premium = MakeActiveSimulation(level, 100000);
                Call(premium, "StartRound");
                SetArrivalFarFuture(premium);
                for (int i = 0; i < 2; i++)
                    Assert.IsTrue((bool)Call(premium, "TryHire", EnumValue(roleType, "ParrilleroPremium")));
                CollectionAssert.AreEqual(expectedPremium, CollectRealCookAssignments(premium, "ParrilleroPremium", 4),
                    "Premium schedule changed at level " + (level + 1) + ".");
            }
        }

        [Test]
        public void HiringDuringRoundUsesTheNextSharedAlternatingTurn()
        {
            object sim = MakeActiveSimulation(5, 100000);
            Call(sim, "StartRound");
            SetArrivalFarFuture(sim);

            int firstItemId = 0;
            int firstProduct = -1;
            for (int tick = 0; tick < 3000 && firstItemId == 0; tick++)
            {
                Step(sim, 1);
                foreach (object worker in Workers(sim))
                    if (Get(worker, "Role").ToString() == "Parrillero" && (int)Get(worker, "CookingItemId") > 0)
                    {
                        firstItemId = (int)Get(worker, "CookingItemId");
                        firstProduct = (int)Get(worker, "Product");
                        break;
                    }
            }

            Assert.Greater(firstItemId, 0, "The first cook must hold a real assigned unit before the mid-round hire.");
            Assert.AreEqual(0, firstProduct);
            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "Parrillero")));

            int nextProduct = -1;
            for (int tick = 0; tick < 3000 && nextProduct < 0; tick++)
            {
                Step(sim, 1);
                foreach (object worker in Workers(sim))
                    if (Get(worker, "Role").ToString() == "Parrillero" &&
                        (int)Get(worker, "CookingItemId") > 0 && (int)Get(worker, "CookingItemId") != firstItemId)
                    {
                        nextProduct = (int)Get(worker, "Product");
                        break;
                    }
            }

            Assert.AreEqual(1, nextProduct,
                "Hiring during a live round must continue the shared sequence at Paty rather than resetting to Chori.");
        }

        [Test]
        public void CompletedQuotaDoesNotBlockRealProductionOrGlobalAlternation()
        {
            int[] goals = { 100, 50, 80, 40, 1000, 1000, 1000 };
            object sim = MakeActiveSimulation(5, 100000, goals);
            Call(sim, "StartRound");
            SetArrivalFarFuture(sim);
            ((int[])simulationType.GetField("deliveredByProduct", PrivateInstance).GetValue(sim))[0] = goals[0];

            List<int> sequence = CollectRealCookAssignments(sim, "Parrillero", 4);
            CollectionAssert.AreEqual(new[] { 0, 1, 0, 1 }, sequence,
                "A completed Chori quota must still produce real units in the shared alternating sequence.");
            Assert.AreEqual("Playing", Get(sim, "Phase").ToString(),
                "Unfinished beverage goals keep the level active while completed sandwich goals continue producing.");
        }

        [TestCase(0, "Parrillero", 1)]
        [TestCase(2, "ParrilleroPremium", 3)]
        public void FullProductIsSkippedUntilItsTableSlotIsFreed(int fullProduct, string role, int alternateProduct)
        {
            object sim = MakeActiveSimulation(5, 100000, new[] { 100, 50, 80, 40, 1000, 1000, 1000 });
            Call(sim, "StartRound");
            SetArrivalFarFuture(sim);
            object kitchen = Get(sim, "Kitchen");
            SeedCookedUnits(kitchen, fullProduct, 48, 2000 + fullProduct * 100);

            List<int> firstAssignment = CollectRealCookAssignments(sim, role, 1);
            CollectionAssert.AreEqual(new[] { alternateProduct }, firstAssignment,
                "A full product is not cooked cosmetically or physically while its specialty partner has room.");
            object worker = Workers(sim).Cast<object>().First(candidate => Get(candidate, "Role").ToString() == role);
            int firstUnitId = (int)Get(worker, "CookingItemId");
            Assert.Greater(firstUnitId, 0);

            int waiter = 9000 + fullProduct;
            Assert.IsNotNull(Call(kitchen, "ReserveTableUnit", fullProduct, waiter));
            object carried = Call(kitchen, "TryTake", fullProduct, EnumValue(roleType, "Mozo"), waiter);
            Assert.IsNotNull(carried);
            Assert.IsTrue((bool)Call(kitchen, "Consume", Get(carried, "Id"), fullProduct, waiter));

            int resumedProduct = -1;
            for (int tick = 0; tick < 3000 && resumedProduct < 0; tick++)
            {
                Step(sim, 1);
                int id = (int)Get(worker, "CookingItemId");
                if (id > 0 && id != firstUnitId) resumedProduct = (int)Get(worker, "Product");
            }
            Assert.AreEqual(fullProduct, resumedProduct,
                "After a Mozo sells one sandwich, the full product returns on its next shared alternating turn.");
        }

        [Test]
        public void ActiveProductsProduceRealUnitsAtZeroStockAfterTheirGoalsAreComplete()
        {
            int[] goals = { 8, 8, 8, 8, 1000, 1000, 1000 };
            object balance = MakeActiveBalance(5, 100000, goals);
            object sim = Activator.CreateInstance(simulationType, new object[] { balance, 5, 5f, 0, 1, 0 });
            object data = Activator.CreateInstance(marketDataType);
            int[] stocked = Enumerable.Repeat(10000, 10).ToArray();
            marketDataType.GetField("stockUnits", PublicInstance).SetValue(data, stocked);
            object market = Activator.CreateInstance(marketType, new[] { data, balance });
            Assert.IsTrue((bool)Call(market, "BeginAttempt", sim));
            Call(sim, "BindIngredientStock", market);
            Call(sim, "StartRound");
            SetArrivalFarFuture(sim);
            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "ParrilleroPremium")));

            stocked = (int[])marketDataType.GetField("stockUnits", PublicInstance).GetValue(Get(market, "Data"));
            Array.Clear(stocked, 0, stocked.Length);
            int[] delivered = (int[])simulationType.GetField("deliveredByProduct", PrivateInstance).GetValue(sim);
            for (int product = 0; product < 4; product++) delivered[product] = goals[product];
            Assert.IsFalse((bool)Get(sim, "GoalReached"), "Outstanding beverage quotas keep the level active.");

            Step(sim, 400);
            object kitchen = Get(sim, "Kitchen");
            var ids = new HashSet<int>();
            foreach (object unit in Units(kitchen))
                Assert.IsTrue(ids.Add((int)Get(unit, "Id")), "Every real sandwich has one unique physical ID.");
            for (int product = 0; product < 4; product++)
            {
                Assert.IsFalse((bool)Call(market, "CanPrepareFood", product), "The Market really has no recipe stock.");
                Assert.IsTrue((bool)Call(kitchen, "HasSupply", product), "Active sandwich demand remains supply-eligible without stock.");
                Assert.Greater(Units(kitchen).Cast<object>().Count(unit => (int)Get(unit, "Product") == product &&
                    Get(unit, "State").ToString() != "Burned"), 0,
                    "Product " + product + " must have real units after its goal is complete and stock is zero.");
            }
            CollectionAssert.AreEqual(new int[10], stocked, "Active cooking cannot debit, reserve, or drive ingredient stock negative.");
            Assert.AreEqual("Playing", Get(sim, "Phase").ToString());
        }

        [Test]
        public void SalesContinueAfterFourSandwichGoalsAtZeroIngredients()
        {
            int[] goals = { 1, 1, 1, 1, 1000, 1000, 1000 };
            object balance = MakeActiveBalance(5, 100000, goals);
            object sim = Activator.CreateInstance(simulationType, new object[] { balance, 5, 5f, 0, 1, 0 });
            object data = Activator.CreateInstance(marketDataType);
            int[] stocked = Enumerable.Repeat(10000, 10).ToArray();
            marketDataType.GetField("stockUnits", PublicInstance).SetValue(data, stocked);
            object market = Activator.CreateInstance(marketType, new[] { data, balance });
            Assert.IsTrue((bool)Call(market, "BeginAttempt", sim));
            Call(sim, "BindIngredientStock", market);
            Call(sim, "StartRound");
            SetArrivalFarFuture(sim);
            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "ParrilleroPremium")));
            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "Mozo")));
            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "Mozo")));

            int[] delivered = (int[])simulationType.GetField("deliveredByProduct", PrivateInstance).GetValue(sim);
            for (int product = 0; product < 4; product++) delivered[product] = goals[product];
            Assert.IsFalse((bool)Get(sim, "GoalReached"), "The level remains active because beverage goals are unfinished.");
            stocked = (int[])marketDataType.GetField("stockUnits", PublicInstance).GetValue(Get(market, "Data"));
            Array.Clear(stocked, 0, stocked.Length);
            for (int product = 0; product < 4; product++)
            {
                Assert.IsTrue((bool)Call(sim, "IsProductAvailable", product));
                Assert.IsFalse((bool)Call(market, "CanPrepareFood", product));
                Assert.IsTrue((bool)Call(sim, "SpawnCustomer", product, 1), "Spawn post-goal extra order for product " + product);
            }

            int coinsBeforeSales = (int)Get(sim, "Coins");
            int expectedRevenue = 0;
            for (int product = 0; product < 4; product++)
                expectedRevenue += Mathf.RoundToInt((float)Call(sim, "GetProductPrice", product));
            object kitchen = Get(sim, "Kitchen");
            var productsSeenOnTable = new HashSet<int>();
            for (int tick = 0; tick < 4000; tick++)
            {
                Call(sim, "Step", .05f);
                var ids = new HashSet<int>();
                foreach (object unit in Units(kitchen))
                {
                    Assert.IsTrue(ids.Add((int)Get(unit, "Id")), "Live physical units cannot share IDs.");
                    int product = (int)Get(unit, "Product");
                    if (product < 4 && Get(unit, "Location").ToString() == "Table") productsSeenOnTable.Add(product);
                }
                bool allExtrasSold = true;
                for (int product = 0; product < 4; product++)
                    allExtrasSold &= (int)Call(sim, "GetProductDelivered", product) == goals[product] + 1;
                if (allExtrasSold) break;
            }

            for (int product = 0; product < 4; product++)
            {
                Assert.AreEqual(goals[product] + 1, Call(sim, "GetProductDelivered", product),
                    "Each product receives exactly one real extra sale after its goal.");
                Assert.IsTrue(productsSeenOnTable.Contains(product), "Product " + product + " must be physically deposited before Mozo delivery.");
            }
            Assert.AreEqual(4, Get(sim, "Delivered"), "Each post-goal order is handed off exactly once.");
            Assert.AreEqual(coinsBeforeSales + expectedRevenue, Get(sim, "Coins"), "Extra sales register revenue exactly once.");
            Assert.AreEqual(expectedRevenue, Get(sim, "CoinsEarned"));
            Assert.AreEqual("Playing", Get(sim, "Phase").ToString(), "No victory condition is changed by extra sales.");
            CollectionAssert.AreEqual(new int[10], stocked, "No sandwich sale consumes or overdraws Market ingredients.");
        }

        [Test]
        public void ActiveSandwichTransactionsDoNotDependOnMarketStock()
        {
            object balance = MakeActiveBalance(5, 100000);
            object sim = Activator.CreateInstance(simulationType, new object[] { balance, 5, 5f, 0, 1, 0 });
            object data = Activator.CreateInstance(marketDataType);
            int[] stocked = Enumerable.Repeat(10000, 10).ToArray();
            marketDataType.GetField("stockUnits", PublicInstance).SetValue(data, stocked);
            object market = Activator.CreateInstance(marketType, new[] { data, balance });
            Assert.IsTrue((bool)Call(market, "BeginAttempt", sim));
            Call(sim, "BindIngredientStock", market);
            Call(sim, "StartRound");
            SetArrivalFarFuture(sim);
            stocked = (int[])marketDataType.GetField("stockUnits", PublicInstance).GetValue(Get(market, "Data"));
            Array.Clear(stocked, 0, stocked.Length);

            for (int roleIndex = 0; roleIndex < 2; roleIndex++)
            {
                string roleName = roleIndex == 0 ? "Parrillero" : "ParrilleroPremium";
                if (roleIndex == 1) Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, roleName)));
                bool sawRealUnit = false;
                object kitchen = Get(sim, "Kitchen");
                for (int tick = 0; tick < 80; tick++)
                {
                    Step(sim, 1);
                    object worker = Workers(sim).Cast<object>().First(candidate => Get(candidate, "Role").ToString() == roleName);
                    int itemId = (int)Get(worker, "CookingItemId");
                    if (itemId <= 0) continue;
                    Assert.IsFalse((bool)Get(worker, "VisualCooking"), roleName + " must not use a cosmetic cycle while capacity is available.");
                    Assert.IsNotNull(Call(kitchen, "Find", itemId));
                    sawRealUnit = true;
                }
                Assert.IsTrue(sawRealUnit, roleName + " must own real units despite zero ingredient stock.");
            }
            CollectionAssert.AreEqual(new int[10], stocked);
        }

        [Test]
        public void FullCookTablesKeepVisualAlternationAndResumeOnTheNextEligibleTurn()
        {
            object sim = MakeActiveSimulation(5, 100000);
            Call(sim, "StartRound");
            SetArrivalFarFuture(sim);
            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "ParrilleroPremium")));
            object kitchen = Get(sim, "Kitchen");
            for (int product = 0; product < 4; product++)
                SeedCookedUnits(kitchen, product, (int)Call(kitchen, "TableCapacity", product), 5000 + product * 100);

            int fullUnitCount = Units(kitchen).Count;
            Step(sim, 100);
            foreach (object worker in Workers(sim))
            {
                string role = Get(worker, "Role").ToString();
                if (role != "Parrillero" && role != "ParrilleroPremium") continue;
                Assert.IsTrue((bool)Get(worker, "VisualCooking"), role + " keeps a product-neutral simulated cycle while tables are full.");
                Assert.AreEqual(-1, Get(worker, "VisualCookingProduct"), "No capped product is represented as being cooked.");
                Assert.AreEqual(0, Get(worker, "CookingItemId"));
                Assert.AreEqual(0, Get(worker, "CarriedItemId"));
            }
            Assert.AreEqual(fullUnitCount, Units(kitchen).Count,
                "Repeated cosmetic cycles reuse worker presentation and add no food objects.");

            foreach (int product in new[] { 0, 2 })
            {
                int owner = 9000 + product;
                object reserved = Call(kitchen, "ReserveTableUnit", product, owner);
                Assert.IsNotNull(reserved);
                object carried = Call(kitchen, "TryTake", product, EnumValue(roleType, "Mozo"), owner);
                Assert.IsNotNull(carried);
                Assert.IsTrue((bool)Call(kitchen, "Consume", Get(carried, "Id"), product, owner));
            }
            Assert.AreEqual(fullUnitCount - 2, Units(kitchen).Count);
            foreach (int product in new[] { 0, 2 })
                Assert.IsTrue((bool)Call(kitchen, "CanReserveCook", product), "Product " + product + " should have one freed reserved slot.");

            Step(sim, 61);
            foreach (object worker in Workers(sim))
            {
                string role = Get(worker, "Role").ToString();
                if (role != "Parrillero" && role != "ParrilleroPremium") continue;
                int expectedProduct = role == "Parrillero" ? 0 : 2;
                Assert.AreEqual(48, (int)Call(kitchen, "TableCount", expectedProduct),
                    role + " must resume real production after its product's slot is freed, then stop at 48 again.");
                Assert.AreEqual(0, Get(worker, "CookingItemId"), role + " must not leave a 49th unit in progress at the cap.");
                Assert.AreEqual(0, Get(worker, "CarriedItemId"));
                Assert.IsTrue((bool)Get(worker, "VisualCooking"), role + " remains visibly active in the product-neutral full-table cycle.");
                Assert.AreEqual(-1, Get(worker, "VisualCookingProduct"), "No capped sandwich is represented as cooking.");
            }
            Assert.AreEqual(fullUnitCount, Units(kitchen).Count,
                "Exactly two real units replace the two consumed table units; visual cycles add none.");
        }

        [Test]
        public void AutomaticKitchenUsesCookRateWithoutChangingBeverageRate()
        {
            object kitchen = Make(new[] { 0, 4 }, false, balance =>
            {
                balanceType.GetField("cookSeconds", PublicInstance).SetValue(balance, 1f);
                balanceType.GetField("drinkPrepareSeconds", PublicInstance).SetValue(balance, .28f);
                balanceType.GetField("barrelCapacity", PublicInstance).SetValue(balance, 1);
            });
            Assert.IsNotNull(Call(kitchen, "TryTake", 4, EnumValue(roleType, "Cocacolero"), 8000));
            Call(kitchen, "Advance", .27f, 1.06f, 1.10f, true, false, true, false);
            Assert.AreEqual(1, Call(kitchen, "TableCount", 4), "Beverage preparation continues at its independent 1.10 rate.");
            object raw = Units(kitchen).Cast<object>().First(unit => (int)Get(unit, "Product") == 0 &&
                Get(unit, "Location").ToString() == "Grill");
            Assert.That((float)Get(raw, "Progress"), Is.EqualTo(.2862f).Within(.001f),
                "Automatic meat cooking uses the differentiated 1.06 cook rate.");
        }

        [Test]
        public void LegacyLevelKeepsAutomaticProductionAndUsesCookUpgradeRate()
        {
            object sim = MakeActiveSimulation(0, 100000);
            Call(sim, "StartRound");
            SetArrivalFarFuture(sim);
            Assert.IsTrue((bool)Call(sim, "TryUpgradeSpeed"));
            Step(sim, 1);

            object kitchen = Get(sim, "Kitchen");
            object raw = Units(kitchen).Cast<object>().First(unit => (int)Get(unit, "Product") == 0 &&
                Get(unit, "Location").ToString() == "Grill");
            Assert.AreEqual("Cooking", Get(raw, "State").ToString());
            Assert.That((float)Get(raw, "Progress"), Is.EqualTo(.01325f).Within(.0001f),
                "Level 1's automatic kitchen must use +6% Parrillero speed without requiring a customer.");
            Assert.AreEqual(0, ((IEnumerable)Get(sim, "Customers")).Cast<object>().Count());
            Assert.Greater((int)Call(kitchen, "TableCount", 0), 0, "The existing automatic starting-stock flow remains intact.");
        }

        [Test]
        public void ActiveSandwichTablesStartEmptyAndLegacyProductionRetainsStock()
        {
            int[] sandwiches = { 0, 1, 2, 3 };
            object legacy = Make(sandwiches, false);
            object active = Make(new[] { 0, 1, 2, 3, 4 }, true);
            for (int i = 0; i < sandwiches.Length; i++)
            {
                Assert.Greater((int)Call(legacy, "TableCount", sandwiches[i]), 0, "Legacy/table-first mode is preserved.");
                Assert.AreEqual(0, (int)Call(active, "TableCount", sandwiches[i]), "Active cooks must produce each sandwich unit.");
            }
            Assert.Greater((int)Call(active, "TableCount", 4), 0, "Beverage stock remains on its existing path.");
        }

        [Test]
        public void ActiveCookReservesUniqueRawUnitAndProgressScalesWithWorkRate()
        {
            object kitchen = Make(new[] { 0 }, true);
            object first = Call(kitchen, "ReserveCook", 0, 1);
            object second = Call(kitchen, "ReserveCook", 0, 2);
            Assert.IsNotNull(first);
            Assert.IsNotNull(second);
            int firstId = (int)Get(first, "Id"), secondId = (int)Get(second, "Id");
            Assert.AreNotEqual(firstId, secondId);
            Assert.AreEqual("Raw", Get(first, "State").ToString(), "Dispatch reserves one identifiable raw grill unit; cooking starts on arrival.");

            Assert.IsFalse((bool)Call(kitchen, "AdvanceCook", firstId, 1, 1f, 1f));
            Assert.That((float)Get(first, "Progress"), Is.EqualTo(.5f).Within(.001f));
            Assert.IsTrue((bool)Call(kitchen, "AdvanceCook", firstId, 1, .5f, 2f));
            Assert.That((float)Get(first, "Progress"), Is.EqualTo(1f).Within(.001f));
            Assert.IsFalse((bool)Call(kitchen, "AdvanceCook", firstId, 99, 1f, 1f), "Only the owning worker advances its cook.");
        }

        [Test]
        public void ActiveCookDefaultDurationIsTwoSeconds()
        {
            object balance = Activator.CreateInstance(balanceType);
            float duration = (float)balanceType.GetField("activeCookSeconds", PublicInstance).GetValue(balance);
            Assert.That(duration, Is.EqualTo(2f).Within(.0001f));

            object kitchen = Make(new[] { 0 }, true);
            object unit = Call(kitchen, "ReserveCook", 0, 5);
            int id = (int)Get(unit, "Id");
            Assert.IsFalse((bool)Call(kitchen, "AdvanceCook", id, 5, 1.99f, 1f));
            Assert.IsTrue((bool)Call(kitchen, "AdvanceCook", id, 5, .01f, 1f));
            Assert.AreEqual("Cooked", Get(unit, "State").ToString());
        }

        [Test]
        public void CookReservationUsesPreferredFreeGrillSlotAndFallsBackWhenTaken()
        {
            object kitchen = Make(new[] { 0 }, true);
            int capacity = (int)Call(kitchen, "GrillCapacity", 0);
            Assert.Greater(capacity, 1);
            int preferred = capacity - 1;
            object first = Call(kitchen, "ReserveCook", 0, 11, preferred);
            Assert.IsNotNull(first);
            Assert.AreEqual(preferred, (int)Get(first, "Slot"));
            Assert.IsFalse((bool)Call(kitchen, "IsGrillSlotFree", 0, preferred));

            object second = Call(kitchen, "ReserveCook", 0, 12, preferred);
            Assert.IsNotNull(second);
            Assert.AreNotEqual(preferred, (int)Get(second, "Slot"), "A preferred but occupied slot must fall back to another valid slot.");
            Assert.IsFalse((bool)Call(kitchen, "IsGrillSlotFree", 0, (int)Get(second, "Slot")));
        }

        private object DepositTestSandwich(object kitchen, int product, int cookId)
        {
            object unit = Call(kitchen, "ReserveCook", product, cookId);
            Assert.NotNull(unit);
            FinishTestSandwich(kitchen, unit, cookId);
            return unit;
        }

        private void FinishTestSandwich(object kitchen, object unit, int cookId)
        {
            int id = (int)Get(unit, "Id");
            Assert.IsTrue((bool)Call(kitchen, "AdvanceCook", id, cookId, 10f, 1f));
            Assert.IsTrue((bool)Call(kitchen, "TakeCooked", id, cookId));
            Assert.IsTrue((bool)Call(kitchen, "DepositCooked", id, cookId));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void MozoReservesLastDepositedProductNotLastCreatedAndKeepsItsClaim(int product)
        {
            object kitchen = Make(new[] { product }, true);
            object createdFirst = Call(kitchen, "ReserveCook", product, 11);
            object createdSecond = Call(kitchen, "ReserveCook", product, 12);
            FinishTestSandwich(kitchen, createdSecond, 12);
            FinishTestSandwich(kitchen, createdFirst, 11); // Earlier-created cook arrives last.
            Assert.AreSame(createdFirst, Call(kitchen, "ReserveTableUnit", product, 100));
            object evenNewer = DepositTestSandwich(kitchen, product, 13);
            Assert.AreSame(createdFirst, Call(kitchen, "ReserveTableUnit", product, 100), "Do not redirect a Mozo already walking to a claimed slot.");
            Assert.AreSame(evenNewer, Call(kitchen, "ReserveTableUnit", product, 101));
            Assert.AreSame(createdSecond, Call(kitchen, "ReserveTableUnit", product, 102));
            Assert.IsNull(Call(kitchen, "ReserveTableUnit", product, 103), "No duplicate claim or fictitious unit.");
            Assert.AreSame(createdFirst, Call(kitchen, "TryTake", product, EnumValue(roleType, "Mozo"), 100));
            int id = (int)Get(createdFirst, "Id");
            Assert.IsTrue((bool)Call(kitchen, "Consume", id, product, 100));
            Assert.IsFalse((bool)Call(kitchen, "Consume", id, product, 100));
            Assert.AreEqual(2, (int)Call(kitchen, "TableCount", product));
        }

        [Test]
        public void ReleasingAClaimDoesNotChangeDepositOrderButReturningFoodIsANewDeposit()
        {
            object kitchen = Make(new[] { 0 }, true);
            object older = DepositTestSandwich(kitchen, 0, 11);
            object newer = DepositTestSandwich(kitchen, 0, 12);
            Assert.AreSame(newer, Call(kitchen, "ReserveTableUnit", 0, 100));
            Assert.AreSame(older, Call(kitchen, "ReserveTableUnit", 0, 101));
            Call(kitchen, "ReleaseTableReservation", 100);
            Call(kitchen, "ReleaseTableReservation", 101);
            Assert.AreSame(newer, Call(kitchen, "ReserveTableUnit", 0, 102), "Releasing the older claim must not make its food newest.");
            Call(kitchen, "ReleaseTableReservation", 102);
            // Take the newest while a different cook deposits, then return it physically to the table.
            Call(kitchen, "ReserveTableUnit", 0, 103);
            Call(kitchen, "TryTake", 0, EnumValue(roleType, "Mozo"), 103);
            DepositTestSandwich(kitchen, 0, 13);
            Call(kitchen, "ReturnCarried", (int)Get(newer, "Id"), 103);
            Assert.AreSame(newer, Call(kitchen, "ReserveTableUnit", 0, 104));
            Assert.AreEqual(3, (int)Call(kitchen, "TableCount", 0));
        }

        [TestCase(false, 0, "Parrillero")]
        [TestCase(false, 4, "Cocacolero")]
        [TestCase(true, 4, "Cocacolero")]
        public void LatestDepositPolicyDoesNotChangeLegacyOrDrinkPickup(bool active, int product, string role)
        {
            object kitchen = Make(new[] { product }, active);
            var candidates = Units(kitchen).Cast<object>()
                .Where(x => (int)Get(x, "Product") == product && Get(x, "Location").ToString() == "Table")
                .ToArray();
            Assert.IsNotEmpty(candidates);
            object expected = candidates[0];
            if (!active && product < 4)
            {
                int baseSlots = (int)Call(kitchen, "TableCapacity", product ^ 1) > 0 ? 6 : 12;
                expected = candidates.OrderByDescending(x => (int)Get(x, "Slot") / baseSlots)
                    .ThenBy(x => (int)Get(x, "Slot") % baseSlots)
                    .ThenBy(x => (int)Get(x, "Id")).First();
            }
            Assert.AreSame(expected, Call(kitchen, "TryTake", product, EnumValue(roleType, role), 100),
                "Legacy stacks take top-layer, front-to-back; drinks keep their stable table order.");
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void MozoWalksToAndCarriesTheLatestDepositedSandwich(int product)
        {
            object sim = MakeActiveSimulation(5, 1000);
            Call(sim, "StartRound");
            SetArrivalFarFuture(sim);
            object kitchen = Get(sim, "Kitchen");
            DepositTestSandwich(kitchen, product, 777);
            object latest = DepositTestSandwich(kitchen, product, 778);
            Assert.IsTrue((bool)Call(sim, "SpawnCustomer", product, 1));
            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "Mozo")));
            object mozo = Workers(sim).Cast<object>().First(x => Get(x, "Role").ToString() == "Mozo");
            object layout = Get(sim, "KitchenLayout");
            Vector2 expected = (Vector2)Call(layout, "TableSlotPickupPosition", product, (int)Get(latest, "Slot"),
                (bool)Call(sim, "IsProductAvailable", product ^ 1), (int)Call(kitchen, "TableCapacity", product));
            bool reached = false, carried = false;
            for (int i = 0; i < 600 && !carried; i++)
            {
                Call(sim, "Step", .05f);
                reached |= Vector2.Distance((Vector2)Get(mozo, "Position"), expected) < .001f;
                if ((int)Get(mozo, "CarriedItemId") > 0)
                {
                    Assert.AreEqual((int)Get(latest, "Id"), (int)Get(mozo, "CarriedItemId"));
                    carried = true;
                }
            }
            Assert.IsTrue(reached, "The Mozo must reach the latest unit's real slot.");
            Assert.IsTrue(carried);
            for (int i = 0; i < 600 && (int)Call(sim, "GetProductDelivered", product) == 0; i++) Call(sim, "Step", .05f);
            Assert.AreEqual(1, (int)Call(sim, "GetProductDelivered", product));
            Assert.IsNull(Call(kitchen, "Find", (int)Get(latest, "Id")), "The reserved unit is consumed once at handoff.");
        }

        [Test]
        public void MozoRouteTracksTheSameClaimedSandwichWhenCompactionMovesItsSlot()
        {
            object sim = MakeActiveSimulation(5, 100000);
            Call(sim, "StartRound");
            SetArrivalFarFuture(sim);
            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "Mozo")),
                "One paid Mozo plus the free Racing starter gives the two workers this route test requires.");
            object[] mozos = Workers(sim).Cast<object>()
                .Where(worker => Get(worker, "Role").ToString() == "Mozo").ToArray();
            Assert.AreEqual(2, mozos.Length);
            object firstMozo = mozos[0], movingMozo = mozos[1];
            int firstMozoId = (int)Get(firstMozo, "Id"), movingMozoId = (int)Get(movingMozo, "Id");
            object kitchen = Get(sim, "Kitchen");
            ((int[])GetInternalField(kitchen, "tableCapacity"))[0] = 4;
            DepositTestSandwich(kitchen, 0, 11); // slot 0
            object pickedFirst = DepositTestSandwich(kitchen, 0, 12); // slot 1
            Assert.AreSame(pickedFirst, Call(kitchen, "ReserveTableUnit", 0, firstMozoId));
            DepositTestSandwich(kitchen, 0, 13); // slot 2
            object claimedToMove = DepositTestSandwich(kitchen, 0, 14); // slot 3
            Assert.AreSame(claimedToMove, Call(kitchen, "ReserveTableUnit", 0, movingMozoId));

            SetWorker(movingMozo, "Product", 0);
            SetWorker(movingMozo, "State", EnumValue(stateType, "ToStation"));
            object layout = Get(sim, "KitchenLayout");
            bool companionAvailable = (bool)Call(sim, "IsProductAvailable", 1);
            int capacity = (int)Call(kitchen, "TableCapacity", 0);
            Vector2 oldTarget = (Vector2)Call(layout, "TableSlotPickupPosition", 0,
                (int)Get(claimedToMove, "Slot"), companionAvailable, capacity);
            MethodInfo setRoute = simulationType.GetMethod("SetWorkerRoute", PrivateInstance);
            MethodInfo refreshRoute = simulationType.GetMethod("RefreshMozoPickupRoute", PrivateInstance);
            Assert.IsNotNull(setRoute);
            Assert.IsNotNull(refreshRoute);
            Assert.IsTrue((bool)setRoute.Invoke(sim, new object[] { movingMozo, oldTarget }));

            Assert.AreSame(pickedFirst, Call(kitchen, "TryTake", 0, EnumValue(roleType, "Mozo"), firstMozoId));
            Assert.AreEqual(2, (int)Get(claimedToMove, "Slot"), "Removing a middle sandwich compacts later entries.");
            Vector2 newTarget = (Vector2)Call(layout, "TableSlotPickupPosition", 0,
                (int)Get(claimedToMove, "Slot"), companionAvailable, capacity);
            Assert.AreNotEqual(oldTarget, newTarget);

            Assert.IsTrue((bool)refreshRoute.Invoke(sim, new object[] { movingMozo }));
            Vector2[] refreshedRoute = (Vector2[])GetInternalField(movingMozo, "Route");
            Assert.AreEqual(newTarget, refreshedRoute[refreshedRoute.Length - 1]);
            Assert.AreEqual(movingMozoId, (int)Get(claimedToMove, "WorkerId"), "The unit claim survives its slot change.");
            Assert.AreSame(claimedToMove, Call(kitchen, "ReserveTableUnit", 0, movingMozoId));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void MozoPickupReleasesItsTableCellAndTheNextDepositUsesTheCompactedSlot(int product)
        {
            object kitchen = Make(new[] { product }, true);
            object sandwich = DepositTestSandwich(kitchen, product, 11);
            int tableSlot = (int)Get(sandwich, "Slot");
            Assert.AreSame(sandwich, Call(kitchen, "ReserveTableUnit", product, 100));

            Assert.AreSame(sandwich, Call(kitchen, "TryTake", product, EnumValue(roleType, "Mozo"), 100));
            Assert.AreEqual(-1, (int)Get(sandwich, "Slot"),
                "Picking up must release the physical table slot immediately.");
            Assert.AreEqual(100, (int)Get(sandwich, "WorkerId"), "The Mozo's exclusive claim to the carried unit remains.");
            Assert.IsNull(Call(kitchen, "TryTake", product, EnumValue(roleType, "Mozo"), 101),
                "Another Mozo cannot take the already-carried sandwich.");

            object nextCook = Call(kitchen, "ReserveCook", product, 12);
            Assert.NotNull(nextCook);
            Assert.IsTrue((bool)GetInternalField(nextCook, "ReservedTableCapacity"),
                "Capacity remains reserved when cooking starts, but no visual cell is assigned yet.");
            Assert.AreEqual("Grill", Get(nextCook, "Location").ToString(),
                "An in-flight cook reserves capacity, not a physical tabletop coordinate.");
            FinishTestSandwich(kitchen, nextCook, 12);
            Assert.AreEqual(tableSlot, (int)Get(nextCook, "Slot"),
                "The newly deposited sandwich fills the first compacted table cell.");
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void PickingUpAMiddleSandwichCompactsTheTableAndPreservesOtherMozoClaims(int product)
        {
            object kitchen = Make(new[] { product }, true);
            ((int[])GetInternalField(kitchen, "tableCapacity"))[product] = 4;
            object first = DepositTestSandwich(kitchen, product, 11);
            object middle = DepositTestSandwich(kitchen, product, 12);
            Assert.AreSame(middle, Call(kitchen, "ReserveTableUnit", product, 100));
            object later = DepositTestSandwich(kitchen, product, 13);
            Assert.AreSame(later, Call(kitchen, "ReserveTableUnit", product, 101));
            object tail = DepositTestSandwich(kitchen, product, 14);
            Assert.AreEqual(3, (int)Get(tail, "Slot"));

            Assert.AreSame(middle, Call(kitchen, "TryTake", product, EnumValue(roleType, "Mozo"), 100));
            Assert.AreEqual(-1, (int)Get(middle, "Slot"));
            Assert.AreEqual(0, (int)Get(first, "Slot"));
            Assert.AreEqual(1, (int)Get(later, "Slot"), "The later claimed sandwich must slide into the vacated cell.");
            Assert.AreEqual(2, (int)Get(tail, "Slot"), "The table stays densely packed from its first slot.");
            Assert.AreEqual(101, (int)Get(later, "WorkerId"), "Compaction cannot steal another Mozo's item claim.");
            Assert.AreSame(later, Call(kitchen, "ReserveTableUnit", product, 101));

            object next = DepositTestSandwich(kitchen, product, 15);
            Assert.AreEqual(3, (int)Get(next, "Slot"), "New deposits append after the compacted table.");
            Assert.AreEqual(4, (int)Call(kitchen, "TableCount", product));
            Assert.IsFalse((bool)Call(kitchen, "CanReserveCook", product), "The physical table capacity stays bounded.");
        }

        [Test]
        public void CanceledMozoTripReturnsFoodToANewSlotWithoutCollidingWithAnInFlightCook()
        {
            object kitchen = Make(new[] { 0 }, true);
            ((int[])GetInternalField(kitchen, "tableCapacity"))[0] = 1;
            object original = DepositTestSandwich(kitchen, 0, 11);
            Assert.AreSame(original, Call(kitchen, "ReserveTableUnit", 0, 100));
            Assert.AreSame(original, Call(kitchen, "TryTake", 0, EnumValue(roleType, "Mozo"), 100));

            object replacementCook = Call(kitchen, "ReserveCook", 0, 12);
            Assert.NotNull(replacementCook, "The just-freed slot can immediately be used by the next cook.");
            Assert.IsTrue((bool)GetInternalField(replacementCook, "ReservedTableCapacity"));
            Assert.AreEqual("Grill", Get(replacementCook, "Location").ToString(),
                "A production reservation is not a visual table cell.");
            Call(kitchen, "ReturnCarried", (int)Get(original, "Id"), 100);
            Assert.AreEqual("Carried", Get(original, "Location").ToString());
            Assert.IsTrue((bool)GetInternalField(original, "PendingTableReturn"),
                "Canceled food remains real and waits off-table rather than colliding with the cook's reserved slot.");
            Assert.IsFalse((bool)Call(kitchen, "CanReserveCook", 0),
                "A pending return has priority over further production.");

            FinishTestSandwich(kitchen, replacementCook, 12);
            Assert.AreEqual(1, (int)Call(kitchen, "TableCount", 0));
            object deliveredReplacement = Call(kitchen, "ReserveTableUnit", 0, 101);
            Assert.AreSame(replacementCook, deliveredReplacement);
            Assert.AreSame(replacementCook, Call(kitchen, "TryTake", 0, EnumValue(roleType, "Mozo"), 101));
            Assert.IsTrue((bool)Call(kitchen, "Consume", (int)Get(replacementCook, "Id"), 0, 101));

            Call(kitchen, "Advance", .05f, 1f, 1f, false, false, false, false);
            Assert.AreEqual(1, (int)Call(kitchen, "TableCount", 0));
            Assert.IsFalse((bool)GetInternalField(original, "PendingTableReturn"));
            Assert.AreEqual("Table", Get(original, "Location").ToString());
            Assert.AreEqual(0, (int)Get(original, "Slot"));
            Assert.AreSame(original, Call(kitchen, "ReserveTableUnit", 0, 102));
        }

        [Test]
        public void CookedUnitReservesCapacityAndGetsAContiguousVisualCellOnlyAtDeposit()
        {
            object kitchen = Make(new[] { 0 }, true);
            object unit = Call(kitchen, "ReserveCook", 0, 7);
            int id = (int)Get(unit, "Id");
            Assert.IsNull(Call(kitchen, "ReserveTableUnit", 0, 7), "An in-flight production item already reserves its destination capacity.");
            Assert.IsFalse((bool)Call(kitchen, "AdvanceCook", id, 8, .5f, 1f));
            Assert.IsTrue((bool)Call(kitchen, "AdvanceCook", id, 7, 2f, 1f));
            Assert.IsFalse((bool)Call(kitchen, "TakeCooked", id, 8));
            Assert.IsTrue((bool)Call(kitchen, "TakeCooked", id, 7));
            Assert.AreEqual("Carried", Get(unit, "Location").ToString());
            Assert.IsTrue((bool)GetInternalField(unit, "ReservedTableCapacity"));
            Assert.AreEqual(-1, (int)Get(unit, "Slot"), "The cook's in-flight item has no tabletop coordinate yet.");
            Assert.IsTrue((bool)Call(kitchen, "DepositCooked", id, 7));
            Assert.AreEqual("Table", Get(unit, "Location").ToString());
            Assert.AreEqual(0, (int)Get(unit, "Slot"));
            Assert.IsFalse((bool)GetInternalField(unit, "ReservedTableCapacity"));
            Assert.AreEqual(1, (int)Call(kitchen, "TableCount", 0));
        }

        [Test]
        public void TableCapacityAndWorkerReservationsPreventOverbookingAndReleaseCleanly()
        {
            object kitchen = Make(new[] { 0 }, true, balance =>
                balanceType.GetField("normalTableCapacity", PublicInstance).SetValue(balance, 1));
            ((int[])GetInternalField(kitchen, "tableCapacity"))[0] = 1;
            object unit = Call(kitchen, "ReserveCook", 0, 11);
            int id = (int)Get(unit, "Id");
            Assert.IsNull(Call(kitchen, "ReserveTableUnit", 0, 11), "An in-flight item is not a tabletop pickup target.");
            Assert.IsNull(Call(kitchen, "ReserveTableUnit", 0, 12), "In-flight items consume finite table capacity.");
            Assert.IsFalse((bool)Call(kitchen, "CanReserveCook", 0), "A reserved in-flight capacity token cannot be overbooked.");
            Call(kitchen, "CancelCook", 11);
            Call(kitchen, "ReleaseTableReservation", 11);
            Assert.AreEqual(0, (int)Call(kitchen, "TableCount", 0));
            object replacement = Call(kitchen, "ReserveCook", 0, 12);
            Assert.IsNotNull(replacement);
            Assert.AreNotEqual(id, (int)Get(replacement, "Id"), "Canceled item identity is never reused.");
        }

        [Test]
        public void TableReservationsAreExclusiveAndCannotClaimMissingStock()
        {
            object kitchen = Make(new[] { 0 }, true);
            Assert.IsNull(Call(kitchen, "ReserveTableUnit", 0, 1), "No phantom unit may be reserved.");
            object unit = Call(kitchen, "ReserveCook", 0, 1);
            int id = (int)Get(unit, "Id");
            Call(kitchen, "AdvanceCook", id, 1, 2f, 1f);
            Assert.IsTrue((bool)Call(kitchen, "TakeCooked", id, 1));
            Assert.IsTrue((bool)Call(kitchen, "DepositCooked", id, 1));
            Assert.IsNotNull(Call(kitchen, "ReserveTableUnit", 0, 3));
            Assert.IsNull(Call(kitchen, "ReserveTableUnit", 0, 4));
            Assert.IsFalse((bool)Call(kitchen, "TakeCooked", id, 4));
        }

        [Test]
        public void ActiveLevelSandwichCookingAndSaleBypassMarketIngredientConsumption()
        {
            object balance = MakeActiveBalance(5, 1000);
            object sim = Activator.CreateInstance(simulationType, new object[] { balance, 5, 5f, 0, 1, 0 });
            object data = Activator.CreateInstance(marketDataType);
            int[] stocked = new int[10];
            for (int i = 0; i < stocked.Length; i++) stocked[i] = 10000;
            marketDataType.GetField("stockUnits", PublicInstance).SetValue(data, stocked);
            object market = Activator.CreateInstance(marketType, new[] { data, balance });
            Assert.IsTrue((bool)Call(market, "BeginAttempt", sim));
            Call(sim, "BindIngredientStock", market);
            Call(sim, "StartRound");
            object kitchen = Get(sim, "Kitchen");
            int chorizoBefore = (int)Call(market, "Units", 0), breadBefore = (int)Call(market, "Units", 1);

            object cancelled = Call(kitchen, "ReserveCook", 0, 1);
            Assert.IsNotNull(cancelled);
            int cancelledId = (int)Get(cancelled, "Id");
            Call(kitchen, "AdvanceCook", cancelledId, 1, .25f, 1f);
            Assert.AreEqual(chorizoBefore, Call(market, "Units", 0), "Reservation cannot spend ingredients.");
            Assert.AreEqual(breadBefore, Call(market, "Units", 1));
            Call(kitchen, "CancelCook", 1);
            Assert.IsNull(Call(kitchen, "Find", cancelledId));
            Assert.AreEqual(chorizoBefore, Call(market, "Units", 0), "Cancelled cooking releases its reserved recipe.");
            Assert.AreEqual(breadBefore, Call(market, "Units", 1));

            object cooked = Call(kitchen, "ReserveCook", 0, 2);
            int cookedId = (int)Get(cooked, "Id");
            Assert.IsTrue((bool)Call(kitchen, "AdvanceCook", cookedId, 2, 2f, 1f));
            Assert.IsTrue((bool)Call(kitchen, "TakeCooked", cookedId, 2));
            Assert.IsTrue((bool)Call(kitchen, "DepositCooked", cookedId, 2));
            Assert.AreEqual(chorizoBefore, Call(market, "Units", 0), "Depositing edible food does not consume its ingredient reservation.");
            Assert.AreEqual(breadBefore, Call(market, "Units", 1));
            Assert.IsNotNull(Call(kitchen, "ReserveTableUnit", 0, 3));
            Assert.IsNotNull(Call(kitchen, "TryTake", 0, EnumValue(roleType, "Mozo"), 3));
            Assert.IsTrue((bool)Call(kitchen, "Consume", cookedId, 0, 3));
            Assert.AreEqual(chorizoBefore, Call(market, "Units", 0), "Racing+ sandwich recipes do not consume Market stock at handoff.");
            Assert.AreEqual(breadBefore, Call(market, "Units", 1), "Racing+ sandwich recipes do not consume Market stock at handoff.");
        }

        [Test]
        public void BeverageRoleStockBehaviorRemainsAvailableInActiveCookMode()
        {
            object kitchen = Make(new[] { 4, 5, 6 }, true);
            Assert.IsNotNull(Call(kitchen, "TryTake", 4, EnumValue(roleType, "Cocacolero"), 21));
            Assert.IsNotNull(Call(kitchen, "TryTake", 5, EnumValue(roleType, "Fernetero"), 22));
            Assert.IsNotNull(Call(kitchen, "TryTake", 6, EnumValue(roleType, "Cocacolero"), 23));
        }

        [Test]
        public void ASingleEnabledSpecialtyRecipeRepeatsWithoutInventingItsDisabledCompanion()
        {
            object normalBalance = MakeActiveBalance(5, 100000);
            string[] normalCatalogs = (string[])simulationBalanceType.GetField("levelProductIds", PublicInstance).GetValue(normalBalance);
            normalCatalogs[5] = "1,4";
            object normal = Activator.CreateInstance(simulationType, new object[] { normalBalance, 5, 5f, 0, 1, 0 });
            Call(normal, "StartRound");
            SetArrivalFarFuture(normal);
            Step(normal, 81);
            object normalCook = Workers(normal)[0];
            object normalKitchen = Get(normal, "Kitchen");
            Assert.IsFalse((bool)Call(normal, "IsProductAvailable", 0));
            Assert.Greater((int)Call(normalKitchen, "SupplyCount", 1), 0, "The one enabled normal recipe remains real production.");
            Assert.AreEqual(0, (int)Call(normal, "GetProductDelivered", 1), "The test has no customer to consume the cooked item.");
            Assert.IsFalse(Units(normalKitchen).Cast<object>().Any(unit => (int)Get(unit, "Product") == 0),
                "An unavailable normal companion is never fabricated.");

            object premiumBalance = MakeActiveBalance(5, 100000);
            string[] premiumCatalogs = (string[])simulationBalanceType.GetField("levelProductIds", PublicInstance).GetValue(premiumBalance);
            premiumCatalogs[5] = "3,4";
            object premium = Activator.CreateInstance(simulationType, new object[] { premiumBalance, 5, 5f, 0, 1, 0 });
            Call(premium, "StartRound");
            SetArrivalFarFuture(premium);
            Step(premium, 81);
            object premiumCook = Workers(premium).Cast<object>().First(worker => Get(worker, "Role").ToString() == "ParrilleroPremium");
            object premiumKitchen = Get(premium, "Kitchen");
            Assert.IsFalse((bool)Call(premium, "IsProductAvailable", 2));
            Assert.Greater((int)Call(premiumKitchen, "SupplyCount", 3), 0, "The one enabled Premium recipe remains real production.");
            Assert.AreEqual(0, (int)Call(premium, "GetProductDelivered", 3), "The test has no customer to consume the cooked item.");
            Assert.IsFalse(Units(premiumKitchen).Cast<object>().Any(unit => (int)Get(unit, "Product") == 2),
                "An unavailable Premium companion is never fabricated.");
        }

        [Test]
        public void ActiveCookGateStartsAtSixthLevelAndDoesNotChangeEarlierMode()
        {
            object balance = Activator.CreateInstance(simulationBalanceType);
            for (int level = 0; level < 11; level++)
            {
                object sim = Activator.CreateInstance(simulationType, new object[] { balance, level, 5f, 0, 1, 0 });
                Assert.AreEqual(level >= 5, (bool)Get(sim, "UsesActiveCooks"), "Unexpected active cook mode at level index " + level);
                Assert.AreEqual(level == 6 ? 2 : 1, Get(sim, "ParrilleroCount"), "Only San Lorenzo starts with two free normal cooks.");
                Assert.AreEqual(level == 5 ? 1 : 0, Get(sim, "ParrilleroPremiumCount"), "Only Racing keeps its free Premium.");
                Assert.AreEqual(level == 9 || level == 10 ? 1 : 0, Get(sim, "CocacoleroCount"), "Camioneros and Los Redondos start with a free Cocacolero.");
                Assert.AreEqual(0, Get(sim, "FerneteroCount"));
                Assert.AreEqual(level == 5 || level == 6 || level == 8 || level == 9 || level == 10 ? 1 : 0, Get(sim, "MozoCount"),
                    "Racing, San Lorenzo, Boca, Camioneros, and Los Redondos receive one free Mozo; all other levels remain unchanged.");
            }
            object early = Activator.CreateInstance(simulationType, new object[] { balance, 4, 5f, 0, 1, 0 });
            object active = Activator.CreateInstance(simulationType, new object[] { balance, 5, 5f, 0, 1, 0 });
            Assert.AreEqual(0, Get(early, "MozoCount"));
            Assert.AreEqual(1, Get(active, "MozoCount"), "Racing starts with exactly one experimental Mozo.");
            object sanLorenzo = Activator.CreateInstance(simulationType, new object[] { balance, 6, 5f, 0, 1, 0 });
            Assert.AreEqual(1, Get(sanLorenzo, "MozoCount"), "San Lorenzo starts with exactly one experimental Mozo.");
            Assert.IsFalse((bool)Get(early, "HasMozo"));
            Assert.IsTrue((bool)Get(active, "HasMozo"));
        }

        [Test]
        public void MozoHiringIsPlayOnlyAndUsesTheExistingIncreasingCostCurve()
        {
            object balance = Activator.CreateInstance(simulationBalanceType);
            Array levels = (Array)simulationBalanceType.GetField("levelBalances", PublicInstance).GetValue(balance);
            object river = levels.GetValue(7);
            river.GetType().GetField("startingCoins", PublicInstance).SetValue(river, 1000);
            object sim = Activator.CreateInstance(simulationType, new object[] { balance, 7, 5f, 10000, 1, 0 });
            object mozo = EnumValue(roleType, "Mozo");
            Assert.AreEqual(0, Get(sim, "MozoCount"));
            Assert.IsFalse((bool)Get(sim, "CanHireMozo"), "Role purchases are offered only during play.");
            int firstCost = (int)Get(sim, "MozoHireCost");
            Assert.AreEqual(60, firstCost, "River is the $60 first-hire reference.");
            Call(sim, "StartRound");
            Assert.IsTrue((bool)Get(sim, "CanHireMozo"));
            int coinsBeforeHire = (int)Get(sim, "Coins");
            Assert.GreaterOrEqual(coinsBeforeHire, firstCost);
            Assert.IsTrue((bool)Call(sim, "TryHire", mozo));
            Assert.AreEqual(1, Get(sim, "MozoCount"));
            Assert.AreEqual(coinsBeforeHire - firstCost, Get(sim, "Coins"));
            int secondCost = (int)Get(sim, "MozoHireCost");
            Assert.Greater(secondCost, firstCost);
            Assert.IsTrue((bool)Call(sim, "TryHire", mozo), "Mozo hiring remains unlimited while affordable.");
            Assert.AreEqual(2, Get(sim, "MozoCount"));
        }

        [Test]
        public void RacingFreeMozoKeepsFirstPaidCostAndResetsToOneOnEveryAttempt()
        {
            object balance = Activator.CreateInstance(simulationBalanceType);
            object sim = Activator.CreateInstance(simulationType, new object[] { balance, 5, 5f, 0, 1, 0 });
            object mozo = EnumValue(roleType, "Mozo");
            Assert.AreEqual(1, Get(sim, "MozoCount"), "Racing's Ready state must contain exactly one free Mozo.");
            Assert.AreEqual(50, Get(sim, "MozoHireCost"), "The free starter must not advance the paid-hire tier.");
            Assert.IsFalse((bool)Get(sim, "CanHireMozo"), "Mozo purchases remain unavailable until play starts.");

            Call(sim, "StartRound");
            Assert.AreEqual(250, Get(sim, "Coins"));
            Assert.AreEqual(260f, Get(sim, "TimeRemaining"));
            Assert.AreEqual(1, Get(sim, "ParrilleroCount"));
            Assert.AreEqual(1, Get(sim, "ParrilleroPremiumCount"), "Keep Racing's pre-existing free Premium worker.");
            Assert.AreEqual(1, Get(sim, "MozoCount"));
            CollectionAssert.AreEqual(new[] { 210, 145, 80, 75, 175, 75, 90 },
                Enumerable.Range(0, 7).Select(product => (int)Call(sim, "GetProductGoal", product)).ToArray());
            CollectionAssert.AreEqual(new[] { 5f, 5f, 10f, 12f, 5f, 12f, 7f },
                Enumerable.Range(0, 7).Select(product => (float)Call(sim, "GetProductPrice", product)).ToArray());
            Assert.AreEqual(40, Call(sim, "HireCostForRole", EnumValue(roleType, "Parrillero")));
            Assert.AreEqual(40, Call(sim, "HireCostForRole", EnumValue(roleType, "Cocacolero")));
            Assert.AreEqual(50, Call(sim, "HireCostForRole", EnumValue(roleType, "ParrilleroPremium")));
            Assert.AreEqual(200, Call(sim, "HireCostForRole", EnumValue(roleType, "Fernetero")));
            Assert.AreEqual(50, Get(sim, "MozoHireCost"));
            Assert.AreEqual(30, Get(sim, "SpeedCost"));
            Assert.IsTrue((bool)Get(sim, "CanHireMozo"));
            Assert.IsTrue((bool)Call(sim, "TryHire", mozo));
            Assert.AreEqual(2, Get(sim, "MozoCount"));
            Assert.AreEqual(200, Get(sim, "Coins"), "The first paid Mozo deducts only its $50 base price.");
            Assert.AreEqual(125, Get(sim, "MozoHireCost"), "The next Mozo keeps the existing 2.5x tier.");
            Assert.IsTrue((bool)Call(sim, "TryHire", mozo));
            Assert.AreEqual(3, Get(sim, "MozoCount"));
            Assert.AreEqual(75, Get(sim, "Coins"));
            Assert.AreEqual(250, Get(sim, "MozoHireCost"), "Subsequent role-local progression remains unchanged.");

            Call(sim, "StartRound");
            Assert.AreEqual(250, Get(sim, "Coins"));
            Assert.AreEqual(1, Get(sim, "MozoCount"), "A new attempt must discard all previously hired Mozos.");
            Assert.AreEqual(1, Get(sim, "ParrilleroCount"));
            Assert.AreEqual(1, Get(sim, "ParrilleroPremiumCount"));
            Assert.AreEqual(0, Get(sim, "SpeedLevel"));
            Assert.AreEqual(50, Get(sim, "MozoHireCost"), "Retry restores the first paid Mozo tier.");
        }

        [Test]
        public void SanLorenzoFreeMozoKeepsFirstPaidCostAndResetsToOneOnEveryAttempt()
        {
            object balance = Activator.CreateInstance(simulationBalanceType);
            object sim = Activator.CreateInstance(simulationType, new object[] { balance, 6, 5f, 0, 1, 0 });
            object mozo = EnumValue(roleType, "Mozo");
            Assert.AreEqual(2, Get(sim, "ParrilleroCount"));
            Assert.AreEqual(1, Get(sim, "MozoCount"));
            Assert.AreEqual(0, Get(sim, "ParrilleroPremiumCount"), "San Lorenzo must not receive a free Premium Parrillero.");
            Assert.AreEqual(55, Get(sim, "MozoHireCost"), "The free starter must not consume San Lorenzo's first paid $55 hire.");

            Call(sim, "StartRound");
            Assert.AreEqual(250, Get(sim, "Coins"));
            Assert.AreEqual(0, Get(sim, "CoinsEarned"), "The free starter does not count as earned or spent money.");
            Assert.AreEqual(300f, Get(sim, "TimeRemaining"));
            Assert.AreEqual(0, Get(sim, "SpeedLevel"), "No purchased speed upgrade means ×1.00.");
            Assert.AreEqual(1f, Get(sim, "WorkRate"));
            Assert.AreEqual(1f, Get(sim, "CookWorkRate"));
            Assert.AreEqual(55, Get(sim, "MozoHireCost"));
            Assert.IsTrue((bool)Get(sim, "CanHireMozo"));
            Assert.IsTrue((bool)Call(sim, "TryHire", mozo));
            Assert.AreEqual(195, Get(sim, "Coins"));
            Assert.AreEqual(2, Get(sim, "MozoCount"));
            Assert.AreEqual(140, Get(sim, "MozoHireCost"), "Second paid Mozo preserves the role-local ×2.5 progression and $5 rounding.");
            Assert.IsTrue((bool)Call(sim, "TryHire", mozo));
            Assert.AreEqual(55, Get(sim, "Coins"));
            Assert.AreEqual(3, Get(sim, "MozoCount"));
            Assert.AreEqual(275, Get(sim, "MozoHireCost"), "Third paid Mozo preserves the role-local ×5 tier.");
            Assert.IsFalse((bool)Get(sim, "CanHireMozo"), "The next hire remains gated by real funds.");

            Call(sim, "StartRound");
            Assert.AreEqual(250, Get(sim, "Coins"));
            Assert.AreEqual(2, Get(sim, "ParrilleroCount"));
            Assert.AreEqual(1, Get(sim, "MozoCount"), "Retry discards hired Mozos and restores exactly one free starter.");
            Assert.AreEqual(0, Get(sim, "ParrilleroPremiumCount"));
            Assert.AreEqual(0, Get(sim, "SpeedLevel"));
            Assert.AreEqual(55, Get(sim, "MozoHireCost"));

            object levelSelection = Activator.CreateInstance(simulationType, new object[] { balance, 6, 5f, 0, 1, 0 });
            Assert.IsTrue((bool)Call(levelSelection, "SelectLevel", 5));
            Assert.AreEqual(1, Get(levelSelection, "MozoCount"), "Selecting Racing retains its existing free Mozo.");
            Assert.AreEqual(1, Get(levelSelection, "ParrilleroPremiumCount"), "Selecting Racing retains its existing free Premium Parrillero.");
            Assert.AreEqual(50, Get(levelSelection, "MozoHireCost"), "Racing's first paid Mozo cost is unchanged.");
            Assert.IsTrue((bool)Call(levelSelection, "SelectLevel", 6));
            Assert.AreEqual(2, Get(levelSelection, "ParrilleroCount"), "Returning to San Lorenzo restores its two normal starters.");
            Assert.AreEqual(1, Get(levelSelection, "MozoCount"), "Returning to San Lorenzo cannot duplicate the starter.");
            Assert.AreEqual(0, Get(levelSelection, "ParrilleroPremiumCount"));
            Assert.AreEqual(55, Get(levelSelection, "MozoHireCost"));
        }

        [Test]
        public void SanLorenzoConfigurationJKeepsPaidParrilleroCurveAndResetsEveryAttempt()
        {
            object balance = Activator.CreateInstance(simulationBalanceType);
            object sim = Activator.CreateInstance(simulationType, new object[] { balance, 6, 5f, 0, 1, 0 });
            object normal = EnumValue(roleType, "Parrillero");
            Assert.AreEqual(2, Get(sim, "ParrilleroCount"));
            Assert.AreEqual(45, Get(sim, "ParrilleroHireCost"));
            for (int attempt = 0; attempt < 3; attempt++)
            {
                Call(sim, "StartRound");
                Assert.AreEqual(2, Get(sim, "ParrilleroCount"));
                Assert.AreEqual(1, Get(sim, "MozoCount"));
                Assert.AreEqual(0, Get(sim, "ParrilleroPremiumCount"));
                Assert.AreEqual(250, Get(sim, "Coins"));
                Assert.AreEqual(0, Get(sim, "CoinsEarned"));
                Assert.AreEqual(300f, Get(sim, "TimeRemaining"));
                Assert.AreEqual(0, Get(sim, "SpeedLevel"));
                Assert.AreEqual(45, Get(sim, "ParrilleroHireCost"));
                Assert.IsTrue((bool)Call(sim, "TryHire", normal));
                Assert.AreEqual(3, Get(sim, "ParrilleroCount"));
                Assert.AreEqual(205, Get(sim, "Coins"));
                Assert.AreEqual(115, Get(sim, "ParrilleroHireCost"));
                Assert.IsTrue((bool)Call(sim, "TryHire", normal));
                Assert.AreEqual(4, Get(sim, "ParrilleroCount"));
                Assert.AreEqual(90, Get(sim, "Coins"));
                Assert.AreEqual(225, Get(sim, "ParrilleroHireCost"));
                Assert.IsFalse((bool)Call(sim, "TryHire", normal), "Real funds still gate paid hires.");
                Assert.IsTrue((bool)Call(sim, "TryUpgradeSpeed"));
                Assert.AreEqual(1, Get(sim, "SpeedLevel"));
            }
        }

        [Test]
        public void SanLorenzoConfigurationJBothInitialParrillerosCookRealUnits()
        {
            object balance = Activator.CreateInstance(simulationBalanceType);
            object sim = Activator.CreateInstance(simulationType, new object[] { balance, 6, 5f, 0, 1, 0 });
            Call(sim, "StartRound");
            SetArrivalFarFuture(sim);
            object[] normals = Workers(sim).Cast<object>().Where(w => Get(w, "Role").ToString() == "Parrillero").ToArray();
            Assert.AreEqual(2, normals.Length);
            Assert.AreNotEqual(Get(normals[0], "CookStationIndex"), Get(normals[1], "CookStationIndex"));
            var observedCooks = new HashSet<int>();
            var observedUnits = new HashSet<int>();
            for (int tick = 0; tick < 800 && observedCooks.Count < 2; tick++)
            {
                Call(sim, "Step", .05f);
                foreach (object cook in normals)
                {
                    int itemId = (int)Get(cook, "CookingItemId");
                    if (itemId <= 0 || (bool)Get(cook, "VisualCooking")) continue;
                    if (observedCooks.Add((int)Get(cook, "Id"))) observedUnits.Add(itemId);
                }
            }
            Assert.AreEqual(2, observedCooks.Count, "Both free cooks must enter the real production system without a hire.");
            Assert.AreEqual(2, observedUnits.Count, "The initial cooks reserve distinct real food units.");
            Assert.AreEqual(250, Get(sim, "Coins"));
            Assert.AreEqual(0, Get(sim, "CoinsEarned"));
            Assert.AreEqual(0, Get(sim, "Delivered"));
        }

        [TestCase(5)]
        [TestCase(6)]
        public void FreeStartingMozoDeliversARealSandwichWithoutBeingHired(int level)
        {
            object sim = MakeActiveSimulation(level, 250);
            Call(sim, "StartRound");
            SetArrivalFarFuture(sim);
            Assert.AreEqual(1, Get(sim, "MozoCount"));
            Assert.IsTrue((bool)Call(sim, "SpawnCustomer", 0, 1));
            for (int tick = 0; tick < 1200 && (int)Call(sim, "GetProductDelivered", 0) == 0; tick++)
                Call(sim, "Step", .05f);
            Assert.AreEqual(1, Call(sim, "GetProductDelivered", 0),
                "The initial Mozo must enter the normal cook-to-table-to-customer handoff loop at level " + (level + 1) + ".");
            Assert.AreEqual(255, Get(sim, "Coins"), "Only the actual $5 Chori handoff awards revenue.");
            Assert.AreEqual(1, Get(sim, "MozoCount"), "Delivery must not create additional Mozos.");
        }

        [Test]
        public void LegacySaveWithoutMozoFieldDefaultsToNoMozo()
        {
            Type gameType = Type.GetType("HayChoriYPaty.StreetGame, Assembly-CSharp", true);
            Type saveType = gameType.GetNestedType("SaveData", BindingFlags.Public);
            object restored = UnityEngine.JsonUtility.FromJson("{\"unlockedLevel\":7,\"coins\":25,\"price\":5}", saveType);
            Assert.AreEqual(0, saveType.GetField("mozos").GetValue(restored));
        }

        [TestCase(5, "Parrillero")]
        [TestCase(5, "ParrilleroPremium")]
        [TestCase(6, "Parrillero")]
        [TestCase(6, "ParrilleroPremium")]
        public void AvailableCookRoutesDirectlyToItsReservedGrillSlot(int level, string roleName)
        {
            object sim = MakeActiveSimulation(level, 1000000);
            Call(sim, "StartRound");
            SetArrivalFarFuture(sim);
            if (roleName == "ParrilleroPremium")
                Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, roleName)));

            object worker = Workers(sim).Cast<object>().First(candidate => Get(candidate, "Role").ToString() == roleName);
            object layout = Get(sim, "KitchenLayout");
            int firstProduct = roleName == "Parrillero" ? 0 : 2;
            Vector2 station = (Vector2)Call(layout, "GrillWorkPosition", firstProduct, 0, 1);
            Vector2 start = station + (roleName == "Parrillero" ? Vector2.right : Vector2.left) * 160f;
            SetWorker(worker, "Position", start);
            SetWorker(worker, "Target", start);

            Call(sim, "Step", .01f);

            Assert.AreEqual("ToGrill", Get(worker, "State").ToString());
            int cookId = (int)Get(worker, "CookingItemId");
            Assert.Greater(cookId, 0, "A real unit is reserved before the first grill route starts.");
            object kitchen = Get(sim, "Kitchen");
            object unit = Call(kitchen, "Find", cookId);
            Assert.IsNotNull(unit);
            int product = (int)Get(unit, "Product"), slot = (int)Get(unit, "Slot");
            int capacity = (int)Call(kitchen, "GrillCapacity", product);
            Vector2 food = (Vector2)Call(layout, "GrillSlotPosition", product, slot, true, capacity);
            Vector2 destination = (Vector2)Call(layout, "GrillSlotWorkPosition", product, food);
            Assert.IsTrue((bool)Call(layout, "IsRouteSegmentClear", start, destination),
                "This fixture uses an unobstructed safe apron segment; obstacle routing remains layout-owned.");
            Vector2[] route = (Vector2[])worker.GetType().GetField("Route", PrivateInstance).GetValue(worker);
            Assert.IsNotNull(route);
            Assert.AreEqual(1, route.Length, "No general-post staging waypoint is inserted before the food-aligned destination.");
            Assert.AreEqual(destination, route[0]);
            Assert.AreEqual(destination, Get(worker, "Target"));
            Assert.AreNotEqual(station, destination);
        }

        [Test]
        public void CookTravelsCooksCarriesAndDepositsButCannotSellUntilMozoIsHired()
        {
            object sim = MakeActiveSimulation(5, 1000);
            Call(sim, "StartRound");
            SetArrivalFarFuture(sim);
            Assert.IsTrue((bool)Call(sim, "SpawnCustomer", 0, 1));
            object kitchen = Get(sim, "Kitchen");
            object kitchenLayout = Get(sim, "KitchenLayout");

            bool sawGrillTravel = false, sawPartialCook = false, sawCarriedUnit = false;
            for (int i = 0; i < 600 && (int)Call(Get(sim, "Kitchen"), "TableCount", 0) == 0; i++)
            {
                Call(sim, "Step", .05f);
                foreach (object worker in Workers(sim))
                {
                    string state = Get(worker, "State").ToString();
                    if (state == "ToGrill") sawGrillTravel = true;
                    if (state == "Cooking")
                    {
                        int itemId = (int)Get(worker, "CookingItemId");
                        object item = Call(kitchen, "Find", itemId);
                        Assert.IsNotNull(item, "A real cooking pose must match its reserved food unit.");
                        int product = (int)Get(item, "Product"), slot = (int)Get(item, "Slot");
                        int capacity = (int)Call(kitchen, "GrillCapacity", product);
                        bool companion = (bool)Call(sim, "IsProductAvailable", product ^ 1);
                        Vector2 food = (Vector2)Call(kitchenLayout, "GrillSlotPosition", product, slot, companion, capacity);
                        Vector2 work = (Vector2)Call(kitchenLayout, "GrillSlotWorkPosition", product, food);
                        Assert.AreEqual(work, Get(worker, "Position"), "Parrillero works beside their reserved grill item.");
                    }
                    if (state == "Cooking" && (float)Get(worker, "CookingProgress") > 0f && (float)Get(worker, "CookingProgress") < 1f)
                        sawPartialCook = true;
                    if (state == "ToTable" && (int)Get(worker, "CarriedItemId") > 0) sawCarriedUnit = true;
                }
            }
            Assert.IsTrue(sawGrillTravel, "Cook must physically reach the grill through its route.");
            Assert.IsTrue(sawPartialCook, "Cooking progress must be a real partial fraction.");
            Assert.IsTrue(sawCarriedUnit, "The finished identified unit must be carried to the table.");
            Assert.AreEqual(1, (int)Call(Get(sim, "Kitchen"), "TableCount", 0));
            Assert.AreEqual(0, Get(sim, "Delivered"), "Active cooks never deliver a sandwich directly.");

            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "Mozo")));
            for (int i = 0; i < 600 && (int)Get(sim, "Delivered") == 0; i++) Call(sim, "Step", .05f);
            Assert.AreEqual(1, Get(sim, "Delivered"), "Mozo must fetch the deposited food and complete the real customer handoff.");
            Assert.AreEqual(1, Call(sim, "GetProductDelivered", 0));
        }

        [Test]
        public void MozoPickupAnchorsFollowEachProductAndActualTableSlot()
        {
            object sim = MakeActiveSimulation(5, 1000);
            object layout = Get(sim, "KitchenLayout");
            object kitchen = Get(sim, "Kitchen");
            Vector2 home = new Vector2(433f, 400f);
            const float workerHalfWidth = 45f;
            float sceneWidth = 540f;

            for (int product = 0; product < 4; product++)
            {
                bool companionAvailable = (bool)Call(sim, "IsProductAvailable", product ^ 1);
                int capacity = (int)Call(kitchen, "TableCapacity", product);
                Vector2 standard = (Vector2)Call(layout, "PickupPosition", product);
                Vector2 firstItem = (Vector2)Call(layout, "TableSlotPosition", product, 0, companionAvailable, capacity);
                int basePositionCount = companionAvailable ? 6 : 12;
                int lastBaseSlot = Mathf.Min(basePositionCount - 1, capacity - 1);
                Vector2 lastBaseItem = (Vector2)Call(layout, "TableSlotPosition", product, lastBaseSlot, companionAvailable, capacity);
                Rect baseFootprint = (Rect)Call(layout, "FootprintForProduct", product);
                float safeFrontApproachY = baseFootprint.yMin - 2f;
                float pickupBaseY = Mathf.Min(standard.y, safeFrontApproachY - (lastBaseItem.y - firstItem.y));
                for (int slot = 0; slot < capacity; slot++)
                {
                    Vector2 item = (Vector2)Call(layout, "TableSlotPosition", product, slot, companionAvailable, capacity);
                    Vector2 baseItem = (Vector2)Call(layout, "TableSlotPosition", product,
                        slot % basePositionCount, companionAvailable, capacity);
                    Vector2 target = (Vector2)Call(layout, "TableSlotPickupPosition", product, slot, companionAvailable, capacity);
                    Assert.AreEqual(Mathf.Clamp(item.x, workerHalfWidth, sceneWidth - workerHalfWidth), target.x,
                        "Product " + product + " slot " + slot + " keeps the full Mozo on screen while following the food's X position.");
                    Assert.AreEqual(pickupBaseY + baseItem.y - firstItem.y, target.y, .001f,
                        "Product " + product + " slot " + slot + " follows its base row, not cosmetic stack height, without entering the solid table base.");
                    Assert.LessOrEqual(target.y, safeFrontApproachY + .001f,
                        "Product " + product + " slot " + slot + " remains accessible from outside the table base.");
                    Vector2[] route = (Vector2[])Call(layout, "FindShortestSafeRoute", home, target);
                    Assert.IsNotNull(route, "Product " + product + " slot " + slot + " must remain reachable.");
                    Assert.Greater(route.Length, 0);
                    Assert.AreEqual(target, route[route.Length - 1]);
                }
                Assert.AreNotEqual(standard,
                    (Vector2)Call(layout, "TableSlotPickupPosition", product, 0, companionAvailable, capacity),
                    "Mozo no longer uses the shared center point for product " + product + ".");
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void MozoWalksToTheReservedSandwichSlotForEachProduct(int product)
        {
            object sim = MakeActiveSimulation(5, 1000);
            Call(sim, "StartRound");
            SetArrivalFarFuture(sim);
            if (product >= 2)
                Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "ParrilleroPremium")));
            Assert.IsTrue((bool)Call(sim, "SpawnCustomer", product, 1));

            object kitchen = Get(sim, "Kitchen");
            for (int i = 0; i < 600 && (int)Call(kitchen, "TableCount", product) == 0; i++)
                Call(sim, "Step", .05f);
            Assert.AreEqual(1, (int)Call(kitchen, "TableCount", product), "The requested product must be physically prepared first.");

            object unit = null;
            foreach (object candidate in Units(kitchen))
                if ((int)Get(candidate, "Product") == product && Get(candidate, "Location").ToString() == "Table" &&
                    Get(candidate, "State").ToString() == "Cooked") { unit = candidate; break; }
            Assert.IsNotNull(unit, "The target must be derived from a real cooked unit.");
            int slot = (int)Get(unit, "Slot");
            object layout = Get(sim, "KitchenLayout");
            bool companionAvailable = (bool)Call(sim, "IsProductAvailable", product ^ 1);
            int capacity = (int)Call(kitchen, "TableCapacity", product);
            Vector2 expected = (Vector2)Call(layout, "TableSlotPickupPosition", product, slot, companionAvailable, capacity);
            Assert.AreNotEqual((Vector2)Call(layout, "PickupPosition", product), expected,
                "Each of Chori, Paty, Bondiola, and Vacío must have a product-specific table destination.");

            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "Mozo")));
            object mozo = null;
            foreach (object worker in Workers(sim))
                if (Get(worker, "Role").ToString() == "Mozo") { mozo = worker; break; }
            Assert.IsNotNull(mozo);

            bool reachedReservedSlot = false;
            for (int i = 0; i < 300 && !reachedReservedSlot; i++)
            {
                Call(sim, "Step", .05f);
                reachedReservedSlot = Vector2.Distance((Vector2)Get(mozo, "Position"), expected) < .001f;
            }
            Assert.IsTrue(reachedReservedSlot, "The Mozo must physically reach product " + product + " slot " + slot + ".");

            for (int i = 0; i < 600 && (int)Call(sim, "GetProductDelivered", product) == 0; i++)
                Call(sim, "Step", .05f);
            Assert.AreEqual(1, (int)Call(sim, "GetProductDelivered", product),
                "Correct slot routing must still complete the real handoff.");
        }

        [Test]
        public void ConcurrentCooksKeepDistinctIdentifiedUnitsAndPerWorkerProgress()
        {
            object sim = MakeActiveSimulation(5, 1000);
            Call(sim, "StartRound");
            SetArrivalFarFuture(sim);
            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "Parrillero")));
            Assert.IsTrue((bool)Call(sim, "SpawnCustomer", 0, 1));

            for (int i = 0; i < 400; i++)
            {
                Call(sim, "Step", .05f);
                var liveCookIds = new HashSet<int>();
                var cookingPositions = new List<Vector2>();
                int progressing = 0;
                foreach (object worker in Workers(sim))
                    if (Get(worker, "State").ToString() == "Cooking" && (float)Get(worker, "CookingProgress") > 0f && (int)Get(worker, "CookingItemId") > 0)
                    {
                        progressing++;
                        Assert.IsTrue(liveCookIds.Add((int)Get(worker, "CookingItemId")), "Two cooks cannot own the same physical unit.");
                        cookingPositions.Add((Vector2)Get(worker, "Position"));
                    }
                if (progressing >= 2)
                {
                    Assert.AreEqual(2, liveCookIds.Count);
                    Assert.GreaterOrEqual(Vector2.Distance(cookingPositions[0], cookingPositions[1]), 84f,
                        "Two Parrilleros cooking at once need a visually clear gap between full-body poses.");
                    return;
                }
            }
            Assert.Fail("Two cooks never made simultaneous, independently progressing reservations; route/capacity may be blocking.");
        }

        [TestCase(5)]
        [TestCase(6)]
        public void MixedProductDeliveriesEndAtEachAssignedCustomerColumn(int level)
        {
            object sim = MakeActiveSimulation(level, 100000);
            Call(sim, "StartRound");
            SetArrivalFarFuture(sim);
            int startingPremiumCount = (int)Get(sim, "ParrilleroPremiumCount");
            Assert.AreEqual(level == 5 ? 1 : 0, startingPremiumCount,
                "Only Racing starts with a free Premium Parrillero.");
            if (startingPremiumCount == 0)
                Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "ParrilleroPremium")));
            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "Cocacolero")));
            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "Fernetero")));
            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "Mozo")));
            for (int product = 0; product < 7; product++)
                Assert.IsTrue((bool)Call(sim, "SpawnCustomer", product, 1),
                    "Expected valid level " + (level + 1) + " order product " + product);

            var alignedHandoffProducts = new HashSet<int>();
            for (int i = 0; i < 1200 && (int)Get(sim, "Delivered") < 7; i++)
            {
                Call(sim, "Step", .05f);
                foreach (object worker in Workers(sim))
                {
                    if (Get(worker, "State").ToString() != "Handoff") continue;
                    object assignedCustomer = GetInternalField(worker, "Customer");
                    Vector2 customerTarget = (Vector2)Get(assignedCustomer, "Target");
                    Vector2 workerPosition = (Vector2)Get(worker, "Position");
                    Assert.AreEqual(customerTarget.x, workerPosition.x,
                        "Every actual handoff in level " + (level + 1) + " must align with its assigned customer's column.");
                    float expectedY = Mathf.Lerp(customerTarget.y, LayoutConstant(sceneLayoutType, "WorkerServiceY"), .5f);
                    Assert.AreEqual(expectedY, workerPosition.y, "The handoff halves the former vertical gap.");
                    Vector2 towardCustomer = customerTarget - workerPosition;
                    Vector2 facing = (Vector2)Get(worker, "FacingVector");
                    Assert.That(Vector2.Dot(facing.normalized, towardCustomer.normalized), Is.GreaterThan(.99f),
                        "The worker turns toward the assigned customer before handing over the real item.");
                    object pose = AnimationCall("Sample", worker, false);
                    Assert.AreEqual("Up", GetField(pose, "Facing").ToString(),
                        "The worker projection uses its face-to-customer direction during the handoff pause.");
                    alignedHandoffProducts.Add((int)Get(worker, "Product"));
                }
            }
            string deliveredByProduct = string.Join(",", Enumerable.Range(0, 7).Select(product => product + "=" + Call(sim, "GetProductDelivered", product)));
            string workerStates = string.Join(" | ", Workers(sim).Cast<object>().Select(worker => Get(worker, "Id") + ":" + Get(worker, "Role") + ":" + Get(worker, "State") + " product=" + Get(worker, "Product") + " pos=" + Get(worker, "Position") + " target=" + Get(worker, "Target")));
            string supplies = string.Join(",", Enumerable.Range(0, 7).Select(product => product + "=" + Call(Get(sim, "Kitchen"), "SupplyCount", product)));
            Assert.AreEqual(7, Get(sim, "Delivered"), "Safe worker routes should make every sandwich and beverage handoff reachable. Delivered by product: " + deliveredByProduct + "; supplies: " + supplies + "; workers: " + workerStates);
            CollectionAssert.AreEquivalent(Enumerable.Range(0, 7).ToArray(), alignedHandoffProducts,
                "Every product must be observed at least once in the Handoff state.");
            for (int product = 0; product < 7; product++) Assert.AreEqual(1, Call(sim, "GetProductDelivered", product), "Incorrect count for product " + product);
            Assert.AreEqual(0, Get(sim, "SpeedLevel"), "A fresh attempt remains at x1.00 until speed is purchased.");
            Assert.AreEqual(1f, Get(sim, "WorkRate"));
            Assert.AreEqual(level == 6 ? 2 : 1, Get(sim, "ParrilleroCount"), "Fresh attempts use the level-specific free normal roster.");
            Assert.AreEqual(1, Get(sim, "ParrilleroPremiumCount"), "One Premium Parrillero must support products 2 and 3.");
        }

        [Test]
        public void OnlyRacingStartsWithFreePremiumAndItsNextHireUsesBaseCostAcrossRetries()
        {
            object previousLevel = MakeActiveSimulation(4, 100000);
            Assert.AreEqual(0, Get(previousLevel, "ParrilleroPremiumCount"));
            Call(previousLevel, "StartRound");
            Assert.AreEqual(0, Get(previousLevel, "ParrilleroPremiumCount"));

            object racing = MakeActiveSimulation(5, 100000);
            Assert.AreEqual(1, Get(racing, "ParrilleroCount"));
            Assert.AreEqual(1, Get(racing, "ParrilleroPremiumCount"));
            int firstPremiumCost = (int)Call(racing, "HireCostForRole", EnumValue(roleType, "ParrilleroPremium"));
            object balance = simulationType.GetField("balance", PrivateInstance).GetValue(racing);
            int premiumBaseCost = (int)Call(balance, "GetHireBaseCost", 5, EnumValue(roleType, "ParrilleroPremium"));
            Assert.AreEqual(premiumBaseCost, firstPremiumCost, "The free starter must not consume the first paid hire tier.");

            Call(racing, "StartRound");
            Assert.AreEqual(1, Get(racing, "ParrilleroPremiumCount"), "Every Racing retry resets to exactly one free Premium cook.");
            Assert.AreEqual(0, Get(racing, "SpeedLevel"));
            Assert.AreEqual(premiumBaseCost, Call(racing, "HireCostForRole", EnumValue(roleType, "ParrilleroPremium")));

            object nextLevel = MakeActiveSimulation(6, 100000);
            Assert.AreEqual(0, Get(nextLevel, "ParrilleroPremiumCount"), "The grant must not spill into San Lorenzo.");
        }

        [TestCase("Parrillero", 1)] [TestCase("Parrillero", 2)] [TestCase("Parrillero", 3)]
        [TestCase("Parrillero", 4)] [TestCase("Parrillero", 5)] [TestCase("Parrillero", 6)] [TestCase("Parrillero", 8)]
        [TestCase("ParrilleroPremium", 1)] [TestCase("ParrilleroPremium", 2)] [TestCase("ParrilleroPremium", 3)]
        [TestCase("ParrilleroPremium", 4)] [TestCase("ParrilleroPremium", 5)] [TestCase("ParrilleroPremium", 6)] [TestCase("ParrilleroPremium", 8)]
        public void CookTeamsKeepFourUnscaledPostsAndQueueTheRest(string roleName, int teamSize)
        {
            object sim = MakeActiveSimulation(5, 1000000);
            Call(sim, "StartRound");
            SetArrivalFarFuture(sim);
            int hired = roleName == "Parrillero" || roleName == "ParrilleroPremium" ? 1 : 0;
            while (hired < teamSize)
            {
                Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, roleName)), "Hire " + roleName + " #" + (hired + 1));
                hired++;
            }

            Type projectionType = Type.GetType("HayChoriYPaty.StreetWorkerProjection, Assembly-CSharp", true);
            MethodInfo presentationScale = projectionType.GetMethod("PresentationScale", BindingFlags.Static | BindingFlags.NonPublic);
            object layout = Get(sim, "KitchenLayout");
            int product = roleName == "Parrillero" ? 0 : 2;
            Rect ownGrill = (Rect)Call(layout, "GrillBoundsForProduct", product);
            Rect otherGrill = (Rect)Call(layout, "GrillBoundsForProduct", roleName == "Parrillero" ? 2 : 0);
            int initiallyAssigned = 0, initiallyWaiting = 0;
            HashSet<int> occupiedPosts = new HashSet<int>();
            foreach (object worker in Workers(sim))
            {
                if (Get(worker, "Role").ToString() != roleName) continue;
                int post = (int)Get(worker, "CookStationIndex");
                Assert.AreEqual(1f, presentationScale.Invoke(null, new[] { sim, worker }),
                    "Hiring beyond three must never shrink a cook's presentation scale.");
                if (post < 0) { initiallyWaiting++; continue; }
                initiallyAssigned++;
                Assert.Less(post, 4, "Only the four real posts may be claimed.");
                Assert.IsTrue(occupiedPosts.Add(post), "One post cannot belong to two cooks.");
                Vector2 anchor = (Vector2)Call(layout, "GrillWorkPosition", product, post, Mathf.Min(teamSize, 4));
                Assert.GreaterOrEqual(anchor.x - 30f, 0f, "Visible body stays inside the scene.");
                Assert.LessOrEqual(anchor.x + 30f, 540f, "Visible body stays inside the scene.");
                Assert.IsTrue(ownGrill.xMin <= anchor.x && anchor.x <= ownGrill.xMax, "Post belongs to its own grill bounds.");
                if (roleName == "Parrillero") Assert.Less(anchor.x + 30f, otherGrill.xMin, "Normal cooks cannot invade the Premium grill.");
                else Assert.Greater(anchor.x - 30f, otherGrill.xMax, "Premium cooks cannot invade the normal grill.");
            }
            Assert.AreEqual(Mathf.Min(teamSize, 4), initiallyAssigned);
            Assert.AreEqual(Mathf.Max(0, teamSize - 4), initiallyWaiting);

            int maxAssigned = initiallyAssigned;
            for (int tick = 0; tick < 400; tick++)
            {
                Call(sim, "Step", .05f);
                occupiedPosts.Clear();
                int assigned = 0;
                foreach (object worker in Workers(sim))
                {
                    if (Get(worker, "Role").ToString() != roleName) continue;
                    int post = (int)Get(worker, "CookStationIndex");
                    if (post >= 0)
                    {
                        assigned++;
                        Assert.IsTrue(occupiedPosts.Add(post), "Station allocation must remain unique during rotation.");
                    }
                    else if (Get(worker, "State").ToString() != "ToTable" && Get(worker, "State").ToString() != "Deposit")
                    {
                        Assert.AreEqual(0, Get(worker, "CookingItemId"), "A queued worker cannot reserve or produce invisible food.");
                        Assert.IsFalse((bool)Get(worker, "VisualCooking"), "A queued worker waits rather than cooking off-post.");
                    }
                }
                maxAssigned = Mathf.Max(maxAssigned, assigned);
            }
            Assert.AreEqual(Mathf.Min(teamSize, 4), maxAssigned, "Every available post should be used without exceeding the cap.");
        }

        [TestCase("Parrillero")] [TestCase("ParrilleroPremium")]
        public void CookLeavingForTableReleasesItsPostAndTheFirstWaiterTakesIt(string roleName)
        {
            object sim = MakeActiveSimulation(5, 1000000);
            Call(sim, "StartRound");
            SetArrivalFarFuture(sim);
            int hired = roleName == "Parrillero" || roleName == "ParrilleroPremium" ? 1 : 0;
            while (hired < 5)
            {
                Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, roleName)));
                hired++;
            }
            int firstWaitingId = 0;
            foreach (object worker in Workers(sim))
                if (Get(worker, "Role").ToString() == roleName && (int)Get(worker, "CookStationIndex") < 0)
                    firstWaitingId = (int)Get(worker, "Id");
            Assert.Greater(firstWaitingId, 0);

            bool waiterPromoted = false, deliveryWhileReplacementWorked = false;
            for (int tick = 0; tick < 1200 && !deliveryWhileReplacementWorked; tick++)
            {
                Call(sim, "Step", .05f);
                var posts = new HashSet<int>();
                bool replacementAtPost = false, originalDelivering = false;
                foreach (object worker in Workers(sim))
                {
                    if (Get(worker, "Role").ToString() != roleName) continue;
                    int post = (int)Get(worker, "CookStationIndex");
                    if (post >= 0)
                    {
                        Assert.IsTrue(posts.Add(post), "Rotation cannot duplicate a physical post.");
                        if ((int)Get(worker, "Id") == firstWaitingId)
                            replacementAtPost |= Get(worker, "State").ToString() == "Cooking" ||
                                Get(worker, "State").ToString() == "ToGrill" || (bool)Get(worker, "VisualCooking");
                    }
                    else if ((Get(worker, "State").ToString() == "ToTable" || Get(worker, "State").ToString() == "Deposit") &&
                        (int)Get(worker, "CarriedItemId") > 0)
                        originalDelivering = true;
                }
                waiterPromoted |= replacementAtPost;
                deliveryWhileReplacementWorked = waiterPromoted && originalDelivering;
            }
            Assert.IsTrue(waiterPromoted, "The fifth cook must physically receive a released grill post.");
            Assert.IsTrue(deliveryWhileReplacementWorked, "The departing cook carries the real cooked unit while the replacement works.");
        }

        private object AddWorkerForTest(object sim, string roleName)
        {
            MethodInfo addWorker = simulationType.GetMethod("AddWorker", PrivateInstance, null, new[] { roleType }, null);
            Assert.IsNotNull(addWorker);
            addWorker.Invoke(sim, new[] { EnumValue(roleType, roleName) });
            return Workers(sim)[Workers(sim).Count - 1];
        }

        [Test]
        public void ConcurrentWorkersReceiveDistinctStationAccessAndIdleWaitPositions()
        {
            object sim = MakeActiveSimulation(5, 0);
            object layout = Get(sim, "KitchenLayout");

            Vector2[] barrelAccess = Enumerable.Range(0, 3)
                .Select(index => (Vector2)Call(layout, "PickupPosition", 4, index, 3)).ToArray();
            Assert.AreEqual(3, barrelAccess.Distinct().Count(), "Concurrent beverage workers use different points along the barrel front.");
            Vector2[] beerAccess = Enumerable.Range(0, 3)
                .Select(index => (Vector2)Call(layout, "PickupPosition", 6, index, 3)).ToArray();
            Assert.AreEqual(3, beerAccess.Distinct().Count(), "The clipped edge of the beer barrel must still provide separate safe access points.");
            Vector2[] tableAccess = Enumerable.Range(0, 4)
                .Select(index => (Vector2)Call(layout, "DepositPosition", 0, index, 4)).ToArray();
            Assert.AreEqual(4, tableAccess.Distinct().Count(), "Four cooks distribute deposits across the table service edge.");
            for (int i = 0; i < tableAccess.Length; i++)
                for (int j = i + 1; j < tableAccess.Length; j++)
                    Assert.Greater(Vector2.Distance(tableAccess[i], tableAccess[j]), 28f,
                        "Deposit points need useful foot separation despite the narrow table.");

            for (int i = 0; i < 12; i++) AddWorkerForTest(sim, "Mozo");
            Vector2[] waits = Workers(sim).Cast<object>().Where(worker => Get(worker, "Role").ToString() == "Mozo")
                .Select(worker => (Vector2)Get(worker, "WaitPosition")).ToArray();
            Assert.AreEqual(waits.Length, waits.Distinct().Count(), "Idle workers must not share their home position.");
            for (int i = 0; i < waits.Length; i++)
            {
                Assert.GreaterOrEqual(waits[i].x, LayoutConstant(workstationLayoutType, "WorkerHalfWidth"));
                Assert.LessOrEqual(waits[i].x, LayoutConstant(sceneLayoutType, "Width") - LayoutConstant(workstationLayoutType, "WorkerHalfWidth"));
                for (int j = i + 1; j < waits.Length; j++)
                    Assert.Greater(Vector2.Distance(waits[i], waits[j]), 20f, "Waiting characters need visibly separate foot anchors.");
                Assert.IsTrue((bool)Call(layout, "IsRouteSegmentClear", waits[i], waits[i] + Vector2.right * .1f),
                    "Idle parking points stay outside furniture footprints.");
            }
        }

        [Test]
        public void MovingWorkerCrossesStationaryWorkerDirectlyAtRoleSpeed()
        {
            object sim = MakeActiveSimulation(5, 1000000);
            Call(sim, "StartRound");
            Assert.IsTrue((bool)Call(sim, "TryUpgradeSpeed"));
            object stationary = Workers(sim)[0];
            object mover = AddWorkerForTest(sim, "Cocacolero");
            Vector2 blockedLane = new Vector2(250f, LayoutConstant(sceneLayoutType, "WorkerServiceY"));
            SetWorker(stationary, "Position", blockedLane);
            SetWorker(stationary, "Target", blockedLane);
            SetWorker(stationary, "State", EnumValue(stateType, "Idle"));
            SetWorker(mover, "Position", new Vector2(150f, blockedLane.y));
            SetWorker(mover, "Target", new Vector2(350f, blockedLane.y));
            SetWorker(mover, "State", EnumValue(stateType, "ToStation"));

            MethodInfo moveWorker = simulationType.GetMethod("MoveWorker", PrivateInstance);
            Assert.IsNotNull(moveWorker);
            const float dt = .03f;
            float expectedStep = 900f * (float)Get(sim, "WorkRate") * dt;
            float minimumSeparation = float.MaxValue;
            for (int i = 0; i < 200 && (Vector2)Get(mover, "Position") != (Vector2)Get(mover, "Target"); i++)
            {
                Vector2 before = (Vector2)Get(mover, "Position");
                moveWorker.Invoke(sim, new object[] { mover, dt });
                Vector2 after = (Vector2)Get(mover, "Position");
                Assert.That(after.y, Is.EqualTo(blockedLane.y).Within(.001f), "Passing staff do not cause a lateral detour.");
                if (i == 0) Assert.That(after.x - before.x, Is.EqualTo(expectedStep).Within(.001f), "Cocacolero movement keeps WorkRateFor speed.");
                minimumSeparation = Mathf.Min(minimumSeparation,
                    Vector2.Distance(after, (Vector2)Get(stationary, "Position")));
            }
            Assert.Less(minimumSeparation, 24f, "Workers may briefly overlap instead of stopping or avoiding each other.");
            Assert.AreEqual(new Vector2(350f, blockedLane.y), Get(mover, "Position"), "The worker follows the direct segment to its target.");
            Assert.That((float)Get(mover, "TravelDistance"), Is.EqualTo(200f).Within(.001f));
            Assert.AreEqual(blockedLane, Get(stationary, "Position"), "Passing workers do not move idle staff.");
        }

        [Test]
        public void OpposingWorkersCrossOnDirectPathsWithoutRightOfWayOrDelay()
        {
            object sim = MakeActiveSimulation(5, 1000000);
            Call(sim, "StartRound");
            Assert.IsTrue((bool)Call(sim, "TryUpgradeSpeed"));
            object first = Workers(sim)[0];
            object second = AddWorkerForTest(sim, "Cocacolero");
            float y = LayoutConstant(sceneLayoutType, "WorkerServiceY");
            SetWorker(first, "Position", new Vector2(150f, y));
            SetWorker(first, "Target", new Vector2(350f, y));
            SetWorker(first, "State", EnumValue(stateType, "ToStation"));
            SetWorker(second, "Position", new Vector2(350f, y));
            SetWorker(second, "Target", new Vector2(150f, y));
            SetWorker(second, "State", EnumValue(stateType, "ToCounter"));

            MethodInfo moveWorker = simulationType.GetMethod("MoveWorker", PrivateInstance);
            Assert.IsNotNull(moveWorker);
            const float dt = .03f;
            float expectedCookStep = 900f * (float)Get(sim, "CookWorkRate") * dt;
            float expectedOtherStep = 900f * (float)Get(sim, "WorkRate") * dt;
            float minimumSeparation = float.MaxValue;
            for (int i = 0; i < 200 && ((Vector2)Get(first, "Position") != (Vector2)Get(first, "Target") ||
                (Vector2)Get(second, "Position") != (Vector2)Get(second, "Target")); i++)
            {
                Vector2 beforeFirst = (Vector2)Get(first, "Position");
                moveWorker.Invoke(sim, new object[] { first, dt });
                Vector2 afterFirst = (Vector2)Get(first, "Position");
                Assert.That(afterFirst.y, Is.EqualTo(y).Within(.001f), "The Parrillero stays on its direct route.");
                if (i == 0) Assert.That(afterFirst.x - beforeFirst.x, Is.EqualTo(expectedCookStep).Within(.001f), "Parrillero movement uses CookWorkRate.");
                minimumSeparation = Mathf.Min(minimumSeparation,
                    Vector2.Distance(afterFirst, (Vector2)Get(second, "Position")));
                Vector2 beforeSecond = (Vector2)Get(second, "Position");
                moveWorker.Invoke(sim, new object[] { second, dt });
                Vector2 afterSecond = (Vector2)Get(second, "Position");
                Assert.That(afterSecond.y, Is.EqualTo(y).Within(.001f), "The Cocacolero stays on its direct route.");
                if (i == 0) Assert.That(beforeSecond.x - afterSecond.x, Is.EqualTo(expectedOtherStep).Within(.001f), "The Cocacolero retains its +10% role rate.");
                minimumSeparation = Mathf.Min(minimumSeparation,
                    Vector2.Distance((Vector2)Get(first, "Position"), afterSecond));
            }
            Assert.Less(minimumSeparation, 24f, "Opposing workers may overlap while crossing; no artificial right-of-way is applied.");
            Assert.AreEqual(new Vector2(350f, y), Get(first, "Position"));
            Assert.AreEqual(new Vector2(150f, y), Get(second, "Position"));
            Assert.That((float)Get(first, "TravelDistance"), Is.EqualTo(200f).Within(.001f));
            Assert.That((float)Get(second, "TravelDistance"), Is.EqualTo(200f).Within(.001f));
        }

        [Test]
        public void MultipleMozosFollowDirectRoutesWhenTheyCross()
        {
            object sim = MakeActiveSimulation(5, 0);
            float y = LayoutConstant(sceneLayoutType, "WorkerServiceY");
            float[] starts = { 100f, 250f, 400f };
            float[] destinations = { 400f, 100f, 250f };
            var mozos = new List<object>();
            for (int i = 0; i < starts.Length; i++)
            {
                object mozo = AddWorkerForTest(sim, "Mozo");
                SetWorker(mozo, "Position", new Vector2(starts[i], y));
                SetWorker(mozo, "Target", new Vector2(destinations[i], y));
                SetWorker(mozo, "State", EnumValue(stateType, "ToCounter"));
                mozos.Add(mozo);
            }

            MethodInfo moveWorker = simulationType.GetMethod("MoveWorker", PrivateInstance);
            Assert.IsNotNull(moveWorker);
            for (int tick = 0; tick < 100; tick++)
            {
                bool allArrived = true;
                foreach (object mozo in mozos)
                {
                    if ((Vector2)Get(mozo, "Position") == (Vector2)Get(mozo, "Target")) continue;
                    allArrived = false;
                    moveWorker.Invoke(sim, new object[] { mozo, .03f });
                    Assert.That(((Vector2)Get(mozo, "Position")).y, Is.EqualTo(y).Within(.001f),
                        "Mozos continue straight through occupied worker space without a lateral avoidance path.");
                }
                if (allArrived) break;
            }

            for (int i = 0; i < mozos.Count; i++)
            {
                Assert.AreEqual(new Vector2(destinations[i], y), Get(mozos[i], "Position"));
                Assert.That((float)Get(mozos[i], "TravelDistance"), Is.EqualTo(Mathf.Abs(destinations[i] - starts[i])).Within(.001f));
            }
        }

        [TestCase(0, "Parrillero")]
        [TestCase(5, "Mozo")]
        public void UnassignedWorkerReturnsToItsOwnSafeWaitingPoint(int level, string roleName)
        {
            object sim = MakeActiveSimulation(level, 0);
            Call(sim, "StartRound");
            object worker = level == 5 && roleName == "Mozo"
                ? Workers(sim).Cast<object>().First(candidate => Get(candidate, "Role").ToString() == "Mozo")
                : AddWorkerForTest(sim, roleName);
            SetArrivalFarFuture(sim);
            SetWorker(worker, "Position", new Vector2(200f, LayoutConstant(sceneLayoutType, "WorkerServiceY")));
            SetWorker(worker, "Target", new Vector2(200f, LayoutConstant(sceneLayoutType, "WorkerServiceY")));

            Vector2 wait = (Vector2)Get(worker, "WaitPosition");
            for (int i = 0; i < 120 && (Vector2)Get(worker, "Position") != wait; i++) Call(sim, "Step", .05f);
            Assert.AreEqual(wait, Get(worker, "Position"), "A worker without an assignment should clear its previous work point and return to its unique waiting spot.");
        }

        [TestCase("Cocacolero", 4)]
        [TestCase("Fernetero", 5)]
        [TestCase("Mozo", 0)]
        public void SameSpecialtyWorkersTakeParallelFrontRowOrdersAndReturnToStandby(string roleName, int product)
        {
            object sim = MakeActiveSimulation(5, 1000000);
            Call(sim, "StartRound"); SetArrivalFarFuture(sim);
            int hires = roleName == "Mozo" ? 1 : 2; // The free Racing starter supplies the first Mozo.
            for (int i = 0; i < hires; i++)
                Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, roleName)), "Hire " + roleName);
            if (roleName == "Mozo") SeedCookedUnits(Get(sim, "Kitchen"), product, 3, 1000);
            for (int i = 0; i < 3; i++) Assert.IsTrue((bool)Call(sim, "SpawnCustomer", product, 1));
            IList customers = (IList)Get(sim, "Customers");
            foreach (object customer in customers)
            {
                customer.GetType().GetProperty("Position", PublicInstance).GetSetMethod(true)
                    .Invoke(customer, new object[] { Get(customer, "Target") });
                customer.GetType().GetProperty("State", PublicInstance).GetSetMethod(true)
                    .Invoke(customer, new[] { Enum.Parse(Get(customer, "State").GetType(), "Waiting") });
            }
            int[] roleIds = Workers(sim).Cast<object>().Where(w => Get(w, "Role").ToString() == roleName).Select(w => (int)Get(w, "Id")).ToArray();
            Assert.AreEqual(2, roleIds.Length);
            object[] assignedTogether = null;
            for (int frame = 0; frame < 10; frame++)
            {
                Call(sim, "Step", .05f);
                assignedTogether = Workers(sim).Cast<object>().Where(w => Get(w, "Role").ToString() == roleName && (int)Get(w, "CustomerId") != 0).ToArray();
                if (assignedTogether.Length == 2) break;
            }
            Assert.AreEqual(2, assignedTogether.Length, "Both specialists should leave standby on distinct simultaneous eligible tickets.");
            Assert.AreNotEqual(Get(assignedTogether[0], "CustomerId"), Get(assignedTogether[1], "CustomerId"), "Customer claims cannot be duplicated.");
            foreach (object worker in assignedTogether)
                Assert.IsTrue((Get(worker, "State").ToString() == "ToStation" || Get(worker, "State").ToString() == "Pickup"),
                    "Assigned staff should be moving to, or collecting from, the actual product source.");

            for (int frame = 0; frame < 4000 && (int)Get(sim, "Delivered") < 3; frame++) Call(sim, "Step", .05f);
            Assert.AreEqual(3, Get(sim, "Delivered"), roleName + " delivered all front-row orders.");
            Assert.AreEqual(3, Call(sim, "GetProductDelivered", product));
            for (int frame = 0; frame < 200; frame++) Call(sim, "Step", .05f);
            foreach (object worker in Workers(sim))
            {
                if (Get(worker, "Role").ToString() != roleName) continue;
                Assert.AreEqual("Idle", Get(worker, "State").ToString(), "The specialist should release its assignment after handoff.");
                Assert.AreEqual(0, Get(worker, "CustomerId"));
                Assert.AreEqual(Get(worker, "WaitPosition"), Get(worker, "Position"), "Standby workers return to their own safe anchor.");
                Assert.Greater((float)Get(worker, "TravelDistance"), 0f, "Every hired role actually moved on a real route.");
            }
            foreach (object customer in customers) Assert.AreEqual(0, Get(customer, "Reserved"));
        }

    }
}
