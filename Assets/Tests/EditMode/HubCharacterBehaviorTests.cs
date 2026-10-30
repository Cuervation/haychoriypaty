using System;
using System.Reflection;
using NUnit.Framework;

namespace HayChoriYPaty.Tests
{
    // Runtime types are in predefined Assembly-CSharp, so this asmdef suite deliberately uses reflection.
    public sealed class HubCharacterBehaviorTests
    {
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        private static Type Runtime(string name) => Type.GetType("HayChoriYPaty." + name + ", Assembly-CSharp", true);
        private static object Settings() => Activator.CreateInstance(Runtime("HubCharacterBehaviorSettings"));
        private static object Behavior(int seed, object settings = null) =>
            Activator.CreateInstance(Runtime("HubCharacterBehavior"), new[] { (object)seed, settings });
        private static object Id(string name) => Enum.Parse(Runtime("HubCharacterId"), name);
        private static object Action(object behavior, string id) => GetState(behavior, id).GetType().GetProperty("Action", PublicInstance).GetValue(GetState(behavior, id));
        private static object GetState(object behavior, string id) => Runtime("HubCharacterBehavior").GetMethod("GetState").Invoke(behavior, new[] { Id(id) });
        private static object StateProperty(object state, string name) => state.GetType().GetProperty(name, PublicInstance).GetValue(state);
        private static void Tick(object behavior, float dt, bool visible) => Runtime("HubCharacterBehavior").GetMethod("Tick").Invoke(behavior, new object[] { dt, visible });
        private static void Notify(object behavior, string eventName) =>
            Runtime("HubCharacterBehavior").GetMethod("Notify").Invoke(behavior, new[] { Enum.Parse(Runtime("HubCharacterEvent"), eventName) });
        private static void Set(object settings, string name, object value) => settings.GetType().GetField(name, PublicInstance).SetValue(settings, value);
        private static string ActionName(object behavior, string id) => StateProperty(GetState(behavior, id), "Action").ToString();
        private static float Elapsed(object behavior, string id) => (float)StateProperty(GetState(behavior, id), "Elapsed");
        private static float Envelope(object behavior, string id) => (float)StateProperty(GetState(behavior, id), "Envelope");

        [Test]
        public void SameSeedProducesSameSchedule()
        {
            object a = Behavior(123), b = Behavior(123);
            Array ids = Enum.GetValues(Runtime("HubCharacterId"));
            for (int i = 0; i < 100; i++)
            {
                Tick(a, .25f, true); Tick(b, .25f, true);
                foreach (object id in ids)
                {
                    object sa = Runtime("HubCharacterBehavior").GetMethod("GetState").Invoke(a, new[] { id });
                    object sb = Runtime("HubCharacterBehavior").GetMethod("GetState").Invoke(b, new[] { id });
                    Assert.AreEqual(StateProperty(sa, "Action"), StateProperty(sb, "Action"));
                    Assert.AreEqual(StateProperty(sa, "Elapsed"), StateProperty(sb, "Elapsed"));
                }
            }
        }

        [Test]
        public void HiddenHubFreezesAndQueuesEvent()
        {
            object behavior = Behavior(9);
            Tick(behavior, .2f, true);
            object before = GetState(behavior, "Seller");
            Notify(behavior, "Victory");
            Tick(behavior, 30f, false);
            object after = GetState(behavior, "Seller");
            Assert.AreEqual(StateProperty(before, "Action"), StateProperty(after, "Action"));
            Assert.AreEqual(StateProperty(before, "Elapsed"), StateProperty(after, "Elapsed"));
            Tick(behavior, .1f, true);
            Assert.AreEqual("Cheer", ActionName(behavior, "Seller"));
        }

