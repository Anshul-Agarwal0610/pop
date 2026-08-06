using BackendAPI.Models;

namespace BackendAPI.Interfaces;

public interface ILiveSessionsRepository
{
    Task<LiveSessionDto> CreateAsync(long hostUserId, CreateLiveSessionRequest request, DateTime utcNow);
    Task<LiveSessionDto?> GetAsync(long id, long callerUserId);
    Task<LiveEventReplayDto> GetEventsAsync(long id, long callerUserId, long afterSequence);
    Task<LiveSessionDto> JoinAsync(long id, long userId, string expectedVersion, DateTime utcNow);
    Task<LiveSessionDto> LeaveAsync(long id, long userId, string expectedVersion, DateTime utcNow);
    Task<LiveSessionDto> StartAsync(long id, long hostUserId, string expectedVersion, DateTime utcNow);
    Task<LiveResponseDto> SubmitResponseAsync(long id, long roundId, long userId, SubmitLiveResponseRequest request, DateTime utcNow);
    Task<LiveSessionDto> CompleteRoundAsync(long id, long roundId, long hostUserId, string expectedVersion, DateTime utcNow);
    Task<LiveSessionDto> CompleteAsync(long id, long hostUserId, string expectedVersion, DateTime utcNow);
    Task<LiveSessionDto> AbandonAsync(long id, long hostUserId, string expectedVersion, DateTime utcNow);
    Task<LiveCleanupResult> CleanupDueAsync(DateTime utcNow);
}
