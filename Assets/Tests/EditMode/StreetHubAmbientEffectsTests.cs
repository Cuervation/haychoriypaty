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
            Assert.AreEqual(12, overlay.GetField("LeafCount", flags).GetRawConstantValue());
            Assert.AreEqual(9, overlay.GetField("SmokeCount", flags).GetRawConstantValue());
            MethodInfo opacity = view.GetMethod("HubAmbientOpacity", flags);
            MethodInfo interval = view.GetMethod("HubAmbientInterval", flags);
            Assert.AreEqual(.405f, (float)opacity.Invoke(null, new object[] { .135f }), .0001f);
            Assert.AreEqual(.27f, (float)opacity.Invoke(null, new object[] { .09f }), .0001f);
            Assert.AreEqual(1f, opacity.Invoke(null, new object[] { .8f }));
            Assert.AreEqual(2f, interval.Invoke(null, new object[] { 6f }));
        }

        [Test]
        public void AmbientImageAnchorsRemainVisibleAcrossPortraitCropsWithoutChangingHotspots()
        {
            const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
            System.Type viewType = System.Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true);
            MethodInfo ambientBounds = viewType.GetMethod("HubAmbientLayoutBounds", StaticPrivate);
            MethodInfo hotspotSource = viewType.GetMethod("HubArtActionSourceRect", StaticPrivate);
            Assert.NotNull(ambientBounds, "The ambient overlay must use the hub's shared artwork mapping.");
            Assert.NotNull(hotspotSource, "Hub hotspot geometry remains the authority for input.");

            Vector2[] sizes =
            {
                new Vector2(360f, 800f), new Vector2(575f, 1280f), new Vector2(1080f, 1920f),
                new Vector2(1080f, 2400f), new Vector2(1200f, 2670f), new Vector2(1536f, 2048f)
            };
            int[] visibleAnchors = { 0, 1, 2, 3, 4 }; // leaves, smoke, coals, ranking star, JUGAR shine
            foreach (Vector2 size in sizes)
            {
                Rect safeArea = new Rect(0f, 28f, size.x, size.y - 72f);
                foreach (int anchor in visibleAnchors)
                {
                    Rect bounds = (Rect)ambientBounds.Invoke(null, new object[] { anchor, size, safeArea });
                    Assert.Greater(bounds.width, 0f, "Ambient anchor " + anchor + " should map horizontally at " + size + ".");
                    Assert.Greater(bounds.height, 0f, "Ambient anchor " + anchor + " should map vertically at " + size + ".");
                }
            }

            Assert.AreEqual(new Rect(467f, 448f, 288f, 143f), hotspotSource.Invoke(null, new object[] { 203 }), "Ranking hotspot is unchanged.");
            Assert.AreEqual(new Rect(100f, 859f, 315f, 155f), hotspotSource.Invoke(null, new object[] { 200 }), "JUGAR hotspot is unchanged.");
            Assert.AreEqual(new Rect(94f, 1238f, 317f, 164f), hotspotSource.Invoke(null, new object[] { 201 }), "Mercado hotspot is unchanged.");
            Assert.AreEqual(new Rect(479f, 1236f, 344f, 168f), hotspotSource.Invoke(null, new object[] { 202 }), "Insignias hotspot is unchanged.");
        }
    }
}
