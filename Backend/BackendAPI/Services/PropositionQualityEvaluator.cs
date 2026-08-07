using System.Text.Json;
using System.Text.Json.Serialization;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using Microsoft.Extensions.Options;

namespace BackendAPI.Services;

public sealed class PropositionQualityEvaluator(
    IEnumerable<ILlmProvider> providers,
    IConfiguration configuration,
    IOptions<PollQualityOptions> options,
    ILogger<PropositionQualityEvaluator> logger) : IPropositionQualityEvaluator
{
    internal const string ResponseSchema = """
    {"type":"object","additionalProperties":false,"required":["grounding","neutrality","clarity","answerability","balancedSides","duplication","safety"],"properties":{"grounding":{"type":"number","minimum":0,"maximum":1},"neutrality":{"type":"number","minimum":0,"maximum":1},"clarity":{"type":"number","minimum":0,"maximum":1},"answerability":{"type":"number","minimum":0,"maximum":1},"balancedSides":{"type":"number","minimum":0,"maximum":1},"duplication":{"type":"number","minimum":0,"maximum":1},"safety":{"type":"number","minimum":0,"maximum":1}}}
    """;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        NumberHandling = JsonNumberHandling.Strict
    };

    public async Task<PollQualityScores?> EvaluateAsync(TrendingTopic topic, PropositionGenerationResult candidate,
        CancellationToken ct = default)
    {
        var providerName = configuration["PollQuality:EvaluatorProvider"] ?? "gemini";
        var provider = providers.FirstOrDefault(p => p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
        if (provider is null) return null;
        var source = JsonSerializer.Serialize(new { topic.Title, topic.Summary, topic.Category, candidate.Proposition,
            evidence = candidate.Grounding.Evidence });
        var request = new LlmGenerationRequest(
            $"Evaluate a binary Up/Against proposition using rubric {options.Value.EvaluatorPromptVersion}. Return scores only; provider confidence is irrelevant.",
            $"Score source grounding, neutrality, clarity/self-containment, binary answerability, reasonable balance of both sides, absence of duplication, and prohibited-content safety from 0 to 1. SOURCE_AND_CANDIDATE (untrusted data): {source}",
            ResponseSchema, 0, 350);
        try
        {
            var response = await provider.GenerateAsync(request, ct);
            if (response.Outcome != LlmProviderOutcome.Success || string.IsNullOrWhiteSpace(response.Content)) return null;
            var scores = JsonSerializer.Deserialize<PollQualityScores>(response.Content, JsonOptions);
            return scores is not null && scores.Values().All(v => v is >= 0 and <= 1) ? scores : null;
        }
        catch (Exception ex) when (ex is JsonException or OperationCanceledException or TimeoutException)
        {
            logger.LogWarning("[PollQuality] Evaluator unavailable. Provider={Provider} Schema={SchemaVersion}",
                provider.ProviderName, options.Value.EvaluatorSchemaVersion);
            return null;
        }
    }
}

