using NUnit.Framework;
using System;

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
}
