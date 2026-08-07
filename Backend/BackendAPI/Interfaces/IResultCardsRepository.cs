using BackendAPI.Models;

namespace BackendAPI.Interfaces;

public interface IResultCardsRepository
{
    Task<ResultCardDto> IssueAsync(NormalizedMultiplayerResult result);
    Task<ResultCardDto?> GetForParticipantAsync(long sessionId, long userId);
    Task<ResultCardPage> GetMineAsync(long userId, int offset, int limit);
    Task<StoredResultCard?> GetPublicAsync(string token);
}
