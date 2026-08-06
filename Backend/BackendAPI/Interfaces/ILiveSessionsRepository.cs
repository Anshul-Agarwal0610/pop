using BackendAPI.Models;

namespace BackendAPI.Interfaces;

public interface ILiveSessionsRepository
{
    Task<bool> IsMemberAsync(Guid sessionId, long userId);
    Task<LiveSessionStateDto?> GetAsync(Guid sessionId, long userId, DateTime utcNow);
    Task<LiveSessionStateDto> SetReadyAsync(Guid sessionId, long userId, bool ready, DateTime utcNow);
    Task<LiveVoteResult> VoteAsync(Guid sessionId, int round, long userId, LiveVoteRequest request, DateTime utcNow);
    Task<LiveSessionStateDto> CompleteAsync(Guid sessionId, long userId, DateTime utcNow);
}

public interface ILiveSessionNotifier
{
    Task PublishAsync(LiveSessionEvent message, CancellationToken cancellationToken = default);
}
