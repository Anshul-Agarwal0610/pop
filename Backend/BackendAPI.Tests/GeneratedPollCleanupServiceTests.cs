using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BackendAPI.Tests;

public class GeneratedPollCleanupServiceTests
{
    [Fact]
    public async Task Dry_run_reports_without_writes()
    {
        var repository = new FakeRepository(Candidate(1, 0));
        var report = await Service(repository).DryRunAsync(1, 10, 10);
        Assert.Equal(1, report.MalformedCount);
        Assert.Empty(repository.AppliedPolls);
        Assert.All(report.Groups, group => Assert.Equal("unvoted", group.VoteClass));
    }

    [Fact]
    public async Task Execute_handles_voted_and_unvoted_and_rerun_is_idempotent()
    {
        var repository = new FakeRepository(Candidate(1, 0), Candidate(2, 4));
        var service = Service(repository);
        var first = await service.ExecuteAsync(1, 10, 10);
        var second = await service.ExecuteAsync(1, 10, 10);
        Assert.Equal(2, first.ChangedCount);
        Assert.Equal(0, second.ChangedCount);
        Assert.Equal(GeneratedPollCleanupPolicy.DeactivateAndRegenerate, repository.Dispositions[1]);
        Assert.Equal(GeneratedPollCleanupPolicy.PreserveAndHide, repository.Dispositions[2]);
    }

    [Fact]
    public async Task One_failure_does_not_stop_later_candidate()
    {
        var repository = new FakeRepository(Candidate(1, 0), Candidate(2, 0)) { FailPollId = 1 };
        var report = await Service(repository).ExecuteAsync(1, 10, 10);
        Assert.Equal(1, report.FailedCount);
        Assert.Contains(2, repository.AppliedPolls);
    }

    [Fact]
    public async Task Repository_final_recount_can_switch_to_preserve()
    {
        var repository = new FakeRepository(Candidate(1, 0)) { VoteAppearsForPollId = 1 };
        await Service(repository).ExecuteAsync(1, 10, 10);
        Assert.Equal(GeneratedPollCleanupPolicy.PreserveAndHide, repository.Dispositions[1]);
    }

    private static GeneratedPollCleanupService Service(FakeRepository repository) =>
        new(repository, new GeneratedPollCleanupClassifier(), NullLogger<GeneratedPollCleanupService>.Instance);

    private static GeneratedPollCleanupCandidate Candidate(long id, long votes) => new()
    {
        PollId = id, IsAIGenerated = true, IsActive = true, VoteCount = votes, Question = "Which choice is best?",
        Options = [new() { Text = "Yes" }, new() { Text = "No" }]
    };

    private sealed class FakeRepository(params GeneratedPollCleanupCandidate[] candidates) : IGeneratedPollCleanupRepository
    {
        public List<long> AppliedPolls { get; } = [];
        public Dictionary<long, string> Dispositions { get; } = [];
        public long? FailPollId { get; set; }
        public long? VoteAppearsForPollId { get; set; }
        private readonly HashSet<long> _completed = [];
        public Task<IReadOnlyList<GeneratedPollCleanupCandidate>> GetCandidatesAsync(long from, long to, int max) =>
            Task.FromResult<IReadOnlyList<GeneratedPollCleanupCandidate>>(candidates.Take(max).ToArray());
        public Task<CleanupApplyResult> ApplyAsync(long pollId, Guid runId, string version, IReadOnlyList<string> reasons, string source)
        {
            if (FailPollId == pollId) throw new InvalidOperationException("simulated partial failure");
            var changed = _completed.Add(pollId); AppliedPolls.Add(pollId);
            var candidate = candidates.Single(x => x.PollId == pollId);
            var disposition = candidate.VoteCount > 0 || VoteAppearsForPollId == pollId
                ? GeneratedPollCleanupPolicy.PreserveAndHide : GeneratedPollCleanupPolicy.DeactivateAndRegenerate;
            Dispositions[pollId] = disposition;
            return Task.FromResult(new CleanupApplyResult(changed, disposition, changed ? "Completed" : "Completed"));
        }
        public Task<IReadOnlyList<RegenerationQueueItem>> ClaimRegenerationBatchAsync(int max) => Task.FromResult<IReadOnlyList<RegenerationQueueItem>>([]);
        public Task<TrendingTopic?> ResolveTopicAsync(RegenerationQueueItem item) => Task.FromResult<TrendingTopic?>(null);
        public Task CompleteRegenerationAsync(RegenerationQueueItem item, long replacementPollId) => Task.CompletedTask;
        public Task FailRegenerationAsync(RegenerationQueueItem item, string error) => Task.CompletedTask;
    }
}
