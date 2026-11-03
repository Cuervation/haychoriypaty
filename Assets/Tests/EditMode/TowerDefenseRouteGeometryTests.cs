using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    /// <summary>
    /// The EditMode assembly is isolated from the predefined Assembly-CSharp runtime
    /// assembly, so these focused tests use reflection rather than adding an assembly
    /// reference that would alter the existing project structure.
    /// </summary>
    public sealed class TowerDefenseRouteGeometryTests
    {
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        private Type configType;
        private Type geometryType;

        [SetUp]
        public void SetUp()
        {
            configType = Type.GetType("HayChoriYPaty.TowerDefenseRouteGeometryConfig, Assembly-CSharp", true);
            geometryType = Type.GetType("HayChoriYPaty.TowerDefenseRouteGeometry, Assembly-CSharp", true);
        }

        [TestCase(3)]
        [TestCase(5)]
        [TestCase(7)]
        [TestCase(9)]
        public void GeneratesOrderedRowsWithAlternatingDirections(int rowCount)
        {
            float[] lengths = MakeLengths(rowCount, 8f);
            object result = Generate(MakeConfig(rowCount, lengths, 2f, 1f, new Rect(-20f, -10f, 40f, 20f), 12));
            AssertValid(result);

            Vector2[] centerline = Get<Vector2[]>(result, "Centerline");
            int startIndex = 0;
            float firstY = (rowCount - 1) * 1f;
            for (int row = 0; row < rowCount; row++)
            {
                Vector2 start = centerline[startIndex];
                Vector2 end = centerline[startIndex + 1];
                float expectedDirection = row % 2 == 0 ? 1f : -1f;
                Assert.That(end.x - start.x, Is.EqualTo(expectedDirection * lengths[row]).Within(0.001f), "row " + row + " horizontal direction/length");
                Assert.That(start.y, Is.EqualTo(firstY - row * 2f).Within(0.001f), "row " + row + " descends from top to bottom");
                Assert.That(end.y, Is.EqualTo(start.y).Within(0.001f), "row " + row + " remains horizontal");

                if (row < rowCount - 1)
                    startIndex += 1 + 12;
            }

            Assert.That(centerline[0].y, Is.GreaterThan(centerline[centerline.Length - 1].y), "Entry is on the top row and exit is on the bottom row.");
            Assert.That(centerline[centerline.Length - 1].x, Is.GreaterThan(centerline[0].x), "For the supported odd row counts, the route exits to the right of its top-left entry.");
        }

        [Test]
        public void ProducesSeparateFenceRailsAtConfiguredCorridorWidth()
        {
            object result = Generate(MakeConfig(3, new[] { 8f, 8f, 8f }, 4f, 2f, new Rect(-20f, -10f, 40f, 20f), 12));
            AssertValid(result);

            Vector2[] centerline = Get<Vector2[]>(result, "Centerline");
            Vector2[] left = Get<Vector2[]>(result, "LeftBoundary");
            Vector2[] right = Get<Vector2[]>(result, "RightBoundary");
            Assert.AreEqual(centerline.Length, left.Length);
            Assert.AreEqual(centerline.Length, right.Length);
            Assert.AreNotSame(left, right, "Each fence rail has its own polyline array.");
            Assert.That(Vector2.Distance(centerline[0], left[0]), Is.EqualTo(1f).Within(0.001f));
            Assert.That(Vector2.Distance(centerline[0], right[0]), Is.EqualTo(1f).Within(0.001f));
            Assert.That(left[0].y, Is.GreaterThan(centerline[0].y));
            Assert.That(right[0].y, Is.LessThan(centerline[0].y));
        }

        [Test]
        public void PreservesDifferentHorizontalLengthsPerRow()
        {
            float[] lengths = { 6f, 8f, 5f, 7f, 9f };
            object result = Generate(MakeConfig(5, lengths, 2f, 1f, new Rect(-20f, -10f, 40f, 20f), 12));
            AssertValid(result);

            Vector2[] centerline = Get<Vector2[]>(result, "Centerline");
            int startIndex = 0;
            for (int row = 0; row < lengths.Length; row++)
            {
                Vector2 start = centerline[startIndex];
                Vector2 end = centerline[startIndex + 1];
                Assert.That(Mathf.Abs(end.x - start.x), Is.EqualTo(lengths[row]).Within(0.001f), "row " + row);
                if (row < lengths.Length - 1)
                    startIndex += 1 + 12;
            }
        }

        [Test]
        public void ReportsFenceEnvelopeOutsidePlayableBoundsWithoutShrinkingSegments()
        {
            object result = Generate(MakeConfig(3, new[] { 100f, 80f, 100f }, 2f, 1f, new Rect(-5f, -5f, 10f, 10f), 12));
            object validation = Get<object>(result, "Validation");

            Assert.IsFalse(Get<bool>(validation, "IsValid"));
            string[] errors = Get<string[]>(validation, "Errors");
            Assert.IsTrue(Array.Exists(errors, message => message.IndexOf("PlayableBounds", StringComparison.OrdinalIgnoreCase) >= 0),
                "The failure should identify that the generated envelope exceeds PlayableBounds.");
            Assert.Greater(Get<Vector2[]>(result, "Centerline").Length, 0, "Out-of-bounds geometry remains inspectable.");
            Vector2[] route = Get<Vector2[]>(result, "Centerline");
            Assert.That(Mathf.Abs(route[1].x - route[0].x), Is.EqualTo(100f).Within(0.001f),
                "The configured first segment length is reported unchanged.");
        }

        private object MakeConfig(int rowCount, float[] lengths, float rowSpacing, float corridorWidth, Rect bounds, int turnSegments)
        {
            object config = Activator.CreateInstance(configType);
            Set(config, "RowCount", rowCount);
            Set(config, "SegmentLengths", lengths);
            Set(config, "RowSpacing", rowSpacing);
            Set(config, "CorridorWidth", corridorWidth);
            Set(config, "TurnRadius", Mathf.Min(rowSpacing * 0.48f, Mathf.Max(corridorWidth * 0.6f, corridorWidth)));
            Set(config, "PlayableBounds", bounds);
            Set(config, "TurnSegments", turnSegments);
            return config;
        }

        private object Generate(object config)
        {
            MethodInfo method = geometryType.GetMethod("Generate", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "TowerDefenseRouteGeometry.Generate should be public and static.");
            return method.Invoke(null, new[] { config });
        }

        private static float[] MakeLengths(int count, float length)
        {
            var result = new float[count];
            for (int i = 0; i < result.Length; i++) result[i] = length;
            return result;
        }

        private static void AssertValid(object result)
        {
            object validation = Get<object>(result, "Validation");
            Assert.IsTrue(Get<bool>(validation, "IsValid"), string.Join("; ", Get<string[]>(validation, "Errors")));
        }

        private static void Set(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, PublicInstance);
            Assert.IsNotNull(field, "Expected serialized config field " + fieldName + ".");
            field.SetValue(target, value);
        }

        private static T Get<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, PublicInstance);
            Assert.IsNotNull(property, "Expected public property " + propertyName + ".");
            return (T)property.GetValue(target, null);
        }
    }
}
