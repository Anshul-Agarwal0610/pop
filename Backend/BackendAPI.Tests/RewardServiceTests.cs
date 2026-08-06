using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Repository;
using BackendAPI.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BackendAPI.Tests;

public sealed class RewardServiceTests
{
    [Fact]
    public async Task Grant_passes_semantic_request_and_returns_snapshotted_event()
    {
        var repository = new FakeRewardRepository();
        var service = new RewardService(repository, NullLogger<RewardService>.Instance);
        var at = new DateTime(2026, 8, 6, 10, 0, 0, DateTimeKind.Utc);

        var result = await service.GrantAsync(new(7, RewardRuleCodes.VoteStandard, "vote", "poll:42", at));

        Assert.Equal(25, result.Event.Value);
        Assert.Equal("Vote cast", result.Event.Reason);
        Assert.Equal(at, result.Event.CreatedAt);
        Assert.Equal("poll:42", repository.LastRequest!.SourceReference);
    }

    [Fact]
    public async Task Repeated_source_is_idempotent()
    {
        var repository = new FakeRewardRepository();
        var service = new RewardService(repository, NullLogger<RewardService>.Instance);
        var request = new RewardGrantRequest(7, RewardRuleCodes.VoteStandard, "vote", "poll:42", DateTime.UtcNow);

        var first = await service.GrantAsync(request);
        var replay = await service.GrantAsync(request);

        Assert.False(first.IsDuplicate);
        Assert.True(replay.IsDuplicate);
        Assert.Equal(first.Event.Id, replay.Event.Id);
        Assert.Equal(25, replay.CurrentXp);
    }

    [Fact]
    public async Task Relay_completed_transfer_source_is_idempotent()
    {
        var service = new RewardService(new FakeRewardRepository(), NullLogger<RewardService>.Instance);
        var request = new RewardGrantRequest(7, RewardRuleCodes.RelayCompleted, "relay-transfer", "relay-transfer:8:3", DateTime.UtcNow);
        Assert.False((await service.GrantAsync(request)).IsDuplicate);
        Assert.True((await service.GrantAsync(request)).IsDuplicate);
    }

    [Fact]
    public async Task Different_relay_milestones_are_independently_awardable()
    {
        var service = new RewardService(new FakeRewardRepository(), NullLogger<RewardService>.Instance);
        var at=DateTime.UtcNow;
        Assert.False((await service.GrantAsync(new(7,RewardRuleCodes.RelayMilestone,"relay-milestone","relay-milestone:8:3",at))).IsDuplicate);
        Assert.False((await service.GrantAsync(new(7,RewardRuleCodes.RelayMilestone,"relay-milestone","relay-milestone:8:5",at))).IsDuplicate);
    }

    [Fact]
    public async Task Non_utc_grant_is_rejected()
    {
        var service = new RewardService(new FakeRewardRepository(), NullLogger<RewardService>.Instance);
        await Assert.ThrowsAsync<ArgumentException>(() => service.GrantAsync(
            new(1, RewardRuleCodes.VoteStandard, "vote", "poll:1", DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local))));
    }

    [Fact]
    public void Period_windows_use_utc_calendar_boundaries()
    {
        var sunday = new DateTime(2026, 8, 9, 23, 59, 0, DateTimeKind.Utc);
        Assert.Equal(new DateTime(2026, 8, 9, 0, 0, 0, DateTimeKind.Utc), RewardRepository.GetPeriodStart(sunday, "day", 1));
        Assert.Equal(new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc), RewardRepository.GetPeriodStart(sunday, "week", 1));
    }

    [Fact]
    public async Task Adjustment_requires_nonzero_value_reason_and_key()
    {
        var service = new RewardService(new FakeRewardRepository(), NullLogger<RewardService>.Instance);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.AdjustAsync(1, 0, 2, "reason", "key"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.AdjustAsync(1, 5, 2, "", "key"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.ReverseAsync(1, 2, "reason", ""));
    }

    private sealed class FakeRewardRepository : IRewardRepository
    {
        private readonly Dictionary<string, RewardLedgerEvent> _events = new();
        public RewardGrantRequest? LastRequest { get; private set; }
        public Task<RewardGrantResult> GrantAsync(RewardGrantRequest request, CancellationToken cancellationToken=default)
        {
            LastRequest=request; var key=$"{request.UserId}:{request.SourceType}:{request.SourceReference}";
            if(_events.TryGetValue(key,out var old)) return Task.FromResult(new RewardGrantResult(old,25,true));
            var e=new RewardLedgerEvent{Id=1,UserId=request.UserId,RuleCode=request.RuleCode,RuleVersion=1,Reason="Vote cast",SourceType=request.SourceType,SourceReference=request.SourceReference,SourceKey=key,Value=25,CreatedAt=request.OccurredAtUtc};
            _events[key]=e; return Task.FromResult(new RewardGrantResult(e,25,false));
        }
        public Task<RewardLedgerEvent> ReverseAsync(long eventId,long actorUserId,string reason,string key,CancellationToken token=default)=>throw new NotImplementedException();
        public Task<RewardLedgerEvent> AdjustAsync(long userId,int value,long actorUserId,string reason,string key,CancellationToken token=default)=>throw new NotImplementedException();
        public Task<IEnumerable<RewardLedgerEvent>> GetEventsAsync(long? userId,int count,CancellationToken token=default)=>throw new NotImplementedException();
        public Task<IEnumerable<RewardRule>> GetActiveRulesAsync(DateTime at,CancellationToken token=default)=>throw new NotImplementedException();
        public Task<IEnumerable<RewardReconciliation>> GetReconciliationAsync(CancellationToken token=default)=>throw new NotImplementedException();
        public Task<IEnumerable<SuspiciousRewardActivity>> GetSuspiciousAsync(DateTime since,int minimum,CancellationToken token=default)=>throw new NotImplementedException();
    }
}
