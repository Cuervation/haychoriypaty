using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    public sealed class CamionerosFanProfileTests
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        Type Profile => Type.GetType("HayChoriYPaty.CamionerosFanProfile, Assembly-CSharp", true);
        ScriptableObject config;
        object mode;
        static Type T(string name) => Type.GetType("HayChoriYPaty." + name + ", Assembly-CSharp", true);
        static object Get(object o, string name) => o.GetType().GetProperty(name, All).GetValue(o);
        static void Set(object o, string name, object value)
        {
            var field = o.GetType().GetField(name, All);
            if (field != null) field.SetValue(o, value);
            else o.GetType().GetProperty(name, All).SetValue(o, value);
        }
        static object Call(object o, string name, params object[] args) => o.GetType().GetMethod(name, All).Invoke(o, args);
        IList Fans => (IList)Get(mode, "Fans");
        object ProfileFor(int appearance) => Profile.GetMethod("ForAppearance").Invoke(null, new object[] { appearance });
        void Make(int level = 9, float scale = .02f)
        {
            config = ScriptableObject.CreateInstance(T("TowerDefenseLevelConfig"));
            Set(config, "sourceCampaignLevelId", level);
            Set(config, "totalFanCount", 120);
            object route = Get(config, "Route");
            object geometry = T("TowerDefenseRouteGeometry").GetMethod("Generate").Invoke(null, new[] { route });
            Vector2 a = (Vector2)Call(config, "WorkZonePosition", geometry, 0);
            Vector2 b = (Vector2)Call(config, "WorkZonePosition", geometry, 1);
            mode = Activator.CreateInstance(T("TowerDefenseGameMode"), new[] { config, Activator.CreateInstance(T("StreetBalance")), route, (object)scale, a, b });
            Call(mode, "Begin");
        }
        [TearDown] public void Cleanup()
        {
            if (mode != null) Call(mode, "Stop");
            if (config != null) UnityEngine.Object.DestroyImmediate(config);
        }
        [Test]
        public void TwelveOutfitsHaveStableProfilesAndTenTwentyThirtyPixelFreeGaps()
        {
            string[] expected = { "Deportivo", "Deportivo", "Abrigado", "Casual", "Deportivo", "Casual", "Abrigado", "Abrigado", "Deportivo", "Casual", "Abrigado", "Casual" };
            for (int i = 0; i < 12; i++)
            {
                object p = ProfileFor(i);
                Assert.AreEqual(expected[i], Get(p, "Kind").ToString());
                float gap = expected[i] == "Deportivo" ? 10f : expected[i] == "Casual" ? 20f : 30f;
                float speed = expected[i] == "Deportivo" ? 1.15f : expected[i] == "Casual" ? 1f : .85f;
                Assert.AreEqual(gap, Get(p, "FreeGapLogical"));
                Assert.AreEqual(speed, Get(p, "SpeedMultiplier"));
                Assert.AreEqual(76f + gap, Get(p, "HeadwayLogical"));
                Assert.AreSame(p, ProfileFor(i + 12));
            }
        }
        [Test]
        public void AutomaticWaveHas120FansWith48Casual36Sport36CoatAndProfileOrders()
        {
            Make(); var counts = new Dictionary<string, int>(); var looks = new HashSet<int>();
            int consecutiveSport = 0;
            for (int i = 0; i < 120; i++)
            {
                Call(mode, "SpawnNextFan");
                Assert.AreEqual(1, Fans.Count);
                object fan = Fans[0], p = Get(fan, "Profile");
                int appearance = (int)Get(fan, "AppearanceId");
                Assert.AreSame(ProfileFor(appearance), p);
                looks.Add(appearance);
                string kind = Get(p, "Kind").ToString();
                if (i < 12) Assert.AreEqual("Casual", kind);
                if (!counts.ContainsKey(kind)) counts[kind] = 0;
                counts[kind]++;
                consecutiveSport = kind == "Deportivo" ? consecutiveSport + 1 : 0;
                Assert.LessOrEqual(consecutiveSport, 2);
                Assert.AreEqual(300f * (float)Get(config, "FanSpeed") * .02f * (float)Get(p, "SpeedMultiplier"),
                    (float)Get(Get(fan, "RouteFollower"), "Speed"), .00001f);
                foreach (object line in (IEnumerable)Get(fan, "Order"))
                {
                    CollectionAssert.Contains(new[] { 0, 4 }, Get(line, "Product"));
                    Assert.That((int)Get(line, "Remaining"), Is.InRange((int)Get(p, "MinQuantity"), (int)Get(p, "MaxQuantity")));
                }
                // Remove only from this synthetic fixture: sample all orders without triggering a loss.
                ((IList)mode.GetType().GetField("fans", All).GetValue(mode)).Clear();
            }
            Assert.AreEqual(48, counts["Casual"]); Assert.AreEqual(36, counts["Deportivo"]); Assert.AreEqual(36, counts["Abrigado"]);
            Assert.AreEqual(12, looks.Count);
            Assert.IsFalse((bool)Call(mode, "TrySpawnFan", new[] { 0 }, new[] { 1 }));
        }
        [Test]
        public void EntryUsesFramePlusFreeGapAndFastFollowerSlowsThenResumesAfterDeparture()
        {
            Make(); Call(mode, "SpawnNextFan");
            object first = Fans[0], front = Get(first, "RouteFollower");
            Assert.IsFalse((bool)Call(mode, "HasEntrySpacing"));
            Call(front, "Advance", 96f * .02f / (float)Get(front, "Speed") + .0001f);
            Assert.IsTrue((bool)Call(mode, "HasEntrySpacing"));
            Call(mode, "SpawnNextFan");
            object second = Fans[1], back = Get(second, "RouteFollower");
            // Controlled slow/fast pair using real followers, starting at valid separation.
            Set(first, "Profile", ProfileFor(7)); Set(second, "Profile", ProfileFor(0));
            Set(front, "Speed", .5f); Set(back, "Speed", 2f);
            float nominal = (float)Get(back, "Speed");
            for (int tick = 0; tick < 100; tick++)
            {
                Call(mode, "AdvanceFans", .05f);
                Assert.GreaterOrEqual((float)Get(front, "Progress") - (float)Get(back, "Progress"), 86f * .02f - .0001f);
            }
            Assert.AreEqual(nominal, Get(back, "Speed"), "Traffic does not permanently change the desired profile speed.");
            Set(first, "IsDeparting", true);
            float before = (float)Get(back, "Progress");
            Call(mode, "AdvanceFans", .05f);
            Assert.AreEqual(before + nominal * .05f, (float)Get(back, "Progress"), .0001f);
        }
        [Test]
        public void ConvoyKeepsHeadwayThroughTurnsAndReflowKeepsProfilesAndOrders()
        {
            Make();
            Call(mode, "SpawnNextFan");
            for (int tick = 0; tick < 300 && Fans.Count < 3; tick++)
            {
                Call(mode, "AdvanceFans", .05f);
                if ((bool)Call(mode, "HasEntrySpacing")) Call(mode, "SpawnNextFan");
            }
            Assert.AreEqual(3, Fans.Count, "Use real staggered entry rather than an already-overlapping injected fixture.");
            for (int tick = 0; tick < 50; tick++)
            {
                Call(mode, "AdvanceFans", .05f);
                for (int i = 1; i < Fans.Count; i++)
                {
                    object front = Get(Fans[i - 1], "RouteFollower"), back = Get(Fans[i], "RouteFollower");
                    Assert.GreaterOrEqual((float)Get(front, "Progress") - (float)Get(back, "Progress"), 1.92f - .0001f);
                    Vector2 delta = (Vector2)Get(front, "Position") - (Vector2)Get(back, "Position");
                    Assert.GreaterOrEqual(Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)), 1.92f - .0001f,
                        "The square render footprints stay apart even through curved turns.");
                }
            }
            var appearances = new List<int>(); var orders = new List<object>();
            foreach (object f in Fans) { appearances.Add((int)Get(f, "AppearanceId")); orders.Add(Get(f, "Order")); }
            object route = Get(config, "Route");
            object geometry = T("TowerDefenseRouteGeometry").GetMethod("Generate").Invoke(null, new[] { route });
            Call(mode, "Reflow", route, .03f, Call(config, "WorkZonePosition", geometry, 0), Call(config, "WorkZonePosition", geometry, 1));
            Assert.IsTrue((bool)Get(mode, "LastReflowSucceeded"));
            for (int i = 0; i < Fans.Count; i++)
            {
                Assert.AreEqual(appearances[i], Get(Fans[i], "AppearanceId")); Assert.AreSame(orders[i], Get(Fans[i], "Order"));
                Assert.AreEqual(300f * (float)Get(config, "FanSpeed") * .03f, (float)Get(Get(Fans[i], "RouteFollower"), "Speed"), .0001f);
                if (i > 0)
                {
                    Assert.GreaterOrEqual((float)Get(Get(Fans[i - 1], "RouteFollower"), "Progress") - (float)Get(Get(Fans[i], "RouteFollower"), "Progress"), 96f * .03f - .0001f);
                    Vector2 delta = (Vector2)Get(Get(Fans[i - 1], "RouteFollower"), "Position") - (Vector2)Get(Get(Fans[i], "RouteFollower"), "Position");
                    Assert.GreaterOrEqual(Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)), 96f * .03f - .0001f,
                        "Reflow must preserve visual clearance around turns, not only arc distance.");
                }
            }
        }
        [TestCase(10)] [TestCase(13)]
        public void OtherDefenseLevelsKeepIdentitySpeedAndLegacyEntryRule(int level)
        {
            Make(level); Call(mode, "SpawnNextFan");
            object fan = Fans[0];
            Assert.IsNull(Get(fan, "Profile")); Assert.AreEqual(Get(fan, "Id"), Get(fan, "AppearanceId"));
            Assert.AreEqual(300f * (float)Get(config, "FanSpeed") * .02f, (float)Get(Get(fan, "RouteFollower"), "Speed"), .0001f);
        }
    }
}
