using BackendAPI.Models;

namespace BackendAPI.Services
{
    public static class GamificationRules
    {
        public const int XpPerLevel = 1000;

        public static int VoteXp(Poll poll) => poll.IsTrending ? 35 : 25;

        public static UserProgression Progression(User user, DateTime utcNow)
        {
            var level = user.Xp / XpPerLevel + 1;
            var start = (level - 1) * XpPerLevel;
            var into = user.Xp - start;
            return new UserProgression
            {
                Xp = user.Xp,
                Level = level,
                CurrentLevelStartXp = start,
                NextLevelXp = start + XpPerLevel,
                XpIntoLevel = into,
                XpRequiredForLevel = XpPerLevel,
                ProgressPercent = into * 100d / XpPerLevel,
                Streak = user.Streak,
                TodayActivityComplete = user.LastVoteDate?.Date == utcNow.Date,
                LastVoteDate = user.LastVoteDate
            };
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
