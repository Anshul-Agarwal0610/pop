using BackendAPI.Models;

namespace BackendAPI.Services
{
    public static class GamificationRules
    {
        public static readonly int[] StreakMilestones = [3, 7, 30, 100];
        public const int RecoveryCooldownDays = 30;
        public const int RecoverableMissedDays = 1;
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
            int longestStreak,
            DateTime? lastVoteDate,
            DateTime? lastRecoveryAt,
            DateTime utcNow,
            bool useRecovery = false)
        {
            var today = NormalizeUtc(utcNow).Date;
            var previousVoteDate = lastVoteDate?.Date;
            var recoveryAvailable = !lastRecoveryAt.HasValue ||
                NormalizeUtc(lastRecoveryAt.Value) <= today.AddDays(-RecoveryCooldownDays);

            if (previousVoteDate == today)
            {
                return new StreakUpdate(currentStreak, Math.Max(longestStreak, currentStreak), false,
                    true, today, false, false, null);
            }

            var consecutive = previousVoteDate == today.AddDays(-1);
            var recoverableGap = previousVoteDate == today.AddDays(-(RecoverableMissedDays + 1));
            var recoveryEligible = recoverableGap && recoveryAvailable;
            var recoveryUsed = recoveryEligible && useRecovery;
            var nextStreak = consecutive || recoveryUsed ? currentStreak + 1 : 1;
            var milestone = StreakMilestones.FirstOrDefault(value => currentStreak < value && nextStreak >= value);

            return new StreakUpdate(nextStreak, Math.Max(longestStreak, nextStreak), true,
                true, today, recoveryEligible, recoveryUsed, milestone == 0 ? null : milestone);
        }

        private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value
        };
    }

    public sealed record StreakUpdate(
        int Streak,
        int LongestStreak,
        bool StreakAdvanced,
        bool TodayComplete,
        DateTime LastVoteDate,
        bool RecoveryEligible,
        bool RecoveryUsed,
        int? MilestoneReached);
}
