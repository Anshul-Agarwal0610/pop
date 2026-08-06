using BackendAPI.Models;

namespace BackendAPI.Interfaces;

public interface IGameSessionsRepository
{
    Task<GameSessionDto?> GetActiveAsync(long userId, DateTime utcNow);
    Task<GameSessionDto?> GetAsync(long id, long userId, DateTime utcNow);
    Task<GameSessionDto> StartOrResumeAsync(long userId, StartGameSessionRequest request, DateTime utcNow);
    Task<GameVoteResult> VoteAsync(long id, long userId, GameVoteRequest request, DateTime utcNow);
    Task<GameSessionDto> CompleteAsync(long id, long userId, DateTime utcNow);
}
