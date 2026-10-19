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

        private object Make(int[] products, bool active, Action<object> configure = null)
        {
            object balance = Activator.CreateInstance(balanceType);
            configure?.Invoke(balance);
            return Activator.CreateInstance(productionType, new object[] { balance, products, null, active });
        }

        private static object Get(object value, string name) => value.GetType().GetProperty(name, PublicInstance).GetValue(value);
        private static object GetField(object value, string name) => value.GetType().GetField(name, PublicInstance).GetValue(value);
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

        [Test]
        public void SpeedUpgradesGiveCooksFivePercentAndOtherWorkersTenPercent()
        {
            object sim = MakeActiveSimulation(5, 1000000);
            Call(sim, "StartRound");
            Assert.AreEqual(1f, Get(sim, "WorkRate"));
            Assert.AreEqual(1f, Get(sim, "CookWorkRate"));

            Assert.IsTrue((bool)Call(sim, "TryUpgradeSpeed"));
            Assert.That((float)Get(sim, "WorkRate"), Is.EqualTo(1.10f).Within(.0001f));
            Assert.That((float)Get(sim, "CookWorkRate"), Is.EqualTo(1.05f).Within(.0001f));
            for (int i = 1; i < 9; i++) Assert.IsTrue((bool)Call(sim, "TryUpgradeSpeed"));
            Assert.That((float)Get(sim, "WorkRate"), Is.EqualTo(1.90f).Within(.0001f));
            Assert.That((float)Get(sim, "CookWorkRate"), Is.EqualTo(1.45f).Within(.0001f));

            MethodInfo workRateFor = simulationType.GetMethod("WorkRateFor", PrivateInstance);
            foreach (string role in new[] { "Parrillero", "ParrilleroPremium" })
                Assert.That((float)workRateFor.Invoke(sim, new[] { EnumValue(roleType, role) }), Is.EqualTo(1.45f).Within(.0001f));
            foreach (string role in new[] { "Cocacolero", "Fernetero", "Mozo" })
                Assert.That((float)workRateFor.Invoke(sim, new[] { EnumValue(roleType, role) }), Is.EqualTo(1.90f).Within(.0001f));
        }

        [Test]
        public void ParallelCooksUseOneGlobalGoalWeightedScheduleWithoutOrdersOrStockBias()
        {
            int[] goals = { 100, 50, 80, 40, 1000, 1000, 1000 };
            object normal = MakeActiveSimulation(5, 100000, goals);
            Call(normal, "StartRound");
            SetArrivalFarFuture(normal);
            Assert.IsTrue((bool)Call(normal, "TryHire", EnumValue(roleType, "Parrillero")));
            Assert.IsTrue((bool)Call(normal, "TryHire", EnumValue(roleType, "Parrillero")));
            SeedCookedUnits(Get(normal, "Kitchen"), 1, 8, 1000);
            var normalFirstAssignments = new Dictionary<int, int>();
            for (int tick = 0; tick < 200 && normalFirstAssignments.Count < 3; tick++)
            {
                Step(normal, 1);
                foreach (object worker in Workers(normal))
                    if (Get(worker, "Role").ToString() == "Parrillero" && (int)Get(worker, "CookingItemId") > 0 &&
                        !normalFirstAssignments.ContainsKey((int)Get(worker, "Id")))
                        normalFirstAssignments.Add((int)Get(worker, "Id"), (int)Get(worker, "Product"));
            }
            Assert.AreEqual(3, normalFirstAssignments.Count, "Every cook must acquire its first real reservation.");
            CollectionAssert.AreEquivalent(new[] { 0, 0, 1 }, normalFirstAssignments.Values,
                "Three simultaneous workers share the 100:50 schedule even with eight prepared Patys and no customers.");
            Assert.AreEqual(0, Get(normal, "Delivered"), "Background production must not manufacture a sale.");

            object premium = MakeActiveSimulation(5, 100000, goals);
            Call(premium, "StartRound");
            SetArrivalFarFuture(premium);
            for (int i = 0; i < 3; i++) Assert.IsTrue((bool)Call(premium, "TryHire", EnumValue(roleType, "ParrilleroPremium")));
            var premiumFirstAssignments = new Dictionary<int, int>();
            for (int tick = 0; tick < 200 && premiumFirstAssignments.Count < 3; tick++)
            {
                Step(premium, 1);
                foreach (object worker in Workers(premium))
                    if (Get(worker, "Role").ToString() == "ParrilleroPremium" && (int)Get(worker, "CookingItemId") > 0 &&
                        !premiumFirstAssignments.ContainsKey((int)Get(worker, "Id")))
                        premiumFirstAssignments.Add((int)Get(worker, "Id"), (int)Get(worker, "Product"));
            }
            Assert.AreEqual(3, premiumFirstAssignments.Count, "Every Premium cook must acquire its first real reservation.");
            CollectionAssert.AreEquivalent(new[] { 2, 2, 3 }, premiumFirstAssignments.Values,
                "Premium workers share the 80:40 schedule globally.");
            Assert.AreEqual(0, Get(premium, "Delivered"));
        }

        [Test]
        public void WeightedScheduleSupportsNonDivisibleTargetsAndSkipsCompletedQuota()
        {
            int[] goals = { 7, 3, 80, 40, 1000, 1000, 1000 };
            object sim = MakeActiveSimulation(5, 100000, goals);
            Call(sim, "StartRound");
            SetArrivalFarFuture(sim);
            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "Parrillero")));
            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "Parrillero")));
            var ids = new HashSet<int>();
            var assignments = new List<int>();
            for (int tick = 0; tick < 1200 && assignments.Count < 10; tick++)
            {
                Step(sim, 1);
                foreach (object worker in Workers(sim))
                {
                    int id = (int)Get(worker, "CookingItemId");
                    if (id > 0 && ids.Add(id)) assignments.Add((int)Get(worker, "Product"));
                }
            }
            Assert.AreEqual(10, assignments.Count, "Cooks should continue producing without customers.");
            Assert.AreEqual(7, assignments.Count(product => product == 0));
            Assert.AreEqual(3, assignments.Count(product => product == 1));

            object completed = MakeActiveSimulation(5, 100000, new[] { 100, 50, 80, 40, 1000, 1000, 1000 });
            Call(completed, "StartRound");
            SetArrivalFarFuture(completed);
            ((int[])simulationType.GetField("deliveredByProduct", PrivateInstance).GetValue(completed))[0] = 100;
            Step(completed, 1);
            Assert.AreEqual(1, Get(Workers(completed)[0], "Product"),
                "Once Chori reaches its real level quota, new cooking must prioritize the still-incomplete Paty quota.");
        }

        [Test]
        public void FullSpecialtyTableFallsBackThenResumesFairScheduling()
        {
            object sim = MakeActiveSimulation(5, 100000, new[] { 100, 50, 80, 40, 1000, 1000, 1000 });
            Call(sim, "StartRound");
            SetArrivalFarFuture(sim);
            object kitchen = Get(sim, "Kitchen");
            SeedCookedUnits(kitchen, 0, (int)Call(kitchen, "TableCapacity", 0), 2000);

            Step(sim, 1);
            object firstCook = Workers(sim)[0];
            Assert.AreEqual(1, Get(firstCook, "Product"), "A full Chori table must not block the available Paty recipe.");

            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "Parrillero")));
            Assert.IsNotNull(Call(kitchen, "ReserveTableUnit", 0, 9000));
            object item = Call(kitchen, "TryTake", 0, EnumValue(roleType, "Mozo"), 9000);
            Assert.IsNotNull(item);
            Assert.IsTrue((bool)Call(kitchen, "Consume", Get(item, "Id"), 0, 9000));
            Step(sim, 1);
            Assert.AreEqual(0, Get(Workers(sim)[1], "Product"),
                "When a Chori table slot opens, virtual-finish scheduling resumes the goal-weighted stream.");
        }

        [Test]
        public void IngredientShortageFallsBackAndResumesWhenStockReturns()
        {
            int[] goals = { 100, 50, 80, 40, 1000, 1000, 1000 };
            object balance = MakeActiveBalance(5, 100000, goals);
            object sim = Activator.CreateInstance(simulationType, new object[] { balance, 5, 5f, 0, 1, 0 });
            object data = Activator.CreateInstance(marketDataType);
            int[] stocked = new int[10];
            for (int i = 0; i < stocked.Length; i++) stocked[i] = 10000;
            marketDataType.GetField("stockUnits", PublicInstance).SetValue(data, stocked);
            object market = Activator.CreateInstance(marketType, new[] { data, balance });
            Assert.IsTrue((bool)Call(market, "BeginAttempt", sim));
            Call(sim, "BindIngredientStock", market);
            Call(sim, "StartRound");
            SetArrivalFarFuture(sim);
            data = Get(market, "Data");
            stocked = (int[])marketDataType.GetField("stockUnits", PublicInstance).GetValue(data);
            stocked[0] = 0; stocked[1] = 0; // Chori recipe unavailable; Paty ingredients remain stocked.

            Step(sim, 1);
            Assert.AreEqual(1, Get(Workers(sim)[0], "Product"), "A missing Chori ingredient must leave the Paty recipe usable.");

            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "Parrillero")));
            stocked[0] = 10000; stocked[1] = 10000;
            Step(sim, 1);
            Assert.AreEqual(0, Get(Workers(sim)[1], "Product"), "The weighted schedule resumes after real ingredients return.");
        }

        [Test]
        public void ParrillerosKeepLoopingVisualCookingAfterTheirQuotasWithoutChangingGameState()
        {
            int[] goals = { 8, 4, 6, 3, 1000, 1000, 1000 };
            object sim = MakeActiveSimulation(5, 100000, goals);
            Call(sim, "StartRound");
            SetArrivalFarFuture(sim);
            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "ParrilleroPremium")));

            int coinsBefore = (int)Get(sim, "Coins");
            int[] deliveredByProduct = (int[])simulationType.GetField("deliveredByProduct", PrivateInstance).GetValue(sim);
            for (int product = 0; product < 4; product++) deliveredByProduct[product] = goals[product];
            object kitchen = Get(sim, "Kitchen");
            int sandwichUnitsBefore = Units(kitchen).Cast<object>().Count(unit => (int)Get(unit, "Product") < 4);
            Step(sim, 245); // More than six visual-only cooking cycles at the real two-second rate.

            Assert.AreEqual("Playing", Get(sim, "Phase").ToString(), "Outstanding beverage goals keep the match running after sandwich quotas are complete.");
            foreach (object worker in Workers(sim))
            {
                string role = Get(worker, "Role").ToString();
                if (role != "Parrillero" && role != "ParrilleroPremium") continue;
                Assert.IsTrue((bool)Get(worker, "VisualCooking"), role + " must stay visually active at its grill.");
                Assert.AreEqual("Idle", Get(worker, "State").ToString(), "Cosmetic cooking never enters a transport state.");
                Assert.AreEqual(0, Get(worker, "CookingItemId"));
                Assert.AreEqual(0, Get(worker, "CarriedItemId"));
                Assert.That((float)Get(worker, "CookingProgress"), Is.GreaterThan(0f).And.LessThan(1f));
                object pose = AnimationCall("Sample", worker, false);
                Assert.IsTrue((bool)GetField(pose, "Cooking"), "The existing arms pose loop must remain active.");
                Assert.IsTrue((bool)GetField(pose, "WorkingAtGrill"));
            }

            long[] cosmetic = (long[])simulationType.GetField("visualCookingAssignmentsByProduct", PrivateInstance).GetValue(sim);
            long[] real = (long[])simulationType.GetField("productionAssignmentsByProduct", PrivateInstance).GetValue(sim);
            Assert.That(Math.Abs(cosmetic[0] - 2 * cosmetic[1]), Is.LessThanOrEqualTo(2),
                "Normal cosmetic cycles preserve the 8:4 goal proportion after both quotas are complete.");
            Assert.That(Math.Abs(cosmetic[2] - 2 * cosmetic[3]), Is.LessThanOrEqualTo(2),
                "Premium cosmetic cycles preserve the 6:3 goal proportion after both quotas are complete.");
            Assert.AreEqual(0, real.Sum(), "Cosmetic cycles never enter real production scheduling.");
            Assert.AreEqual(sandwichUnitsBefore, Units(kitchen).Cast<object>().Count(unit => (int)Get(unit, "Product") < 4),
                "No physical sandwich units are created.");
            Assert.AreEqual(coinsBefore, Get(sim, "Coins"));
            Assert.AreEqual(0, Get(sim, "Delivered"));
            Assert.AreEqual(0, ((IEnumerable)Get(sim, "Sales")).Cast<object>().Count());
        }

        [Test]
        public void VisualCookingUsesNoIngredientsAndAutomaticallyResumesRealProductionWhenStockReturns()
        {
            int[] goals = { 100, 50, 80, 40, 1000, 1000, 1000 };
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
            data = Get(market, "Data");
            stocked = (int[])marketDataType.GetField("stockUnits", PublicInstance).GetValue(data);
            for (int i = 0; i < stocked.Length; i++) stocked[i] = 0;
            int[] stockBefore = (int[])stocked.Clone();

            Step(sim, 180);
            object kitchen = Get(sim, "Kitchen");
            var visualPhases = new Dictionary<int, float>();
            foreach (object worker in Workers(sim))
            {
                string role = Get(worker, "Role").ToString();
                if (role != "Parrillero" && role != "ParrilleroPremium") continue;
                Assert.IsTrue((bool)Get(worker, "VisualCooking"), role + " must stay in visual-only mode while all ingredients are absent.");
                Assert.AreEqual(0, Get(worker, "CookingItemId"));
                Assert.AreEqual(0, Get(worker, "CarriedItemId"));
                visualPhases[(int)Get(worker, "Id")] = (float)Get(worker, "CookingCycleProgress");
            }
            CollectionAssert.AreEqual(stockBefore, stocked, "Visual-only cooking must not reserve or consume ingredients.");
            Assert.AreEqual(0, Units(kitchen).Cast<object>().Count(unit => (int)Get(unit, "Product") < 4));

            for (int i = 0; i < stocked.Length; i++) stocked[i] = 10000;
            Step(sim, 1);
            foreach (object worker in Workers(sim))
            {
                string role = Get(worker, "Role").ToString();
                if (role != "Parrillero" && role != "ParrilleroPremium") continue;
                Assert.Greater((int)Get(worker, "CookingItemId"), 0, role + " should reserve a real item when stock returns.");
                Assert.IsFalse((bool)Get(worker, "VisualCooking"));
                Assert.AreEqual(visualPhases[(int)Get(worker, "Id")], Get(worker, "CookingCycleProgress"),
                    "Switching from visual to real cooking must not reset the hand-animation phase.");
            }
            Step(sim, 3);
            foreach (object worker in Workers(sim))
            {
                string role = Get(worker, "Role").ToString();
                if (role != "Parrillero" && role != "ParrilleroPremium") continue;
                Assert.AreEqual("Cooking", Get(worker, "State").ToString());
                Assert.AreNotEqual(0f, Get(worker, "CookingCycleProgress"),
                    "Real cooking continues the prior visual arm phase rather than restarting at zero.");
                object unit = Call(kitchen, "Find", Get(worker, "CookingItemId"));
                Assert.IsNotNull(unit);
                Assert.AreEqual("Cooking", Get(unit, "State").ToString());
                Assert.That((float)Get(unit, "Progress"), Is.GreaterThan(0f));
            }
        }

        [Test]
        public void FullCookTablesUseReusableVisualCyclesAndResumeProductionWhenSlotsOpen()
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
                Assert.IsTrue((bool)Get(worker, "VisualCooking"), role + " must keep cooking visually while all specialty tables are full.");
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
            Step(sim, 1);
            foreach (object worker in Workers(sim))
            {
                string role = Get(worker, "Role").ToString();
                if (role != "Parrillero" && role != "ParrilleroPremium") continue;
                Assert.Greater((int)Get(worker, "CookingItemId"), 0, role + " should resume real cooking without a visual-mode restart delay.");
                Assert.IsFalse((bool)Get(worker, "VisualCooking"));
            }
            Assert.AreEqual(fullUnitCount, Units(kitchen).Count,
                "Only two real units replace the two table units consumed to free capacity; no simulated food remains.");
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
            Call(kitchen, "Advance", .27f, 1.05f, 1.10f, true, false, true, false);
            Assert.AreEqual(1, Call(kitchen, "TableCount", 4), "Beverage preparation continues at its independent 1.10 rate.");
            object raw = Units(kitchen).Cast<object>().First(unit => (int)Get(unit, "Product") == 0 &&
                Get(unit, "Location").ToString() == "Grill");
            Assert.That((float)Get(raw, "Progress"), Is.EqualTo(.2835f).Within(.001f),
                "Automatic meat cooking uses the differentiated 1.05 cook rate.");
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
            Assert.That((float)Get(raw, "Progress"), Is.EqualTo(.013125f).Within(.0001f),
                "Level 1's automatic kitchen must use +5% Parrillero speed without requiring a customer.");
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

        [Test]
        public void CookedUnitMustBeClaimedAndDepositedIntoItsReservedTableSlot()
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
            Assert.IsTrue((bool)Call(kitchen, "DepositCooked", id, 7));
            Assert.AreEqual("Table", Get(unit, "Location").ToString());
            Assert.AreEqual(1, (int)Call(kitchen, "TableCount", 0));
        }

        [Test]
        public void TableCapacityAndWorkerReservationsPreventOverbookingAndReleaseCleanly()
        {
            object kitchen = Make(new[] { 0 }, true, balance =>
                balanceType.GetField("normalTableCapacity", PublicInstance).SetValue(balance, 1));
            object unit = Call(kitchen, "ReserveCook", 0, 11);
            int id = (int)Get(unit, "Id");
            Assert.IsNull(Call(kitchen, "ReserveTableUnit", 0, 11), "The in-flight item owns its destination slot directly.");
            Assert.IsNull(Call(kitchen, "ReserveTableUnit", 0, 12), "In-flight items consume finite table capacity.");
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
        public void RealMarketIngredientsAreReleasedOnCookCancellationAndConsumedOnlyAtSale()
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
            Assert.AreEqual(chorizoBefore - 1, Call(market, "Units", 0));
            Assert.AreEqual(breadBefore - 1, Call(market, "Units", 1));
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
        public void ActiveCookGateStartsAtSixthLevelAndDoesNotChangeEarlierMode()
        {
            object balance = Activator.CreateInstance(simulationBalanceType);
            for (int level = 0; level < 11; level++)
            {
                object sim = Activator.CreateInstance(simulationType, new object[] { balance, level, 5f, 0, 1, 0 });
                Assert.AreEqual(level >= 5, (bool)Get(sim, "UsesActiveCooks"), "Unexpected active cook mode at level index " + level);
                Assert.AreEqual(0, Get(sim, "MozoCount"), "A fresh team never receives a free Mozo.");
            }
            object early = Activator.CreateInstance(simulationType, new object[] { balance, 4, 5f, 0, 1, 0 });
            object active = Activator.CreateInstance(simulationType, new object[] { balance, 5, 5f, 0, 1, 0 });
            Assert.AreEqual(0, Get(early, "MozoCount"));
            Assert.AreEqual(0, Get(active, "MozoCount"), "Mozo is never granted automatically.");
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
        public void LegacySaveWithoutMozoFieldDefaultsToNoMozo()
        {
            Type gameType = Type.GetType("HayChoriYPaty.StreetGame, Assembly-CSharp", true);
            Type saveType = gameType.GetNestedType("SaveData", BindingFlags.Public);
            object restored = UnityEngine.JsonUtility.FromJson("{\"unlockedLevel\":7,\"coins\":25,\"price\":5}", saveType);
            Assert.AreEqual(0, saveType.GetField("mozos").GetValue(restored));
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
                HashSet<int> slots = new HashSet<int> { 0, 1, 3, capacity / 2, capacity - 1 };

                foreach (int slot in slots)
                {
                    Vector2 item = (Vector2)Call(layout, "TableSlotPosition", product, slot, companionAvailable, capacity);
                    Vector2 target = (Vector2)Call(layout, "TableSlotPickupPosition", product, slot, companionAvailable, capacity);
                    Assert.AreEqual(Mathf.Clamp(item.x, workerHalfWidth, sceneWidth - workerHalfWidth), target.x,
                        "Product " + product + " slot " + slot + " keeps the full Mozo on screen while following the food's X position.");
                    Assert.AreEqual(standard.y + item.y - firstItem.y, target.y, .001f,
                        "Product " + product + " slot " + slot + " follows its row/depth on the same reachable table front.");
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

        [Test]
        public void FourSandwichRolesAndMixedBeveragesAllCompleteRealDeliveriesWithoutRouteBlocking()
        {
            object sim = MakeActiveSimulation(5, 3000);
            Call(sim, "StartRound");
            SetArrivalFarFuture(sim);
            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "ParrilleroPremium")));
            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "Cocacolero")));
            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "Fernetero")));
            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "Mozo")));
            for (int product = 0; product < 7; product++)
                Assert.IsTrue((bool)Call(sim, "SpawnCustomer", product, 1), "Expected valid level-6 order product " + product);

            for (int i = 0; i < 1200 && (int)Get(sim, "Delivered") < 7; i++) Call(sim, "Step", .05f);
            string deliveredByProduct = string.Join(",", Enumerable.Range(0, 7).Select(product => product + "=" + Call(sim, "GetProductDelivered", product)));
            string workerStates = string.Join(" | ", Workers(sim).Cast<object>().Select(worker => Get(worker, "Id") + ":" + Get(worker, "Role") + ":" + Get(worker, "State") + " product=" + Get(worker, "Product") + " pos=" + Get(worker, "Position") + " target=" + Get(worker, "Target")));
            string supplies = string.Join(",", Enumerable.Range(0, 7).Select(product => product + "=" + Call(Get(sim, "Kitchen"), "SupplyCount", product)));
            Assert.AreEqual(7, Get(sim, "Delivered"), "Safe worker routes should make every sandwich and beverage handoff reachable. Delivered by product: " + deliveredByProduct + "; supplies: " + supplies + "; workers: " + workerStates);
            for (int product = 0; product < 7; product++) Assert.AreEqual(1, Call(sim, "GetProductDelivered", product), "Incorrect count for product " + product);
            Assert.AreEqual(0, Get(sim, "SpeedLevel"), "A fresh attempt remains at x1.00 until speed is purchased.");
            Assert.AreEqual(1f, Get(sim, "WorkRate"));
            Assert.AreEqual(1, Get(sim, "ParrilleroCount"), "A fresh attempt starts with exactly one Parrillero.");
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
            int hired = roleName == "Parrillero" ? 1 : 0;
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
            int hired = roleName == "Parrillero" ? 1 : 0;
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
                if (i == 0) Assert.That(afterFirst.x - beforeFirst.x, Is.EqualTo(expectedCookStep).Within(.001f), "Parrillero retains its +5% role rate.");
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
            object worker = AddWorkerForTest(sim, roleName);
            Call(sim, "StartRound");
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
            for (int i = 0; i < 2; i++)
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
