using BackendAPI.Interfaces;
using BackendAPI.Services;

namespace BackendAPI.Jobs;

public sealed class RelayDeadlineJob(IRelayRepository relays, ISystemClock clock, ILogger<RelayDeadlineJob> logger)
{
    public async Task RunAsync()
    {
        var count=await relays.ExpireOverdueAsync(clock.UtcNow);
        if(count>0) logger.LogInformation("Expired {Count} overdue relay handoffs",count);
    }
}
