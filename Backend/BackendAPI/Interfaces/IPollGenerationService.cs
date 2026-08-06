using BackendAPI.Models;
using BackendAPI.Services;
using System.Text.Json.Serialization;

namespace BackendAPI.Interfaces;

public enum GenerationOutcome { Succeeded, ProviderTransientFailure, ProviderPermanentFailure, ContentRejected, Unconvertible }

public sealed record PollGenerationOutcome(GenerationOutcome Outcome, PropositionGenerationResult? Poll = null,
    string? Reason = null, string? AttemptedMethod = null);

public interface IPollGenerationService { Task<PollGenerationOutcome> GenerateAsync(TrendingTopic topic); }

public sealed class PropositionGenerationResult
{
    [JsonPropertyName("proposition")] public required string Proposition { get; set; }
    [JsonPropertyName("category")] public required string Category { get; set; }
    [JsonPropertyName("sourceGrounding")] public required SourceGrounding Grounding { get; set; }
    [JsonPropertyName("quality")] public required PropositionQuality Quality { get; set; }
    [JsonIgnore] public List<string> Options { get; } = [GeneratedPollContract.Up, GeneratedPollContract.Against];
    [JsonIgnore] public string GenerationMethod { get; set; } = GenerationMethods.Llm;
    [JsonIgnore] public List<string> QualityWarnings { get; set; } = [];
    [JsonIgnore] public long? SimilarPollId { get; set; }
    [JsonIgnore] public string? SourceTitle { get; set; }
    [JsonIgnore] public string? SourceUrl { get; set; }
    [JsonIgnore] public string ReviewNotes => $"{GenerationMethod} validation: {Grounding.Rationale} Evidence: {string.Join("; ", Grounding.Evidence)} Confidence: {Quality.Confidence:0.00}. {string.Join(" ", QualityWarnings)}".Trim();
}
public sealed class SourceGrounding
{
    [JsonPropertyName("rationale")] public required string Rationale { get; set; }
    [JsonPropertyName("evidence")] public required List<string> Evidence { get; set; }
}
public sealed class PropositionQuality
{
    [JsonPropertyName("isSelfContained")] public required bool IsSelfContained { get; set; }
    [JsonPropertyName("isNeutral")] public required bool IsNeutral { get; set; }
    [JsonPropertyName("isBinary")] public required bool IsBinary { get; set; }
    [JsonPropertyName("isGrounded")] public required bool IsGrounded { get; set; }
    [JsonPropertyName("confidence")] public required double Confidence { get; set; }
    [JsonPropertyName("isAmbiguous")] public required bool IsAmbiguous { get; set; }
    [JsonPropertyName("ambiguityReason")] public required string? AmbiguityReason { get; set; }
}
