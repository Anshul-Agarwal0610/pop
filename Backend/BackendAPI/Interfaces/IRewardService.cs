using BackendAPI.Models;

namespace BackendAPI.Interfaces;

public interface IRewardService
{
    Task<RewardGrantResult> GrantAsync(RewardGrantRequest request, CancellationToken cancellationToken = default);
    Task<RewardEvent> ReverseAsync(long eventId, long actorUserId, string reason, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<RewardEvent> AdjustAsync(long userId, int value, long actorUserId, string reason, string idempotencyKey, CancellationToken cancellationToken = default);
}
