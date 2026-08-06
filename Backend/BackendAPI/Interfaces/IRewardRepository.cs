using BackendAPI.Models;

namespace BackendAPI.Interfaces;

public interface IRewardRepository
{
    Task<RewardGrantResult> GrantAsync(RewardGrantRequest request, CancellationToken cancellationToken = default);
    Task<RewardEvent> ReverseAsync(long eventId, long actorUserId, string reason, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<RewardEvent> AdjustAsync(long userId, int value, long actorUserId, string reason, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<IEnumerable<RewardEvent>> GetEventsAsync(long? userId, int count, CancellationToken cancellationToken = default);
    Task<IEnumerable<RewardRule>> GetActiveRulesAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task<IEnumerable<RewardReconciliation>> GetReconciliationAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<SuspiciousRewardActivity>> GetSuspiciousAsync(DateTime sinceUtc, int minimumEvents, CancellationToken cancellationToken = default);
}
