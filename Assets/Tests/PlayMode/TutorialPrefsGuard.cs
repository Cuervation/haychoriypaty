using System.Collections.Generic;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    /// <summary>Marks the level 2-6 tutorials as completed during a PlayMode test and restores the previous PlayerPrefs afterwards.</summary>
    internal sealed class TutorialPrefsGuard
    {
        private static readonly string[] Keys =
        {
            "HayChoriYPaty.LevelTwoCocacoleroTutorial.Completed.v1",
            "HayChoriYPaty.LevelThreePatyTutorial.Completed.v1",
            "HayChoriYPaty.LevelFourSpecialistsTutorial.Completed.v1",
            "HayChoriYPaty.LevelFiveSpecialistsTutorial.Completed.v1",
            "HayChoriYPaty.LevelSixRacingTutorial.Completed.v1",
        };

        private readonly Dictionary<string, int?> previous = new Dictionary<string, int?>();

        public void Suppress()
        {
            foreach (string key in Keys)
            {
                previous[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : (int?)null;
                PlayerPrefs.SetInt(key, 1);
            }
        }

        public void Restore()
        {
            foreach (KeyValuePair<string, int?> pair in previous)
            {
                if (pair.Value.HasValue) PlayerPrefs.SetInt(pair.Key, pair.Value.Value);
                else PlayerPrefs.DeleteKey(pair.Key);
            }
            previous.Clear();
        }
    }
}
