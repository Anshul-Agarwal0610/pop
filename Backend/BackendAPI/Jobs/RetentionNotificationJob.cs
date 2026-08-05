using BackendAPI.Interfaces;

namespace BackendAPI.Jobs
{
    public class RetentionNotificationJob
    {
        private readonly IChallengesRepository _challengesRepo;
        private readonly INotificationsRepository _notificationsRepo;
        private readonly IPushNotificationService _pushNotificationService;
        private readonly ILogger<RetentionNotificationJob> _logger;

        public RetentionNotificationJob(
            IChallengesRepository challengesRepo,
            INotificationsRepository notificationsRepo,
            IPushNotificationService pushNotificationService,
            ILogger<RetentionNotificationJob> logger)
        {
            _challengesRepo = challengesRepo;
            _notificationsRepo = notificationsRepo;
            _pushNotificationService = pushNotificationService;
            _logger = logger;
        }

        public async Task RunAsync()
        {
            var utcNow = DateTime.UtcNow;
            await _challengesRepo.EnsureCurrentOccurrencesAsync(utcNow);

            var challengeCount = await _notificationsRepo.CreateDailyChallengeNotificationsAsync(utcNow);
            var streakCount = await _notificationsRepo.CreateStreakReminderNotificationsAsync(utcNow);
            var trendingCount = await _notificationsRepo.CreateTrendingPollNotificationsAsync(utcNow);
            var expiringCount = await _notificationsRepo.CreateExpiringPollNotificationsAsync(utcNow);
            var pushCount = await _pushNotificationService.SendPendingAsync();

            _logger.LogInformation(
                "[RetentionNotificationJob] Created notifications. challenge={ChallengeCount}, streak={StreakCount}, trending={TrendingCount}, expiring={ExpiringCount}, push={PushCount}",
                challengeCount,
                streakCount,
                trendingCount,
                expiringCount,
                pushCount);
        }
    }
}
