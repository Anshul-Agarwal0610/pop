using BackendAPI.Models;

namespace BackendAPI.Interfaces;

public interface IGeneratedPollQualityGate
{
    Task<GeneratedPollQualityDecision> EvaluateAsync(TrendingTopic topic, PropositionGenerationResult candidate,
        IReadOnlyList<string> options, CancellationToken ct = default);
}

public interface IPropositionQualityEvaluator
{
    Task<PollQualityScores?> EvaluateAsync(TrendingTopic topic, PropositionGenerationResult candidate,
        CancellationToken ct = default);
}

public interface IGeneratedPollDuplicateDetector
{
    Task<DuplicateMatch?> FindAsync(string proposition, string? sourceUrl, CancellationToken ct = default);
    string Fingerprint(string proposition);
}

