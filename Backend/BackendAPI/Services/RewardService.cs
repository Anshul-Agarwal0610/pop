using System.Diagnostics.Metrics;
using BackendAPI.Interfaces;
using BackendAPI.Models;

namespace BackendAPI.Services;

public sealed class RewardService : IRewardService
{
    public const string MeterName = "Pollify.Rewards";
    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> Outcomes = Meter.CreateCounter<long>("pollify.reward.outcomes");
    private readonly IRewardRepository _repository;
    private readonly ILogger<RewardService> _logger;

    public RewardService(IRewardRepository repository, ILogger<RewardService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<RewardGrantResult> GrantAsync(RewardGrantRequest request, CancellationToken cancellationToken = default)
    {
        if (request.OccurredAtUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Reward timestamps must be UTC.");
        try
        {
            var result = await _repository.GrantAsync(request, cancellationToken);
            var outcome = result.IsDuplicate ? "duplicate" : "granted";
            Outcomes.Add(1, new("rule", request.RuleCode), new("outcome", outcome));
            _logger.LogInformation("Reward {Outcome}: rule {RuleCode}, user {UserId}, source {SourceType}/{SourceReference}",
                outcome, request.RuleCode, request.UserId, request.SourceType, request.SourceReference);
            return result;
        }
        catch
        {
            Outcomes.Add(1, new("rule", request.RuleCode), new("outcome", "failed"));
            _logger.LogError("Reward failed: rule {RuleCode}, user {UserId}, source {SourceType}/{SourceReference}",
                request.RuleCode, request.UserId, request.SourceType, request.SourceReference);
            throw;
        }
    }

    public Task<RewardLedgerEvent> ReverseAsync(long eventId, long actorUserId, string reason, string idempotencyKey, CancellationToken cancellationToken = default)
        => _repository.ReverseAsync(eventId, actorUserId, RequireReason(reason), RequireKey(idempotencyKey), cancellationToken);

    public Task<RewardLedgerEvent> AdjustAsync(long userId, int value, long actorUserId, string reason, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (value == 0) throw new ArgumentOutOfRangeException(nameof(value), "Adjustment must be non-zero.");
        return _repository.AdjustAsync(userId, value, actorUserId, RequireReason(reason), RequireKey(idempotencyKey), cancellationToken);
    }

    private static string RequireReason(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("A reason is required.") : value.Trim();
    private static string RequireKey(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("An idempotency key is required.") : value.Trim();
}
