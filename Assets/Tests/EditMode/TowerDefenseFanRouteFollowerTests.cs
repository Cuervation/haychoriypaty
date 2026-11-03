using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    public sealed class TowerDefenseFanRouteFollowerTests
    {
        private const float Tolerance = 0.0001f;
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        private Type followerType;

        [SetUp]
        public void SetUp()
        {
            followerType = Type.GetType("HayChoriYPaty.TowerDefenseFanRouteFollower, Assembly-CSharp", true);
        }

        [Test]
        public void HorizontalMovementUsesWorldDistanceAndKeepsFanIdentity()
        {
            object follower = Make(73, new[] { new Vector2(0f, 2f), new Vector2(4f, 2f) }, 2f);

            Advance(follower, 0.25f);

            Assert.AreEqual(73, Get<int>(follower, "FanId"));
            AssertVector(new Vector2(0.5f, 2f), Get<Vector2>(follower, "Position"));
            AssertVector(Vector2.right, Get<Vector2>(follower, "Facing"));
            Assert.That(Get<float>(follower, "Progress"), Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(Get<float>(follower, "TotalDistance"), Is.EqualTo(4f).Within(Tolerance));
            Assert.IsFalse(Get<bool>(follower, "IsComplete"));
            Assert.IsFalse(Get<bool>(follower, "HasExited"));
        }

        [Test]
        public void FacingChangesAtConnectedTurnsWithoutTeleporting()
        {
            object follower = Make(1,
                new[]
                {
                    Vector2.zero,
                    new Vector2(2f, 0f),
                    new Vector2(2f, -1f),
                    new Vector2(0f, -1f)
                }, 1f);

            Advance(follower, 2f);

            AssertVector(new Vector2(2f, 0f), Get<Vector2>(follower, "Position"));
            AssertVector(Vector2.down, Get<Vector2>(follower, "Facing"));
            Assert.That(Get<float>(follower, "Progress"), Is.EqualTo(2f).Within(Tolerance));

            Advance(follower, 1f);

            AssertVector(new Vector2(2f, -1f), Get<Vector2>(follower, "Position"));
            AssertVector(Vector2.left, Get<Vector2>(follower, "Facing"));
            Assert.That(Get<float>(follower, "Progress"), Is.EqualTo(3f).Within(Tolerance));

            Advance(follower, 0.5f);

            AssertVector(new Vector2(1.5f, -1f), Get<Vector2>(follower, "Position"));
            AssertVector(Vector2.left, Get<Vector2>(follower, "Facing"));
            Assert.That(Get<float>(follower, "Progress"), Is.EqualTo(3.5f).Within(Tolerance));
        }

        [Test]
        public void OneLargeStepTraversesSeveralWaypointsContinuously()
        {
            object follower = Make(2,
                new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(2f, 1f)
                }, 2.5f);

            Advance(follower, 1f);

            AssertVector(new Vector2(1.5f, 1f), Get<Vector2>(follower, "Position"));
            AssertVector(Vector2.right, Get<Vector2>(follower, "Facing"));
            Assert.That(Get<float>(follower, "Progress"), Is.EqualTo(2.5f).Within(Tolerance));
            Assert.IsFalse(Get<bool>(follower, "IsComplete"));
        }

        [Test]
        public void ExitClampsOversizedStepAndRemainsStableAfterCompletion()
        {
            object follower = Make(3,
                new[] { Vector2.zero, Vector2.right, Vector2.right + Vector2.up }, 2f);

            Advance(follower, 10f);

            Assert.IsTrue(Get<bool>(follower, "IsComplete"));
            Assert.IsTrue(Get<bool>(follower, "HasExited"));
            AssertVector(Vector2.right + Vector2.up, Get<Vector2>(follower, "Position"));
            AssertVector(Vector2.up, Get<Vector2>(follower, "Facing"));
            Assert.That(Get<float>(follower, "Progress"), Is.EqualTo(2f).Within(Tolerance));

            Advance(follower, 1f);

            Assert.IsTrue(Get<bool>(follower, "IsComplete"));
            Assert.IsTrue(Get<bool>(follower, "HasExited"));
            AssertVector(Vector2.right + Vector2.up, Get<Vector2>(follower, "Position"));
            AssertVector(Vector2.up, Get<Vector2>(follower, "Facing"));
            Assert.That(Get<float>(follower, "Progress"), Is.EqualTo(2f).Within(Tolerance));
        }

        [Test]
        public void SpeedCanChangeAndZeroSpeedOrZeroDeltaPauseMovement()
        {
            object follower = Make(4,
                new[] { Vector2.zero, new Vector2(10f, 0f) }, 2f);

            Advance(follower, 0.5f);
            Assert.That(Get<float>(follower, "Progress"), Is.EqualTo(1f).Within(Tolerance));

            Set(follower, "Speed", 4f);
            Advance(follower, 0.25f);
            Assert.That(Get<float>(follower, "Progress"), Is.EqualTo(2f).Within(Tolerance));

            Set(follower, "Speed", 0f);
            Advance(follower, 1f);
            Advance(follower, 0f);

            Assert.That(Get<float>(follower, "Progress"), Is.EqualTo(2f).Within(Tolerance));
            AssertVector(new Vector2(2f, 0f), Get<Vector2>(follower, "Position"));
            Assert.AreEqual(0f, Get<float>(follower, "Speed"));
        }

        [Test]
        public void TinyNonZeroSegmentsStillExposeNormalizedFacing()
        {
            object follower = Make(6,
                new[] { Vector2.zero, new Vector2(0.000001f, 0f) }, 0.000001f);

            Advance(follower, 0.5f);

            AssertVector(Vector2.right, Get<Vector2>(follower, "Facing"));
            Assert.That(Get<float>(follower, "Progress"), Is.EqualTo(0.0000005f).Within(0.0000001f));
        }

        [Test]
        public void RejectsInvalidInputsButSkipsDuplicateAdjacentWaypoints()
        {
            IReadOnlyList<Vector2> missing = null;
            Assert.Throws<ArgumentNullException>(() => Make(0, missing, 1f));
            Assert.Throws<ArgumentException>(() => Make(0,
                new[] { Vector2.zero }, 1f));
            Assert.Throws<ArgumentException>(() => Make(0,
                new[] { Vector2.zero, Vector2.zero, Vector2.zero }, 1f));
            Assert.Throws<ArgumentException>(() => Make(0,
                new[] { Vector2.zero, new Vector2(float.NaN, 0f) }, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => Make(0,
                new[] { Vector2.zero, Vector2.right }, -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => Make(0,
                new[] { Vector2.zero, Vector2.right }, float.PositiveInfinity));

            object follower = Make(5,
                new[]
                {
                    Vector2.zero,
                    Vector2.zero,
                    Vector2.right,
                    Vector2.right,
                    Vector2.right + Vector2.up,
                    Vector2.right + Vector2.up
                }, 1f);

            Advance(follower, 1f);
            AssertVector(Vector2.right, Get<Vector2>(follower, "Position"));
            AssertVector(Vector2.up, Get<Vector2>(follower, "Facing"));
            Assert.That(Get<float>(follower, "Progress"), Is.EqualTo(1f).Within(Tolerance));
            Assert.IsFalse(Get<bool>(follower, "IsComplete"));

            Assert.Throws<ArgumentOutOfRangeException>(() => Advance(follower, -0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => Advance(follower, float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => Set(follower, "Speed", float.NaN));
        }

        private object Make(int fanId, IReadOnlyList<Vector2> waypoints, float speed)
        {
            try
            {
                return Activator.CreateInstance(followerType, new object[] { fanId, waypoints, speed });
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException;
            }
        }

        private static void Advance(object follower, float deltaTime)
        {
            MethodInfo method = follower.GetType().GetMethod("Advance", PublicInstance);
            Assert.IsNotNull(method, "Expected public Advance(float) method.");
            try
            {
                method.Invoke(follower, new object[] { deltaTime });
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException;
            }
        }

        private static T Get<T>(object follower, string propertyName)
        {
            PropertyInfo property = follower.GetType().GetProperty(propertyName, PublicInstance);
            Assert.IsNotNull(property, "Expected public property " + propertyName + ".");
            return (T)property.GetValue(follower, null);
        }

        private static void Set(object follower, string propertyName, object value)
        {
            PropertyInfo property = follower.GetType().GetProperty(propertyName, PublicInstance);
            Assert.IsNotNull(property, "Expected public property " + propertyName + ".");
            try
            {
                property.SetValue(follower, value, null);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException;
            }
        }

        private static void AssertVector(Vector2 expected, Vector2 actual)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(Tolerance), "x coordinate");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(Tolerance), "y coordinate");
        }
    }
}
