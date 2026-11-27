using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    /// <summary>
    /// Los Redondos ID10: one continuous five-run serpentine on the left (3%..62% of the
    /// width) and four worker sectors on the right (34.5% = 1.15 x the previous 30% strip).
    /// </summary>
    public sealed class LosRedondosRightLayoutTests
    {
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;

        private GameObject cameraObject;
        private RenderTexture target;
        private UnityEngine.Object runtimeConfig;

        [TearDown]
        public void TearDown()
        {
            if (runtimeConfig != null) UnityEngine.Object.DestroyImmediate(runtimeConfig);
            if (cameraObject != null)
            {
                cameraObject.GetComponent<Camera>().targetTexture = null;
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
            if (target != null)
            {
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
            runtimeConfig = null;
            cameraObject = null;
            target = null;
        }

        [TestCase(960f)]
        [TestCase(1170f)]
        [TestCase(1200f)]
        [Ignore("Los Redondos (ID10) is now a horizontal Ferro kitchen level; this test covered its former five-run serpentine route and right-hand bays.")]
        public void FiveRunRouteAndFourRightSectorsPassTheSharedValidatorWithFullCrews(float logicalHeight)
        {
            object mode = CreateLosRedondosMode(logicalHeight, out object route, out float px);
            Assert.AreEqual(5, Read<int>(route, "RowCount"));
            object geometry = Read<object>(mode, "RouteGeometry");
            object report = RuntimeType("TowerDefenseResponsiveLayout").GetMethod("ValidateLosRedondos", PublicStatic)
                .Invoke(null, new[] { route, geometry, (object)px });
            Assert.IsTrue(Read<bool>(report, "IsValid"), report.ToString());

            // Horizontal proportions derive from one layout: route 3%..62%, workers 65.5%..100%.
            Rect envelope = Read<Rect>(route, "PlayableBounds");
            Rect work = Read<Rect>(route, "WorkBounds");
            float left = -270f * px; // camera centered on x = 0 for a 540 px wide canvas
            Assert.That((envelope.xMin - left) / px, Is.EqualTo(540f * .03f).Within(.05f));
            Assert.That((envelope.xMax - left) / px, Is.EqualTo(540f * (1f - .30f * 1.15f) - 20f).Within(.05f),
                "Route ends at ~62% (61.8%): the 20 px fence sprite of the right connectors stops at the worker strip.");
            Assert.That((work.xMin - left) / px, Is.EqualTo(540f * (1f - .30f * 1.15f)).Within(.05f));
            Assert.That((work.width + 2f * px) / px, Is.EqualTo(162f * 1.15f).Within(.05f),
                "The worker strip is 15% wider than the previous 30% (162 px) strip.");
            Assert.That(Read<float>(route, "HorizontalCorridorWidth") / px, Is.EqualTo(76.8f).Within(.01f),
                "Runs keep the Camioneros corridor so full-size fans fit.");
            Assert.That(Read<float>(route, "CorridorWidth") / px, Is.EqualTo(96f).Within(.01f));

            // Fill every sector with its maximum crew and audit stations/workers.
            SetPrivate(mode, "coins", 1000000);
            Invoke(mode, "Begin");
            Type roleType = RuntimeType("StreetWorkerRole");
            foreach (string roleName in new[] { "Parrillero", "Cocacolero" })
                for (int zone = 0; zone < 2; zone++)
                    for (int i = 0; i < 3; i++)
                        Assert.IsTrue((bool)Invoke(mode, "TryHire", Enum.Parse(roleType, roleName), zone),
                            roleName + " zone " + zone + " hire " + (i + 1));
            Assert.IsFalse((bool)Invoke(mode, "TryHire", Enum.Parse(roleType, "Parrillero"), 0),
                "Existing capacity rule: three per role and area.");
            Assert.AreEqual(12, ((ICollection)Read<object>(mode, "Workers")).Count);
            string[] issues = (string[])Invoke(mode, "AuditLosRedondosLayout");
            Assert.IsEmpty(issues, string.Join("\n", issues));

            // Every worker is to the right of the fenced route; none at old Camioneros anchors.
            foreach (object worker in (IEnumerable)Read<object>(mode, "Workers"))
            {
                Rect bounds = (Rect)Invoke(mode, "LosRedondosWorkerBounds", worker);
                Assert.Greater(bounds.xMin, envelope.xMax, "Worker must stand in the right-side work strip.");
            }
        }

        [TestCase(960f)]
        [TestCase(1200f)]
        [Ignore("Los Redondos (ID10) is now a horizontal Ferro kitchen level; this test covered its former five-run serpentine route and right-hand bays.")]
        public void EachRowSeparationIsDrawnAsOneFenceWhileCollidersKeepBothRails(float logicalHeight)
        {
            CreateLosRedondosMode(logicalHeight, out object _, out float px);
            Type generatorType = RuntimeType("TowerDefenseModularFenceGenerator");
            Type pieceType = RuntimeType("TowerDefenseFencePiece");
            var owner = new GameObject("Los Redondos single row fence test");
            // Same 3:1 proportion as fence-panel-v1: a 120 px panel is drawn 40 px thick.
            var texture = new Texture2D(300, 100, TextureFormat.RGBA32, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 300f, 100f), new Vector2(.5f, .5f), 100f);
            try
            {
                Component generator = owner.AddComponent(generatorType);
                SetPrivate(generator, "levelConfig", runtimeConfig);
                SetPrivate(generator, "fencePanelSprite", sprite);
                Invoke(generator, "SetLogicalPanelSize", px);
                Assert.IsTrue((bool)Invoke(generator, "Generate"), Read<string>(generator, "LatestError"));
                object geometry = Read<object>(generator, "LatestGeometry");
                Vector2[] left = Read<Vector2[]>(geometry, "LeftBoundary");
                Vector2[] right = Read<Vector2[]>(geometry, "RightBoundary");

                // Horizontal rail runs grouped by height, top to bottom: row 1 top, row 1 bottom,
                // row 2 top, ... row 5 bottom. Pairs (1,2), (3,4), (5,6), (7,8) face each other.
                var runs = new List<Vector3>(); // (y, xMin, xMax)
                foreach (Vector2[] rail in new[] { left, right })
                    for (int i = 1; i < rail.Length; i++)
                    {
                        if (Mathf.Abs(rail[i].y - rail[i - 1].y) > 1e-5f || Mathf.Abs(rail[i].x - rail[i - 1].x) < 1e-5f) continue;
                        float y = rail[i].y, x0 = Mathf.Min(rail[i].x, rail[i - 1].x), x1 = Mathf.Max(rail[i].x, rail[i - 1].x);
                        int index = runs.FindIndex(run => Mathf.Abs(run.x - y) < 1e-4f);
                        if (index < 0) runs.Add(new Vector3(y, x0, x1));
                        else runs[index] = new Vector3(y, Mathf.Min(runs[index].y, x0), Mathf.Max(runs[index].z, x1));
                    }
                runs.Sort((a, b) => b.x.CompareTo(a.x));
                Assert.AreEqual(10, runs.Count, "Five runs, each with a top and a bottom rail.");

                var horizontalPanels = new List<SpriteRenderer>();
                foreach (SpriteRenderer panel in owner.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    Component metadata = panel.GetComponent(pieceType);
                    if (metadata == null) continue;
                    string kind = metadata.GetType().GetProperty("Kind", PublicInstance).GetValue(metadata, null).ToString();
                    if ((kind == "Straight" || kind == "ShortStraight") && Mathf.Abs(panel.transform.eulerAngles.z) < .5f)
                        horizontalPanels.Add(panel);
                }

                float artThickness = 40f * px;
                for (int gap = 0; gap < 4; gap++)
                {
                    Vector3 upper = runs[gap * 2 + 1], lower = runs[gap * 2 + 2];
                    Assert.That(upper.x - lower.x, Is.LessThanOrEqualTo(artThickness + 1e-4f),
                        "Rows " + (gap + 1) + "-" + (gap + 2) + ": rail gap must fit inside one fence's art.");
                    float mid = (upper.x + lower.x) * .5f;
                    float xMin = float.PositiveInfinity, xMax = float.NegativeInfinity;
                    int onRails = 0, onMidline = 0;
                    foreach (SpriteRenderer panel in horizontalPanels)
                    {
                        float y = panel.transform.position.y;
                        if (Mathf.Abs(y - upper.x) < 1e-4f || Mathf.Abs(y - lower.x) < 1e-4f) onRails++;
                        if (Mathf.Abs(y - mid) >= 1e-4f) continue;
                        onMidline++;
                        xMin = Mathf.Min(xMin, panel.bounds.min.x);
                        xMax = Mathf.Max(xMax, panel.bounds.max.x);
                    }
                    Assert.AreEqual(0, onRails, "Rows " + (gap + 1) + "-" + (gap + 2) + " must not draw a doubled fence.");
                    Assert.Greater(onMidline, 0, "Rows " + (gap + 1) + "-" + (gap + 2) + " keep one separating fence.");
                    Assert.That(xMin, Is.EqualTo(Mathf.Min(upper.y, lower.y)).Within(1e-3f), "Single fence starts at the wall.");
                    Assert.That(xMax, Is.EqualTo(Mathf.Max(upper.z, lower.z)).Within(1e-3f), "Single fence reaches the U-turn tip.");
                }

                int outerTop = 0, outerBottom = 0, terminals = 0;
                foreach (SpriteRenderer panel in horizontalPanels)
                {
                    if (Mathf.Abs(panel.transform.position.y - runs[0].x) < 1e-4f) outerTop++;
                    if (Mathf.Abs(panel.transform.position.y - runs[9].x) < 1e-4f) outerBottom++;
                }
                foreach (Transform child in owner.GetComponentsInChildren<Transform>(true))
                    if (child.name.Contains("terminal")) terminals++;
                Assert.Greater(outerTop, 0, "Row 1 keeps its outer fence.");
                Assert.Greater(outerBottom, 0, "Row 5 keeps its outer fence.");
                Assert.AreEqual(4, terminals, "Both rails keep their entry and exit terminals.");

                // Navigation and collisions are unchanged: both colliders follow the full rails.
                EdgeCollider2D[] colliders = owner.GetComponentsInChildren<EdgeCollider2D>(true);
                Assert.AreEqual(2, colliders.Length);
                foreach (EdgeCollider2D collider in colliders)
                    Assert.AreEqual(collider.name.Contains("rail 0") ? left.Length : right.Length, collider.pointCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        [Ignore("Los Redondos (ID10) is now a horizontal Ferro kitchen level; this test covered its former five-run serpentine route and right-hand bays.")]
        public void SectorsServeTheirAdjacentRunsInTopToBottomOrder()
        {
            object mode = CreateLosRedondosMode(960f, out object route, out float px);
            Type roleType = RuntimeType("StreetWorkerRole");
            object parrillero = Enum.Parse(roleType, "Parrillero");
            object cocacolero = Enum.Parse(roleType, "Cocacolero");
            Rect[] sectors =
            {
                (Rect)Invoke(mode, "RightBayBounds", parrillero, 0),
                (Rect)Invoke(mode, "RightBayBounds", cocacolero, 0),
                (Rect)Invoke(mode, "RightBayBounds", parrillero, 1),
                (Rect)Invoke(mode, "RightBayBounds", cocacolero, 1)
            };
            for (int i = 1; i < sectors.Length; i++)
                Assert.Less(sectors[i].center.y, sectors[i - 1].center.y, "Sectors stack P0, C0, P1, C1 from the top.");

            Vector2[] waypoints = (Vector2[])mode.GetType().GetField("fanRouteWaypoints", PrivateInstance).GetValue(mode);
            object geometry = Read<object>(mode, "RouteGeometry");
            Vector2[] centerline = Read<Vector2[]>(geometry, "Centerline");
            int stride = Read<int>(route, "TurnSegments") + 1;
            object[] roles = { parrillero, cocacolero, parrillero, cocacolero };
            int[] zones = { 0, 0, 1, 1 };
            for (int sector = 0; sector < 4; sector++)
            {
                // Middle of the sector's connector (run k -> run k+1) is served; the far end of the route is not.
                Vector2 connectorMiddle = centerline[sector * stride + 1 + stride / 2];
                Assert.IsTrue(IsServed(mode, waypoints, connectorMiddle, roles[sector], zones[sector]),
                    "Sector " + sector + " serves its own connector.");
                Vector2 unrelated = sector < 2 ? centerline[centerline.Length - 1] : centerline[0];
                Assert.IsFalse(IsServed(mode, waypoints, unrelated, roles[sector], zones[sector]),
                    "Sector " + sector + " does not serve unrelated runs.");
            }
        }

        [Test]
        [Ignore("Los Redondos (ID10) is now a horizontal Ferro kitchen level; this test covered its former five-run serpentine route and right-hand bays.")]
        public void ValidatorReportsTheSegmentAndCoordinatesOfConcreteDefects()
        {
            object mode = CreateLosRedondosMode(960f, out object route, out float px);
            Type geometryType = RuntimeType("TowerDefenseRouteGeometry");
            MethodInfo validate = RuntimeType("TowerDefenseResponsiveLayout").GetMethod("ValidateLosRedondos", PublicStatic);

            // Too narrow runs for the fans' bodies.
            object narrow = Clone(route);
            SetField(narrow, "HorizontalCorridorWidth", 60f * px);
            object narrowGeometry = geometryType.GetMethod("Generate", PublicStatic).Invoke(null, new[] { narrow });
            string narrowReport = validate.Invoke(null, new[] { narrow, narrowGeometry, (object)px }).ToString();
            StringAssert.Contains("ancho útil", narrowReport);
            StringAssert.Contains("tramo 1", narrowReport);

            // Work column dragged over the route.
            object invading = Clone(route);
            Rect work = Read<Rect>(invading, "WorkBounds");
            work.x -= 120f * px;
            SetField(invading, "WorkBounds", work);
            object invadingGeometry = geometryType.GetMethod("Generate", PublicStatic).Invoke(null, new[] { invading });
            StringAssert.Contains("zona de trabajo",
                validate.Invoke(null, new[] { invading, invadingGeometry, (object)px }).ToString());

            // Rows too close: rails cross, path runs into fences.
            object crowded = Clone(route);
            SetField(crowded, "RowSpacing", 70f * px);
            SetField(crowded, "CorridorWidth", 96f * px);
            object crowdedGeometry = geometryType.GetMethod("Generate", PublicStatic).Invoke(null, new[] { crowded });
            string crowdedReport = validate.Invoke(null, new[] { crowded, crowdedGeometry, (object)px }).ToString();
            Assert.That(crowdedReport, Does.Contain("intersección").Or.Contain("bloqueo").Or.Contain("generador"));
            StringAssert.Contains("@ (", crowdedReport, "Issues carry concrete coordinates.");
        }

        [Test]
        [Ignore("Los Redondos (ID10) is now a horizontal Ferro kitchen level; this test covered its former five-run serpentine route and right-hand bays.")]
        public void SeveralFansWalkTheWholeRouteThroughAllFourTurnsInsideTheRails()
        {
            object mode = CreateLosRedondosMode(960f, out object route, out float px);
            object geometry = Read<object>(mode, "RouteGeometry");
            Vector2[] centerline = Read<Vector2[]>(geometry, "Centerline");
            Vector2[] leftRail = Read<Vector2[]>(geometry, "LeftBoundary");
            Vector2[] rightRail = Read<Vector2[]>(geometry, "RightBoundary");
            float halfRun = Read<float>(route, "HorizontalCorridorWidth") * .5f;
            int stride = Read<int>(route, "TurnSegments") + 1;
            var turnMiddles = new float[4];
            for (int turn = 0; turn < 4; turn++) turnMiddles[turn] = centerline[turn * stride + 1 + stride / 2].y;
            float lastRunY = centerline[centerline.Length - 1].y;
            float lastRunMiddleX = (centerline[centerline.Length - 1].x + centerline[centerline.Length - 2].x) * .5f;

            Invoke(mode, "Begin");
            var visited = new Dictionary<int, bool[]>();
            int maximumSimultaneous = 0, completedRoutes = 0;
            IList fans = (IList)Read<object>(mode, "Fans");
            for (int step = 0; step < 40000 && completedRoutes == 0; step++)
            {
                Invoke(mode, "Step", .05f);
                fans = (IList)Read<object>(mode, "Fans");
                maximumSimultaneous = Math.Max(maximumSimultaneous, fans.Count);
                foreach (object fan in fans)
                {
                    int id = Read<int>(fan, "Id");
                    object follower = Read<object>(fan, "RouteFollower");
                    Vector2 position = Read<Vector2>(follower, "Position");
                    float clearance = Mathf.Min(Distance(position, leftRail), Distance(position, rightRail));
                    Assert.GreaterOrEqual(clearance, halfRun - .01f * px,
                        "Fan " + id + " touched a fence at " + position + " (step " + step + ").");
                    if (!visited.TryGetValue(id, out bool[] turns)) visited[id] = turns = new bool[4];
                    for (int turn = 0; turn < 4; turn++)
                        if (Mathf.Abs(position.y - turnMiddles[turn]) < 2f * px &&
                            Mathf.Abs(Mathf.Abs(position.x - centerline[turn * stride + 1].x)) < .5f * px)
                            turns[turn] = true;
                    if (Mathf.Abs(position.y - lastRunY) < .5f * px && position.x > lastRunMiddleX &&
                        Array.TrueForAll(turns, value => value)) completedRoutes++;
                }
            }
            Assert.GreaterOrEqual(maximumSimultaneous, 5, "Several fans must share the route simultaneously.");
            Assert.Greater(completedRoutes, 0, "At least one fan crossed all four turns and reached the exit.");
        }

        [Test]
        [Ignore("Camioneros (ID9) is now a horizontal Nueva Chicago kitchen level: it no longer has two shared-role zones.")]
        public void IdNineKeepsTheExistingTwoZoneSharedRoleAnchors()
        {
            object config = Resources.Load("TowerDefense/Camioneros-PlazaDeMayo");
            Assert.IsNotNull(config);
            Assert.AreEqual(9, Read<int>(config, "SourceCampaignLevelId"));
            object route = Read<object>(config, "Route");
            object geometry = RuntimeType("TowerDefenseRouteGeometry").GetMethod("Generate", PublicStatic)
                .Invoke(null, new[] { route });
            Vector2 zone0 = (Vector2)RuntimeType("TowerDefenseLevelConfig")
                .GetMethod("WorkZonePosition", PublicInstance).Invoke(config, new[] { geometry, (object)0 });
            Vector2 zone1 = (Vector2)RuntimeType("TowerDefenseLevelConfig")
                .GetMethod("WorkZonePosition", PublicInstance).Invoke(config, new[] { geometry, (object)1 });
            object balance = Activator.CreateInstance(RuntimeType("StreetBalance"));
            object mode = Activator.CreateInstance(RuntimeType("TowerDefenseGameMode"),
                new[] { config, balance, route, (object)1f, zone0, zone1 });
            Type roleType = RuntimeType("StreetWorkerRole");
            object parrillero = Enum.Parse(roleType, "Parrillero");
            object cocacolero = Enum.Parse(roleType, "Cocacolero");
            Vector2 p0 = (Vector2)Invoke(mode, "StationPosition", parrillero, 0);
            Vector2 c0 = (Vector2)Invoke(mode, "StationPosition", cocacolero, 0);
            Vector2 p1 = (Vector2)Invoke(mode, "StationPosition", parrillero, 1);
            Assert.AreNotEqual(p0.x, c0.x, "ID9 keeps its pre-existing role-specific service anchors.");
            Assert.AreNotEqual(p0, p1, "ID9 keeps its two existing service zones, not ID10's four sectors.");
            Assert.AreEqual(Rect.zero, (Rect)Invoke(mode, "RightBayBounds", parrillero, 0), "ID9 has no right-side sectors.");
            Assert.IsEmpty((string[])Invoke(mode, "AuditLosRedondosLayout"), "The ID10 audit does not apply to ID9.");
        }

        // 2026-11-26 (a pedido): speed x1.10 rising 0.10 every 10 fans (24 tiers up to x3.40), orders of up to 5 units and up to 4 different products.
        [Test]
        public void LosRedondosRampStartsAtOnePointOneAndEndsWithFourProductOrdersOfFourOrFiveUnits()
        {
            object config = Resources.Load("TowerDefense/LosRedondos-Tandil");
            MethodInfo tierFor = config.GetType().GetMethod("DifficultyTierForFan");
            for (int tierIndex = 0; tierIndex < 24; tierIndex++)
            {
                object tier = tierFor.Invoke(config, new object[] { tierIndex * 10 + 9 });
                Assert.AreEqual(tierIndex * 10, tier.GetType().GetField("FromFan").GetValue(tier), "tier " + tierIndex);
                Assert.AreEqual(1.1f + .1f * tierIndex, (float)tier.GetType().GetField("SpeedMultiplier").GetValue(tier), .0001f, "speed of tier " + tierIndex);
                int quantities = 0;
                foreach (string field in new[] { "Quantity2Percent", "Quantity3Percent", "Quantity4Percent", "Quantity5Percent" })
                    quantities += (int)tier.GetType().GetField(field).GetValue(tier);
                Assert.LessOrEqual(quantities, 100);
            }
            object mode = CreateLosRedondosMode(1200f, out object route, out float px);
            SetPrivate(mode, "spawnedFans", 230);
            Invoke(mode, "SpawnNextFan");
            IList fans = (IList)mode.GetType().GetProperty("Fans").GetValue(mode);
            Assert.AreEqual(1, fans.Count);
            IList order = (IList)fans[0].GetType().GetProperty("Order").GetValue(fans[0]);
            Assert.AreEqual(4, order.Count, "the last tier always asks for four different products");
            var seen = new HashSet<int>();
            foreach (object line in order)
            {
                int product = (int)line.GetType().GetProperty("Product").GetValue(line);
                int remaining = (int)line.GetType().GetProperty("Remaining").GetValue(line);
                Assert.IsTrue(seen.Add(product), "products are different");
                Assert.That(remaining, Is.InRange(4, 5), "the last tier asks for 4 or 5 units of each");
            }
        }

        private object CreateLosRedondosMode(float logicalHeight, out object route, out float px)
        {
            object config = Resources.Load("TowerDefense/LosRedondos-Tandil");
            Assert.IsNotNull(config);
            Assert.AreEqual(10, Read<int>(config, "SourceCampaignLevelId"));
            cameraObject = new GameObject("Los Redondos layout test camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            target = new RenderTexture(540, Mathf.RoundToInt(logicalHeight), 16);
            camera.targetTexture = target;
            object[] args = { config, camera, new Rect(0f, 0f, 540f, logicalHeight), 540f, logicalHeight,
                null, null, 0f, Vector2.zero, Vector2.zero, null };
            MethodInfo tryCreate = RuntimeType("TowerDefenseResponsiveLayout").GetMethod("TryCreate", PublicStatic);
            Assert.IsTrue((bool)tryCreate.Invoke(null, args), (string)args[10]);
            runtimeConfig = (UnityEngine.Object)args[5];
            route = args[6];
            px = (float)args[7];
            object balance = Activator.CreateInstance(RuntimeType("StreetBalance"));
            return Activator.CreateInstance(RuntimeType("TowerDefenseGameMode"),
                new[] { runtimeConfig, balance, route, (object)px, args[8], args[9] });
        }

        private static bool IsServed(object mode, Vector2[] waypoints, Vector2 point, object role, int zone)
        {
            Type followerType = RuntimeType("TowerDefenseFanRouteFollower");
            ConstructorInfo followerConstructor = followerType.GetConstructor(new[] { typeof(int), typeof(IReadOnlyList<Vector2>), typeof(float) });
            ConstructorInfo fanConstructor = RuntimeType("TowerDefenseFanState").GetConstructor(PrivateInstance, null,
                new[] { typeof(int), followerType, typeof(int[]), typeof(int[]) }, null);
            float progress = 0f;
            for (int i = 1; i < waypoints.Length; i++)
            {
                Vector2 a = waypoints[i - 1], b = waypoints[i];
                float length = Vector2.Distance(a, b);
                Vector2 ab = b - a;
                float t = length <= 0f ? 0f : Mathf.Clamp01(Vector2.Dot(point - a, ab) / (length * length));
                if (Vector2.Distance(a + ab * t, point) < 1e-4f) { progress += length * t; break; }
                progress += length;
            }
            object follower = followerConstructor.Invoke(new object[] { 1, waypoints, 1f });
            followerType.GetMethod("Advance", PublicInstance).Invoke(follower, new object[] { progress });
            object fan = fanConstructor.Invoke(new object[] { 1, follower, new[] { 0 }, new[] { 1 } });
            return (bool)mode.GetType().GetMethod("IsAdjacentToRoleArea", PrivateInstance).Invoke(mode, new[] { fan, role, (object)zone });
        }

        private static float Distance(Vector2 p, Vector2[] polyline)
        {
            float best = float.PositiveInfinity;
            for (int i = 1; i < polyline.Length; i++)
            {
                Vector2 a = polyline[i - 1], ab = polyline[i] - a;
                float t = ab.sqrMagnitude < 1e-12f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
            }
            return best;
        }

        private static object Clone(object route)
        {
            object copy = Activator.CreateInstance(route.GetType());
            foreach (FieldInfo field in route.GetType().GetFields(PublicInstance))
            {
                object value = field.GetValue(route);
                field.SetValue(copy, value is float[] array ? array.Clone() : value);
            }
            return copy;
        }

        private static Type RuntimeType(string name) => Type.GetType("HayChoriYPaty." + name + ", Assembly-CSharp", true);

        private static object Invoke(object target, string name, params object[] args)
        {
            foreach (MethodInfo method in target.GetType().GetMethods(PublicInstance | PrivateInstance))
                if (method.Name == name && method.GetParameters().Length == args.Length) return method.Invoke(target, args);
            Assert.Fail("Missing method " + name + " on " + target.GetType().Name);
            return null;
        }

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, PublicInstance).SetValue(target, value);

        private static void SetPrivate(object target, string name, object value) =>
            target.GetType().GetField(name, PrivateInstance).SetValue(target, value);

        private static T Read<T>(object target, string name)
        {
            PropertyInfo property = target.GetType().GetProperty(name, PublicInstance);
            if (property != null) return (T)property.GetValue(target, null);
            FieldInfo field = target.GetType().GetField(name, PublicInstance | PrivateInstance);
            Assert.IsNotNull(field, "Expected " + name + " on " + target.GetType().Name + ".");
            return (T)field.GetValue(target);
        }
    }
}
