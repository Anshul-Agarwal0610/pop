using BackendAPI.Models;

namespace BackendAPI.Interfaces;

public interface ILiveSessionsRepository
{
    Task<LiveSessionStateDto> CreateAsync(long userId, CreateLiveSessionRequest request, DateTime now);
    Task<LiveSessionStateDto?> GetAsync(string publicId, long userId, DateTime now);
    Task<LiveSessionStateDto> JoinAsync(string publicId, long userId, DateTime now);
    Task<LiveSessionStateDto> VoteAsync(string publicId, long userId, LockLiveSessionVoteRequest request, DateTime now);
    Task<LiveSessionStateDto> RemoveAsync(string publicId, long hostUserId, long participantId, DateTime now);
    Task<LiveSessionStateDto> SetNotificationsAsync(string publicId, long userId, bool enabled, DateTime now);
    Task<IReadOnlyList<LiveSessionEventDto>> EventsAsync(string publicId, long userId, long afterSequence, DateTime now);
    Task<int> ExpireDueAsync(DateTime now);
}

public interface ILiveSessionNotifier
{
    Task StateChangedAsync(string publicId, LiveSessionEventDto @event, CancellationToken cancellationToken = default);
}
