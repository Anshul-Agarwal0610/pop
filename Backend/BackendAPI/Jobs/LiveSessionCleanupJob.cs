using BackendAPI.Interfaces;
using BackendAPI.Services;

namespace BackendAPI.Jobs;

public sealed class LiveSessionCleanupJob(ILiveSessionsRepository sessions,ISystemClock clock,ILogger<LiveSessionCleanupJob> logger)
{
    public async Task RunAsync(){var result=await sessions.CleanupDueAsync(clock.UtcNow);logger.LogInformation("PoP Live cleanup terminalized {Expired} expired and {Abandoned} abandoned sessions.",result.Expired,result.Abandoned);}
}
