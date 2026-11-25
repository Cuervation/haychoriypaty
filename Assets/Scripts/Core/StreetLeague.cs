using System.Text.RegularExpressions;

namespace HayChoriYPaty
{
    /// <summary>
    /// Puntos de Liga del ranking global (2026-11-25): se derivan solo de las estadísticas ya guardadas, sin contadores nuevos.
    /// Una cancha ganada al menos una vez vale <see cref="WinPoints"/>; cada victoria repetida suma <see cref="RepeatWinPoints"/> hasta
    /// <see cref="MaxRepeatWins"/> veces. Huracán (bonus track) no cuenta: su puntaje es otro. El puntaje nunca baja.
    /// </summary>
    public static class StreetLeague
    {
        public const int WinPoints = 100, RepeatWinPoints = 5, MaxRepeatWins = 9;
        /// <summary>Tope que las reglas de Firestore también exigen (14 canchas × 145 puntos = 2030).</summary>
        public const int MaxPoints = 3000, MaxWonLevels = 14;
        public const int NicknameMinLength = 3, NicknameMaxLength = 16;
        /// <summary>Misma expresión que <c>firebase/firestore.rules</c>: sin espacios al borde, 3 a 16 caracteres.</summary>
        public const string NicknamePattern = "^[A-Za-z0-9ÁÉÍÓÚÜÑáéíóúüñ_.-][A-Za-z0-9ÁÉÍÓÚÜÑáéíóúüñ _.-]{1,14}[A-Za-z0-9ÁÉÍÓÚÜÑáéíóúüñ_.-]$";
        private static readonly Regex NicknameRule = new Regex(NicknamePattern, RegexOptions.CultureInvariant);

        public static long Points(StreetPlayerStatistics statistics)
        {
            long points = 0;
            ForEachCountedLevel(statistics, wins => { points += WinPoints + RepeatWinPoints * System.Math.Min(MaxRepeatWins, wins - 1); });
            return points;
        }

        public static int WonLevels(StreetPlayerStatistics statistics)
        {
            int won = 0;
            ForEachCountedLevel(statistics, wins => won++);
            return won;
        }

        private static void ForEachCountedLevel(StreetPlayerStatistics statistics, System.Action<int> won)
        {
            if (statistics == null || statistics.winsByLevel == null) return;
            for (int position = 0; position < StreetSimulation.CampaignOrder.Length; position++)
            {
                int levelId = StreetSimulation.CampaignLevelAt(position);
                if (levelId == StreetSimulation.HuracanBonusLevelId || levelId < 0 || levelId >= statistics.winsByLevel.Length) continue;
                long wins = statistics.winsByLevel[levelId];
                if (wins > 0) won((int)System.Math.Min(wins, int.MaxValue));
            }
        }

        /// <summary>Recorta y valida el apodo; devuelve false si no cumple las reglas del servidor.</summary>
        public static bool TryNormalizeNickname(string raw, out string nickname)
        {
            nickname = raw == null ? "" : Regex.Replace(raw.Trim(), " {2,}", " ");
            return NicknameRule.IsMatch(nickname);
        }
    }
}
