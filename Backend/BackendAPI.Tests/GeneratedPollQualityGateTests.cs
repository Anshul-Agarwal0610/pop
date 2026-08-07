using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace BackendAPI.Tests;

public class GeneratedPollQualityGateTests
{
    [Fact]
    public async Task Deterministic_prohibited_failure_rejects_without_evaluator()
    {
        var evaluator = new FakeEvaluator(High());
        var decision = await Gate(evaluator).EvaluateAsync(Topic(), Candidate("Should officials kill all members of the affected group?"), ["Up", "Against"]);
        Assert.Equal(PollQualityDisposition.Rejected, decision.Disposition);
        Assert.Contains(PollQualityReasonCodes.SafetyProhibited, decision.ReasonCodes);
        Assert.Equal(0, evaluator.Calls);
    }

    [Fact]
    public async Task Valid_candidate_above_threshold_is_accepted_with_versions()
    {
        var decision = await Gate(new FakeEvaluator(High())).EvaluateAsync(Topic(), Candidate(), ["Up", "Against"]);
        Assert.Equal(PollQualityDisposition.Accepted, decision.Disposition);
        Assert.Equal("quality-rules-v1", decision.RulesVersion);
        Assert.Equal(PollGenerationService.GenerationSchemaVersion, decision.GenerationSchemaVersion);
    }

    [Fact]
    public async Task Sensitive_topic_uses_stricter_threshold_and_routes_uncertainty_to_review()
    {
        var scores = High(.85);
        var decision = await Gate(new FakeEvaluator(scores)).EvaluateAsync(
            Topic("Election reform", "Parliament debates election rules"), Candidate(), ["Up", "Against"]);
        Assert.True(decision.IsSensitive);
        Assert.Equal(.9, decision.PublishThreshold);
        Assert.Equal(PollQualityDisposition.NeedsReview, decision.Disposition);
        Assert.Contains(PollQualityReasonCodes.SensitiveReview, decision.ReasonCodes);
    }

    [Fact]
    public async Task Evaluator_failure_routes_to_review_and_confidence_cannot_override_it()
    {
        var candidate = Candidate();
        candidate.Quality.Confidence = 1;
        var decision = await Gate(new FakeEvaluator(null)).EvaluateAsync(Topic(), candidate, ["Up", "Against"]);
        Assert.Equal(PollQualityDisposition.NeedsReview, decision.Disposition);
        Assert.Contains(PollQualityReasonCodes.EvaluatorUnavailable, decision.ReasonCodes);
    }

    [Fact]
    public async Task Exact_duplicate_rejects_without_evaluator()
    {
        var evaluator = new FakeEvaluator(High());
        var decision = await Gate(evaluator, new DuplicateMatch(42, "exact", 1)).EvaluateAsync(Topic(), Candidate(), ["Up", "Against"]);
        Assert.Equal(PollQualityDisposition.Rejected, decision.Disposition);
        Assert.Equal(0, evaluator.Calls);
        Assert.Equal(42, decision.DuplicatePollId);
    }

    private static GeneratedPollQualityGate Gate(FakeEvaluator evaluator, DuplicateMatch? duplicate = null) =>
        new(evaluator, new FakeDuplicateDetector(duplicate), Options.Create(new PollQualityOptions()));

    private static TrendingTopic Topic(string title = "Data privacy law", string summary = "Parliament is considering a privacy law") =>
        new() { Id = 1, Title = title, Summary = summary, Category = "Technology", SourceUrl = "https://example.test/a" };

    private static PropositionGenerationResult Candidate(string question = "Should Parliament adopt the proposed data privacy law?") => new()
    {
        Proposition = question, Category = "Technology",
        Grounding = new SourceGrounding { Rationale = "The source describes the proposal.", Evidence = ["Parliament is considering the law."] },
        Quality = new PropositionQuality { IsSelfContained = true, IsNeutral = true, IsBinary = true, IsGrounded = true,
            Confidence = .99, IsAmbiguous = false, AmbiguityReason = null }, ProviderName = "fake"
    };

    private static PollQualityScores High(double value = .95) => new()
    { Grounding = value, Neutrality = value, Clarity = value, Answerability = value,
      BalancedSides = value, Duplication = value, Safety = value };

    private sealed class FakeEvaluator(PollQualityScores? result) : IPropositionQualityEvaluator
    {
        public int Calls { get; private set; }
        public Task<PollQualityScores?> EvaluateAsync(TrendingTopic topic, PropositionGenerationResult candidate, CancellationToken ct = default)
        { Calls++; return Task.FromResult(result); }
    }

    private sealed class FakeDuplicateDetector(DuplicateMatch? result) : IGeneratedPollDuplicateDetector
    {
        public Task<DuplicateMatch?> FindAsync(string proposition, string? sourceUrl, CancellationToken ct = default) => Task.FromResult(result);
        public string Fingerprint(string proposition) => "fingerprint";
    }
}
