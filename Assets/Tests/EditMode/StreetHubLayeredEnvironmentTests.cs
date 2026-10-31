using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    public sealed class StreetHubLayeredEnvironmentTests
    {
        private const BindingFlags StaticNonPublic = BindingFlags.Static | BindingFlags.NonPublic;

        [Test]
        public void GeneratedLayerResourceContractAndClothRegionsAreSeparated()
        {
            System.Type view = System.Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true);
            Assert.AreEqual("StreetHubEnvironment/", view.GetField("HubLayeredResourceRoot", StaticNonPublic).GetRawConstantValue());
            Assert.AreEqual(841, view.GetField("HubLayeredCanvasWidth", StaticNonPublic).GetRawConstantValue());
            Assert.AreEqual(1870, view.GetField("HubLayeredCanvasHeight", StaticNonPublic).GetRawConstantValue());

            Rect[] flags = (Rect[])view.GetField("HubLayeredFlagBounds", StaticNonPublic).GetValue(null);
            Rect[] shirts = (Rect[])view.GetField("HubLayeredShirtBounds", StaticNonPublic).GetValue(null);
            Rect[] awnings = (Rect[])view.GetField("HubLayeredAwningBounds", StaticNonPublic).GetValue(null);
            Rect[] flagUvs = (Rect[])view.GetField("HubLayeredFlagSourceUv", StaticNonPublic).GetValue(null);
            Rect[] shirtUvs = (Rect[])view.GetField("HubLayeredShirtSourceUv", StaticNonPublic).GetValue(null);
            Rect[] awningUvs = (Rect[])view.GetField("HubLayeredAwningSourceUv", StaticNonPublic).GetValue(null);
            Assert.AreEqual(5, flags.Length);
            Assert.AreEqual(1, shirts.Length);
            Assert.AreEqual(2, awnings.Length);
            Assert.AreEqual(5, flagUvs.Length);
            Assert.AreEqual(1, shirtUvs.Length);
            Assert.AreEqual(2, awningUvs.Length);
            Assert.AreNotEqual(flags[0].x, flagUvs[0].x, "Foreground UVs are independent of original artwork-space targets.");
            Assert.AreEqual(1006f, shirts[0].yMax, .001f,
                "The replacement and clean patch must cover the full original-height shirt group without crushing it.");
            Assert.AreEqual(0f, awnings[0].xMin);
            Assert.Greater(awnings[1].yMin, awnings[0].yMin);
        }

        [Test]
        public void ClothPoseIsPinnedAtAttachmentsAndMovesIndependentlyOverTime()
        {
            MethodInfo pose = System.Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true)
                .GetMethod("HubLayeredClothOffsetForTesting", StaticNonPublic);
            Assert.NotNull(pose);
            Vector2 left = (Vector2)pose.Invoke(null, new object[] { 0f, 2f, .4f, 2.2f });
            Vector2 right = (Vector2)pose.Invoke(null, new object[] { 1f, 2f, .4f, 2.2f });
            Vector2 middleA = (Vector2)pose.Invoke(null, new object[] { .5f, 0f, .4f, 2.2f });
            Vector2 middleB = (Vector2)pose.Invoke(null, new object[] { .5f, 1.3f, .4f, 2.2f });
            Assert.AreEqual(0f, left.y, .0001f);
            Assert.AreEqual(0f, right.y, .0001f);
            Assert.AreNotEqual(middleA.y, middleB.y, "Cloth should change pose over its animation cycle.");
            Assert.LessOrEqual(Mathf.Abs(middleA.y), 2.2f);
        }

        [Test]
        public void LayeredAnchorsUseSharedArtworkMappingAcrossPortraitRatios()
        {
            System.Type view = System.Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true);
            MethodInfo mapping = view.GetMethod("HubArtworkRectToLayoutUnclamped", StaticNonPublic);
            Assert.NotNull(mapping);
            Vector2[] sizes = { new Vector2(360, 800), new Vector2(1080, 1920), new Vector2(1080, 2400) };
            foreach (Vector2 size in sizes)
            {
                Rect safeArea = new Rect(0, 24, size.x, size.y - 48);
                Rect mapped = (Rect)mapping.Invoke(null, new object[] { new Rect(486, 302, 90, 86), new Vector2(841, 1870), size, safeArea });
                Assert.Greater(mapped.width, 0f);
                Assert.Greater(mapped.height, 0f);
            }
        }

        [TestCase(1080f, 1920f)]
        [TestCase(1080f, 2400f)]
        public void CleanBackingAndForegroundUseUniformPhysicalArtworkScale(float width, float height)
        {
            System.Type view = System.Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true);
            MethodInfo map = view.GetMethod("HubLayeredArtworkScreenRect", StaticNonPublic);
            Rect art = new Rect(590f, 850f, 167f, 156f);
            Rect mapped = (Rect)map.Invoke(null, new object[] { art, new Vector2(width, height) });
            float scale = Mathf.Max(width / 841f, height / 1870f);
            Assert.AreEqual(art.width * scale, mapped.width, .001f);
            Assert.AreEqual(art.height * scale, mapped.height, .001f, "UI y-only scaling used to truncate clean backing on tall screens.");
            Rect adjacent = (Rect)map.Invoke(null, new object[] { new Rect(art.x, art.yMax, art.width, 10f), new Vector2(width, height) });
            Assert.AreEqual(mapped.yMax, adjacent.yMin, .001f, "Backing and moving art must meet without seams at any ratio.");
        }

        [Test]
        public void AmbientFamilySamplesRemainDeterministicAndNonStatic()
        {
            System.Type view = System.Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true);
            MethodInfo cloth = view.GetMethod("HubLayeredClothOffsetForTesting", StaticNonPublic);
            MethodInfo cloud = view.GetMethod("HubLayeredCloudSampleForTesting", StaticNonPublic);
            MethodInfo bird = view.GetMethod("HubLayeredBirdSampleForTesting", StaticNonPublic);
            MethodInfo light = view.GetMethod("HubLayeredLightPulseForTesting", StaticNonPublic);
            Assert.NotNull(cloth); Assert.NotNull(cloud); Assert.NotNull(bird); Assert.NotNull(light);
            Vector2 cloudA = (Vector2)cloud.Invoke(null, new object[] { 0f, 0 });
            Vector2 cloudB = (Vector2)cloud.Invoke(null, new object[] { 3f, 0 });
            Vector2 birdA = (Vector2)bird.Invoke(null, new object[] { 0f, 0 });
            Vector2 birdB = (Vector2)bird.Invoke(null, new object[] { 3f, 0 });
            Assert.AreNotEqual(cloudA.x, cloudB.x);
            Assert.AreNotEqual(birdA.x, birdB.x);
            Assert.AreNotEqual((float)light.Invoke(null, new object[] { 0f }), (float)light.Invoke(null, new object[] { 3f }));
            Assert.Less((float)light.Invoke(null, new object[] { 0f }), .1f, "Shop light pulse remains restrained.");
        }

        [Test]
        public void TopOriginForegroundSlicesMapToCorrectTextureRows()
        {
            System.Type renderer = System.Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true)
                .GetNestedType("HubLayeredEnvironmentRenderer", BindingFlags.NonPublic);
            MethodInfo convert = renderer.GetMethod("HubLayeredNormalizedPatchToTextureUv", StaticNonPublic);
            Assert.NotNull(convert);
            Rect source = new Rect(.1f, .2f, .4f, .6f);
            Rect top = (Rect)convert.Invoke(null, new object[] { source, new Rect(0f, 0f, 1f, .25f) });
            Rect bottom = (Rect)convert.Invoke(null, new object[] { source, new Rect(0f, .75f, 1f, .25f) });
            Assert.AreEqual(.65f, top.y, .0001f, "The first top-origin slice must sample the top rows, not the bottom.");
            Assert.AreEqual(.2f, bottom.y, .0001f, "The final top-origin slice must sample the bottom rows.");
            Assert.AreEqual(.15f, top.height, .0001f);
            Assert.AreEqual(.15f, bottom.height, .0001f);
        }
    }
}
