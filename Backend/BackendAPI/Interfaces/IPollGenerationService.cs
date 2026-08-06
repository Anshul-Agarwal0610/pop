using BackendAPI.Models;
using System.Text.Json.Serialization;

namespace BackendAPI.Interfaces;

public interface IPollGenerationService { Task<PropositionGenerationResult?> GenerateAsync(TrendingTopic topic); }

public sealed class PropositionGenerationResult
{
    [JsonPropertyName("proposition")] public required string Proposition { get; set; }
    [JsonPropertyName("category")] public required string Category { get; set; }
    [JsonPropertyName("sourceGrounding")] public required SourceGrounding Grounding { get; set; }
    [JsonPropertyName("quality")] public required PropositionQuality Quality { get; set; }
    [JsonIgnore] public List<string> QualityWarnings { get; set; } = new();
    [JsonIgnore] public long? SimilarPollId { get; set; }
    [JsonIgnore] public string? SourceTitle { get; set; }
    [JsonIgnore] public string? SourceUrl { get; set; }
    [JsonIgnore] public string ReviewNotes => $"AI validation: {Grounding.Rationale} Evidence: {string.Join("; ", Grounding.Evidence)} Confidence: {Quality.Confidence:0.00}. {string.Join(" ", QualityWarnings)}".Trim();
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
