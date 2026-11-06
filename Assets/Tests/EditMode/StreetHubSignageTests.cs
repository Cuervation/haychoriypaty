using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    public sealed class StreetHubSignageTests
    {
        private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;

        [Test]
        public void ReferenceArtworkMatchesTheHubMasterCanvas()
        {
            Texture2D artwork = Resources.Load<Texture2D>("street-hub-stadium-v1");
            Assert.NotNull(artwork, "The exact user-provided hub reference must load as the live backdrop.");
            Assert.AreEqual(841, artwork.width);
            Assert.AreEqual(1870, artwork.height);
        }

        [Test]
        public void FourArtworkHotspotsFollowTheReferencePlacements()
        {
            System.Type view = System.Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true);
            MethodInfo hotspot = view.GetMethod("HubArtActionSourceRect", StaticPrivate);
            MethodInfo signBounds = view.GetMethod("HubSignageArtworkSourceBounds", StaticPrivate | BindingFlags.Public);
            Assert.NotNull(hotspot);
            Assert.NotNull(signBounds);
            Assert.AreEqual(new Rect(486f, 447f, 236f, 128f), hotspot.Invoke(null, new object[] { 203 }), "Ranking");
            Assert.AreEqual(new Rect(121f, 837f, 224f, 112f), hotspot.Invoke(null, new object[] { 200 }), "JUGAR");
            Assert.AreEqual(new Rect(91f, 1207f, 292f, 104f), hotspot.Invoke(null, new object[] { 201 }), "Mercado");
            Assert.AreEqual(new Rect(594f, 837f, 242f, 142f), hotspot.Invoke(null, new object[] { 202 }), "Insignias flag");
            Assert.AreEqual(hotspot.Invoke(null, new object[] { 201 }), signBounds.Invoke(null, new object[] { 0 }));
            Assert.AreEqual(hotspot.Invoke(null, new object[] { 202 }), signBounds.Invoke(null, new object[] { 1 }));
        }

        [TestCase(200)]
        [TestCase(201)]
        [TestCase(202)]
        [TestCase(203)]
        public void EachActualSignPulsesAndPressesInsideAStableTouchEnvelope(int action)
        {
            System.Type view = System.Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true);
            MethodInfo scale = view.GetMethod("HubSignagePulseScale", StaticPrivate | BindingFlags.Public);
            MethodInfo rect = view.GetMethod("HubSignagePulseBounds", StaticPrivate | BindingFlags.Public);
            MethodInfo hit = view.GetMethod("HubSignageTouchSourceBounds", StaticPrivate | BindingFlags.Public);
            Assert.AreEqual(4, view.GetProperty("HubSignageAnimationCount", StaticPrivate | BindingFlags.Public).GetValue(null));
            Rect envelope = (Rect)hit.Invoke(null, new object[] {action});
            float minimum = 2f, maximum = 0f;
            for (int n = 0; n <= 28; n++)
            {
                float elapsed = n * .1f;
                float value = (float)scale.Invoke(null, new object[] {action, elapsed, false});
                minimum = Mathf.Min(minimum, value); maximum = Mathf.Max(maximum, value);
                Rect drawn = (Rect)rect.Invoke(null, new object[] {action, elapsed, false});
                Assert.GreaterOrEqual(drawn.xMin + .001f, envelope.xMin);
                Assert.GreaterOrEqual(drawn.yMin + .001f, envelope.yMin);
                Assert.LessOrEqual(drawn.xMax - .001f, envelope.xMax);
                Assert.LessOrEqual(drawn.yMax - .001f, envelope.yMax);
            }
            Assert.That(minimum, Is.EqualTo(1f).Within(.001f));
            Assert.That(maximum, Is.EqualTo(1.08f).Within(.001f));
            Assert.AreEqual(.95f, scale.Invoke(null, new object[] {action, 0f, true}));
            Assert.IsNull(view.GetField("hubSignageSparkleTexture", BindingFlags.Instance | BindingFlags.NonPublic),
                "No painted-star overlay or runtime sparkle texture remains.");
        }
    }
}
