using BackendAPI.Models;

namespace BackendAPI.Interfaces;

public enum GenerationOutcomeKind { Poll, TerminalContentDecision, RetryableFailure, TerminalFailure, InvalidOutput }

public sealed record GenerationOutcome(
    GenerationOutcomeKind Kind,
    GeneratedPoll? Poll = null,
    LlmFailureClass FailureClass = LlmFailureClass.None,
    string? Provider = null,
    DateTimeOffset? RetryAtUtc = null,
    string? Reason = null)
{
    public static GenerationOutcome Generated(GeneratedPoll poll) => new(GenerationOutcomeKind.Poll, poll);
}

public interface IPollGenerationService
{
    Task<GenerationOutcome> GenerateAsync(TrendingTopic topic, CancellationToken ct = default);
}

public class GeneratedPoll
{
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public string Category { get; set; } = "General";
    public List<string> QualityWarnings { get; set; } = new();
    public long? SimilarPollId { get; set; }
    public string? SourceTitle { get; set; }
    public string? SourceUrl { get; set; }
    public string ReviewNotes => QualityWarnings.Count == 0
        ? "AI review: passed automated quality checks."
        : $"AI review: {string.Join(" ", QualityWarnings)}";
}
