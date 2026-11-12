using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        [Test]
        public void BocaPricingAndStarterRosterAreOptInAndKeepLujanIdentity()
        {
            BuildFreeConstructionMode();
            object route = Get<object>(config, "Route");
            Type balanceType = RuntimeType("StreetBalance");
            object balance = Activator.CreateInstance(balanceType);
            Invoke(mode, "Stop");
            Set(config, "sourceCampaignLevelId", 14);
            Set(config, "economicBalanceCampaignLevelId", 8);
            Set(config, "useCampaignEconomyPricing", true);
            Set(config, "startingWorkersByRole", new[] { 1, 0, 0, 0, 1 });
            Set(config, "hireCostsByRole", new[] { 55, 55, 70, 65, 65 });
            mode = Activator.CreateInstance(RuntimeType("TowerDefenseGameMode"), new object[]
            {
                config, balance, route, 1f, new Vector2(254,184), new Vector2(254,184),
                new int[5], new long[5], 1200
            });

            Assert.AreEqual(14, Get<int>(config, "SourceCampaignLevelId"), "Economy-source ID must not replace Luján's campaign identity.");
            Assert.AreEqual(8, Get<int>(config, "EconomicBalanceCampaignLevelId"));
            Assert.AreEqual(1200, Get<int>(mode, "Coins"), "Luján's opening cash remains level-specific.");
            CollectionAssert.AreEqual(new[] { 1, 0, 0, 0, 1 }, Get<int[]>(mode, "WorkerCountsByRole"));
            Type roleType = RuntimeType("StreetWorkerRole");
            Assert.AreEqual(55, Invoke(mode, "HireCost", new[] { Enum.ToObject(roleType, 0) }));
            Assert.AreEqual(55, Invoke(mode, "HireCost", new[] { Enum.ToObject(roleType, 1) }));
            Assert.AreEqual(70, Invoke(mode, "HireCost", new[] { Enum.ToObject(roleType, 2) }));
            Assert.AreEqual(65, Invoke(mode, "HireCost", new[] { Enum.ToObject(roleType, 3) }));
            Assert.AreEqual(65, Invoke(mode, "HireCost", new[] { Enum.ToObject(roleType, 4) }));
            Assert.AreEqual(70, Get<int>(mode, "SpeedUpgradeCost"));
            IList workers = Get<IList>(mode, "Workers");
            Assert.AreEqual(2, workers.Count);
            Vector2 first = Get<Vector2>(workers[0], "Position"), second = Get<Vector2>(workers[1], "Position");
            Assert.That(Mathf.Abs(first.x - second.x), Is.GreaterThan(88.5f), "Starter workers must not overlap on spawn.");
        }

        [Test]
        public void LujanStartsWithAllSevenStationsAndRejectsEquipmentPurchases()
        {
            Type configType = RuntimeType("TowerDefenseLevelConfig");
            ScriptableObject source = Resources.Load("TowerDefense/Peregrinacion-LujanMoreno", configType) as ScriptableObject;
            Assert.IsNotNull(source);
            config = UnityEngine.Object.Instantiate(source);
            object route = Get<object>(config, "Route");
            object balance = Activator.CreateInstance(RuntimeType("StreetBalance"));
            float logicalPixelScale = Get<Rect>(route, "WorkBounds").width / 508f;
            mode = Activator.CreateInstance(RuntimeType("TowerDefenseGameMode"), new object[]
            {
                config, balance, route, logicalPixelScale, Vector2.zero, Vector2.zero,
                new int[5], new long[5], Get<int>(config, "StartingCoins")
            });

            IList stations = Get<IList>(mode, "PlacedStations");
            Assert.AreEqual(7, stations.Count, "The complete Luján kitchen is physically installed before the run starts.");
            var kinds = new int[stations.Count];
            var expected = new[] { 0, 1, 2, 3, 4, 5, 6 };
            for (int i = 0; i < stations.Count; i++)
            {
                kinds[i] = Convert.ToInt32(Get<object>(stations[i], "Kind"));
                Vector2 work = Get<Vector2>(stations[i], "WorkPosition");
                Assert.IsFalse(float.IsNaN(work.x) || float.IsInfinity(work.x) ||
                    float.IsNaN(work.y) || float.IsInfinity(work.y), "Every opening station needs a worker access point.");
                Rect bounds = Get<Rect>(stations[i], "Bounds");
                Rect workBounds = Get<Rect>(mode, "WorkBounds");
                Assert.GreaterOrEqual(bounds.xMin, workBounds.xMin);
                Assert.LessOrEqual(bounds.xMax, workBounds.xMax);
                Assert.GreaterOrEqual(bounds.yMin, workBounds.yMin);
                Assert.LessOrEqual(bounds.yMax, workBounds.yMax);
                for (int prior = 0; prior < i; prior++)
                    Assert.IsFalse(Overlaps(bounds, Get<Rect>(stations[prior], "Bounds")), "Station footprints cannot overlap.");
            }
            CollectionAssert.AreEqual(expected, kinds, "One station of each supported type starts installed.");

            Type kitchenLayoutType = RuntimeType("StreetKitchenLayout");
            object bocaLayout = Activator.CreateInstance(kitchenLayoutType,
                new object[] { new[] { 0, 1, 2, 3, 4, 5, 6 }, true });
            Rect[] bocaByKind =
            {
                Get<Rect>(bocaLayout, "NormalGrillBounds"), Get<Rect>(bocaLayout, "PremiumGrillBounds"),
                Get<Rect>(bocaLayout, "CocaBarrelBounds"), Get<Rect>(bocaLayout, "FernetTableBounds"),
                Get<Rect>(bocaLayout, "BeerBarrelBounds"), Get<Rect>(bocaLayout, "NormalTableBounds"),
                Get<Rect>(bocaLayout, "PremiumTableBounds")
            };
            float sourceMinX = bocaByKind.Min(bounds => bounds.xMin);
            float sourceMinY = bocaByKind.Min(bounds => bounds.yMin);
            float sourceMaxX = bocaByKind.Max(bounds => bounds.xMax);
            float sourceMaxY = bocaByKind.Max(bounds => bounds.yMax);
            Rect bocaEnvelope = Rect.MinMaxRect(sourceMinX, sourceMinY, sourceMaxX, sourceMaxY);
            Rect workArea = Get<Rect>(mode, "WorkBounds");
            float pixel = Get<float>(mode, "WorldUnitsPerLogicalPixel");
            float fitScale = Mathf.Min(workArea.width / pixel / bocaEnvelope.width,
                (workArea.height / pixel - 100f) / bocaEnvelope.height);
            for (int kind = 0; kind < stations.Count; kind++)
            {
                Rect actual = Get<Rect>(stations[kind], "Bounds");
                Rect sourceBounds = bocaByKind[kind];
                Vector2 expectedCenter = new Vector2(
                    workArea.center.x + (sourceBounds.center.x - bocaEnvelope.center.x) * fitScale * pixel,
                    workArea.min.y + (bocaEnvelope.yMax - sourceBounds.center.y) * fitScale * pixel);
                Vector2 expectedSize = sourceBounds.size * fitScale * pixel;
                if (kind >= 2) expectedSize.x -= 2.5f * pixel;
                Assert.That(Vector2.Distance(actual.center, expectedCenter), Is.LessThan(pixel * .2f),
                    "Station centers must follow Boca's shared kitchen layout, kind " + kind + ".");
                Assert.That(Vector2.Distance(actual.size, expectedSize), Is.LessThan(pixel * .2f),
                    "Station prefab bounds must preserve Boca proportions, kind " + kind + ".");
            }
            int[] bocaTopRow = { 5, 6, 3, 2, 4 };
            float rowBottom = Get<Rect>(stations[bocaTopRow[0]], "Bounds").yMin;
            for (int i = 0; i < bocaTopRow.Length; i++)
            {
                Rect current = Get<Rect>(stations[bocaTopRow[i]], "Bounds");
                Assert.That(Mathf.Abs(current.yMin - rowBottom), Is.LessThan(pixel * .2f),
                    "Boca's five upper stations share one service edge.");
                if (i > 0)
                    Assert.Greater(current.center.x, Get<Rect>(stations[bocaTopRow[i - 1]], "Bounds").center.x,
                        "Boca's tables and barrels keep their left-to-right order.");
            }
            Rect normalGrill = Get<Rect>(stations[0], "Bounds"), premiumGrill = Get<Rect>(stations[1], "Bounds");
            Assert.That(Mathf.Abs(normalGrill.center.y - premiumGrill.center.y), Is.LessThan(pixel * .2f),
                "Boca's two grills share one horizontal row.");
            Assert.Less(normalGrill.center.x, premiumGrill.center.x);
            Assert.Less(normalGrill.yMax, rowBottom, "The grills sit below the tables and drink stations.");
            Assert.AreEqual(Get<Vector2>(stations[2], "WorkPosition"), Get<Vector2>(stations[4], "WorkPosition"),
                "Coca and beer use the same Cocacolero access point; a single hire handles both barrels.");
            Assert.AreEqual(Get<Vector2>(stations[5], "WorkPosition"), Get<Vector2>(stations[6], "WorkPosition"),
                "The Mozo uses one shared access point for both sandwich tables.");
            var uniqueAccess = new List<(Vector2 position, int role, Rect bounds)>();
            MethodInfo stationRole = RuntimeType("TowerDefenseGameMode").GetMethod("StationWorkerRole", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo workerBoundsForRole = RuntimeType("TowerDefenseGameMode").GetMethod("WorkerBounds", PrivateInstance);
            for (int i = 0; i < stations.Count; i++)
            {
                object station = stations[i];
                int role = Convert.ToInt32(stationRole.Invoke(null, new[] { Get<object>(station, "Kind") }));
                Vector2 point = Get<Vector2>(station, "WorkPosition");
                if (uniqueAccess.Any(item => item.role == role)) continue;
                Rect worker = (Rect)workerBoundsForRole.Invoke(mode,
                    new[] { (object)point, Enum.ToObject(RuntimeType("StreetWorkerRole"), role) });
                Assert.IsTrue(Contains(Get<Rect>(mode, "WorkBounds"), worker), "Role access stays inside the play area.");
                foreach (object otherStation in stations)
                    Assert.IsFalse(Overlaps(worker, Get<Rect>(otherStation, "Bounds")), "Role access cannot collide with installed furniture.");
                foreach (var previous in uniqueAccess)
                    Assert.IsFalse(Overlaps(worker, previous.bounds), "Different role access points cannot overlap.");
                uniqueAccess.Add((point, role, worker));
            }
            Assert.AreEqual(5, uniqueAccess.Count, "The seven physical stations use five collision-free role access points.");
            Assert.AreEqual(1200, Get<int>(mode, "Coins"));
            Assert.AreEqual(0L, Get<long>(mode, "EquipmentSpendCents"), "Configured opening stations are free and do not charge the run.");
            Assert.IsFalse(Get<bool>(config, "EquipmentPlacementAllowed"));
            Invoke(mode, "Begin");
            int openingCoins = Get<int>(mode, "Coins");
            Type stationKind = RuntimeType("TowerDefenseStationKind");
            for (int kind = 0; kind < 7; kind++)
            {
                object station = Enum.ToObject(stationKind, kind);
                Assert.IsTrue((bool)Invoke(mode, "HasStation", new[] { station }));
                Assert.AreEqual(-1, Invoke(mode, "StationCost", new[] { station }), "Installed equipment has no purchasable price.");
                Vector2 location = Get<Rect>(stations[0], "Bounds").center;
                object preview = Invoke(mode, "PreviewStationPlacement", new[] { station, (object)location });
                Assert.IsFalse(Get<bool>(preview, "IsValid"), "Neither a free duplicate nor a paid extra can be placed.");
                Assert.IsFalse((bool)Invoke(mode, "TryPlaceStation", new[] { station, (object)location, null }));
            }
            Assert.AreEqual(7, Get<IList>(mode, "PlacedStations").Count);
            Assert.AreEqual(openingCoins, Get<int>(mode, "Coins"));
            Assert.AreEqual(0L, Get<long>(mode, "EquipmentSpendCents"));
            IList openingWorkers = Get<IList>(mode, "Workers");
            Assert.AreEqual(2, openingWorkers.Count, "The Boca-like Parrillero + Mozo starting roster is unchanged.");
            MethodInfo workerBounds = RuntimeType("TowerDefenseGameMode").GetMethod("WorkerBounds", PrivateInstance);
            Rect firstWorker = (Rect)workerBounds.Invoke(mode, new[]
                { Get<Vector2>(openingWorkers[0], "Position"), Get<object>(openingWorkers[0], "Role") });
            Rect secondWorker = (Rect)workerBounds.Invoke(mode, new[]
                { Get<Vector2>(openingWorkers[1], "Position"), Get<object>(openingWorkers[1], "Role") });
            Assert.IsFalse(Overlaps(firstWorker, secondWorker), "Opening workers cannot overlap at their access points.");
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
            Set(config, "equipmentPlacementAllowed", true);
            Set(config, "sourceCampaignLevelId", 9);
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
        private static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin && inner.xMax <= outer.xMax && inner.yMin >= outer.yMin && inner.yMax <= outer.yMax;
        private static bool Overlaps(Rect a, Rect b) => a.xMin < b.xMax && a.xMax > b.xMin && a.yMin < b.yMax && a.yMax > b.yMin;
    }
}
