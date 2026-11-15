using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    /// <summary>The hub's top controls were authored for a 540-wide canvas and must grow with wider screens.</summary>
    public sealed class StreetHubControlScaleTests
    {
        private const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
        private static Type View => Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true);

        private static Rect Bounds(string method, Vector2 screen, bool needsSafeArea)
        {
            MethodInfo m = View.GetMethod(method, Static);
            Assert.NotNull(m, method);
            object[] args = needsSafeArea ? new object[] { screen, new Rect(0, 0, screen.x, screen.y) } : new object[] { screen };
            return (Rect)m.Invoke(null, args);
        }

        [TestCase(360f, 1f)]
        [TestCase(540f, 1f)]
        [TestCase(1080f, 2f)]
        [TestCase(2400f, 3f)]
        public void ControlScaleFollowsScreenWidthWithinLimits(float width, float expected)
        {
            float scale = (float)View.GetMethod("HubControlScale", Static).Invoke(null, new object[] { new Vector2(width, width * 2f) });
            Assert.AreEqual(expected, scale, .001f);
        }

        [Test]
        public void AccountButtonGrowsWithTheScreenAndStaysInsideIt()
        {
            Rect reference = Bounds("HubAccountButtonScreenBounds", new Vector2(540, 960), false);
            Rect wide = Bounds("HubAccountButtonScreenBounds", new Vector2(1080, 2400), false);
            Assert.AreEqual(reference.width * 2f, wide.width, .01f);
            Assert.AreEqual(reference.height * 2f, wide.height, .01f);
            Assert.AreEqual(reference.y, wide.y, .01f, "The top offset stays so the button keeps covering the baked account pill.");
            Assert.LessOrEqual(wide.xMax, 1080f);
            Assert.GreaterOrEqual(wide.xMin, 0f);
        }

        [TestCase(540, 960)]
        [TestCase(720, 1600)]
        [TestCase(1080, 2400)]
        public void SettingsAndMusicTouchTargetsScaleAndStayOnScreen(int width, int height)
        {
            var screen = new Vector2(width, height);
            float scale = Mathf.Clamp(width / 540f, 1f, 3f);
            Rect settings = Bounds("HubSettingsButtonScreenBounds", screen, true);
            Rect music = Bounds("HubTopMusicButtonScreenBounds", screen, true);
            Assert.GreaterOrEqual(settings.width, 48f * scale - .01f);
            Assert.GreaterOrEqual(music.width, 44f * scale - .01f);
            foreach (Rect r in new[] { settings, music })
            {
                Assert.GreaterOrEqual(r.xMin, -.01f);
                Assert.GreaterOrEqual(r.yMin, -.01f);
                Assert.LessOrEqual(r.xMax, width + .01f);
                Assert.LessOrEqual(r.yMax, height + .01f);
            }
            Assert.IsFalse(settings.Overlaps(music), "Settings and music targets must not overlap.");
        }
    }
}
