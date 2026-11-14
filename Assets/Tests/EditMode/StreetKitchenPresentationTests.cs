using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    /// <summary>Focused contracts for the native prefab/resources and responsive route projection.</summary>
    public sealed class StreetKitchenPresentationTests
    {
        private static Type Runtime(string name) => Type.GetType("HayChoriYPaty." + name + ", Assembly-CSharp", true);

        private static object Simulation(int level)
        {
            object balance = Activator.CreateInstance(Runtime("StreetBalance"));
            return Activator.CreateInstance(Runtime("StreetSimulation"), new[] { balance, (object)level, 5f, 0, 1, 0 });
        }

        private static object Layout(object simulation) => Get(simulation, "KitchenLayout");
        private static object FullLayout() => Layout(Simulation(4));

        private static int ExpectedInitialSandwichStock(object sim, int product)
        {
            if (product < 0 || product >= 4 || (bool)Get(sim, "UsesActiveCooks") || !(bool)Call(sim, "IsProductAvailable", product)) return 0;
            int first = product < 2 ? 0 : 2, second = first + 1;
            bool firstAvailable = (bool)Call(sim, "IsProductAvailable", first);
            bool secondAvailable = (bool)Call(sim, "IsProductAvailable", second);
            int count = (firstAvailable ? 1 : 0) + (secondAvailable ? 1 : 0);
            if (count == 0) return 0;
            object balance = Get(Get(sim, "Kitchen"), "Balance");
            string capacityField = first == 0 ? "normalTableCapacity" : "premiumTableCapacity";
            int configured = (int)balance.GetType().GetField(capacityField, BindingFlags.Public | BindingFlags.Instance).GetValue(balance);
            int each = configured / count, remainder = configured % count;
            int initial = each + (product == first && remainder > 0 ? 1 :
                product == second && firstAvailable && remainder > 1 ? 1 : 0);
            int physicalCapacity = (int)Call(Get(sim, "Kitchen"), "TableCapacity", product);
            return Mathf.Min(initial, physicalCapacity);
        }

        private static object Call(object target, string name, params object[] args)
        {
            MethodInfo method = null;
            foreach (MethodInfo candidate in target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (candidate.Name != name || candidate.GetParameters().Length != args.Length) continue;
                if (method != null) throw new AmbiguousMatchException(target.GetType().Name + "." + name);
                method = candidate;
            }
            if (method == null) throw new MissingMethodException(target.GetType().Name + "." + name);
            return method.Invoke(target, args);
        }

        private static object Get(object target, string name) => target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance).GetValue(target);
        private static void Set(object target, string name, object value) => target.GetType()
            .GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .GetSetMethod(true).Invoke(target, new[] { value });
        private static Rect RectProperty(object layout, string name) => (Rect)Runtime("StreetKitchenLayout").GetProperty(name, BindingFlags.Public | BindingFlags.Instance).GetValue(layout);
        private static float LayoutConstant(string name) => (float)Runtime("StreetKitchenLayout").GetField(name, BindingFlags.Public | BindingFlags.Static).GetRawConstantValue();
        private static float SceneWidth() => (float)Runtime("StreetSceneLayout").GetField("Width", BindingFlags.Public | BindingFlags.Static).GetValue(null);
        private static float WorkerServiceY() => (float)Runtime("StreetSceneLayout").GetField("WorkerServiceY", BindingFlags.Public | BindingFlags.Static).GetValue(null);
        private static float CounterHandoffY()
        {
            float frontQueueY = (float)Runtime("StreetSimulation").GetField("FrontQueueY", BindingFlags.Public | BindingFlags.Static).GetRawConstantValue();
            return Mathf.Lerp(frontQueueY, WorkerServiceY(), .5f);
        }

        [Test]
        public void ParrillerosUseSeparateStationsAlongEachSharedGrill()
        {
            object sim = Simulation(5), layout = Layout(sim);
            foreach (int product in new[] { 0, 2 })
            foreach (int count in new[] { 1, 2, 3, 4 })
            {
                var stations = new HashSet<Vector2>();
                Vector2 first = Vector2.zero;
                for (int index = 0; index < count; index++)
                {
                    Vector2 station = (Vector2)Call(layout, "GrillWorkPosition", product, index, count);
                    if (index == 0) first = station;
                    Assert.IsTrue(stations.Add(station), "Cooker " + index + " must not share a station on grill " + product + ".");
                    Assert.AreEqual(first.y, station.y, .001f, "Every team size remains in one horizontal row.");
                }
                Assert.AreEqual(2f, LayoutConstant("GrillWorkClearance"), .001f, "Cooks stand closer to the grill rim.");
                if (count == 2)
                {
                    Vector2 right = (Vector2)Call(layout, "GrillWorkPosition", product, 1, count);
                    Assert.Greater(Vector2.Distance(first, right), 120f,
                        "The first two cooks should be visibly separated, not overlapping at the same grill slot.");
                }
            }
        }

        [TestCase(5)] [TestCase(6)] [TestCase(8)]
        public void CookWaitersHaveTheirOwnVisibleQueueAndSafeRoutes(int teamSize)
        {
            object sim = Simulation(5), layout = Layout(sim);
            int queued = teamSize - 4;
            foreach (int product in new[] { 0, 1, 2, 3 })
            {
                var points = new HashSet<Vector2>();
                Vector2 deposit = (Vector2)Call(layout, "DepositPosition", product);
                Rect grill = (Rect)Call(layout, "GrillBoundsForProduct", product);
                for (int index = 0; index < queued; index++)
                {
                    Vector2 waiting = (Vector2)Call(layout, "CookQueuePosition", product, index, queued);
                    Assert.IsTrue(points.Add(waiting), "Every queued worker needs its own staging position.");
                    Assert.IsFalse(grill.Contains(waiting), "The wait zone must not occupy the working grill.");
                    Assert.GreaterOrEqual(waiting.x - 30f, grill.xMin - .01f,
                        "The entire queued cook must remain inside its own grill's horizontal span.");
                    Assert.LessOrEqual(waiting.x + 30f, grill.xMax + .01f,
                        "A waiting cook must not spill into the other specialty's grill.");
                    if (product < 2 && queued == 2)
                    {
                        Rect table = (Rect)Call(layout, "BoundsForProduct", product);
                        Assert.GreaterOrEqual(waiting.x - 30f, table.xMin - .01f,
                            "Both waiting normal cooks must stay below their own table.");
                        Assert.LessOrEqual(waiting.x + 30f, table.xMax + .01f,
                            "The second waiting normal cook must not appear under the Premium table.");
                    }
                    Rect otherGrill = (Rect)Call(layout, "GrillBoundsForProduct", product < 2 ? 2 : 0);
                    Assert.IsFalse(waiting.x >= otherGrill.xMin && waiting.x <= otherGrill.xMax,
                        "Normal and Premium waiting zones must never swap or mix.");
                    Assert.IsNotNull(Call(layout, "FindShortestSafeRoute", deposit, waiting), "Table→queue route must be safe.");
                    Assert.IsNotNull(Call(layout, "FindShortestSafeRoute", waiting,
                        Call(layout, "GrillWorkPosition", product, 0, 4)), "Queue→grill route must be safe.");
                }
            }
        }

        [TestCase(960f)] [TestCase(1110f)] [TestCase(1200f)]
        public void ActiveBackdropLeavesStreetVisibleAndUsesExistingCleanCounterEdge(float height)
        {
            Type scene = Runtime("StreetSceneLayout");
            float scale = height / 960f;
            Rect band = (Rect)scene.GetMethod("ActiveMuralBand").Invoke(null, new object[] { height, scale, 105f });
            float counter = (float)scene.GetMethod("CounterTop").Invoke(null, new object[] { height });
            Assert.GreaterOrEqual(counter - band.yMax, 90f * scale - .01f, "A club mural must not cover the public street.");
            Rect source = (Rect)scene.GetField("CleanCounterSideSource").GetValue(null);
            Rect oldTable = (Rect)scene.GetField("BakedSideTableSource").GetValue(null);
            Assert.AreEqual(source.size, oldTable.size, "Reuse native pixels without scaling/distortion.");
            Rect replacement = (Rect)scene.GetMethod("ProjectSourceRect").Invoke(null, new object[] { oldTable, height });
            Assert.AreEqual(counter, replacement.y, .01f);
            Assert.LessOrEqual(source.xMax, oldTable.xMin, "The replacement source cannot include the unwanted side table.");
        }

        [TestCase(960f)] [TestCase(1110f)] [TestCase(1200f)]
        public void ActiveFloorContinuationSamplesPavementInsteadOfCreamFooterAcrossAspectRatios(float height)
        {
            Type scene = Runtime("StreetSceneLayout");
            float verticalScale = height / 960f;
            Vector2 textureSize = new Vector2(940f, 1673f);
            Rect source = (Rect)scene.GetMethod("PavementContinuationSource").Invoke(null,
                new object[] { textureSize });
            float tileHeight = (float)scene.GetMethod("PavementContinuationTileHeight").Invoke(null,
                new object[] { height, textureSize });
            float footerExtension = 70f * verticalScale;

            Assert.AreEqual(0f, source.x, .001f);
            Assert.AreEqual(935f, source.y, .01f, "Begin inside the clean tile interior, below the upper grout joint.");
            Assert.AreEqual(120f, source.height, .01f);
            Assert.Less(source.yMax, textureSize.y);
            Assert.LessOrEqual(source.yMax, 1055f, "Keep the source below the next grout joint and its filtering footprint.");
            Assert.AreEqual(footerExtension, tileHeight, .01f,
                "The clean grout-free crop exactly spans the active-cook footer extension without repeating a joint.");
        }

        [TestCase(720f)] [TestCase(1080f)] [TestCase(1440f)]
        public void PavementContinuationOverlapCoversCutAtResponsiveWidths(float screenWidth)
        {
            Type scene = Runtime("StreetSceneLayout");
            float overlap = (float)scene.GetMethod("PavementContinuationOverlap").Invoke(null,
                new object[] { screenWidth });
            Assert.GreaterOrEqual(overlap, 4f,
                "The seam bleed has a four-logical-unit minimum, even when Editor display pixels exceed Game View pixels.");
            Assert.GreaterOrEqual(overlap * screenWidth / 540f, 4f,
                "The seam bleed must cover at least four physical pixels at compact, standard, and high resolutions.");
        }

        [TestCase(5)] [TestCase(10)]
        public void GrillContactPresentationPositionIsSharedByNativeAndFallbackWorkers(int level)
        {
            object sim = Simulation(level);
            Call(sim, "StartRound");
            var workers = (System.Collections.IList)Get(sim, "Workers");
            object worker = workers[0];
            Assert.AreEqual("Parrillero", Get(worker, "Role").ToString());
            object layout = Layout(sim);
            Vector2 post = (Vector2)Call(layout, "GrillWorkPosition", 0, 0, 1);
            Set(worker, "Position", post);
            Set(worker, "State", Enum.Parse(Runtime("StreetWorkerState"), "Cooking"));

            MethodInfo presentationPosition = Runtime("StreetWorkerProjection").GetMethod(
                "PresentationPosition", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(presentationPosition);
            Vector2 visual = (Vector2)presentationPosition.Invoke(null, new[] { sim, worker });
            float expectedY = post.y + (float)Call(layout, "GrillVisibleTopInset", 0)
                + (float)Call(layout, "GrillVisualContactOffset", 0);

            Assert.AreEqual(post.x, visual.x, .001f, "Grill contact changes depth only, not worker stations.");
            Assert.AreEqual(expectedY, visual.y, .001f,
                "The native camera and StreetView's fallback must use the same fitted-art foot anchor.");
            Assert.Greater(visual.y, post.y, "The rendered feet must close the apparent gap to the grill rim.");
            Assert.AreEqual(post, (Vector2)Get(worker, "Position"),
                "A visual correction must not move the route/obstacle-safe simulation post.");
        }

        [Test]
        public void AmbientGrillSmokeFadesGentlyAndRisesThroughAThreeSecondCycle()
        {
            Type renderer = Runtime("StreetKitchenRenderer");
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
            MethodInfo phaseAt = renderer.GetMethod("AmbientSmokePhaseAt", flags);
            MethodInfo alphaAt = renderer.GetMethod("AmbientSmokeAlphaAt", flags);
            MethodInfo positionAt = renderer.GetMethod("AmbientSmokePositionAt", flags);
            Assert.NotNull(phaseAt); Assert.NotNull(alphaAt); Assert.NotNull(positionAt);

            Assert.AreEqual(0f, (float)phaseAt.Invoke(null, new object[] { 0f, 0f, 0f }), .0001f);
            Assert.AreEqual(0f, (float)phaseAt.Invoke(null, new object[] { 3.125f, 0f, 0f }), .001f);
            Assert.AreEqual(0f, (float)alphaAt.Invoke(null, new object[] { 0f }), .0001f);
            Assert.AreEqual(.18f, (float)alphaAt.Invoke(null, new object[] { .5f }), .0001f);

            Rect grill = new Rect(12f, 20f, 100f, 80f);
            Vector2 lower = (Vector2)positionAt.Invoke(null, new object[] { grill, .17f, .1f });
            Vector2 upper = (Vector2)positionAt.Invoke(null, new object[] { grill, .17f, .8f });
            Assert.AreEqual(29f, lower.x, .001f);
            Assert.Less(upper.y, lower.y, "Each faint puff rises as its phase advances.");
            Assert.AreEqual(new Vector2(12f, 26f),
                (Vector2)renderer.GetField("AmbientSmokeSize", flags).GetValue(null));
        }

        [Test]
        public void AllElevenCatalogsExposeOnlyRealStockForTheirSevenProductProfile()
        {
            for (int level = 0; level < 11; level++)
            {
                object sim = Simulation(level);
                object kitchen = Get(sim, "Kitchen");
                var units = (System.Collections.IEnumerable)Get(kitchen, "Units");
                var ids = new HashSet<int>();
                int[] capacities = new int[7], grills = new int[7];
                for (int p = 0; p < 7; p++)
                {
                    capacities[p] = (int)Call(kitchen, "TableCapacity", p);
                    grills[p] = (int)Call(kitchen, "GrillCapacity", p);
                    bool available = (bool)Call(sim, "IsProductAvailable", p);
                    Assert.AreEqual(available, capacities[p] > 0 || grills[p] > 0, "L" + level + " product " + p);
                    if (p < 4) Assert.AreEqual(available, grills[p] > 0, "L" + level + " grill product " + p);
                }
                foreach (object unit in units)
                {
                    int id = (int)Get(unit, "Id"), product = (int)Get(unit, "Product");
                    Assert.IsTrue(ids.Add(id), "Duplicate food unit ID in level " + level);
                    Assert.That(product, Is.InRange(0, 6));
                    Assert.IsTrue((bool)Call(sim, "IsProductAvailable", product), "Locked product in level " + level);
                }
                for (int p = 0; p < 7; p++)
                {
                    int listedTable = 0, listedGrill = 0;
                    foreach (object unit in units)
                    {
                        if ((int)Get(unit, "Product") != p) continue;
                        string location = Get(unit, "Location").ToString();
                        if (location == "Table") listedTable++;
                        if (location == "Grill") listedGrill++;
                    }
                    int expectedInitialTable = p < 4 ? ExpectedInitialSandwichStock(sim, p) : capacities[p];
                    Assert.AreEqual(expectedInitialTable, listedTable, "L" + level + " initial table count for product " + p);
                    Assert.AreEqual(level >= 5 ? 0 : grills[p], listedGrill, "L" + level + " initial grill count for product " + p);
                }
                Assert.AreEqual(18, grills[0] + grills[1], "Normal grill has 18 real meat units when available.");
                bool bondiola = (bool)Call(sim, "IsProductAvailable", 2);
                bool vacio = (bool)Call(sim, "IsProductAvailable", 3);
                if (bondiola && vacio)
                {
                    Assert.AreEqual(6, grills[2], "Whole Bondiola uses its authored 3x2 grid.");
                    Assert.AreEqual(4, grills[3], "Whole Vacío fills the remaining compact 2x2 grid.");
                    Assert.AreEqual(10, grills[2] + grills[3]);
                }
                else if (bondiola) Assert.AreEqual(6, grills[2], "Whole Bondiola uses its authored 3x2 grid.");
                else if (vacio) Assert.AreEqual(7, grills[3], "Vacío alone uses the full premium grill.");
            }
        }

        [Test]
        public void GrillMeatVisualBoundsStayInsideTheGrillForEveryLevel()
        {
            for (int level = 0; level < 11; level++)
            {
                object sim = Simulation(level), layout = Layout(sim), kitchen = Get(sim, "Kitchen");
                for (int product = 0; product < 4; product++)
                {
                    int capacity = (int)Call(kitchen, "GrillCapacity", product);
                    if (capacity <= 0) continue;
                    int companion = product % 2 == 0 ? product + 1 : product - 1;
                    bool companionAvailable = (bool)Call(sim, "IsProductAvailable", companion);
                    Rect grill = RectProperty(layout, product < 2 ? "NormalGrillBounds" : "PremiumGrillBounds");
                    Vector2 size = (Vector2)Call(layout, "GrillMeatSize", product, companionAvailable);
                    for (int slot = 0; slot < capacity; slot++)
                    {
                        Vector2 position = (Vector2)Call(layout, "GrillSlotPosition", product, slot, companionAvailable, capacity);
                        Rect meat = new Rect(position - size * .5f, size);
                        string context = "Level " + level + ", product " + product + ", slot " + slot;
                        float grateTop = grill.y + grill.height * .115f;
                        float grateBottom = grill.y + grill.height * .46f;
                        Assert.GreaterOrEqual(meat.yMin, grateTop - .02f, context + " behind the grate");
                        Assert.LessOrEqual(meat.yMax, grateBottom + .02f, context + " past the front grate edge");
                        float topDepth = Mathf.Clamp01((meat.yMin - grateTop) / (grateBottom - grateTop));
                        float bottomDepth = Mathf.Clamp01((meat.yMax - grateTop) / (grateBottom - grateTop));
                        float leftAtTop = Mathf.Lerp(.145f, .035f, topDepth);
                        float leftAtBottom = Mathf.Lerp(.145f, .035f, bottomDepth);
                        float rightAtTop = Mathf.Lerp(.855f, .965f, topDepth);
                        float rightAtBottom = Mathf.Lerp(.855f, .965f, bottomDepth);
                        float grateLeft = grill.x + grill.width * Mathf.Max(leftAtTop, leftAtBottom);
                        float grateRight = grill.x + grill.width * Mathf.Min(rightAtTop, rightAtBottom);
                        Assert.GreaterOrEqual(meat.xMin, grateLeft - .02f, context + " outside the tapered left grate edge");
                        float allowedRight = product == 3 && companionAvailable ? grill.x + grill.width * 1.10f : grateRight;
                        Assert.LessOrEqual(meat.xMax, allowedRight + .02f, context + " too far beyond the tapered right grate edge");
                    }
                }
            }
        }

        [Test]
        public void BarrelDrinkSlotsScatterAcrossTheOpeningWithMixedStableAngles()
        {
            object layout = Layout(Simulation(4));
            Rect bounds = RectProperty(layout, "CocaBarrelBounds");
            var positions = new HashSet<Vector2>();
            var angles = new HashSet<float>();
            for (int slot = 0; slot < 12; slot++)
            {
                Vector2 position = (Vector2)Call(layout, "BarrelSlotPosition", 4, slot);
                float normalizedY = (position.y - bounds.y) / bounds.height;
                float normalizedX = (position.x - bounds.x) / bounds.width;
                Assert.That(normalizedX, Is.InRange(.15f, .86f));
                Assert.That(normalizedY, Is.InRange(.04f, .19f));
                positions.Add(position);
                angles.Add((float)Call(layout, "BarrelSlotRotation", slot));
            }
            Assert.AreEqual(12, positions.Count, "All twelve real inventory slots need their own visual position.");
            Assert.AreEqual(12, angles.Count, "Each drink receives a deterministic but distinct tossed angle.");
            Assert.IsTrue(angles.Contains(106f), "Some drinks should lie diagonally/sideways rather than all standing upright.");
            Assert.IsTrue(angles.Contains(-72f));
        }

        [Test]
        public void EveryCatalogJourneyUsesShortestSafePathsInBothDirectionsAndReportsSavings()
        {
            Vector2 home = new Vector2(433f, WorkerServiceY());
            float oldDistanceTotal = 0f, newDistanceTotal = 0f, percentageTotal = 0f, maxPercentage = 0f;
            int journeys = 0;
            for (int level = 0; level < 11; level++)
            {
                object sim = Simulation(level), layout = Layout(sim);
                var allBases = new List<Rect>();
                if ((bool)Get(layout, "HasNormalTable")) allBases.Add((Rect)Call(layout, "FootprintForProduct", 0));
                if ((bool)Get(layout, "HasPremiumTable")) allBases.Add((Rect)Call(layout, "FootprintForProduct", 2));
                if ((bool)Get(layout, "HasCocaBarrel")) allBases.Add((Rect)Call(layout, "FootprintForProduct", 4));
                if ((bool)Get(layout, "HasFernetTable")) allBases.Add((Rect)Call(layout, "FootprintForProduct", 5));
                if ((bool)Get(layout, "HasBeerBarrel")) allBases.Add((Rect)Call(layout, "FootprintForProduct", 6));
                if ((bool)Get(layout, "HasNormalGrill")) allBases.Add(RectProperty(layout, "NormalGrillBounds"));
                if ((bool)Get(layout, "HasPremiumGrill")) allBases.Add(RectProperty(layout, "PremiumGrillBounds"));

                for (int product = 0; product < 7; product++)
                {
                    if (!(bool)Call(sim, "IsProductAvailable", product)) continue;
                    Vector2 pickup = (Vector2)Call(layout, "PickupPosition", product);
                    Vector2[] legacy = LegacyApproachRoute(product, pickup);
                    Vector2[] fromHome = (Vector2[])Call(layout, "FindShortestSafeRoute", home, pickup);
                    Assert.Greater(fromHome.Length, 0, "L" + level + " P" + product + " initial home route exists");
                    AssertRouteClear(layout, home, fromHome, allBases, "L" + level + " P" + product + " initial home");

                    for (int column = 0; column < 7; column++)
                    {
                        Vector2 handoff = new Vector2(58 + column * 70, CounterHandoffY());
                        Vector2[] outbound = (Vector2[])Call(layout, "FindShortestSafeRoute", handoff, pickup);
                        Assert.Greater(outbound.Length, 0, "L" + level + " P" + product + " C" + column + " outbound route exists");
                        Assert.AreEqual(pickup, outbound[outbound.Length - 1]);
                        AssertRouteClear(layout, handoff, outbound, allBases, "L" + level + " P" + product + " C" + column + " outbound");
                        float oldOut = PathDistance(handoff, legacy);
                        float newOut = PathDistance(handoff, outbound);
                        Assert.LessOrEqual(newOut, oldOut + .01f, "L" + level + " P" + product + " C" + column + " outbound cannot exceed the former center-lane route.");
                        AccumulateRouteSavings(oldOut, newOut, ref oldDistanceTotal, ref newDistanceTotal, ref percentageTotal, ref maxPercentage, ref journeys);

                        Vector2[] returning = (Vector2[])Call(layout, "FindShortestSafeRoute", pickup, handoff);
                        Assert.Greater(returning.Length, 0, "L" + level + " P" + product + " C" + column + " return route exists");
                        Assert.AreEqual(handoff, returning[returning.Length - 1]);
                        AssertRouteClear(layout, pickup, returning, allBases, "L" + level + " P" + product + " C" + column + " return");

                        var oldReturn = new List<Vector2>();
                        for (int i = legacy.Length - 2; i >= 0; i--) oldReturn.Add(legacy[i]);
                        oldReturn.Add(handoff);
                        float oldBack = PathDistance(pickup, oldReturn.ToArray());
                        float newBack = PathDistance(pickup, returning);
                        Assert.LessOrEqual(newBack, oldBack + .01f, "L" + level + " P" + product + " C" + column + " return cannot exceed the former reversed route.");
                        AccumulateRouteSavings(oldBack, newBack, ref oldDistanceTotal, ref newDistanceTotal, ref percentageTotal, ref maxPercentage, ref journeys);
                    }
                }
            }
            Assert.Greater(journeys, 0);
            TestContext.WriteLine("Shortest-route baseline: " + journeys + " outbound/return journeys; average per-journey distance reduction " + (percentageTotal / journeys).ToString("F1") + "%; max " + maxPercentage.ToString("F1") + "%; aggregate reduction " + ((1f - newDistanceTotal / oldDistanceTotal) * 100f).ToString("F1") + "%.");
        }

        [Test]
        public void BeveragePickupsReachBarrelFrontWithoutOverlappingAcrossCatalog()
        {
            float workerHalfWidth = (float)Runtime("StreetWorkstationLayout").GetField("WorkerHalfWidth").GetRawConstantValue();
            float sceneWidth = (float)Runtime("StreetSceneLayout").GetField("Width").GetRawConstantValue();
            for (int level = 0; level < 11; level++)
            {
                object sim = Simulation(level), layout = Layout(sim);
                foreach (int product in new[] { 4, 6 })
                {
                    if (!(bool)Call(sim, "IsProductAvailable", product)) continue;
                    Rect barrel = (Rect)Call(layout, "BoundsForProduct", product);
                    Vector2 pickup = (Vector2)Call(layout, "PickupPosition", product);
                    Rect feet = new Rect(pickup.x - 18f, pickup.y - 12f, 36f, 12f);
                    float safeCenterX = Mathf.Clamp(barrel.center.x, workerHalfWidth, sceneWidth - workerHalfWidth);
                    Assert.AreEqual(safeCenterX, pickup.x, .001f, "L" + level + " P" + product + " centers the worker where its full sprite stays on screen.");
                    Assert.GreaterOrEqual(pickup.x - workerHalfWidth, 0f, "L" + level + " P" + product + " keeps the complete worker in the left screen bound.");
                    Assert.LessOrEqual(pickup.x + workerHalfWidth, sceneWidth, "L" + level + " P" + product + " keeps the complete worker in the right screen bound.");
                    Assert.AreEqual(barrel.yMin, pickup.y, .001f, "L" + level + " P" + product + " stands two points closer, directly at the front edge.");
                    Assert.AreEqual(barrel.yMin, feet.yMax, .001f, "L" + level + " P" + product + " touches but does not pass the barrel front.");
                    Assert.IsFalse(feet.Overlaps(barrel), "L" + level + " P" + product + " worker feet do not enter the barrel.");
                }
            }
        }

        [Test]
        public void VisibilityRouteGoesAroundActualWorkerFootObstacleWithoutDetoursOnClearSegments()
        {
            object layout = Layout(Simulation(0));
            Rect baseRect = (Rect)Call(layout, "FootprintForProduct", 0);
            Vector2 start = new Vector2(baseRect.xMin - 60f, baseRect.center.y);
            Vector2 destination = new Vector2(baseRect.xMax + 60f, baseRect.center.y);
            Vector2[] route = (Vector2[])Call(layout, "FindShortestSafeRoute", start, destination);
            Assert.Greater(route.Length, 1, "The straight segment crosses the real table base, so a safe detour is required.");
            Assert.IsFalse((bool)Call(layout, "IsRouteSegmentClear", start, destination));
            var solidBases = new[] { baseRect, RectProperty(layout, "NormalGrillBounds") };
            Vector2 previous = start;
            for (int i = 0; i < route.Length; i++)
            {
                Assert.IsTrue((bool)Call(layout, "IsRouteSegmentClear", previous, route[i]), "Every complete swept segment clears the feet-expanded solids.");
                for (int sample = 0; sample <= 100; sample++)
                {
                    Vector2 feet = Vector2.Lerp(previous, route[i], sample / 100f);
                    Rect footBounds = new Rect(feet.x - 18f, feet.y - 12f, 36f, 12f);
                    foreach (Rect solid in solidBases) Assert.IsFalse(footBounds.Overlaps(solid), "The worker's actual feet rectangle never overlaps a solid.");
                }
                previous = route[i];
            }
            Assert.AreEqual(destination, route[route.Length - 1]);
            Rect expandedBase = new Rect(baseRect.xMin - 18f, baseRect.yMin, baseRect.width + 36f, baseRect.height + 12f);
            Vector2 lowerLeft = new Vector2(expandedBase.xMin, expandedBase.yMin);
            Vector2 lowerRight = new Vector2(expandedBase.xMax, expandedBase.yMin);
            float geometricShortest = Vector2.Distance(start, lowerLeft) + expandedBase.width + Vector2.Distance(lowerRight, destination);
            Assert.That(PathDistance(start, route), Is.InRange(geometricShortest, geometricShortest + .25f),
                "The obstacle detour matches the geometric shortest path around the exposed lower base corners.");
            Assert.IsNull(Call(layout, "FindShortestSafeRoute", baseRect.center, destination),
                "A start inside a solid obstacle is unreachable and must never fall back to a direct through-obstacle line.");
        }

        private static Vector2[] LegacyApproachRoute(int product, Vector2 pickup)
        {
            Vector2 center = new Vector2(SceneWidth() * .5f, LayoutConstant("WorkerLaneY"));
            Vector2 lane = new Vector2(pickup.x, LayoutConstant("WorkerLaneY"));
            float approachY = product < 4 || product == 5 ? LayoutConstant("TablePickupY") : LayoutConstant("BarrelPickupY");
            Vector2 approach = new Vector2(pickup.x, approachY);
            var result = new List<Vector2> { center };
            if (Mathf.Abs(center.x - lane.x) > 1f) result.Add(lane);
            if (Mathf.Abs(lane.y - approach.y) > 1f) result.Add(approach);
            return result.ToArray();
        }

        private static void AssertRouteClear(object layout, Vector2 start, Vector2[] route, List<Rect> solidBases, string label)
        {
            if (route.Length > 0 && (bool)Call(layout, "IsRouteSegmentClear", start, route[route.Length - 1]))
                Assert.AreEqual(1, route.Length, label + " uses the direct path without waypoints when unobstructed.");
            Vector2 from = start;
            Vector2 beforeFrom = Vector2.zero;
            for (int i = 0; i < route.Length; i++)
            {
                Assert.Greater((route[i] - from).sqrMagnitude, .0001f, label + " has no duplicate consecutive waypoints.");
                if (i > 0)
                    Assert.Greater(Mathf.Abs((from.x - beforeFrom.x) * (route[i].y - from.y) - (from.y - beforeFrom.y) * (route[i].x - from.x)), .001f,
                        label + " has no redundant collinear waypoints.");
                Assert.IsTrue((bool)Call(layout, "IsRouteSegmentClear", from, route[i]), label + " complete segment is collision-free.");
                for (int sample = 0; sample <= 100; sample++)
                {
                    Vector2 feet = Vector2.Lerp(from, route[i], sample / 100f);
                    Rect footBounds = new Rect(feet.x - 18f, feet.y - 12f, 36f, 12f);
                    foreach (Rect solid in solidBases)
                        Assert.IsFalse(footBounds.Overlaps(solid), label + " feet " + footBounds + " overlap solid base " + solid);
                }
                beforeFrom = from; from = route[i];
            }
        }

        private static float PathDistance(Vector2 start, Vector2[] route)
        {
            float distance = 0f;
            for (int i = 0; i < route.Length; i++) { distance += Vector2.Distance(start, route[i]); start = route[i]; }
            return distance;
        }

        private static void AccumulateRouteSavings(float oldDistance, float newDistance, ref float oldTotal, ref float newTotal, ref float percentageTotal, ref float maxPercentage, ref int journeys)
        {
            Assert.Greater(oldDistance, 0f);
            float reduction = Mathf.Max(0f, (oldDistance - newDistance) / oldDistance * 100f);
            oldTotal += oldDistance; newTotal += newDistance; percentageTotal += reduction;
            maxPercentage = Mathf.Max(maxPercentage, reduction); journeys++;
        }

        [Test]
        public void SelectingAndAdvancingLevelRefreshesTheCatalogDrivenKitchenLayout()
        {
            object sim = Simulation(0);
            Assert.IsFalse((bool)Get(Layout(sim), "HasCocaBarrel"));
            Assert.IsTrue((bool)Call(sim, "SelectLevel", 2));
            object selectedLayout = Layout(sim);
            Assert.IsTrue((bool)Get(selectedLayout, "HasCocaBarrel"));
            Assert.IsFalse((bool)Get(selectedLayout, "HasPremiumGrill"));

            PropertyInfo phase = Runtime("StreetSimulation").GetProperty("Phase", BindingFlags.Public | BindingFlags.Instance);
            phase.GetSetMethod(true).Invoke(sim, new[] { Enum.Parse(phase.PropertyType, "Won") });
            Assert.IsTrue((bool)Call(sim, "NextLevel"));
            Assert.AreEqual(12, Get(sim, "LevelIndex"));
            object nextLayout = Layout(sim);
            Assert.IsTrue((bool)Get(nextLayout, "HasPremiumGrill"));
            Assert.IsTrue((bool)Get(nextLayout, "HasBeerBarrel"));
            Assert.IsTrue((bool)Get(nextLayout, "HasFernetTable"));
        }

        [Test]
        public void TableServingFootprintsStayInsideTheWoodTopForEveryMeatSlotAndLayout()
        {
            object layout = FullLayout();
            Type t = Runtime("StreetKitchenLayout");
            foreach (int product in new[] { 0, 1, 2, 3 })
            foreach (bool companion in new[] { false, true })
            {
                Rect logical = (Rect)Call(layout, "BoundsForProduct", product);
                Rect visual = (Rect)t.GetMethod("FoodTableVisualBounds").Invoke(null, new object[] { logical });
                float depth = visual.height * LayoutConstant("SandwichTableTopDepthRatio");
                Vector2 size = (Vector2)t.GetMethod("TableServingFootprint").Invoke(null, new object[] { product });
                string[] names = { "sandwich-chori", "sandwich-paty", "sandwich-bondiola", "sandwich-vacio" };
                Sprite sprite = Resources.Load<Sprite>("ModularKitchen/Sprites/" + names[product] + "/" + names[product]);
                Assert.AreEqual(sprite.rect.width / sprite.rect.height, size.x / size.y, .0001f,
                    "Layout silhouettes must match the actual Sprite art.");
                int capacity = 48;
                for (int slot = 0; slot < capacity; slot++)
                {
                    Vector2 center = (Vector2)Call(layout, "TableSlotPosition", product, slot, companion, capacity);
                    Assert.GreaterOrEqual(center.x - size.x * .5f, visual.xMin);
                    Assert.LessOrEqual(center.x + size.x * .5f, visual.xMax);
                    if (slot < (companion ? 6 : 12))
                    {
                        float contact = center.y + size.y * .5f;
                        Assert.GreaterOrEqual(contact, visual.y, "Base contact cannot float behind the wood.");
                        Assert.LessOrEqual(contact, visual.y + depth * .71f, "Base contact stays before the front lip.");
                    }
                }
            }
        }

        [Test]
        public void SandwichTableSlotsUseThreeDepthRowsAndKeepFullCapacityInsideEachProductZone()
        {
            object layout = FullLayout();
            Type t = Runtime("StreetKitchenLayout");
            MethodInfo order = t.GetMethod("TableSlotDepthOrder", BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(int), typeof(bool) }, null);
            MethodInfo rotation = t.GetMethod("TableSlotRotation", BindingFlags.Public | BindingFlags.Static);
            foreach (int product in new[] { 0, 1, 2, 3 })
            foreach (bool companion in new[] { false, true })
            {
                int capacity = 48;
                int columns = companion ? 2 : 4, baseCount = columns * 3;
                var positions = new HashSet<Vector2>();
                var rows = new HashSet<float>();
                for (int slot = 0; slot < baseCount; slot++)
                {
                    Vector2 point = (Vector2)Call(layout, "TableSlotPosition", product, slot, companion, capacity);
                    Assert.IsTrue(positions.Add(point));
                    rows.Add(point.y);
                }
                Assert.AreEqual(3, rows.Count);
                Assert.AreEqual(baseCount, positions.Count);
                var secondLayerOffsets = new HashSet<Vector2>();
                for (int baseSlot = 0; baseSlot < baseCount; baseSlot++)
                {
                    Vector2 basePoint = (Vector2)Call(layout, "TableSlotPosition", product, baseSlot, companion, capacity);
                    Vector2 secondLayerPoint = (Vector2)Call(layout, "TableSlotPosition", product, baseCount + baseSlot, companion, capacity);
                    secondLayerOffsets.Add(secondLayerPoint - basePoint);
                }
                Assert.GreaterOrEqual(secondLayerOffsets.Count, Mathf.Min(5, baseCount),
                    "Upper servings use visibly varied, stable hand-stacked offsets rather than a repeated grid.");
                var variedRotations = new HashSet<float>();
                for (int slot = 0; slot < capacity; slot++)
                {
                    float angle = (float)rotation.Invoke(null, new object[] { product, slot, companion });
                    Assert.LessOrEqual(Mathf.Abs(angle), slot < baseCount ? 5f : 16f);
                    Assert.AreEqual(angle, (float)rotation.Invoke(null, new object[] { product, slot, companion }), .0001f,
                        "Scatter and tilt are deterministic across renders.");
                    if (slot >= baseCount) variedRotations.Add(angle);
                }
                Assert.GreaterOrEqual(variedRotations.Count, Mathf.Min(5, baseCount),
                    "Upper servings have different subtle tilts, not a repeated aligned stack.");
                for (int slot = 0; slot < capacity; slot++)
                {
                    Vector2 point = (Vector2)Call(layout, "TableSlotPosition", product, slot, companion, capacity);
                    Vector2 original = (Vector2)Call(layout, "TableSlotPosition", product, slot % baseCount, companion, capacity);
                    int layer = slot / baseCount;
                    if (layer == 0) Assert.AreEqual(original.y, point.y, .0001f);
                    else
                    {
                        Vector2 below = (Vector2)Call(layout, "TableSlotPosition", product, slot - baseCount, companion, capacity);
                        Vector2 footprint = (Vector2)t.GetMethod("TableServingFootprint", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { product });
                        Rect thisSandwich = new Rect(point.x - footprint.x * .5f, point.y - footprint.y * .5f, footprint.x, footprint.y);
                        Rect support = new Rect(below.x - footprint.x * .5f, below.y - footprint.y * .5f, footprint.x, footprint.y);
                        Assert.IsTrue(thisSandwich.Overlaps(support), "Product " + product + " slot " + slot + " overlaps its supporting layer; it cannot float.");
                        float visibleLayerStep = Mathf.Abs(point.y - below.y) / footprint.y;
                        Assert.Less(point.y, below.y, "Canvas Y decreases upward; every added stack layer must rise above its support.");
                        Assert.GreaterOrEqual(visibleLayerStep, .30f, "Lower sandwiches retain a substantial visible face.");
                        Assert.LessOrEqual(visibleLayerStep, .70f, "Every upper sandwich remains closely supported by the layer below.");
                        float horizontalSupport = Mathf.Min(thisSandwich.xMax, support.xMax) - Mathf.Max(thisSandwich.xMin, support.xMin);
                        Assert.Greater(horizontalSupport, footprint.x * .5f, "Upper sandwiches keep at least half their width on the support below.");
                    }
                    Assert.GreaterOrEqual((int)order.Invoke(null, new object[] { slot, companion }),
                        (int)order.Invoke(null, new object[] { slot % baseCount, companion }));
                }
                Vector2 rear = (Vector2)Call(layout, "TableSlotPosition", product, 0, companion, capacity);
                Vector2 front = (Vector2)Call(layout, "TableSlotPosition", product, columns * 2, companion, capacity);
                Assert.Greater(front.y, rear.y);
                Assert.Less(front.y - rear.y, 13f, "Approved depth rows remain compact, with perspective overlap.");
                for (int slot = 0; slot < capacity; slot++)
                {
                    Vector2 pickup = (Vector2)Call(layout, "TableSlotPickupPosition", product, slot, companion, capacity);
                    Vector2 point = (Vector2)Call(layout, "TableSlotPosition", product, slot, companion, capacity);
                    int baseSlot = slot % baseCount;
                    Vector2 basePoint = (Vector2)Call(layout, "TableSlotPosition", product, baseSlot, companion, capacity);
                    Vector2 basePickup = (Vector2)Call(layout, "TableSlotPickupPosition", product, baseSlot, companion, capacity);
                    float halfWorker = (float)Runtime("StreetWorkstationLayout").GetField("WorkerHalfWidth", BindingFlags.Public | BindingFlags.Static).GetRawConstantValue();
                    float width = (float)Runtime("StreetSceneLayout").GetField("Width", BindingFlags.Public | BindingFlags.Static).GetRawConstantValue();
                    Assert.AreEqual(Mathf.Clamp(point.x, halfWorker, width - halfWorker), pickup.x, .001f,
                        "Pickup X follows the actual item, including its stable stack jitter.");
                    Assert.AreEqual(basePickup.y, pickup.y, .001f,
                        "Pickup Y follows the base row's worker-feet anchor rather than the cosmetic stack height.");
                    Assert.IsNotNull(Call(layout, "FindShortestSafeRoute", new Vector2(433f, WorkerServiceY()), pickup),
                        "Every product/paired-mode/slot pickup remains reachable from the service aisle.");
                }
                Rect visual = (Rect)t.GetMethod("FoodTableVisualBounds").Invoke(null,
                    new object[] { (Rect)Call(layout, "BoundsForProduct", product) });
                Vector2 footprintSize = (Vector2)t.GetMethod("TableServingFootprint", BindingFlags.Public | BindingFlags.Static)
                    .Invoke(null, new object[] { product });
                for (int slot = 0; slot < capacity; slot++)
                {
                    Vector2 point = (Vector2)Call(layout, "TableSlotPosition", product, slot, companion, capacity);
                    Rect itemRect = new Rect(point.x - footprintSize.x * .5f, point.y - footprintSize.y * .5f,
                        footprintSize.x, footprintSize.y);
                    Vector2 basePoint = (Vector2)Call(layout, "TableSlotPosition", product, slot % baseCount, companion, capacity);
                    float baseContactY = basePoint.y + footprintSize.y * .5f;
                    Assert.LessOrEqual(itemRect.yMax, baseContactY + .001f,
                        "No item, including an upper layer, may hang down below its wood-supported base row; upward layers remain allowed.");
                }
            }
        }

        [Test]
        public void TableAndGrillSlotsDoNotMixTheTwoMeatFamilies()
        {
            object layout = FullLayout();
            Type layoutType = Runtime("StreetKitchenLayout");
            Vector2 chori = (Vector2)layoutType.GetMethod("TableServingFootprint", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { 0 });
            Vector2 paty = (Vector2)layoutType.GetMethod("TableServingFootprint", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { 1 });
            Vector2 bondiola = (Vector2)layoutType.GetMethod("TableServingFootprint", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { 2 });
            Vector2 vacio = (Vector2)layoutType.GetMethod("TableServingFootprint", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { 3 });
            AssertSlotsSeparated(layout, 0, 1, 48, chori, paty);
            AssertSlotsSeparated(layout, 2, 3, 48, bondiola, vacio);
            AssertSlotsSeparated(layout, 0, 1, 18, new Vector2(36.4f, 14f), new Vector2(34f, 20f), true);
            AssertSlotsSeparated(layout, 2, 3, 12, new Vector2(34f, 38f), new Vector2(90f, 14f), true);
        }

        private static void AssertSlotsSeparated(object layout, int first, int second, int count, Vector2 firstSize, Vector2 secondSize, bool grill = false)
        {
            var firstRects = new List<Rect>();
            var secondRects = new List<Rect>();
            for (int i = 0; i < count; i++)
            {
                Vector2 a = grill ? (Vector2)Call(layout, "GrillSlotPosition", first, i, true, first < 2 ? 12 : 4)
                    : (Vector2)Call(layout, "TableSlotPosition", first, i, true, count);
                Vector2 b = grill ? (Vector2)Call(layout, "GrillSlotPosition", second, i, true, second < 2 ? 6 : 6)
                    : (Vector2)Call(layout, "TableSlotPosition", second, i, true, count);
                if (grill)
                {
                    firstSize = (Vector2)Call(layout, "GrillMeatSize", first, true);
                    secondSize = (Vector2)Call(layout, "GrillMeatSize", second, true);
                }
                int firstLimit = grill ? (first == 0 ? 12 : first == 1 ? 6 : 6) : count;
                int secondLimit = grill ? (second == 0 ? 12 : second == 1 ? 6 : second == 3 ? 4 : 6) : count;
                if (i < firstLimit) firstRects.Add(new Rect(a - firstSize * .5f, firstSize));
                if (i < secondLimit) secondRects.Add(new Rect(b - secondSize * .5f, secondSize));
            }
            foreach (Rect a in firstRects) foreach (Rect b in secondRects)
                Assert.IsFalse(a.Overlaps(b), "Cross-family grill/table slots overlap for products " + first + "/" + second + ": " + a + " vs " + b);
        }

        [Test]
        public void FourProductionSubstatesProjectToThreeWarmVisualStates()
        {
            Type stateType = Runtime("StreetFoodState");
            MethodInfo visualState = Runtime("StreetKitchenRenderer").GetMethod("VisualStateName", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(visualState);
            Func<string, string> project = state => (string)visualState.Invoke(null, new[] { Enum.Parse(stateType, state) });
            Assert.AreEqual("Raw", project("Raw"));
            Assert.AreEqual("Raw", project("Cooking"), "Cooking remains a timer stage, not a fourth visual state.");
            Assert.AreEqual("Cooked", project("Cooked"));
            Assert.AreEqual("Passed", project("Burned"));
            MethodInfo spriteState = Runtime("StreetKitchenRenderer").GetMethod("ArtStateName", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(spriteState);
            Assert.AreEqual("Cooked", spriteState.Invoke(null, new object[] { "Passed" }),
                "Passed must reuse warm cooked art, not the black-char frame.");
        }

        [Test]
        public void IndependentResourcesKeepCookingSourceFramesAndSeparateStationPrefabs()
        {
            string[] meat = { "meat-chori", "meat-paty", "meat-bondiola", "meat-vacio" };
            string[] states = { "Raw", "Cooking", "Cooked", "Burned" };
            foreach (string name in meat)
            {
                string path = "Assets/Resources/ModularKitchen/Art/" + name + ".png";
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.NotNull(importer, path);
                Assert.IsTrue(importer.DoesSourceTextureHaveAlpha(), path);
                Assert.AreEqual(TextureImporterType.Default, importer.textureType);
                Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale, "Original sprite aspect must not be resampled to a different power-of-two ratio.");
                Assert.IsTrue(importer.mipmapEnabled, "High resolution food minification needs mipmaps to avoid sparkle.");
                Assert.AreEqual(FilterMode.Trilinear, importer.filterMode);
                Sprite[] sprites = Resources.LoadAll<Sprite>("ModularKitchen/Sprites/" + name);
                Assert.AreEqual(4, sprites.Length, name);
                Texture2D decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                Assert.IsTrue(ImageConversion.LoadImage(decoded, File.ReadAllBytes(path)), path);
                int cell = decoded.width / 4;
                Texture2D importedTexture = sprites[0].texture;
                Assert.LessOrEqual(importedTexture.width, importer.maxTextureSize, name + " imported texture respects its max-size setting.");
                Assert.LessOrEqual(importedTexture.height, importer.maxTextureSize, name + " imported texture respects its max-size setting.");
                Assert.AreEqual(decoded.width / (float)decoded.height, importedTexture.width / (float)importedTexture.height, .01f,
                    name + " import downscaling must preserve source aspect ratio.");
                for (int state = 0; state < 4; state++)
                {
                    string expectedName = name + "_" + states[state];
                    Sprite found = Array.Find(sprites, s => s.name == expectedName);
                    Assert.NotNull(found, expectedName);
                    Assert.Greater(found.rect.width, cell * .5f, expectedName + " should use most of its authored cell.");
                    Assert.Less(found.rect.width, cell, expectedName + " should trim alpha margin without altering source PNG.");
                    string spriteAssetPath = "Assets/Resources/ModularKitchen/Sprites/" + name + "/" + expectedName + ".asset";
                    Assert.AreSame(found, AssetDatabase.LoadAssetAtPath<Sprite>(spriteAssetPath), spriteAssetPath);
                    Assert.IsNotEmpty(AssetDatabase.AssetPathToGUID(spriteAssetPath), spriteAssetPath);
                    Assert.IsTrue(ContainsOpaquePixel(decoded, state * cell, (state + 1) * cell), expectedName);
                }
                UnityEngine.Object.DestroyImmediate(decoded);
            }

            AssertPrefab("Station_NormalGrill", "empty-grill", 1);
            AssertPrefab("Station_PremiumGrill", "empty-grill", 1);
            AssertFoodTablePrefab("Station_NormalTable");
            AssertFoodTablePrefab("Station_PremiumTable");
            AssertPrefab("Station_FernetTable", "empty-table", 1);
            AssertPrefab("Station_BeerBarrel", "blue-barrel", 2);
            AssertPrefab("Station_CocaBarrel", "blue-barrel", 2);
            string[] singles = { "empty-grill", "empty-table", "food-table-folding-v2", "blue-barrel", "sandwich-chori", "sandwich-paty", "sandwich-bondiola", "sandwich-vacio", "coca", "beer", "fernet", "ice", "smoke", "heat" };
            foreach (string name in singles)
            {
                string path = "Assets/Resources/ModularKitchen/Art/" + name + ".png";
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.NotNull(importer, path);
                Assert.IsTrue(importer.DoesSourceTextureHaveAlpha(), path);
                Assert.AreEqual(TextureImporterType.Default, importer.textureType);
                Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale, "Original sprite aspect must not be resampled to a different power-of-two ratio.");
                Assert.IsTrue(importer.mipmapEnabled, "High resolution food minification needs mipmaps to avoid sparkle.");
                Assert.AreEqual(FilterMode.Trilinear, importer.filterMode);
                string spriteResourceName = name == "food-table-folding-v2" ? "food-table-plank" : name;
                Assert.AreEqual(1, Resources.LoadAll<Sprite>("ModularKitchen/Sprites/" + spriteResourceName).Length, spriteResourceName);
            }
            foreach (string name in new[] { "Food_Chori", "Food_Paty", "Food_Bondiola", "Food_Vacio", "Sandwich_Chori", "Sandwich_Paty", "Sandwich_Bondiola", "Sandwich_Vacio", "Drink_Coca", "Drink_Beer", "Drink_Fernet", "Prop_Ice", "Effect_Smoke", "Effect_Heat" })
            {
                GameObject prefab = Resources.Load<GameObject>("ModularKitchen/Prefabs/" + name);
                Assert.NotNull(prefab, name);
                SpriteRenderer[] renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
                Assert.Greater(renderers.Length, 0, name);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Assert.NotNull(renderers[i].sprite, name + " missing sprite.");
                    Assert.NotNull(renderers[i].sharedMaterial, name + " missing default sprite material.");
                    Assert.NotNull(renderers[i].sharedMaterial.shader, name + " missing sprite shader.");
                    Assert.IsTrue(renderers[i].sharedMaterial.shader.isSupported, name + " sprite shader is unsupported.");
                }
            }
        }

        [Test]
        public void SandwichArtworkKeepsTransparencyAndPrefabSpriteLinks()
        {
            string[] artNames = { "sandwich-chori", "sandwich-paty", "sandwich-bondiola", "sandwich-vacio" };
            string[] prefabNames = { "Sandwich_Chori", "Sandwich_Paty", "Sandwich_Bondiola", "Sandwich_Vacio" };
            for (int i = 0; i < artNames.Length; i++)
            {
                string artPath = "Assets/Resources/ModularKitchen/Art/" + artNames[i] + ".png";
                TextureImporter importer = AssetImporter.GetAtPath(artPath) as TextureImporter;
                Assert.NotNull(importer, artPath);
                Assert.IsTrue(importer.DoesSourceTextureHaveAlpha(), artPath);
                Assert.AreEqual(TextureImporterType.Default, importer.textureType, artPath);
                Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale, artPath);

                Texture2D decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                Assert.IsTrue(ImageConversion.LoadImage(decoded, File.ReadAllBytes(artPath)), artPath);
                Assert.Less(decoded.GetPixel(0, 0).a, .05f, artPath + " must retain a transparent corner.");
                string spritePath = "Assets/Resources/ModularKitchen/Sprites/" + artNames[i] + "/" + artNames[i] + ".asset";
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                Assert.NotNull(sprite, spritePath);
                Assert.AreEqual(decoded.width, sprite.texture.width, spritePath);
                Assert.AreEqual(decoded.height, sprite.texture.height, spritePath);
                Assert.Greater(sprite.rect.width, 0f, spritePath);
                Assert.Greater(sprite.rect.height, 0f, spritePath);
                if (i == 1)
                {
                    Sprite chori = AssetDatabase.LoadAssetAtPath<Sprite>(
                        "Assets/Resources/ModularKitchen/Sprites/sandwich-chori/sandwich-chori.asset");
                    float patyAspect = sprite.rect.width / sprite.rect.height;
                    Assert.Greater(patyAspect, 1.7f, "Paty stays low-profile and single-layer, not a tall double-decker.");
                    Assert.Less(patyAspect, chori.rect.width / chori.rect.height,
                        "Chori must read as the more elongated sandwich.");
                }

                GameObject prefab = Resources.Load<GameObject>("ModularKitchen/Prefabs/" + prefabNames[i]);
                Assert.NotNull(prefab, prefabNames[i]);
                SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
                Assert.NotNull(renderer, prefabNames[i]);
                Assert.AreSame(sprite, renderer.sprite, prefabNames[i] + " must keep its stable sprite reference.");
                UnityEngine.Object.DestroyImmediate(decoded);
            }
        }

        private static bool ContainsOpaquePixel(Texture2D texture, int xMin, int xMax)
        {
            for (int y = 0; y < texture.height; y++)
            for (int x = xMin; x < xMax; x++) if (texture.GetPixel(x, y).a > .05f) return true;
            return false;
        }

        private static void AssertPrefab(string prefabName, string spriteName, int expectedRendererCount)
        {
            GameObject prefab = Resources.Load<GameObject>("ModularKitchen/Prefabs/" + prefabName);
            Assert.NotNull(prefab, prefabName);
            SpriteRenderer[] renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
            Assert.AreEqual(expectedRendererCount, renderers.Length, prefabName + " must have independent station artwork only.");
            Assert.AreEqual(spriteName, renderers[0].sprite.name, prefabName);
            for (int i = 0; i < renderers.Length; i++)
            {
                Assert.NotNull(renderers[i].sharedMaterial, prefabName + " missing default sprite material.");
                Assert.NotNull(renderers[i].sharedMaterial.shader, prefabName + " missing sprite shader.");
                Assert.IsTrue(renderers[i].sharedMaterial.shader.isSupported, prefabName + " sprite shader is unsupported by the active render pipeline.");
            }
        }

        private static void AssertFoodTablePrefab(string prefabName)
        {
            GameObject prefab = Resources.Load<GameObject>("ModularKitchen/Prefabs/" + prefabName);
            Assert.NotNull(prefab, prefabName);
            Assert.IsNull(prefab.GetComponent<SpriteRenderer>(), prefabName + " should be a composed station root.");
            Transform left = prefab.transform.Find("Trestle_Left");
            Transform right = prefab.transform.Find("Trestle_Right");
            Transform tabletop = prefab.transform.Find("Tabletop");
            Assert.NotNull(left, prefabName + " missing left trestle.");
            Assert.NotNull(right, prefabName + " missing right trestle.");
            Assert.NotNull(tabletop, prefabName + " missing single tabletop plank.");
            Assert.AreEqual("food-table-trestle", left.GetComponent<SpriteRenderer>().sprite.name);
            Assert.AreEqual("food-table-trestle-right", right.GetComponent<SpriteRenderer>().sprite.name);
            Assert.AreEqual("food-table-plank", tabletop.GetComponent<SpriteRenderer>().sprite.name);
            Assert.AreEqual(3, prefab.GetComponentsInChildren<SpriteRenderer>(true).Length,
                prefabName + " must have exactly two trestles and one plank.");
            Assert.AreEqual(0, left.GetComponent<SpriteRenderer>().sortingOrder);
            Assert.AreEqual(0, right.GetComponent<SpriteRenderer>().sortingOrder);
            Assert.AreEqual(2, tabletop.GetComponent<SpriteRenderer>().sortingOrder,
                "The tabletop plank draws in front of both support trestles.");
            foreach (SpriteRenderer renderer in prefab.GetComponentsInChildren<SpriteRenderer>(true))
            {
                Assert.NotNull(renderer.sharedMaterial, prefabName + " missing default sprite material.");
                Assert.NotNull(renderer.sharedMaterial.shader, prefabName + " missing sprite shader.");
                Assert.IsTrue(renderer.sharedMaterial.shader.isSupported, prefabName + " sprite shader is unsupported.");
            }
        }
    }
}
