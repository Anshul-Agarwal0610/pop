using BackendAPI.Models;

namespace BackendAPI.Services
{
    public static class GamificationRules
    {
        public static int VoteXp(Poll poll) => poll.IsTrending ? 35 : 25;

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
