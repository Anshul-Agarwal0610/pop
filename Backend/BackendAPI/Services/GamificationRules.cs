using BackendAPI.Models;

namespace BackendAPI.Services
{
    public static class GamificationRules
    {
        /// <summary>
        /// The single progression threshold definition. Level 1 starts at 0 XP and every
        /// exact multiple of 1,000 XP starts the next level (1,000 XP is level 2).
        /// Existing XP is never modified by this calculation.
        /// </summary>
        public const int XpPerLevel = 1000;

        public static int VoteXp(Poll poll) => poll.IsTrending ? 35 : 25;

        public static ProgressionSnapshot FromTotalXp(int totalXp)
        {
            if (totalXp < 0)
                throw new ArgumentOutOfRangeException(nameof(totalXp), "Total XP cannot be negative.");

            var level = totalXp / XpPerLevel + 1;
            var currentLevelXp = (level - 1) * XpPerLevel;
            var nextLevelXp = level * XpPerLevel;
            var xpIntoLevel = totalXp - currentLevelXp;
            return new ProgressionSnapshot(
                totalXp,
                level,
                currentLevelXp,
                nextLevelXp,
                xpIntoLevel,
                XpPerLevel,
                (int)Math.Floor(xpIntoLevel * 100d / XpPerLevel));
        }

        public static StreakUpdate ApplyDailyStreak(
            int currentStreak,
            DateTime? lastVoteDate,
            DateTime utcNow)
        {
            var today = utcNow.Date;
            var previousVoteDate = lastVoteDate?.Date;

            if (previousVoteDate == today)
            {
                return new StreakUpdate(currentStreak, false, today);
            }

            var nextStreak = previousVoteDate == today.AddDays(-1)
                ? currentStreak + 1
                : 1;

            return new StreakUpdate(nextStreak, true, today);
        }
    }

    public sealed record StreakUpdate(
        int Streak,
        bool StreakAdvanced,
        DateTime LastVoteDate);
}
