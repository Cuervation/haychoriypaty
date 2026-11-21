using NUnit.Framework;
using System;
using UnityEngine;

public class StreetRewardShutterTests
{
    [TestCase(-1f, 0f)]
    [TestCase(0f, 0f)]
    [TestCase(.27f, .5f)]
    [TestCase(.54f, 1f)]
    [TestCase(.84f, 1f)]
    [TestCase(1.14f, 1f)]
    [TestCase(1.41f, .5f)]
    [TestCase(1.68f, 0f)]
    [TestCase(3f, 0f)]
    public void ShutterClosesHoldsAndOpens(float elapsed, float expected)
    {
        Type view = Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true);
        float progress = (float)view.GetMethod("RewardShutterProgress").Invoke(null, new object[] { elapsed });
        Assert.That(progress, Is.EqualTo(expected).Within(.0001f));
    }
    // sector numerators: 0 -> x0, 1 -> x1.5, 2 -> x1, 3 -> x2, 4 -> x1, 5 -> x1.5, 6 -> x0
    [TestCase(false, false, -1, 0)]
    [TestCase(true, false, -1, 1)]
    [TestCase(true, true, -1, 1)]
    [TestCase(true, true, 0, 0)]
    [TestCase(true, true, 1, 2)]
    [TestCase(true, true, 2, 1)]
    [TestCase(true, true, 3, 2)]
    [TestCase(true, true, 6, 0)]
    public void ClaimSoundMatchesPayout(bool pending, bool locked, int sector, int expected)
    {
        Type view = Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true);
        int kind = (int)view.GetMethod("RewardClaimSoundKind").Invoke(null, new object[] { pending, locked, sector });
        Assert.That(kind, Is.EqualTo(expected));
    }

    [TestCase("Audio/reward-shutter-close", .54f)]
    [TestCase("Audio/reward-shutter-slam", .38f)]
    [TestCase("Audio/reward-shutter-open", .56f)]
    [TestCase("Audio/reward-claim-coins", .60f)]
    [TestCase("Audio/reward-claim-coins-bright", .85f)]
    public void ShutterSoundsLoadWithExpectedLength(string path, float seconds)
    {
        AudioClip clip = Resources.Load<AudioClip>(path);
        Assert.That(clip, Is.Not.Null, path);
        Assert.That(clip.length, Is.EqualTo(seconds).Within(.02f), path);
    }
}
