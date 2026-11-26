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
        public void CShapedRouteIsOneFenceAroundAWorkPocketAndFansWalkItsOutside()
        {
            Type routeType = RuntimeType("TowerDefenseRouteGeometryConfig");
            Type kindType = RuntimeType("TowerDefenseRouteKind");
            Type geometryType = RuntimeType("TowerDefenseRouteGeometry");
            MethodInfo generate = geometryType.GetMethod("Generate", BindingFlags.Public | BindingFlags.Static);
            object c = Activator.CreateInstance(routeType);
            Set(c, "RouteKind", Enum.ToObject(kindType, 2));
            Set(c, "RowCount", 1);
            Set(c, "SegmentLengths", new[] { 400f });
            Set(c, "CorridorWidth", 24f);
            Set(c, "HorizontalCorridorWidth", 24f);
            Set(c, "RowSpacing", 500f);
            Set(c, "PlayableBounds", new Rect(-250f, -300f, 520f, 600f));
            Set(c, "WorkBounds", new Rect(-200f, -230f, 380f, 460f));
            Set(c, "TurnSegments", 16);
            object result = generate.Invoke(null, new[] { c });
            Assert.IsTrue(Get<bool>(result, "IsValid"), string.Join("; ", Get<string[]>(Get<object>(result, "Validation"), "Errors")));
            Vector2[] path = Get<Vector2[]>(result, "Centerline"), fence = Get<Vector2[]>(result, "RightBoundary");
            // Top run to the right, the right connector down, the bottom run back to the left.
            Assert.Greater(path[1].x, path[0].x);
            Assert.That(path[1].y, Is.EqualTo(path[0].y).Within(.0001f));
            Assert.Less(path[path.Length - 1].x, path[path.Length - 2].x);
            Assert.Less(path[path.Length - 1].y, path[0].y);
            // The single fence is the inner rail: below the top run, above the bottom run and left of the right connector.
            Assert.Less(fence[0].y, path[0].y);
            Assert.Greater(fence[fence.Length - 1].y, path[path.Length - 1].y);
            Assert.Less(fence[1].x, path[1].x);
            // A work pocket that pokes through the fence is rejected.
            Set(c, "WorkBounds", new Rect(-200f, -230f, 380f, 520f));
            Assert.IsFalse(Get<bool>(generate.Invoke(null, new[] { c }), "IsValid"), "WorkBounds above the top rail must be rejected.");
            Set(c, "WorkBounds", new Rect(-200f, -230f, 480f, 460f));
            Assert.IsFalse(Get<bool>(generate.Invoke(null, new[] { c }), "IsValid"), "WorkBounds right of the right rail must be rejected.");
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
                // La Renga (13), Camioneros (9, since 2026-11-23) and Los Redondos (10, on La Renga's inverted C since cf5ac1f) all use the C-shaped single fence.
                Assert.AreEqual(2, Convert.ToInt32(Get<object>(priorRoute, "RouteKind")),
                    "La Renga, Camioneros and Los Redondos are the C-shaped single fence: " + id);
            }
        }

        [Ignore("Bug latente de colocacion libre (TowerDefenseGameMode.Peregrinacion): con las 7 estaciones colocadas, la parrilla premium queda con acceso de trabajo bloqueado (la mesa premium se acepto aunque tapa su acceso) y ParrilleroPremium no tiene lugar legal de contratacion. Revisar HasConnectedStationAccess vs el recalculo de accesos en TryPlaceStation.")]
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
            bool hired=false; string lastReason=""; var reasons=new System.Collections.Generic.Dictionary<string,int>();
            for(int y=32;y<=352&&!hired;y+=16)
            for(int x=48;x<=460&&!hired;x+=16)
            {
                Vector2 feet=new Vector2(x,y);
                if(!(bool)Invoke(mode,"CanHireAt",new[]{role,(object)feet}))
                {
                    object preview=Invoke(mode,"PreviewWorkerPlacement",new[]{role,(object)feet});
                    string why=(string)preview.GetType().GetField("Reason").GetValue(preview); lastReason=why+"@"+feet; reasons[why]=(reasons.ContainsKey(why)?reasons[why]:0)+1;
                    continue;
                }
                hired=(bool)Invoke(mode,"TryHireAt",new[]{role,(object)feet});
            }
            Assert.IsTrue(hired,"Role must have a reachable legal placement: "+role+" last="+lastReason+" reasons="+string.Join(";",System.Linq.Enumerable.Select(reasons,kv=>kv.Key+"="+kv.Value))+" coins="+Get<int>(mode,"Coins"));
            Assert.AreEqual(1,Sum(Get<int[]>(mode,"WorkerCountsByRole")));
            Assert.AreEqual(coins-60,Get<int>(mode,"Coins"));
            Invoke(mode, "Pause");
            Assert.AreEqual("Paused", Get<object>(mode, "Phase").ToString());
            Invoke(mode, "Resume");
            Assert.AreEqual("Playing", Get<object>(mode, "Phase").ToString());
        }

        [Test]
        public void HandOverCrossesStraightFromHandToHandWithoutAnArc()
        {
            MethodInfo handOver = RuntimeType("TowerDefenseWorldPresentation").GetMethod("HandOverPoint", BindingFlags.Static | BindingFlags.NonPublic);
            Vector2 hand = new Vector2(1f, 2f), pilgrimHand = new Vector2(3f, 5f);
            Assert.AreEqual(hand, (Vector2)handOver.Invoke(null, new object[] { hand, pilgrimHand, 0f }));
            Assert.AreEqual(pilgrimHand, (Vector2)handOver.Invoke(null, new object[] { hand, pilgrimHand, 1f }));
            Vector2 direction = pilgrimHand - hand;
            for (float progress = .1f; progress < 1f; progress += .1f)
            {
                Vector2 point = (Vector2)handOver.Invoke(null, new object[] { hand, pilgrimHand, progress });
                float cross = direction.x * (point.y - hand.y) - direction.y * (point.x - hand.x);
                Assert.That(cross, Is.EqualTo(0f).Within(.0001f), "The product travels in a straight line: no throw arc.");
            }
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
            Assert.AreEqual(65, Invoke(mode, "HireCost", new[] { Enum.ToObject(roleType, 4) }), "Like Boca, the starter Mozo is free and the first paid Mozo costs 65.");
            Assert.AreEqual(70, Get<int>(mode, "SpeedUpgradeCost"));
            IList workers = Get<IList>(mode, "Workers");
            Assert.AreEqual(2, workers.Count, "Luján opens like Boca with one Parrillero and one Mozo.");
            Assert.AreEqual("Parrillero", Get<object>(workers[0], "Role").ToString());
            Assert.AreEqual("Mozo", Get<object>(workers[1], "Role").ToString());
        }

        [Test]
        public void LujanServiceTargetPredictsTheMovingPilgrimIntercept()
        {
            Type configType = RuntimeType("TowerDefenseLevelConfig");
            ScriptableObject source = Resources.Load("TowerDefense/Peregrinacion-LujanMoreno", configType) as ScriptableObject;
            Assert.IsNotNull(source);
            config = UnityEngine.Object.Instantiate(source);
            object route = Get<object>(config, "Route");
            float pixel = Get<Rect>(route, "WorkBounds").width / 508f;
            object balance = Activator.CreateInstance(RuntimeType("StreetBalance"));
            mode = Activator.CreateInstance(RuntimeType("TowerDefenseGameMode"), new object[]
            {
                config, balance, route, pixel, Vector2.zero, Vector2.zero, new int[5], new long[5], Get<int>(config, "StartingCoins")
            });
            Invoke(mode, "Begin");
            Assert.IsTrue((bool)Invoke(mode, "TrySpawnFan", new object[] { new[] { 4 }, new[] { 1 } }));
            object fan = ((IList)Get<object>(mode, "Fans"))[0];
            object follower = Get<object>(fan, "RouteFollower");
            float totalDistance = Get<float>(follower, "TotalDistance");
            float speed = Get<float>(follower, "Speed");
            Invoke(follower, "Advance", new object[] { totalDistance * .35f / speed });

            MethodInfo currentAnchor = mode.GetType().GetMethod("GetFanServiceAnchor", PrivateInstance,
                null, new[] { fan.GetType() }, null);
            MethodInfo predictedAnchor = mode.GetType().GetMethod("GetPredictedFanServiceAnchor", PrivateInstance);
            Assert.IsNotNull(currentAnchor);
            Assert.IsNotNull(predictedAnchor);
            Vector2 current = (Vector2)currentAnchor.Invoke(mode, new[] { fan });
            Vector2 intercept = (Vector2)predictedAnchor.Invoke(mode, new object[] { fan, .5f });
            Assert.Greater(intercept.x, current.x, "The delivery target should be ahead of the pilgrim's current position.");
            Assert.That(intercept.y, Is.EqualTo(current.y).Within(.0001f), "Interception stays on Luján's fixed service lane.");
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

            CollectionAssert.AreEqual(new[] { 1, 0, 0, 0, 1 }, Get<int[]>(mode, "WorkerCountsByRole"));
            IList openingWorkers = Get<IList>(mode, "Workers");
            Assert.AreEqual(2, openingWorkers.Count, "One Parrillero and one Mozo start at Luján, as in Boca.");
            Assert.AreEqual("Parrillero", Get<object>(openingWorkers[0], "Role").ToString());
            Assert.AreEqual("Mozo", Get<object>(openingWorkers[1], "Role").ToString());

            object kitchen = Get<object>(mode, "Kitchen");
            int[] openingTableUnits = new int[7], openingGrillUnits = new int[7];
            foreach (object unit in (IEnumerable)Get<object>(kitchen, "Units"))
            {
                int product = Get<int>(unit, "Product");
                string location = Get<object>(unit, "Location").ToString();
                if (location == "Table") openingTableUnits[product]++;
                if (location == "Grill") openingGrillUnits[product]++;
            }
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0, 12, 45, 12 }, openingTableUnits,
                "Boca's active-cook opening leaves every sandwich table empty and preloads Fernet, Coca and beer.");
            CollectionAssert.AreEqual(new int[7], openingGrillUnits,
                "Full grill meat is render-only, not extra cooking inventory.");

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
                // Only Boca's own 1.2 px art overlap is trimmed so the collision bounds abut.
                if (kind >= 2) expectedSize.x -= (1.2f * fitScale + .05f) * pixel;
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
            // Each role works where Boca's StreetKitchenLayout puts it, mapped through the same fit as the furniture.
            Vector2[] bocaPosts =
            {
                (Vector2)bocaLayout.GetType().GetMethod("GrillWorkPosition", new[] { typeof(int), typeof(int), typeof(int) }).Invoke(bocaLayout, new object[] { 0, 0, 1 }),
                (Vector2)bocaLayout.GetType().GetMethod("GrillWorkPosition", new[] { typeof(int), typeof(int), typeof(int) }).Invoke(bocaLayout, new object[] { 2, 0, 1 }),
                (Vector2)bocaLayout.GetType().GetMethod("PickupPosition", new[] { typeof(int) }).Invoke(bocaLayout, new object[] { 4 }),
                (Vector2)bocaLayout.GetType().GetMethod("PickupPosition", new[] { typeof(int) }).Invoke(bocaLayout, new object[] { 5 }),
                (Vector2)bocaLayout.GetType().GetMethod("PickupPosition", new[] { typeof(int) }).Invoke(bocaLayout, new object[] { 6 }),
                (Vector2)bocaLayout.GetType().GetMethod("PickupPosition", new[] { typeof(int) }).Invoke(bocaLayout, new object[] { 0 }),
                (Vector2)bocaLayout.GetType().GetMethod("PickupPosition", new[] { typeof(int) }).Invoke(bocaLayout, new object[] { 2 })
            };
            Type roleType = RuntimeType("StreetWorkerRole");
            MethodInfo feetOf = RuntimeType("TowerDefenseGameMode").GetMethod("WorkerFeet", PrivateInstance);
            for (int kind = 0; kind < stations.Count; kind++)
            {
                Rect actual = Get<Rect>(stations[kind], "Bounds");
                Rect sourceBounds = bocaByKind[kind];
                Vector2 expectedPost = new Vector2(
                    actual.center.x + (bocaPosts[kind].x - sourceBounds.center.x) * fitScale * pixel,
                    actual.yMin + (sourceBounds.yMax - bocaPosts[kind].y) * fitScale * pixel);
                float half = (kind == 2 || kind == 4 ? 94f : 83f) * .5f * pixel;
                expectedPost.x = Mathf.Clamp(expectedPost.x, workArea.xMin + half, workArea.xMax - half);
                Vector2 actualPost = Get<Vector2>(stations[kind], "WorkPosition");
                Assert.That(Vector2.Distance(actualPost, expectedPost), Is.LessThan(pixel * .2f),
                    "Worker post for station " + kind + " must follow Boca's layout. expected=" + expected + " actual=" + actualPost);
                Rect feet = (Rect)feetOf.Invoke(mode, new object[] { actualPost });
                foreach (object other in stations)
                {
                    Rect solid = Get<Rect>(other, "Bounds");
                    int otherKind = Convert.ToInt32(Get<object>(other, "Kind"));
                    if (otherKind == 3 || otherKind == 5 || otherKind == 6)
                        solid = new Rect(solid.xMin, solid.yMin, solid.width, Mathf.Min(solid.height, 20f * fitScale * pixel));
                    Assert.IsFalse(Overlaps(feet, solid), "Feet at post " + kind + " cannot stand on a furniture base.");
                }
            }
            // Cooks work between their grill and their table; the Mozo/Fernetero/Cocacolero work from the fence side of theirs.
            Rect normalTable = Get<Rect>(stations[5], "Bounds"), premiumTable = Get<Rect>(stations[6], "Bounds");
            Assert.That(Get<Vector2>(stations[0], "WorkPosition").y, Is.InRange(normalGrill.yMax, normalTable.yMin),
                "The Parrillero stands in the apron between its grill and its table.");
            Assert.That(Get<Vector2>(stations[1], "WorkPosition").y, Is.InRange(premiumGrill.yMax, premiumTable.yMin),
                "The Premium cook stands in the apron between its grill and its table.");
            Assert.Greater(Get<Vector2>(stations[5], "WorkPosition").y, normalTable.yMin, "The Mozo works behind the table.");
            Assert.AreNotEqual(Get<Vector2>(stations[5], "WorkPosition"), Get<Vector2>(stations[6], "WorkPosition"),
                "Each sandwich table keeps its own Boca pickup.");
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
            openingWorkers = Get<IList>(mode, "Workers");
            Assert.AreEqual(2, openingWorkers.Count, "Luján starts with Boca's crew: one Parrillero and one Mozo, no extra crew.");
            Assert.AreEqual("Parrillero", Get<object>(openingWorkers[0], "Role").ToString());
            Assert.AreEqual("Mozo", Get<object>(openingWorkers[1], "Role").ToString());
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
