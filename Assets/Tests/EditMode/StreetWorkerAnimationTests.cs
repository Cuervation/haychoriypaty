using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    // Runtime remains in Assembly-CSharp; the test asmdef intentionally binds by reflection.
    public sealed class StreetWorkerAnimationTests
    {
        private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        private static Type Runtime(string name) => Type.GetType("HayChoriYPaty." + name + ", Assembly-CSharp", true);
        private static object Facing(string name) => Enum.Parse(Runtime("StreetFacing"), name);
        private static object Call(string method, params object[] args) => Runtime("StreetWorkerAnimation").GetMethod(method, PublicStatic).Invoke(null, args);
        private static object Field(object value, string name) => value.GetType().GetField(name, PublicInstance).GetValue(value);
        private static void Set(object value, string name, object contents) =>
            value.GetType().GetProperty(name, PublicInstance).GetSetMethod(true).Invoke(value, new[] { contents });

        [TestCase(0f, 1f, "Down")]
        [TestCase(-1f, 1f, "DownLeft")]
        [TestCase(-1f, 0f, "Left")]
        [TestCase(-1f, -1f, "UpLeft")]
        [TestCase(0f, -1f, "Up")]
        [TestCase(1f, -1f, "UpRight")]
        [TestCase(1f, 0f, "Right")]
        [TestCase(1f, 1f, "DownRight")]
        public void ResolveFacingMapsEightDirections(float x, float y, string expected) =>
            Assert.AreEqual(Facing(expected), Call("ResolveFacing", new Vector2(x, y), Facing("Down")));

        [Test]
        public void ResolveFacingUsesFallbackForZeroAndEightWayAngularThreshold()
        {
            Assert.AreEqual(Facing("UpLeft"), Call("ResolveFacing", Vector2.zero, Facing("UpLeft")));
            float boundary = Mathf.Tan(22.5f * Mathf.Deg2Rad);
            Assert.AreEqual(Facing("Right"), Call("ResolveFacing", new Vector2(1f, boundary * .99f), Facing("Down")));
            Assert.AreEqual(Facing("DownRight"), Call("ResolveFacing", new Vector2(1f, boundary * 1.01f), Facing("Down")));
        }

        [Test]
        public void WalkFrameHasFourSamplesAndClampsNegativeDistance()
        {
            Assert.AreEqual(80f, (float)Runtime("StreetWorkerAnimation").GetField("StrideLength", PublicStatic).GetValue(null));
            Assert.AreEqual(0, Call("WalkFrame", 0f));
            Assert.AreEqual(1, Call("WalkFrame", 20f));
            Assert.AreEqual(2, Call("WalkFrame", 40f));
            Assert.AreEqual(3, Call("WalkFrame", 60f));
            Assert.AreEqual(0, Call("WalkFrame", 80f));
            Assert.AreEqual(0, Call("WalkFrame", -20f));
        }

        [Test]
        public void PoseUsesTraveledDistanceAndOneDirectionAcrossAllFourRoles()
        {
            object worker = Activator.CreateInstance(Runtime("StreetWorker"), true);
            Type stateType = Runtime("StreetWorkerState");
            Set(worker, "State", Enum.Parse(stateType, "ToCounter"));
            Set(worker, "FacingVector", Vector2.right);
            Set(worker, "TravelDistance", 40f);
            Set(worker, "StepDistance", 1f);
            Set(worker, "AnimationTime", 0f);
            object pose = Call("Sample", worker);
            Assert.AreEqual(Facing("Right"), Field(pose, "Facing"));
            Assert.AreEqual(2, Field(pose, "Frame"));
            Assert.IsTrue((bool)Field(pose, "Moving"));
            Set(worker, "AnimationTime", 1000f);
            object clockChangedPose = Call("Sample", worker);
            Assert.AreEqual(Field(pose, "Frame"), Field(clockChangedPose, "Frame"));
            Assert.AreEqual(Field(pose, "Bob"), Field(clockChangedPose, "Bob"));
            Type roleType = Runtime("StreetWorkerRole");
            foreach (string role in new[] { "Parrillero", "Cocacolero", "ParrilleroPremium", "Fernetero" })
            {
                Set(worker, "Role", Enum.Parse(roleType, role));
                foreach (string state in new[] { "ToStation", "Pickup", "ToCounter", "Handoff" })
                {
                    Set(worker, "State", Enum.Parse(stateType, state));
                    object rolePose = Call("Sample", worker);
                    Assert.AreEqual(Facing("Right"), Field(rolePose, "Facing"), role + " / " + state);
                    Assert.AreEqual(2, Field(rolePose, "Frame"), role + " / " + state);
                }
            }
        }

        [Test]
        public void SimulationDistanceMatchesPositionAndIdleDoesNotAccumulate()
        {
            object balance = Activator.CreateInstance(Runtime("StreetBalance"));
            object simulation = Activator.CreateInstance(Runtime("StreetSimulation"),
                new[] { balance, (object)0, 5f, 0, 1, 0 });
            Runtime("StreetBalance").GetField("customerArrivalSeconds", PublicInstance).SetValue(balance, 1000f);
            Set(simulation, "Phase", Enum.Parse(Runtime("RoundPhase"), "Playing"));
            MethodInfo spawn = simulation.GetType().GetMethod("SpawnCustomer", PublicInstance, null,
                new[] { typeof(int), typeof(int) }, null);
            Assert.IsTrue((bool)spawn.Invoke(simulation, new object[] { 0, 3 }));
            System.Collections.IList workers = (System.Collections.IList)simulation.GetType()
                .GetProperty("Workers", PublicInstance).GetValue(simulation);
            object worker = workers[0];
            Vector2 previous = (Vector2)worker.GetType().GetProperty("Position", PublicInstance).GetValue(worker);
            float previousDistance = (float)worker.GetType().GetProperty("TravelDistance", PublicInstance).GetValue(worker);
            MethodInfo step = simulation.GetType().GetMethod("Step", PublicInstance);
            for (int i = 0; i < 200; i++)
            {
                step.Invoke(simulation, new object[] { .05f });
                float distance = (float)worker.GetType().GetProperty("StepDistance", PublicInstance).GetValue(worker);
                if (distance > 0f) break;
            }
            Vector2 position = (Vector2)worker.GetType().GetProperty("Position", PublicInstance).GetValue(worker);
            float stepDistance = (float)worker.GetType().GetProperty("StepDistance", PublicInstance).GetValue(worker);
            float travelDistance = (float)worker.GetType().GetProperty("TravelDistance", PublicInstance).GetValue(worker);
            Assert.Greater(stepDistance, 0f);
            Assert.AreEqual(Vector2.Distance(previous, position), stepDistance, .001f);
            Assert.AreEqual(previousDistance + stepDistance, travelDistance, .001f);

            Set(worker, "State", Enum.Parse(Runtime("StreetWorkerState"), "Idle"));
            step.Invoke(simulation, new object[] { .05f });
            Assert.AreEqual(0f, worker.GetType().GetProperty("StepDistance", PublicInstance).GetValue(worker));
            Assert.AreEqual(travelDistance, worker.GetType().GetProperty("TravelDistance", PublicInstance).GetValue(worker));
        }

        [TestCase("Idle")]
        [TestCase("Pickup")]
        [TestCase("Handoff")]
        public void IdlePickupAndHandoffDoNotWalkOrBob(string state)
        {
            object worker = Activator.CreateInstance(Runtime("StreetWorker"), true);
            Set(worker, "State", Enum.Parse(Runtime("StreetWorkerState"), state));
            Set(worker, "FacingVector", Vector2.up);
            Set(worker, "StepDistance", 0f);
            object pose = Call("Sample", worker);
            Assert.IsFalse((bool)Field(pose, "Moving"));
            Assert.AreEqual(0f, (float)Field(pose, "Bob"));
        }

        [Test]
        public void PickupAnimationIsProgressDrivenWithoutFakingACarriedItem()
        {
            object worker = Activator.CreateInstance(Runtime("StreetWorker"), true);
            Set(worker, "State", Enum.Parse(Runtime("StreetWorkerState"), "Pickup"));
            Set(worker, "CarriedItemId", 0);
            Set(worker, "StepDistance", 0f);
            Set(worker, "PickupProgress", .5f);
            object reaching = Call("Sample", worker);
            Assert.IsTrue((bool)Field(reaching, "PickingUp"));
            Assert.IsFalse((bool)Field(reaching, "Moving"));
            Assert.IsFalse((bool)Field(reaching, "Carrying"));
            Assert.AreEqual(.5f, (float)Field(reaching, "PickupProgress"));
            Set(worker, "CarriedItemId", 12);
            object taken = Call("Sample", worker);
            Assert.IsFalse((bool)Field(taken, "PickingUp"));
            Assert.IsTrue((bool)Field(taken, "Carrying"));
        }

        [TestCase("Pickup")]
        [TestCase("ToCounter")]
        [TestCase("Handoff")]
        public void CarryingTracksOnlyARealPhysicalItemId(string state)
        {
            object worker = Activator.CreateInstance(Runtime("StreetWorker"), true);
            Set(worker, "State", Enum.Parse(Runtime("StreetWorkerState"), state));
            Set(worker, "CarriedItemId", 0);
            Assert.IsFalse((bool)Field(Call("Sample", worker), "Carrying"));
            Set(worker, "CarriedItemId", 1);
            Assert.IsTrue((bool)Field(Call("Sample", worker), "Carrying"));
        }
    }
}
