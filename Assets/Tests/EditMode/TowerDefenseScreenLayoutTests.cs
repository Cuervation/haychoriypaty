using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    public sealed class TowerDefenseScreenLayoutTests
    {
        [TestCase(960f)]
        [TestCase(1200f)]
        public void ExistingEightControlsAndFooterFitResponsiveSafeCanvas(float height)
        {
            Rect[] primary = InvokeBounds("TowerDefensePrimaryActionBounds", height);
            Rect[] hires = InvokeBounds("TowerDefenseHireBounds", height);

            Assert.AreEqual(3, primary.Length, "Speed, pause and exit stay the only primary actions.");
            Assert.AreEqual(5, hires.Length, "The five existing hires remain the only secondary actions.");
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
            Assert.AreEqual(7824, ActionId("TowerDefenseHireLastAction"));
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
    }
}
