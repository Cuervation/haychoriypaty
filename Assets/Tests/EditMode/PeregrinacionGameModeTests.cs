using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    /// <summary>Reflection-only domain checks so EditMode does not bind to Assembly-CSharp directly.</summary>
    public sealed class PeregrinacionGameModeTests
    {
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private object mode;
        private ScriptableObject config;

        [TearDown]
        public void TearDown()
        {
            Invoke(mode, "Stop");
            if (config != null) UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void HorizontalRouteIsOneStraightRowAndSerpentineLevelsKeepTheirOldRowRule()
        {
            Type routeType = RuntimeType("TowerDefenseRouteGeometryConfig");
            Type kindType = RuntimeType("TowerDefenseRouteKind");
            Type geometryType = RuntimeType("TowerDefenseRouteGeometry");
            object horizontal = Activator.CreateInstance(routeType);
            Set(horizontal, "RouteKind", Enum.ToObject(kindType, 1));
            Set(horizontal, "RowCount", 1);
            Set(horizontal, "SegmentLengths", new[] { 620f });
            Set(horizontal, "CorridorWidth", .9f);
            Set(horizontal, "HorizontalCorridorWidth", .9f);
            Set(horizontal, "RowSpacing", 1.8f);
            Set(horizontal, "PlayableBounds", new Rect(-320f, 380f, 640f, 200f));
            Set(horizontal, "WorkBounds", new Rect(0f, 0f, 508f, 368f));
            Set(horizontal, "TurnSegments", 16);
            object result = geometryType.GetMethod("Generate", BindingFlags.Public | BindingFlags.Static).Invoke(null, new[] { horizontal });
            Assert.IsTrue(Get<bool>(result, "IsValid"), string.Join("; ", Get<string[]>(Get<object>(result, "Validation"), "Errors")));
            Vector2[] path = Get<Vector2[]>(result, "Centerline");
            Assert.AreEqual(2, path.Length);
            Assert.Greater(path[1].x, path[0].x);
            Assert.That(path[1].y, Is.EqualTo(path[0].y).Within(.0001f));

            Set(horizontal, "RowCount", 3);
            Set(horizontal, "SegmentLengths", new[] { 200f, 200f, 200f });
            result = geometryType.GetMethod("Generate", BindingFlags.Public | BindingFlags.Static).Invoke(null, new[] { horizontal });
            Assert.IsFalse(Get<bool>(result, "IsValid"), "The single-fence route must reject accidental extra rows.");

            object legacy = Activator.CreateInstance(routeType);
            Set(legacy, "RowCount", 3);
            Set(legacy, "SegmentLengths", new[] { 3.8f, 3.8f, 3.8f });
            result = geometryType.GetMethod("Generate", BindingFlags.Public | BindingFlags.Static).Invoke(null, new[] { legacy });
            Assert.IsTrue(Get<bool>(result, "IsValid"), "The legacy serpentine row configuration remains supported.");

            Type configType = RuntimeType("TowerDefenseLevelConfig");
            MethodInfo pathMethod = configType.GetMethod("ResourcesPathForCampaignLevel", BindingFlags.Public | BindingFlags.Static);
            foreach (int id in new[] { 9, 10, 13 })
            {
                string pathName = (string)pathMethod.Invoke(null, new object[] { id });
                object level = Resources.Load(pathName, configType);
                Assert.IsNotNull(level, "Missing prior TD config for campaign ID " + id);
                object priorRoute = Get<object>(level, "Route");
                object priorGeometry = geometryType.GetMethod("Generate", BindingFlags.Public | BindingFlags.Static).Invoke(null, new[] { priorRoute });
                Assert.IsTrue(Get<bool>(priorGeometry, "IsValid"), "Prior level route regressed: " + id);
                Assert.AreEqual(0, Convert.ToInt32(Get<object>(priorRoute, "RouteKind")), "Prior level should remain serpentine: " + id);
            }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void SevenFreeStationsRejectOverlapAndEachRoleHasALegalPaidPosition(int roleId)
        {
            BuildFreeConstructionMode();
            Invoke(mode, "Begin");
            Type stationType = RuntimeType("TowerDefenseStationKind");
            object[] stationValues =
            {
                Enum.ToObject(stationType, 0), Enum.ToObject(stationType, 1), Enum.ToObject(stationType, 2),
                Enum.ToObject(stationType, 3), Enum.ToObject(stationType, 4), Enum.ToObject(stationType, 5), Enum.ToObject(stationType, 6)
            };
            Vector2[] stationPositions = { new Vector2(128,64), new Vector2(384,64), new Vector2(256,320),
                new Vector2(64,320), new Vector2(448,320), new Vector2(128,160), new Vector2(384,160) };
            foreach (object kind in stationValues)
            {
                object[] args = { kind, stationPositions[Convert.ToInt32(kind)], null };
                Assert.IsTrue((bool)Invoke(mode, "TryPlaceStation", args), "Could not place equipment kind " + kind);
                Assert.IsNotNull(args[2]);
            }
            int coins = Get<int>(mode, "Coins");
            Assert.IsFalse((bool)Invoke(mode, "TryPlaceStation", new[] { stationValues[0], (object)new Vector2(128,64), null }));
            Assert.IsFalse((bool)Invoke(mode, "TryPlaceStation", new[] { stationValues[0], (object)new Vector2(-64,64), null }));
            Assert.AreEqual(coins, Get<int>(mode, "Coins"), "Rejected previews/placements must not charge.");

            Type roles = RuntimeType("StreetWorkerRole");
            object role=Enum.ToObject(roles,roleId);
            bool hired=false;
            for(int y=32;y<=256&&!hired;y+=32)
            for(int x=64;x<=448&&!hired;x+=32)
            {
                Vector2 feet=new Vector2(x,y);
                if(!(bool)Invoke(mode,"CanHireAt",new[]{role,(object)feet}))continue;
                hired=(bool)Invoke(mode,"TryHireAt",new[]{role,(object)feet});
            }
            Assert.IsTrue(hired,"Role must have a reachable legal placement: "+role);
            Assert.AreEqual(1,Sum(Get<int[]>(mode,"WorkerCountsByRole")));
            Assert.AreEqual(coins-60,Get<int>(mode,"Coins"));
            Invoke(mode, "Pause");
            Assert.AreEqual("Paused", Get<object>(mode, "Phase").ToString());
            Invoke(mode, "Resume");
            Assert.AreEqual("Playing", Get<object>(mode, "Phase").ToString());
        }

        private void BuildFreeConstructionMode()
        {
            Type configType = RuntimeType("TowerDefenseLevelConfig");
            Type routeType = RuntimeType("TowerDefenseRouteGeometryConfig");
            Type kindType = RuntimeType("TowerDefenseRouteKind");
            Type balanceType = RuntimeType("StreetBalance");
            Type modeType = RuntimeType("TowerDefenseGameMode");
            config = ScriptableObject.CreateInstance(configType);
            object route = Activator.CreateInstance(routeType);
            Set(route, "RouteKind", Enum.ToObject(kindType, 1));
            Set(route, "RowCount", 1);
            Set(route, "SegmentLengths", new[] { 620f });
            Set(route, "CorridorWidth", .9f);
            Set(route, "HorizontalCorridorWidth", .9f);
            Set(route, "RowSpacing", 1.8f);
            Set(route, "PlayableBounds", new Rect(-320f, 380f, 640f, 200f));
            Set(route, "WorkBounds", new Rect(0f, 0f, 508f, 368f));
            Set(route, "TurnSegments", 16);
            Set(config, "route", route);
            Set(config, "routeKind", Enum.ToObject(kindType, 1));
            Set(config, "freeConstruction", true);
            Set(config, "sourceCampaignLevelId", 14);
            Set(config, "totalFanCount", 60);
            Set(config, "fanSpeed", .035f);
            Set(config, "availableProducts", new[] { 0, 1, 2, 3, 4, 5, 6 });
            Set(config, "freeStationCounts", new[] { 1, 1, 1, 1, 1, 1, 1 });
            Set(config, "hireCostsByRole", new[] { 60, 60, 60, 60, 60 });
            Set(config, "maximumWorkersPerRole", 3);
            Set(config, "startingCoins", 1200);
            object balance = Activator.CreateInstance(balanceType);
            mode = Activator.CreateInstance(modeType, new object[] { config, balance, route, 1f,
                new Vector2(254,184), new Vector2(254,184), new int[5], new long[5], 1200 });
        }

        private static object Invoke(object target, string method, object[] args = null)
        {
            if (target == null) return null;
            MethodInfo info = target.GetType().GetMethod(method, PublicInstance);
            Assert.IsNotNull(info, "Missing public API " + method);
            try { return info.Invoke(target, args); }
            catch (TargetInvocationException e) { throw e.InnerException; }
        }
        private static T Get<T>(object target, string property)
        {
            PropertyInfo info = target.GetType().GetProperty(property, PublicInstance);
            if(info!=null)return (T)info.GetValue(target,null);
            FieldInfo field=target.GetType().GetField(property,PublicInstance);
            Assert.IsNotNull(field,"Missing public member "+property);
            return (T)field.GetValue(target);
        }
        private static void Set(object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field, PrivateInstance | BindingFlags.Public);
            Assert.IsNotNull(info, "Missing field " + field);
            info.SetValue(target, value);
        }
        private static Type RuntimeType(string shortName) => Type.GetType("HayChoriYPaty." + shortName + ", Assembly-CSharp", true);
        private static int Sum(int[] values) { int total = 0; for (int i = 0; i < values.Length; i++) total += values[i]; return total; }
    }
}
