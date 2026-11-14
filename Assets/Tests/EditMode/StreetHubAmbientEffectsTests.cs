using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    public sealed class StreetHubAmbientEffectsTests
    {
        [Test]
        public void AmbientActivityTriplesDensityOpacityAndFrequencyWithoutUnboundedAlpha()
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            System.Type view = System.Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true);
            Assert.AreEqual(3, view.GetField("HubAmbientActivityMultiplier", flags).GetRawConstantValue());
            System.Type overlay = view.GetNestedType("HubAmbientOverlay", BindingFlags.NonPublic);
            Assert.AreEqual(15, overlay.GetField("LeafCount", flags).GetRawConstantValue());
            Assert.AreEqual(9, overlay.GetField("SmokeCount", flags).GetRawConstantValue());
            Assert.AreEqual(3, view.GetProperty("HubAmbientSmokeShapeCountForTesting", flags).GetValue(null));
            Assert.AreEqual(2, view.GetProperty("HubAmbientSparkCountForTesting", flags).GetValue(null));
            MethodInfo opacity = view.GetMethod("HubAmbientOpacity", flags);
            MethodInfo interval = view.GetMethod("HubAmbientInterval", flags);
            Assert.AreEqual(.405f, (float)opacity.Invoke(null, new object[] { .135f }), .0001f);
            Assert.AreEqual(.27f, (float)opacity.Invoke(null, new object[] { .09f }), .0001f);
            Assert.AreEqual(1f, opacity.Invoke(null, new object[] { .8f }));
            Assert.AreEqual(2f, interval.Invoke(null, new object[] { 6f }));
        }

        [Test]
        public void FallingLeavesLaunchFromFiveZonesAndTravelDownThroughTheArtwork()
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            System.Type view = System.Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true);
            MethodInfo count = view.GetProperty("HubAmbientLeafRouteCountForTesting", flags).GetGetMethod(true);
            MethodInfo start = view.GetMethod("HubAmbientLeafRouteStartForTesting", flags);
            MethodInfo end = view.GetMethod("HubAmbientLeafRouteEndForTesting", flags);
            Assert.AreEqual(5, count.Invoke(null, null));
            Assert.NotNull(start); Assert.NotNull(end);
            float previousStartY = -1f;
            for (int route = 0; route < 5; route++)
            {
                Vector2 origin = (Vector2)start.Invoke(null, new object[] { route });
                Vector2 destination = (Vector2)end.Invoke(null, new object[] { route });
                Assert.Greater(destination.y, origin.y + 80f, "Leaves must drift from their source canopy into the neighborhood.");
                Assert.AreNotEqual(previousStartY, origin.y, "Each route launches from a distinct vertical zone.");
                previousStartY = origin.y;
            }
            Vector2 lowerLeft = (Vector2)start.Invoke(null, new object[] { 3 });
            Vector2 lowerRight = (Vector2)start.Invoke(null, new object[] { 4 });
            Assert.Greater(lowerLeft.y, 800f, "The lower-left shop tree contributes falling leaves.");
            Assert.Greater(lowerRight.y, 850f, "The lower-right tree contributes falling leaves.");
        }

    }
}
