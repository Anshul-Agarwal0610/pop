using BackendAPI.Interfaces;
using BackendAPI.Services;

namespace BackendAPI.Jobs;

public sealed class LiveSessionExpiryJob(ILiveSessionsRepository sessions, ISystemClock clock)
{
    public Task<int> RunAsync() => sessions.ExpireDueAsync(clock.UtcNow);
}
