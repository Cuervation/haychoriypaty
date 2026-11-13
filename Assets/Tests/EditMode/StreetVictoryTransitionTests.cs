using System;
using System.Reflection;
using NUnit.Framework;

namespace HayChoriYPaty.Tests
{
    public sealed class StreetVictoryTransitionTests
    {
        private object clock, attempt;
        private Type type;
        [SetUp] public void SetUp()
        {
            type = Type.GetType("HayChoriYPaty.StreetVictoryTransition, Assembly-CSharp", true);
            clock = Activator.CreateInstance(type); attempt = new object();
        }
        private void Observe(bool playing, bool won, float now) => type.GetMethod("Observe").Invoke(clock, new[] { attempt, (object)playing, won, now });
        private bool Active(float now) => (bool)type.GetMethod("Active").Invoke(clock, new object[] { now });
        private float Elapsed(float now) => (float)type.GetMethod("Elapsed").Invoke(clock, new object[] { now });
        private float SmokeCoverage(float elapsed) => (float)type.GetMethod("SmokeCoverageAt").Invoke(null, new object[] { elapsed });

        [Test] public void LiveWinRunsForThreeUnscaledSecondsAndClampsElapsed()
        {
            Observe(true, false, 100); Observe(false, true, 101);
            Assert.IsTrue(Active(101)); Assert.IsTrue(Active(103.99f)); Assert.IsFalse(Active(104));
            Assert.AreEqual(1.5f, Elapsed(102.5f)); Assert.AreEqual(3f, Elapsed(110));
        }
        [Test] public void RepaintsAndResultRetriesCannotRestartFinishedWin()
        {
            Observe(true, false, 1); Observe(false, true, 2);
            Observe(false, true, 7); Observe(false, true, 8);
            Assert.IsFalse(Active(8));
        }
        [Test] public void ResumedPendingWinBypassesTransition()
        {
            Observe(false, true, 5); Assert.IsFalse(Active(5));
            Observe(false, true, 6); Assert.IsFalse(Active(6));
        }
        [Test] public void SkipDoesNotReplayWhenWonIsObservedAgain()
        {
            Observe(true, false, 1); Observe(false, true, 2);
            type.GetMethod("Skip").Invoke(clock, null); Observe(false, true, 2.5f);
            Assert.IsFalse(Active(2.5f));
        }
        [Test] public void NewAttemptCanAnimateAndLossCancels()
        {
            Observe(true, false, 1); Observe(false, true, 2);
            Observe(false, false, 2.5f); Assert.IsFalse(Active(2.5f));
            Observe(true, false, 3); Observe(false, true, 4); Assert.IsTrue(Active(4));
        }
        [Test] public void ChangedSimulationCannotAnimateAnOldPendingWin()
        {
            Observe(true, false, 1); attempt = new object(); Observe(false, true, 2);
            Assert.IsFalse(Active(2));
        }

        [Test] public void WhiteSmokeFillsViewportBeforeChulengoRevealThenClears()
        {
            float revealAt = (float)type.GetField("ChulengoRevealStart").GetRawConstantValue();
            Assert.AreEqual(0f, SmokeCoverage(.65f), .001f, "Smoke stays out of the opening victory lettering.");
            Assert.Greater(SmokeCoverage(1.5f), .5f, "The white plume visibly rises during the middle of the transition.");
            Assert.AreEqual(1f, SmokeCoverage(2.05f), .001f, "The smoke reaches full-screen coverage before the reveal.");
            Assert.Greater(SmokeCoverage(revealAt), .99f, "The real Chulengo panel starts behind the full smoke layer.");
            Assert.Greater(SmokeCoverage(2.64f), 0f, "Smoke dissipates gradually as the Chulengo arrives.");
            Assert.AreEqual(0f, SmokeCoverage(3f), .001f, "The full result is clear at the end of the presentation.");
        }
    }
}