        [Test]
        public void PoliceGesturesNeverOverlap()
        {
            object behavior = Behavior(42);
            for (int i = 0; i < 5000; i++)
            {
                Tick(behavior, .05f, true);
                int active = 0;
                foreach (string id in new[] { "PoliceLeft", "PoliceCenter", "PoliceRight" })
                    if (ActionName(behavior, id) != "Idle") active++;
                Assert.LessOrEqual(active, 1);
            }
        }

        [Test]
        public void DefeatWaitsForVictoryToReturnAndReplacesLowerPriorityQueue()
        {
            object settings = Settings(); Set(settings, "EventDuration", .4f);
            object behavior = Behavior(1, settings);
            Notify(behavior, "Victory"); Tick(behavior, .1f, true);
            Assert.AreEqual("Cheer", ActionName(behavior, "Boy"));
            Notify(behavior, "Victory"); Notify(behavior, "Defeat"); Tick(behavior, .1f, true);
            Assert.AreEqual("Cheer", ActionName(behavior, "Boy"));
            Tick(behavior, .3f, true);
            Assert.AreEqual("Disappointed", ActionName(behavior, "Boy"));
            Assert.AreEqual(0f, Envelope(behavior, "Boy"));
        }

        [Test]
        public void EventEnvelopeReturnsContinuouslyToIdle()
        {
            object settings = Settings(); Set(settings, "EventDuration", 1f);
            object behavior = Behavior(1, settings);
            Notify(behavior, "Victory"); Tick(behavior, .01f, true);
            Assert.AreEqual(0f, Envelope(behavior, "Boy"));
            Tick(behavior, .5f, true);
            Assert.AreEqual(1f, Envelope(behavior, "Boy"), .0001f);
            Tick(behavior, .499f, true);
            Assert.Less(Envelope(behavior, "Boy"), .001f);
            Tick(behavior, .001f, true);
            Assert.AreEqual("Idle", ActionName(behavior, "Boy"));
            Assert.AreEqual(0f, Envelope(behavior, "Boy"));
        }

        [Test]
        public void ConfiguredZeroGroupGapsStartDeterministically()
        {
            object settings = Settings();
            Set(settings, "SellerGapMin", 100f); Set(settings, "SellerGapMax", 100f);
            Set(settings, "PoliceGapMin", 0f); Set(settings, "PoliceGapMax", 0f);
            Set(settings, "StoreGapMin", 0f); Set(settings, "StoreGapMax", 0f);
            Set(settings, "KidsGapMin", 0f); Set(settings, "KidsGapMax", 0f);
            Set(settings, "GestureDurationMin", 1f); Set(settings, "GestureDurationMax", 1f);
            object behavior = Behavior(27, settings); Tick(behavior, .01f, true);
            Assert.AreNotEqual("Idle", ActionName(behavior, "PoliceLeft"));
            Assert.AreEqual("Idle", ActionName(behavior, "PoliceCenter"));
            Assert.AreNotEqual("Idle", ActionName(behavior, "Store"));
            Assert.AreNotEqual("Idle", ActionName(behavior, "Boy"));
            Assert.AreNotEqual("Idle", ActionName(behavior, "Girl"));
        }

        [Test]
        public void SellerUsesConfiguredGapAndBehaviorHandlesInvalidTimeAndIds()
        {
            object settings = Settings(); Set(settings, "SellerGapMin", 8f); Set(settings, "SellerGapMax", 8f);
            object behavior = Behavior(2, settings);
            Tick(behavior, float.NaN, true); Tick(behavior, 7.9f, true);
            Assert.AreEqual("Idle", ActionName(behavior, "Seller"));
            Tick(behavior, .2f, true);
            CollectionAssert.Contains(new[] { "Wave", "Approve", "Look" }, ActionName(behavior, "Seller"));
            object invalidId = Enum.ToObject(Runtime("HubCharacterId"), 99);
            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() =>
                Runtime("HubCharacterBehavior").GetMethod("GetState").Invoke(behavior, new[] { invalidId }));
            Assert.IsInstanceOf<ArgumentOutOfRangeException>(exception.InnerException);
        }
    }
}
