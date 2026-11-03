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
            Assert.AreEqual(380f, Get<float>(mode, "DurationSeconds"));
            Assert.AreEqual(500, Get<int>(mode, "Coins"));
            Assert.IsTrue(Get<bool>(RuntimeType("TowerDefenseGameMode"), "OwnsRuntime", null));
            Assert.IsTrue(Get<bool>(RuntimeType("TowerDefenseGameMode"), "CampaignStepSuspended", null));
            Assert.AreEqual(7, ((int[])Get<object>(mode, "GoalByProduct")).Length);
            Assert.Greater(Get<int>(mode, "MaximumFans"), 0);
        }

        [Test]
        public void DeliversOnePhysicalSandwichExactlyOnceAndCompletesCorrectFan()
        {
            object kitchenBalance = balanceType.GetField("kitchen", PublicInstance).GetValue(balance);
            kitchenBalance.GetType().GetField("activeCookSeconds", PublicInstance).SetValue(kitchenBalance, 0.01f);
            mode = CreateMode(0.02f);
            Set(mode, "spawnTimer", 100000f);
            Invoke(mode, "Begin");

            Assert.IsTrue((bool)Invoke(mode, "TrySpawnFan", new[] { new[] { 0 }, new[] { 1 } }));
            for (int i = 0; i < 7; i++) Invoke(mode, "Step", new[] { (object)1f });

            Assert.AreEqual(1, Get<int>(mode, "TotalDelivered"));
            Assert.AreEqual(1, Get<int[]>(mode, "DeliveredByProduct")[0]);
            Assert.AreEqual(1, Get<int>(mode, "CompletedOrdersAtExit"));
            Assert.AreEqual(0, Get<int>(mode, "IncompleteOrdersAtExit"));
        }

        [Test]
        public void IncompleteOrdersExitWithoutImpatienceTimeoutOrInventedPenalty()
        {
            Set(config, "startingWorkersByRole", new[] { 0, 0, 0, 0, 0 });
            mode = CreateMode(0.02f);
            Set(mode, "spawnTimer", 100000f);
            Invoke(mode, "Begin");
            Assert.IsTrue((bool)Invoke(mode, "TrySpawnFan", new[] { new[] { 6 }, new[] { 4 } }));

            for (int i = 0; i < 7; i++) Invoke(mode, "Step", new[] { (object)1f });

            Assert.AreEqual(1, Get<int>(mode, "IncompleteOrdersAtExit"));
            Assert.AreEqual(0, Get<int>(mode, "CompletedOrdersAtExit"));
            Assert.IsFalse(Get<bool>(mode, "TimedOut"));
            Assert.AreEqual(0, Get<int>(mode, "TotalDelivered"));
        }

        [Test]
        public void SortingSampledOrderLinesKeepsEachQuantityWithItsProduct()
        {
            int[] products = { 5, 1, 3 };
            int[] quantities = { 4, 2, 1 };
            MethodInfo sort = modeType.GetMethod("SortOrderPairs", PrivateInstance | BindingFlags.Static);
            Assert.IsNotNull(sort);
            sort.Invoke(null, new object[] { products, quantities });

            CollectionAssert.AreEqual(new[] { 1, 3, 5 }, products);
            CollectionAssert.AreEqual(new[] { 2, 1, 4 }, quantities);
        }

        [Test]
        public void DemandSamplingPrioritizesUnrequestedQuotaAndDownweightsCompletedProducts()
        {
            mode = CreateMode(0.02f);
            Invoke(mode, "Begin");
            Set(mode, "elapsed", 190f);
            MethodInfo demandWeight = modeType.GetMethod("ProductDemandWeight", PrivateInstance);
            Assert.IsNotNull(demandWeight);

            float unrequestedWeight = (float)demandWeight.Invoke(mode, new object[] { 0 });
            Assert.IsTrue((bool)Invoke(mode, "TrySpawnFan", new[] { new[] { 0 }, new[] { 4 } }));
            float reservedWeight = (float)demandWeight.Invoke(mode, new object[] { 0 });
            Assert.Less(reservedWeight, unrequestedWeight, "An existing order should reduce additional demand for the same quota.");

            int[] delivered = (int[])modeType.GetField("deliveredByProduct", PrivateInstance).GetValue(mode);
            delivered[0] = Get<int[]>(mode, "GoalByProduct")[0];
            Assert.AreEqual(0.10f, (float)demandWeight.Invoke(mode, new object[] { 0 }), 0.0001f,
                "Completed products remain orderable but use the campaign's lower extra-sales weight.");
        }

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

        private static Type RuntimeType(string shortName) => Type.GetType("HayChoriYPaty." + shortName + ", Assembly-CSharp", true);

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
