using BackendAPI.Models;

namespace BackendAPI.Interfaces;

public interface IPredictionsRepository
{
    Task<IReadOnlyList<PredictionOpportunity>> GetAvailableAsync(long userId, string predictionType, int count, DateTime utcNow);
    Task<IReadOnlyList<PredictionEntry>> GetMineAsync(long userId, int count, DateTime utcNow);
    Task<PredictionEntry> CreateAsync(long userId, CreatePredictionRequest request, DateTime utcNow);
    Task<IReadOnlyList<PredictionRewardCandidate>> SettleMineAsync(long userId, DateTime utcNow);
    Task MarkRewardedAsync(long predictionId, int xp);
}
