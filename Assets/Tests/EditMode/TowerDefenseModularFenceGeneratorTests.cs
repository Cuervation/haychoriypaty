using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    public sealed class TowerDefenseModularFenceGeneratorTests
    {
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        [TestCase(.01f)]
        [TestCase(.02f)]
        public void PanelSizeTracksLogicalViewportAndOverlapsTransparentMargins(float worldPerPixel)
        {
            Type generatorType = Type.GetType("HayChoriYPaty.TowerDefenseModularFenceGenerator, Assembly-CSharp", true);
            var owner = new GameObject("TowerDefense responsive fence sizing test");
            try
            {
                Component generator = owner.AddComponent(generatorType);
                generatorType.GetMethod("SetLogicalPanelSize", PublicInstance).Invoke(generator, new object[] { worldPerPixel });
                float length = (float)generatorType.GetField("panelWorldLength", PrivateInstance).GetValue(generator);
                float shortLength = (float)generatorType.GetField("shortPanelWorldLength", PrivateInstance).GetValue(generator);
                float overlap = (float)generatorType.GetField("panelOverlap", PrivateInstance).GetValue(generator);
                Assert.That(length / worldPerPixel, Is.EqualTo(120f).Within(.001f));
                Assert.That(shortLength / worldPerPixel, Is.EqualTo(60f).Within(.001f));
                Assert.That(overlap / length, Is.EqualTo(.42f).Within(.001f));
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }

        [Test]
        public void GeneratesIndependentFenceSpritesAndTwoContinuousColliderRails()
        {
            Type configType = Type.GetType("HayChoriYPaty.TowerDefenseLevelConfig, Assembly-CSharp", true);
            Type generatorType = Type.GetType("HayChoriYPaty.TowerDefenseModularFenceGenerator, Assembly-CSharp", true);
            var config = ScriptableObject.CreateInstance(configType);
            var owner = new GameObject("TowerDefense fence test");
            var texture = new Texture2D(128, 48, TextureFormat.RGBA32, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 128f, 48f), new Vector2(.5f, .5f), 100f);

            try
            {
                Component generator = owner.AddComponent(generatorType);
                Set(generator, "levelConfig", config);
                Set(generator, "fencePanelSprite", sprite);
                generatorType.GetProperty("SortingOrder", PublicInstance).SetValue(generator, 32);
                generatorType.GetMethod("SetLogicalPanelSize", PublicInstance).Invoke(generator, new object[] { .01f });
                bool generated = (bool)generatorType.GetMethod("Generate", PublicInstance).Invoke(generator, null);
                Assert.IsTrue(generated, Get<string>(generator, "LatestError"));
                Assert.Greater(Get<int>(generator, "GeneratedPieceCount"), 10);

                Transform generatedRoot = owner.transform.GetChild(0);
                SpriteRenderer[] pieces = generatedRoot.GetComponentsInChildren<SpriteRenderer>(true);
                Assert.Greater(pieces.Length, 10, "Each fence module should remain a separate SpriteRenderer object.");
                int straightCount = 0, shortStraightCount = 0, turnCount = 0, terminalCount = 0;
                float longFencePanelScale = -1f, shortFencePanelScale = -1f;
                foreach (SpriteRenderer piece in pieces)
                {
                    Assert.IsNotNull(piece.GetComponent<BoxCollider2D>(), "Every visible fence segment has a physical collider.");
                    Assert.That(piece.GetComponent<BoxCollider2D>().size.y * piece.transform.localScale.y,
                        Is.EqualTo(.12f).Within(.0001f), "Visual resizing must not thicken the collider.");
                    int depthOrder = 32 + Mathf.RoundToInt(-piece.transform.position.y * 12f);
                    Component metadata = piece.GetComponent(Type.GetType("HayChoriYPaty.TowerDefenseFencePiece, Assembly-CSharp", true));
                    Assert.IsNotNull(metadata);
                    object kind = metadata.GetType().GetProperty("Kind", PublicInstance).GetValue(metadata, null);
                    string label = kind.ToString();
                    if (label == "Straight" || label == "ShortStraight" || label == "Terminal")
                    {
                        Assert.AreEqual(depthOrder, piece.sortingOrder,
                            "Fence panels must depth-sort with the supporters at their world Y.");
                        Vector3 axis = piece.transform.right;
                        Assert.IsTrue(Mathf.Abs(Mathf.Abs(axis.x) - 1f) < .001f ||
                            Mathf.Abs(Mathf.Abs(axis.y) - 1f) < .001f,
                            "Fence panels may align only to horizontal or vertical straight runs.");
                        Vector3 expectedAxis = Mathf.Abs(axis.x) >= Mathf.Abs(axis.y)
                            ? Vector3.right
                            : Vector3.up;
                        Assert.That(Vector3.Dot(axis.normalized, expectedAxis), Is.GreaterThan(.999f),
                            "Fence art must keep one screen-facing orientation; reversing the route must not turn panels upside down or swap their supports.");
                        if (label == "ShortStraight")
                        {
                            if (shortFencePanelScale < 0f) shortFencePanelScale = piece.transform.localScale.x;
                            Assert.That(piece.transform.localScale.x, Is.EqualTo(shortFencePanelScale).Within(.0001f));
                            Assert.That(Mathf.Abs(axis.x), Is.LessThan(.001f),
                                "The short modules are reserved for vertical connectors in the default route.");
                        }
                        else
                        {
                            if (longFencePanelScale < 0f) longFencePanelScale = piece.transform.localScale.x;
                            Assert.That(piece.transform.localScale.x, Is.EqualTo(longFencePanelScale).Within(.0001f),
                                "Long horizontal and terminal modules retain one consistent scale.");
                        }
                    }
                    if (label == "Straight") straightCount++;
                    else if (label == "ShortStraight") shortStraightCount++;
                    else if (label == "Turn") turnCount++;
                    else if (label == "Terminal") terminalCount++;
                }
                Assert.Greater(straightCount, 0);
                Assert.Greater(shortStraightCount, 0, "Short modules are used on route legs where long panels are unnecessary.");
                Assert.That(shortFencePanelScale, Is.EqualTo(longFencePanelScale * .5f).Within(.0001f));
                Assert.AreEqual(0, turnCount, "No curved/turn-specific fence panels are generated.");
                Assert.AreEqual(4, terminalCount, "Each rail has independent entry and exit terminators.");

                EdgeCollider2D[] rails = generatedRoot.GetComponentsInChildren<EdgeCollider2D>(true);
                Assert.AreEqual(2, rails.Length);
                Assert.Greater(rails[0].pointCount, 2);
                Assert.Greater(rails[1].pointCount, 2);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void FencePanelsStayOnTheirOwnStraightLegAndShrinkForShortTurnGaps()
        {
            Type configType = Type.GetType("HayChoriYPaty.TowerDefenseLevelConfig, Assembly-CSharp", true);
            Type routeType = Type.GetType("HayChoriYPaty.TowerDefenseRouteGeometryConfig, Assembly-CSharp", true);
            Type generatorType = Type.GetType("HayChoriYPaty.TowerDefenseModularFenceGenerator, Assembly-CSharp", true);
            Type pieceType = Type.GetType("HayChoriYPaty.TowerDefenseFencePiece, Assembly-CSharp", true);
            var config = ScriptableObject.CreateInstance(configType);
            var owner = new GameObject("TowerDefense complete passage fence test");
            var texture = new Texture2D(128, 48, TextureFormat.RGBA32, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 128f, 48f), new Vector2(.5f, .5f), 100f);
            try
            {
                object route = Activator.CreateInstance(routeType);
                routeType.GetField("RowCount", PublicInstance).SetValue(route, 3);
                routeType.GetField("SegmentLengths", PublicInstance).SetValue(route, new[] { 2f, 2f, 2f });
                routeType.GetField("CorridorWidth", PublicInstance).SetValue(route, 1f);
                routeType.GetField("RowSpacing", PublicInstance).SetValue(route, 1.25f);
                routeType.GetField("TurnSegments", PublicInstance).SetValue(route, 4);
                routeType.GetField("PlayableBounds", PublicInstance).SetValue(route, new Rect(-3f, -2f, 6f, 4f));
                configType.GetMethod("ConfigureRoute", PublicInstance).Invoke(config, new[] { route });

                Component generator = owner.AddComponent(generatorType);
                Set(generator, "levelConfig", config);
                Set(generator, "fencePanelSprite", sprite);
                generatorType.GetMethod("SetLogicalPanelSize", PublicInstance).Invoke(generator, new object[] { .01f });
                Assert.IsTrue((bool)generatorType.GetMethod("Generate", PublicInstance).Invoke(generator, null),
                    Get<string>(generator, "LatestError"));

                object geometry = generatorType.GetProperty("LatestGeometry", PublicInstance).GetValue(generator, null);
                Vector2[][] boundaries =
                {
                    (Vector2[])geometry.GetType().GetProperty("LeftBoundary", PublicInstance).GetValue(geometry, null),
                    (Vector2[])geometry.GetType().GetProperty("RightBoundary", PublicInstance).GetValue(geometry, null)
                };
                SpriteRenderer[] panels = owner.transform.GetChild(0).GetComponentsInChildren<SpriteRenderer>(true);
                int shortPanels = 0;
                float shortestTurnSpan = float.PositiveInfinity;
                foreach (SpriteRenderer panel in panels)
                {
                    Component metadata = panel.GetComponent(pieceType);
                    string kind = metadata.GetType().GetProperty("Kind", PublicInstance).GetValue(metadata, null).ToString();
                    if (kind != "Straight" && kind != "ShortStraight") continue;

                    int railIndex = (int)metadata.GetType().GetProperty("RailIndex", PublicInstance).GetValue(metadata, null);
                    float halfLength = panel.GetComponent<BoxCollider2D>().size.x * panel.transform.lossyScale.x * .5f;
                    Vector2 center = panel.transform.position;
                    Vector2 axis = panel.transform.right.normalized;
                    bool fitsOneLeg = false;
                    foreach (Vector2[] leg in CollapseStraightRuns(boundaries[railIndex]))
                    {
                        Vector2 delta = leg[1] - leg[0];
                        float length = delta.magnitude;
                        Vector2 tangent = delta / length;
                        if (Mathf.Abs(Vector2.Dot(axis, tangent)) < .999f) continue;
                        if (Mathf.Abs(Cross(center - leg[0], tangent)) > .0001f) continue;
                        float along = Vector2.Dot(center - leg[0], tangent);
                        if (along - halfLength >= -.0001f && along + halfLength <= length + .0001f)
                        {
                            fitsOneLeg = true;
                            if (kind == "ShortStraight") shortestTurnSpan = Mathf.Min(shortestTurnSpan, length);
                            break;
                        }
                    }
                    Assert.IsTrue(fitsOneLeg,
                        "A fence panel must remain within one straight boundary leg; it cannot spill across a corner into the passage.");
                    if (kind == "ShortStraight") shortPanels++;
                }

                Assert.Greater(shortPanels, 0);
                Assert.That(shortestTurnSpan, Is.EqualTo(.25f).Within(.001f),
                    "The test route has a turn gap shorter than the standard 60-logical-pixel module.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void VisibleFenceEntryAndExitReachConfiguredScreenEdgesWithoutChangingRouteColliders()
        {
            Type configType = Type.GetType("HayChoriYPaty.TowerDefenseLevelConfig, Assembly-CSharp", true);
            Type generatorType = Type.GetType("HayChoriYPaty.TowerDefenseModularFenceGenerator, Assembly-CSharp", true);
            Type pieceType = Type.GetType("HayChoriYPaty.TowerDefenseFencePiece, Assembly-CSharp", true);
            var config = ScriptableObject.CreateInstance(configType);
            var owner = new GameObject("TowerDefense screen-edge fence test");
            var texture = new Texture2D(128, 48, TextureFormat.RGBA32, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 128f, 48f), new Vector2(.5f, .5f), 100f);

            try
            {
                Component generator = owner.AddComponent(generatorType);
                Set(generator, "levelConfig", config);
                Set(generator, "fencePanelSprite", sprite);
                Assert.IsTrue((bool)generatorType.GetMethod("Generate", PublicInstance).Invoke(generator, null),
                    Get<string>(generator, "LatestError"));

                object geometry = generatorType.GetProperty("LatestGeometry", PublicInstance).GetValue(generator, null);
                Vector2[] originalLeft = (Vector2[])geometry.GetType().GetProperty("LeftBoundary").GetValue(geometry, null);
                Vector2[] originalRight = (Vector2[])geometry.GetType().GetProperty("RightBoundary").GetValue(geometry, null);
                // Deliberately put viewport edges inside the authored route bounds:
                // visible art must snap to them, not merely extend from inside.
                float screenLeft = Mathf.Min(originalLeft[0].x, originalRight[0].x) + .25f;
                float screenRight = Mathf.Max(originalLeft[originalLeft.Length - 1].x,
                    originalRight[originalRight.Length - 1].x) - .25f;
                Assert.IsTrue((bool)generatorType.GetMethod("SetVisualHorizontalBounds", PublicInstance)
                    .Invoke(generator, new object[] { screenLeft, screenRight }));
                Assert.IsTrue((bool)generatorType.GetMethod("Generate", PublicInstance).Invoke(generator, null),
                    Get<string>(generator, "LatestError"));

                Transform generatedRoot = owner.transform.GetChild(0);
                SpriteRenderer[] pieces = generatedRoot.GetComponentsInChildren<SpriteRenderer>(true);
                int leftTerminals = 0, rightTerminals = 0;
                foreach (SpriteRenderer piece in pieces)
                {
                    Component metadata = piece.GetComponent(pieceType);
                    if (metadata == null || metadata.GetType().GetProperty("Kind").GetValue(metadata, null).ToString() != "Terminal")
                        continue;
                    if (Mathf.Abs(piece.transform.position.x - screenLeft) < .0001f) leftTerminals++;
                    if (Mathf.Abs(piece.transform.position.x - screenRight) < .0001f) rightTerminals++;
                }
                Assert.AreEqual(2, leftTerminals, "Both modular rails begin at the left screen edge.");
                Assert.AreEqual(2, rightTerminals, "Both modular rails end at the right screen edge.");

                EdgeCollider2D[] rails = generatedRoot.GetComponentsInChildren<EdgeCollider2D>(true);
                Assert.AreEqual(2, rails.Length);
                Assert.That(rails[0].points[0].x, Is.EqualTo(originalLeft[0].x).Within(.0001f));
                Assert.That(rails[0].points[rails[0].pointCount - 1].x,
                    Is.EqualTo(originalLeft[originalLeft.Length - 1].x).Within(.0001f));
                Assert.That(rails[1].points[0].x, Is.EqualTo(originalRight[0].x).Within(.0001f));
                Assert.That(rails[1].points[rails[1].pointCount - 1].x,
                    Is.EqualTo(originalRight[originalRight.Length - 1].x).Within(.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void InvalidRouteDoesNotCreateAnyFencePieces()
        {
            Type configType = Type.GetType("HayChoriYPaty.TowerDefenseLevelConfig, Assembly-CSharp", true);
            Type routeType = Type.GetType("HayChoriYPaty.TowerDefenseRouteGeometryConfig, Assembly-CSharp", true);
            Type generatorType = Type.GetType("HayChoriYPaty.TowerDefenseModularFenceGenerator, Assembly-CSharp", true);
            var config = ScriptableObject.CreateInstance(configType);
            object route = Activator.CreateInstance(routeType);
            routeType.GetField("PlayableBounds", PublicInstance).SetValue(route, new Rect(-1f, -1f, 2f, 2f));
            configType.GetField("route", PrivateInstance).SetValue(config, route);
            var owner = new GameObject("Invalid TowerDefense fence test");
            var texture = new Texture2D(128, 48, TextureFormat.RGBA32, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 128f, 48f), new Vector2(.5f, .5f), 100f);

            try
            {
                Component generator = owner.AddComponent(generatorType);
                Set(generator, "levelConfig", config);
                Set(generator, "fencePanelSprite", sprite);
                bool generated = (bool)generatorType.GetMethod("Generate", PublicInstance).Invoke(generator, null);
                Assert.IsFalse(generated);
                Assert.That(Get<string>(generator, "LatestError"), Does.Contain("PlayableBounds"));
                Assert.AreEqual(0, Get<int>(generator, "GeneratedPieceCount"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        private static void Set(Component component, string name, object value)
        {
            FieldInfo field = component.GetType().GetField(name, PrivateInstance);
            Assert.IsNotNull(field, "Expected serialized field " + name + ".");
            field.SetValue(component, value);
        }

        private static T Get<T>(Component component, string name)
        {
            PropertyInfo property = component.GetType().GetProperty(name, PublicInstance);
            Assert.IsNotNull(property, "Expected property " + name + ".");
            return (T)property.GetValue(component, null);
        }

        private static System.Collections.Generic.List<Vector2[]> CollapseStraightRuns(Vector2[] points)
        {
            var runs = new System.Collections.Generic.List<Vector2[]>();
            if (points == null || points.Length < 2) return runs;
            var corners = new System.Collections.Generic.List<Vector2> { points[0] };
            Vector2 direction = Vector2.zero;
            for (int i = 1; i < points.Length; i++)
            {
                Vector2 delta = points[i] - points[i - 1];
                if (delta.sqrMagnitude <= .000001f) continue;
                Vector2 nextDirection = delta.normalized;
                if (direction.sqrMagnitude > .000001f &&
                    (Mathf.Abs(Cross(direction, nextDirection)) > .001f || Vector2.Dot(direction, nextDirection) < .999f))
                {
                    Vector2 corner = points[i - 1];
                    if ((corner - corners[corners.Count - 1]).sqrMagnitude > .000001f)
                        corners.Add(corner);
                }
                direction = nextDirection;
            }
            Vector2 end = points[points.Length - 1];
            if ((end - corners[corners.Count - 1]).sqrMagnitude > .000001f)
                corners.Add(end);
            for (int i = 1; i < corners.Count; i++) runs.Add(new[] { corners[i - 1], corners[i] });
            return runs;
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
    }
}
