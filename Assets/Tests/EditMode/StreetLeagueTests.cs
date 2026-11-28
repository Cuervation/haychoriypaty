using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    /// <summary>Liga points and apodo rules of the ranking (2026-11-25).</summary>
    public sealed class StreetLeagueTests
    {
        private static Type League => Type.GetType("HayChoriYPaty.StreetLeague, Assembly-CSharp", true);
        private static Type Statistics => Type.GetType("HayChoriYPaty.StreetPlayerStatistics, Assembly-CSharp", true);
        private static object Stats(params (int level, long wins)[] wins)
        {
            object stats = Activator.CreateInstance(Statistics);
            var array = (long[])Statistics.GetField("winsByLevel").GetValue(stats);
            foreach (var (level, count) in wins) array[level] = count;
            return stats;
        }
        private static long Points(object stats) => (long)League.GetMethod("Points").Invoke(null, new[] { stats });
        private static int Won(object stats) => (int)League.GetMethod("WonLevels").Invoke(null, new[] { stats });
        private static bool Nick(string raw, out string clean)
        {
            object[] args = { raw, null };
            bool ok = (bool)League.GetMethod("TryNormalizeNickname").Invoke(null, args);
            clean = (string)args[1]; return ok;
        }

        [Test] public void NoStatisticsMeansNoPoints()
        {
            Assert.AreEqual(0L, Points(null)); Assert.AreEqual(0, Won(null));
            Assert.AreEqual(0L, Points(Stats())); Assert.AreEqual(0, Won(Stats()));
        }

        [Test] public void AFirstWinIsWorthAHundredAndEachRepeatFivePointsUpToNine()
        {
            Assert.AreEqual(100L, Points(Stats((0, 1))));
            Assert.AreEqual(110L, Points(Stats((0, 3))));
            Assert.AreEqual(145L, Points(Stats((0, 10))));
            Assert.AreEqual(145L, Points(Stats((0, 5000))), "Repeats stop counting after nine.");
            Assert.AreEqual(245L, Points(Stats((0, 1), (1, 10))));
            Assert.AreEqual(2, Won(Stats((0, 1), (1, 10))));
        }

        [Test] public void HuracanBonusTrackNeverCountsAndTheMaximumMatchesTheServerCap()
        {
            int huracan = (int)Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true).GetField("HuracanBonusLevelId").GetValue(null);
            Assert.AreEqual(0L, Points(Stats((huracan, 99))));
            var all = new System.Collections.Generic.List<(int, long)>();
            for (int i = 0; i < 15; i++) all.Add((i, 99));
            object stats = Stats(all.ToArray());
            Assert.AreEqual(14 * 145L, Points(stats));
            Assert.AreEqual(14, Won(stats));
            Assert.LessOrEqual(Points(stats), (long)(int)League.GetField("MaxPoints").GetRawConstantValue());
            Assert.AreEqual(14, (int)League.GetField("MaxWonLevels").GetRawConstantValue());
        }

        [TestCase("El Negro", "El Negro")] [TestCase("  Chori_10  ", "Chori_10")] [TestCase("Ñandú", "Ñandú")]
        [TestCase("El    Negro", "El Negro")] [TestCase("Paty.Doble-2", "Paty.Doble-2")] [TestCase("abc", "abc")]
        [TestCase("1234567890123456", "1234567890123456")]
        public void ValidApodosAreAccepted(string raw, string expected)
        {
            Assert.IsTrue(Nick(raw, out string clean)); Assert.AreEqual(expected, clean);
        }

        [TestCase(null)] [TestCase("")] [TestCase("ab")] [TestCase("   ")] [TestCase("12345678901234567")]
        [TestCase("a<b>c")] [TestCase("hola!")] [TestCase("a@b.com")] [TestCase("😀😀😀")]
        public void InvalidApodosAreRejected(string raw) => Assert.IsFalse(Nick(raw, out _));

        [Test] public void FirestoreRulesUseTheSameApodoPatternAndPointCap()
        {
            string rules = File.ReadAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "../firebase/firestore.rules")));
            string pattern = (string)League.GetField("NicknamePattern").GetRawConstantValue();
            StringAssert.Contains("d.nickname.matches('" + pattern + "')", rules);
            StringAssert.Contains("d.points <= " + (int)League.GetField("MaxPoints").GetRawConstantValue(), rules);
            StringAssert.Contains("d.wonLevels <= " + (int)League.GetField("MaxWonLevels").GetRawConstantValue(), rules);
            StringAssert.Contains("match /leaderboard/{uid}", rules);
        }
    }
}
