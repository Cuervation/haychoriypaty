using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    public sealed class TowerDefenseScreenLayoutTests
    {
        [TestCase(0f, 0f, 1f, 1f)]
        [TestCase(.5f, 8f, .9f, .5f)]
        [TestCase(1f, 16f, .8f, 0f)]
        public void CompleteFanDepartureLiftsShrinksAndFadesExistingArt(float progress, float lift, float scale, float opacity)
        {
            MethodInfo sample = RuntimeType("TowerDefenseWorldPresentation").GetMethod("FanDepartureVisual", BindingFlags.NonPublic | BindingFlags.Static);
            Vector3 visual = (Vector3)sample.Invoke(null, new object[] {progress});
            Assert.That(visual.x, Is.EqualTo(lift).Within(.001f));
            Assert.That(visual.y, Is.EqualTo(scale).Within(.001f));
            Assert.That(visual.z, Is.EqualTo(opacity).Within(.001f));
        }

        [Test]
        public void SerializedDefenseConfigOnlyRequestsChoriAndCocaAndStartsWithoutWorkers()
        {
            UnityEngine.Object config = Resources.Load("TowerDefense/Camioneros-PlazaDeMayo");
            Assert.IsNotNull(config);
            Type type = config.GetType();
            CollectionAssert.AreEqual(new[] {0, 4}, (int[])type.GetProperty("AvailableProducts").GetValue(config));
            CollectionAssert.AreEqual(new int[5], (int[])type.GetProperty("StartingWorkersByRole").GetValue(config));
            Assert.AreEqual(60, type.GetProperty("TotalFanCount").GetValue(config));
        }

        [Test]
        public void BothDefenseConfigsShareGameplayAndLoadTheirOwnExistingVisualTheme()
        {
            object camioneros = Resources.Load("TowerDefense/Camioneros-PlazaDeMayo");
            object redondos = Resources.Load("TowerDefense/LosRedondos-Tandil");
            Assert.IsNotNull(camioneros);
            Assert.IsNotNull(redondos);
            Assert.AreEqual(9, Read(camioneros, "SourceCampaignLevelId"));
            Assert.AreEqual(10, Read(redondos, "SourceCampaignLevelId"));
            Assert.AreEqual("tower-defense.camioneros.plaza-de-mayo.level-1", Read(camioneros, "LevelId"));
            Assert.AreEqual("tower-defense.los-redondos.tandil.level-1", Read(redondos, "LevelId"));
            Assert.AreEqual("Camioneros / Plaza de Mayo", Read(camioneros, "DisplayName"));
            Assert.AreEqual("Los Redondos / Tandil", Read(redondos, "DisplayName"));
            Assert.AreEqual("street-background-camioneros-v1", Read(camioneros, "BackgroundResource"));
            Assert.AreEqual("street-background-losredondos-v1", Read(redondos, "BackgroundResource"));
            Assert.AreEqual(9, Read(redondos, "EconomicBalanceCampaignLevelId"));

            Assert.AreEqual(Read(camioneros, "TotalFanCount"), Read(redondos, "TotalFanCount"));
            Assert.AreEqual(60, Read(redondos, "TotalFanCount"));
            Assert.AreEqual(Read(camioneros, "FanSpeed"), Read(redondos, "FanSpeed"));
            Assert.That((float)Read(camioneros, "FanSpeed"), Is.EqualTo(0.1575f).Within(0.0001f));
            CollectionAssert.AreEqual((int[])Read(camioneros, "AvailableProducts"), (int[])Read(redondos, "AvailableProducts"));
            CollectionAssert.AreEqual((int[])Read(camioneros, "StartingWorkersByRole"), (int[])Read(redondos, "StartingWorkersByRole"));
            object camionerosRoute = Read(camioneros, "Route");
            object redondosRoute = Read(redondos, "Route");
            Assert.AreEqual(Read(camionerosRoute, "RowCount"), Read(redondosRoute, "RowCount"));
            CollectionAssert.AreEqual((float[])Read(camionerosRoute, "SegmentLengths"), (float[])Read(redondosRoute, "SegmentLengths"));
            Assert.AreEqual(Read(camionerosRoute, "CorridorWidth"), Read(redondosRoute, "CorridorWidth"));
            Assert.AreEqual(Read(camionerosRoute, "RowSpacing"), Read(redondosRoute, "RowSpacing"));
            Assert.AreEqual(Read(camionerosRoute, "PlayableBounds"), Read(redondosRoute, "PlayableBounds"));
            Assert.AreEqual(Read(camioneros, "ServiceRange"), Read(redondos, "ServiceRange"));

            Type themeType = RuntimeType("ClubVisualTheme");
            object camionerosTheme = themeType.GetMethod("ForLevel", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] {9});
            object redondosTheme = themeType.GetMethod("ForLevel", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] {10});
            Assert.AreEqual("street-background-camioneros-v1", Read(camionerosTheme, "MuralResource"));
            Assert.AreEqual("street-background-losredondos-v1", Read(redondosTheme, "MuralResource"));
            object camionerosFans = Read(camionerosTheme, "Fans");
            object redondosFans = Read(redondosTheme, "Fans");
            Assert.AreEqual("street-ferro-fans-front-v2", Read(camionerosFans, "FrontFans"));
            Assert.AreEqual("street-losredondos-fans-front-v1", Read(redondosFans, "FrontFans"));
            Assert.IsNotNull(Resources.Load<Texture2D>((string)Read(camionerosFans, "FrontFans")));
            Assert.IsNotNull(Resources.Load<Texture2D>((string)Read(redondosFans, "FrontFans")));
            Assert.IsNotNull(Resources.Load<Texture2D>((string)Read(redondosFans, "WalkingFans")));
            Assert.IsNotNull(Resources.Load<Texture2D>((string)Read(redondosFans, "RiotFans")));
            Assert.IsNotNull(Resources.Load<Texture2D>("street-los-redondos-crest-v1"));
        }

        [Test]
        public void DefenseResultSummaryDoesNotRestoreTraditionalProductQuotas()
        {
            Type simulation = RuntimeType("StreetSimulation");
            object balance = Activator.CreateInstance(RuntimeType("StreetBalance"));
            object defense = Activator.CreateInstance(simulation, new object[] {balance, 9, 5f, 0, 1, 0});
            object traditional = Activator.CreateInstance(simulation, new object[] {balance, 8, 5f, 0, 1, 0});
            MethodInfo summary = RuntimeType("StreetView").GetMethod("FormatQuotaSummary", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.AreEqual(2, simulation.GetProperty("ProductCount").GetValue(defense));
            MethodInfo caption = RuntimeType("StreetView").GetMethod("MarketObjectiveCaption", BindingFlags.NonPublic | BindingFlags.Static);
            StringAssert.DoesNotContain("×", (string)caption.Invoke(null, new[] {defense, (object)0, (object)false}));
            StringAssert.Contains("×", (string)caption.Invoke(null, new[] {traditional, (object)0, (object)false}));
            Assert.AreEqual("60 HINCHAS ATENDIDOS", summary.Invoke(null, new[] {defense, (object)false}));
            Assert.AreEqual("UN HINCHA LLEGÓ A LA SALIDA", summary.Invoke(null, new[] {defense, (object)true}));
            StringAssert.Contains("/", (string)summary.Invoke(null, new[] {traditional, (object)false}));
        }

        [TestCase(100f, 100f, 76f)]
        [TestCase(200f, 100f, 39f)]
        [TestCase(100f, 200f, 76f)]
        public void FanFrameFitsSameBoundsAsCampaign(float width, float height, float expectedHeight)
        {
            Type presentation = Type.GetType("HayChoriYPaty.TowerDefenseWorldPresentation, Assembly-CSharp", true);
            MethodInfo method = presentation.GetMethod("FanTargetHeightLogical", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method);
            float actual = (float)method.Invoke(null, new object[] { width, height });
            Assert.That(actual, Is.EqualTo(expectedHeight).Within(.001f));
        }

        [Test]
        public void CamionerosFansCycleThreeAuthoredWalkingFramesAsTheyAdvance()
        {
            Type presentation = RuntimeType("TowerDefenseWorldPresentation");
            MethodInfo method = presentation.GetMethod("FanWalkingFrameIndex", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method);

            System.Func<int, int, float, float, int> pose = (levelId, fanId, progress, pixel) =>
                (int)method.Invoke(null, new object[] { levelId, fanId, progress, pixel });
            const float logicalPixel = .01f;
            Assert.AreEqual(0, pose(9, 0, 0f, logicalPixel));
            Assert.AreEqual(0, pose(9, 0, logicalPixel * 17.9f, logicalPixel));
            Assert.AreEqual(1, pose(9, 0, logicalPixel * 18f, logicalPixel));
            Assert.AreEqual(2, pose(9, 0, logicalPixel * 36f, logicalPixel));
            Assert.AreEqual(0, pose(9, 0, logicalPixel * 54f, logicalPixel));
            Assert.AreEqual(1, pose(9, 1, 0f, logicalPixel), "Adjacent fans should not all freeze in the same gait pose.");
            Assert.AreEqual(2, pose(9, 2, 0f, logicalPixel), "Three starting poses should be staggered across the crowd.");
            Assert.AreEqual(0, pose(10, 0, logicalPixel * 180f, logicalPixel), "Los Redondos retains its current single walk pose.");

            Texture2D baseAtlas = Resources.Load<Texture2D>("street-ferro-fans-walk-v2");
            Texture2D reverseAtlas = Resources.Load<Texture2D>("street-ferro-fans-walk-reverse-v1");
            Texture2D passingAtlas = Resources.Load<Texture2D>("street-ferro-fans-walk-passing-v1");
            Texture2D downAtlas = Resources.Load<Texture2D>("street-ferro-fans-walk-down-v1");
            Texture2D downReverseAtlas = Resources.Load<Texture2D>("street-ferro-fans-walk-down-reverse-v1");
            Texture2D downPassingAtlas = Resources.Load<Texture2D>("street-ferro-fans-walk-down-passing-v1");
            Assert.IsNotNull(baseAtlas);
            Assert.IsNotNull(reverseAtlas);
            Assert.IsNotNull(passingAtlas);
            Assert.IsNotNull(downAtlas);
            Assert.IsNotNull(downReverseAtlas);
            Assert.IsNotNull(downPassingAtlas);
            Assert.AreEqual(baseAtlas.width, reverseAtlas.width);
            Assert.AreEqual(baseAtlas.height, reverseAtlas.height);
            Assert.AreEqual(baseAtlas.width, passingAtlas.width);
            Assert.AreEqual(baseAtlas.height, passingAtlas.height);
            Assert.AreEqual(baseAtlas.width, downAtlas.width);
            Assert.AreEqual(baseAtlas.height, downAtlas.height);
            Assert.AreEqual(baseAtlas.width, downReverseAtlas.width);
            Assert.AreEqual(baseAtlas.height, downReverseAtlas.height);
            Assert.AreEqual(baseAtlas.width, downPassingAtlas.width);
            Assert.AreEqual(baseAtlas.height, downPassingAtlas.height);
        }

        [Test]
        public void CamionerosFansUseDownwardGaitOnlyWhenTheirRouteFacesDown()
        {
            Type presentation = RuntimeType("TowerDefenseWorldPresentation");
            MethodInfo method = presentation.GetMethod("IsWalkingDownwards", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method);
            Assert.IsTrue((bool)method.Invoke(null, new object[] { Vector2.down }));
            Assert.IsFalse((bool)method.Invoke(null, new object[] { Vector2.left }));
            Assert.IsFalse((bool)method.Invoke(null, new object[] { Vector2.right }));
            Assert.IsFalse((bool)method.Invoke(null, new object[] { Vector2.up }));
            Assert.IsFalse((bool)method.Invoke(null, new object[] { new Vector2(.8f, -.6f).normalized }),
                "The front-facing set is reserved for a predominantly vertical descent.");
        }

        [TestCase(960f)]
        [TestCase(1200f)]
        public void TwoLargeCampaignHireCardsReplaceAllPrimaryControls(float height)
        {
            Rect[] primary = InvokeBounds("TowerDefensePrimaryActionBounds", height);
            Rect[] hires = InvokeBounds("TowerDefenseHireBounds", height);

            Assert.AreEqual(0, primary.Length, "No speed, pause or back buttons are exposed in the TD footer.");
            Assert.AreEqual(2, hires.Length, "Only Parrillero and Cocacolero hire actions remain.");
            Type viewType = RuntimeType("StreetView");
            Array roles = (Array)viewType.GetField("TowerDefenseHireRoles", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            Assert.AreEqual(2, roles.Length);
            Assert.AreEqual("Parrillero", roles.GetValue(0).ToString());
            Assert.AreEqual("Cocacolero", roles.GetValue(1).ToString());
            foreach (Rect card in hires) { Assert.AreEqual(234f, card.width); Assert.AreEqual(120f, card.height); }
            float footerTop = height - LayoutConstant("FooterReserve");
            for (int i = 0; i < primary.Length; i++)
            {
                Assert.GreaterOrEqual(primary[i].yMin, footerTop);
                Assert.LessOrEqual(primary[i].yMax, height);
                for (int j = i + 1; j < primary.Length; j++) Assert.IsFalse(primary[i].Overlaps(primary[j]));
            }
            for (int i = 0; i < hires.Length; i++)
            {
                Assert.GreaterOrEqual(hires[i].yMin, footerTop);
                Assert.LessOrEqual(hires[i].yMax + 18f, height, "Worker-count text remains inside the safe canvas.");
                for (int j = i + 1; j < hires.Length; j++) Assert.IsFalse(hires[i].Overlaps(hires[j]));
            }

            Assert.AreEqual(7810, ActionId("TowerDefenseSpeedAction"));
            Assert.AreEqual(7811, ActionId("TowerDefensePauseAction"));
            Assert.AreEqual(7812, ActionId("TowerDefenseExitAction"));
            Assert.AreEqual(7820, ActionId("TowerDefenseHireFirstAction"));
            Assert.AreEqual(7821, ActionId("TowerDefenseHireLastAction"));
        }

        [Test]
        public void BothWorkZonesUsePreparedChoriTableAndCocaBarrelFromTheStart()
        {
            Type presentation = RuntimeType("TowerDefenseWorldPresentation");
            MethodInfo resources = presentation.GetMethod("StartingStationResources", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(resources);
            string[] names = (string[])resources.Invoke(null, new object[] {new[] {0, 1, 2, 3, 4, 5, 6}});
            CollectionAssert.AreEqual(new[] {"Station_NormalTable", "Station_CocaBarrel"}, names);
            Assert.IsNotNull(Resources.Load<GameObject>("ModularKitchen/Prefabs/Station_NormalTable"));
            Assert.IsNotNull(Resources.Load<GameObject>("ModularKitchen/Prefabs/Sandwich_Chori"));
            Assert.IsNotNull(Resources.Load<GameObject>("ModularKitchen/Prefabs/Drink_Coca"));
            Assert.IsNotNull(Resources.Load<GameObject>("ModularKitchen/Prefabs/Station_CocaBarrel"));
        }

        [Test]
        public void ThreeRunPlayfieldIsReservedBetweenMuralAndFooter()
        {
            const float logicalHeight = 960f;
            float routeTop = LayoutConstant("RouteTopReserve");
            float footer = LayoutConstant("FooterReserve");
            Assert.Greater(routeTop, 144f,
                "The route begins below the existing top HUD/product counters.");
            Assert.Less(routeTop, logicalHeight - footer,
                "There is a positive playfield between the mural and footer.");
            Assert.AreEqual(240f, footer,
                "The compact footer has room for club/location identity and existing controls without squeezing the route.");
            Assert.AreEqual(24f, LayoutConstant("VisualStageLift"),
                "Only the presentation is raised to clear the footer; the simulated route remains unchanged.");
            Assert.AreEqual(240f, LayoutConstant("RouteTopReserve"),
                "The route starts immediately below the far sidewalk, preserving room for three broad lanes.");
            Assert.AreEqual(96f, LayoutConstant("CorridorWidthLogical"));
            Assert.That(LayoutConstant("CorridorWidthLogical") * .8f, Is.EqualTo(76.8f).Within(.001f));
        }

        [Test]
        public void PortraitCampaignViewportCreatesWidePassableFansAndServiceGaps()
        {
            const float expectedCorridor = 96f;
            Type configType = RuntimeType("TowerDefenseLevelConfig");
            Type routeType = RuntimeType("TowerDefenseRouteGeometryConfig");
            Type layoutType = RuntimeType("TowerDefenseResponsiveLayout");
            Type geometryType = RuntimeType("TowerDefenseRouteGeometry");
            ScriptableObject source = ScriptableObject.CreateInstance(configType);
            var cameraObject = new GameObject("TowerDefense route layout test camera");
            var target = new RenderTexture(540, 960, 24);
            ScriptableObject runtime = null;
            object mode = null;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.targetTexture = target;
            try
            {
                var arguments = new object[] { source, camera, new Rect(0f, 0f, 540f, 960f),
                    540f, 960f, null, null, 0f, Vector2.zero, Vector2.zero, null };
                MethodInfo create = layoutType.GetMethod("TryCreate", BindingFlags.Public | BindingFlags.Static);
                Assert.IsNotNull(create);
                Assert.IsTrue((bool)create.Invoke(null, arguments), arguments[10] as string);
                runtime = (ScriptableObject)arguments[5];
                object route = arguments[6];
                float worldPerPixel = (float)arguments[7];

                Assert.That((float)routeType.GetField("CorridorWidth").GetValue(route) / worldPerPixel,
                    Is.EqualTo(expectedCorridor).Within(.1f), "Vertical turns retain their responsive corridor width.");
                Assert.That((float)routeType.GetField("HorizontalCorridorWidth").GetValue(route) / worldPerPixel,
                    Is.EqualTo(expectedCorridor * .8f).Within(.1f), "Horizontal lanes are 20% narrower.");
                Assert.That((float)routeType.GetField("RowSpacing").GetValue(route)
                    / worldPerPixel - (float)routeType.GetField("CorridorWidth").GetValue(route) / worldPerPixel,
                    Is.GreaterThanOrEqualTo(60f), "The spaces between routes remain available to existing workers/stations.");
                object generated = geometryType.GetMethod("Generate", BindingFlags.Public | BindingFlags.Static)
                    .Invoke(null, new[] { route });
                Assert.IsTrue((bool)generated.GetType().GetProperty("IsValid").GetValue(generated, null));
                Vector2[] centerline = (Vector2[])generated.GetType().GetProperty("Centerline").GetValue(generated, null);
                Vector2[] left = (Vector2[])generated.GetType().GetProperty("LeftBoundary").GetValue(generated, null);
                Vector2[] right = (Vector2[])generated.GetType().GetProperty("RightBoundary").GetValue(generated, null);
                float entryExtension = (float)routeType.GetField("FanEntryExtensionWorldUnits").GetValue(route);
                Assert.That(entryExtension / worldPerPixel, Is.EqualTo(109f).Within(.1f),
                    "The follower-only entry extension starts the full 78 px fan frame outside the left viewport without widening the fenced route.");
                ScriptableObject redondosSource = ScriptableObject.CreateInstance(configType);
                ScriptableObject redondosRuntime = null;
                try
                {
                    configType.GetField("sourceCampaignLevelId", BindingFlags.Instance | BindingFlags.NonPublic)
                        .SetValue(redondosSource, Convert.ToInt32(configType
                            .GetField("LosRedondosCampaignLevelId", BindingFlags.Public | BindingFlags.Static)
                            .GetRawConstantValue()));
                    object[] redondosArguments = (object[])arguments.Clone();
                    redondosArguments[0] = redondosSource;
                    Assert.IsTrue((bool)create.Invoke(null, redondosArguments), redondosArguments[10] as string);
                    redondosRuntime = (ScriptableObject)redondosArguments[5];
                    object redondosRoute = redondosArguments[6];
                    float redondosEntryExtension = (float)routeType.GetField("FanEntryExtensionWorldUnits")
                        .GetValue(redondosRoute);
                    Assert.That(redondosEntryExtension / worldPerPixel, Is.EqualTo(31f).Within(.1f),
                        "Los Redondos keeps its existing full-frame-at-edge entry.");
                }
                finally
                {
                    if (redondosRuntime != null) UnityEngine.Object.DestroyImmediate(redondosRuntime);
                    UnityEngine.Object.DestroyImmediate(redondosSource);
                }

                Type modeType = RuntimeType("TowerDefenseGameMode");
                object balance = Activator.CreateInstance(RuntimeType("StreetBalance"));
                mode = Activator.CreateInstance(modeType, new object[] {runtime, balance, route, worldPerPixel,
                    arguments[8], arguments[9]});
                modeType.GetMethod("Begin", BindingFlags.Public | BindingFlags.Instance).Invoke(mode, null);
                Assert.IsTrue((bool)modeType.GetMethod("TrySpawnFan", BindingFlags.Public | BindingFlags.Instance)
                    .Invoke(mode, new object[] {new[] {0}, new[] {1}}));
                var fans = (System.Collections.IList)modeType.GetProperty("Fans").GetValue(mode, null);
                object fan = fans[0];
                object follower = fan.GetType().GetProperty("RouteFollower").GetValue(fan, null);
                Vector2 spawnPosition = (Vector2)follower.GetType().GetProperty("Position").GetValue(follower, null);
                float halfFanWidth = (float)layoutType.GetField("FanFrameWidthLogical").GetRawConstantValue() * .5f;
                Vector3 spawnScreenPoint = camera.WorldToScreenPoint(spawnPosition);
                Assert.That(spawnScreenPoint.x + halfFanWidth, Is.EqualTo(0f).Within(.1f),
                    "The first fan starts fully outside screen left; only the leading foot/edge can appear as it walks in.");
                float followerSpeed = (float)follower.GetType().GetProperty("Speed").GetValue(follower, null);
                const float revealAdvanceLogical = 18f;
                follower.GetType().GetMethod("Advance").Invoke(follower,
                    new object[] {revealAdvanceLogical * worldPerPixel / followerSpeed});
                Vector2 revealPosition = (Vector2)follower.GetType().GetProperty("Position").GetValue(follower, null);
                Vector3 revealScreenPoint = camera.WorldToScreenPoint(revealPosition);
                Assert.That(revealScreenPoint.x + halfFanWidth, Is.EqualTo(revealAdvanceLogical).Within(.1f),
                    "After an 18 px stride, only the matching leading slice of the fan frame is inside the viewport.");
                float fullFrameWidthLogical = halfFanWidth * 2f;
                Assert.Less(revealScreenPoint.x + halfFanWidth, fullFrameWidthLogical,
                    "The body is still progressively entering rather than appearing all at once.");
                follower.GetType().GetMethod("Advance").Invoke(follower,
                    new object[] {(fullFrameWidthLogical - revealAdvanceLogical) * worldPerPixel / followerSpeed});
                Vector2 fullFramePosition = (Vector2)follower.GetType().GetProperty("Position").GetValue(follower, null);
                Vector3 fullFrameScreenPoint = camera.WorldToScreenPoint(fullFramePosition);
                Assert.That(fullFrameScreenPoint.x - halfFanWidth, Is.EqualTo(0f).Within(.1f),
                    "After one full frame-width of walking, the fan's trailing edge reaches the viewport.");
                Assert.That(fullFrameScreenPoint.x + halfFanWidth, Is.EqualTo(fullFrameWidthLogical).Within(.1f),
                    "The full walking sprite becomes visible only after the progressive entrance completes.");
                Assert.Greater(centerline[1].x, centerline[0].x,
                    "The route still starts by moving from left to right.");
                Assert.Greater(centerline[centerline.Length - 1].x, centerline[centerline.Length - 2].x,
                    "The route still finishes at the right-hand exit.");
                follower.GetType().GetMethod("Advance").Invoke(follower,
                    new object[] {Mathf.Max(0f, entryExtension - fullFrameWidthLogical * worldPerPixel) / followerSpeed});
                Vector2 routeJoinPosition = (Vector2)follower.GetType().GetProperty("Position").GetValue(follower, null);
                Assert.That(Vector2.Distance(routeJoinPosition, centerline[0]) / worldPerPixel, Is.LessThan(.1f),
                    "After entering from screen left, the fan joins the original S route without moving any turn.");
                MethodInfo isAdjacentToArea = modeType.GetMethod("IsAdjacentToArea", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsTrue((bool)isAdjacentToArea.Invoke(mode, new[] {fan, (object)0}),
                    "The entry extension is excluded from service-area progress, preserving the original zone boundary.");
                Assert.That(Vector2.Distance(left[0], right[0]) / worldPerPixel, Is.EqualTo(76.8f).Within(.1f));
                int turnSegments = (int)routeType.GetField("TurnSegments").GetValue(route);
                for (int row = 0; row < (int)routeType.GetField("RowCount").GetValue(route) - 1; row++)
                {
                    int rowStart = row * (turnSegments + 1);
                    for (int sample = 1; sample <= turnSegments; sample++)
                    {
                        int k = rowStart + 1 + sample;
                        Assert.Less(left[k].y, left[k - 1].y, "Left turn rail must never double back.");
                        Assert.Less(right[k].y, right[k - 1].y, "Right turn rail must never double back.");
                    }
                    float connectorX = centerline[rowStart + 1].x;
                    for (int sample = 1; sample <= turnSegments; sample++)
                        Assert.That(centerline[rowStart + 1 + sample].x, Is.EqualTo(connectorX).Within(.001f),
                            "Responsive connectors stay straight instead of bulging into rounded turns.");
                }
            }
            finally
            {
                if (mode != null) mode.GetType().GetMethod("Stop", BindingFlags.Public | BindingFlags.Instance).Invoke(mode, null);
                if (runtime != null) UnityEngine.Object.DestroyImmediate(runtime);
                UnityEngine.Object.DestroyImmediate(source);
                camera.targetTexture = null;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Rect[] InvokeBounds(string name, float height)
        {
            Type streetViewType = Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true);
            MethodInfo method = streetViewType.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "Expected shared draw/hit geometry method " + name + ".");
            return (Rect[])method.Invoke(null, new object[] { height });
        }

        private static int ActionId(string name)
        {
            Type streetViewType = Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true);
            FieldInfo field = streetViewType.GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(field, "Existing Tower Defense action id should remain stable: " + name);
            return (int)field.GetRawConstantValue();
        }

        private static float LayoutConstant(string name)
        {
            Type layoutType = Type.GetType("HayChoriYPaty.TowerDefenseResponsiveLayout, Assembly-CSharp", true);
            FieldInfo field = layoutType.GetField(name, BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(field, "Expected Tower Defense layout constant " + name + ".");
            return (float)field.GetRawConstantValue();
        }

        private static Type RuntimeType(string shortName)
        {
            return Type.GetType("HayChoriYPaty." + shortName + ", Assembly-CSharp", true);
        }

        private static object Read(object instance, string propertyName)
        {
            Assert.IsNotNull(instance);
            PropertyInfo property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property != null) return property.GetValue(instance, null);
            FieldInfo field = instance.GetType().GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(field, "Expected runtime property or field " + propertyName + ".");
            return field.GetValue(instance);
        }
    }
}
