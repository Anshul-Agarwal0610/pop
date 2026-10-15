using System.Text.Json;
using System.Text.Json.Serialization;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services.Llm;

namespace BackendAPI.Services;

public sealed record TopicPollBatch(List<PropositionGenerationResult> Polls);
public sealed record TopicBatchResult(LlmProviderResult Provider, TopicPollBatch? Batch);
public sealed record MultiTopicPoll(long TopicId, List<PropositionGenerationResult> Polls);
public sealed record MultiTopicPollBatch(List<MultiTopicPoll> Topics);
public sealed record MultiTopicBatchResult(LlmProviderResult Provider, MultiTopicPollBatch? Batch);

// Phase one deliberately uses one provider request, not a generation/evaluation pair.
public sealed class TopicBatchGenerator(IEnumerable<ILlmProvider> providers,
    LlmProviderReadinessService readiness, IConfiguration config)
{
    public async Task<MultiTopicBatchResult> GenerateBatchAsync(IReadOnlyList<TrendingTopic> topics)
    {
        if (topics.Count is < 1 or > 10) throw new ArgumentOutOfRangeException(nameof(topics), "A generation request contains 1 to 10 topics.");
        var name = config["PhaseOne:Provider"] ?? "anthropic";
        var provider = providers.FirstOrDefault(p => p.ProviderName == name);
        if (provider is null || !readiness.GetStatus().Any(p => p.Provider == name && p.State == LlmProviderReadinessState.Available))
            return new(new(LlmProviderOutcome.TransientFailure, Reason: "Selected provider is not configured", Provider: name, FailureClass: LlmFailureClass.Configuration), null);

        var schema = "{\"type\":\"object\",\"additionalProperties\":false,\"required\":[\"topics\"],\"properties\":{\"topics\":{\"type\":\"array\",\"minItems\":1,\"maxItems\":10,\"items\":{\"type\":\"object\",\"additionalProperties\":false,\"required\":[\"topicId\",\"polls\"],\"properties\":{\"topicId\":{\"type\":\"integer\"},\"polls\":{\"type\":\"array\",\"minItems\":1,\"maxItems\":1,\"items\":" + PollGenerationService.ResponseSchema + "}}}}}}";
        JsonDocument.Parse(schema).Dispose();
        var parts = topics.Select(topic => $"TOPIC ID: {topic.Id}\nTITLE: {topic.Title}\nDESCRIPTION: {topic.Summary}");
        var request = PollGenerationService.BuildRequest(topics[0]);
        var categories = string.Join(", ", CategoryCatalog.All.Select(category => category.Name));
        var prompt = $"""
            Return exactly one poll candidate for every source topic below. Never omit a topic and never return an empty polls array. When a source has no explicit proposal, create a neutral opinion, public-response, preparedness, accountability, agreement, or reaction question grounded only in the source. Election, conflict, weather, health, sports, culture, and personal-commentary topics must still receive one poll.

            This request contains one source language only. Write every proposition and both position statements in English, even when the source is Hindi. For supportStatement, write a short affirmative position supporting the proposition. For againstStatement, write a short opposing position. Each statement must be 5-120 characters, neutral, distinct, and understandable beside the question. Use the exact TOPIC ID and never mix evidence between topics. Every proposition must be a 20-160 character, neutral, self-contained question ending in exactly one ?. Never write a headline, prediction, ranking, preference question, or compound question. Evidence excerpts must be verbatim from that topic. Use exactly one of these categories: {categories}.

            Follow the supplied JSON schema exactly.

            {string.Join("\n\n---\n\n", parts)}
            """;
        var result = await provider.GenerateAsync(new(request.SystemInstruction, prompt, schema, .1, 5000));
        if (!result.IsSuccess) return new(result, null);
        try
        {
            var content = ExtractJson(result.Content!);
            var batch = JsonSerializer.Deserialize<MultiTopicPollBatch>(content, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true });
            if (batch?.Topics is null || batch.Topics.Count != topics.Count || batch.Topics.GroupBy(x => x.TopicId).Any(x => x.Count() != 1) || batch.Topics.Any(x => !topics.Any(t => t.Id == x.TopicId) || x.Polls is null || x.Polls.Count != 1 || x.Polls.Any(p => p is null))) throw new JsonException();
            foreach (var poll in batch.Topics.SelectMany(x => x.Polls)) { poll.GenerationProvider = result.Provider; poll.GenerationModel = result.Model; }
            return new(result, batch);
        }
        catch (JsonException) { return new(result with { Outcome = LlmProviderOutcome.PermanentFailure, Reason = "Invalid multi-topic batch JSON", FailureClass = LlmFailureClass.InvalidResponse }, null); }
    }

    private static string ExtractJson(string content)
    {
        var trimmed = content.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstBreak = trimmed.IndexOf('\n');
            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (firstBreak >= 0 && lastFence > firstBreak) trimmed = trimmed[(firstBreak + 1)..lastFence].Trim();
        }
        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        return start >= 0 && end > start ? trimmed[start..(end + 1)] : trimmed;
    }

    public async Task<TopicBatchResult> GenerateAsync(TrendingTopic topic)
    {
        var name = config["PhaseOne:Provider"] ?? "anthropic";
        var provider = providers.FirstOrDefault(p => p.ProviderName == name);
        if (provider is null || !readiness.GetStatus().Any(p => p.Provider == name && p.State == LlmProviderReadinessState.Available))
            return new(new(LlmProviderOutcome.TransientFailure, Reason: "Selected provider is not configured", Provider: name, FailureClass: LlmFailureClass.Configuration), null);
        var schema = "{\"type\":\"object\",\"additionalProperties\":false,\"required\":[\"polls\"],\"properties\":{\"polls\":{\"type\":\"array\",\"minItems\":0,\"maxItems\":5,\"items\":" + PollGenerationService.ResponseSchema + "}}}";
        var single = PollGenerationService.BuildRequest(topic);
        var prompt = single.UserPrompt.Replace("Create exactly one concise", "Create up to five distinct concise") +
            "\nReturn a polls array. Aim for 4-5 different policy/action angles; return fewer or zero if evidence is insufficient. Never invent extra facts to fill the quota. Write in English for an Indian audience. Evidence entries must be verbatim excerpts from the supplied title or description. Do not output Health questions in this public-news phase. Each question must stand alone.";
        prompt += "\nSource text may use an Indian regional language: generate the question in English but keep evidence excerpts verbatim in the source language. Social posts are unverified claims; do not recast them as confirmed news. A trend label or event listing alone does not establish a policy or controversy. Return zero polls unless the source supports a meaningful binary proposition.";
        var result = await provider.GenerateAsync(new(single.SystemInstruction, prompt, schema, .1, 4000));
        if (!result.IsSuccess) return new(result, null);
        try
        {
            var batch = JsonSerializer.Deserialize<TopicPollBatch>(result.Content!, new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow });
            if (batch?.Polls is null || batch.Polls.Count > 5 || batch.Polls.Any(p => p is null)) throw new JsonException();
            foreach (var poll in batch.Polls) { poll.GenerationProvider = result.Provider; poll.GenerationModel = result.Model; }
            return new(result, batch);
        }
        catch (JsonException) { return new(result with { Outcome = LlmProviderOutcome.PermanentFailure, Reason = "Invalid batch JSON", FailureClass = LlmFailureClass.InvalidResponse }, null); }
    }

    public static string? Validate(TrendingTopic topic, PropositionGenerationResult poll, PollQualityOptions policy, double minimumConfidence = .60)
    {
        if (!PollGenerationService.Validate(poll, out var reason, requirePositiveQualityMetadata: false, requireSourceGrounding: false)) return reason;
        poll.Category = CategoryCatalog.NormalizeName(poll.Category);
        var generatedText = $"{poll.Proposition} {poll.SupportStatement} {poll.AgainstStatement}";
        if (policy.ProhibitedTerms.Any(t => generatedText.Contains(t, StringComparison.OrdinalIgnoreCase))) return "Prohibited framing";
        if (new[] { "obviously", "reckless", "disgraceful", "only an idiot", " as well as " }.Any(t => poll.Proposition.Contains(t, StringComparison.OrdinalIgnoreCase))) return "Leading or compound framing";
        return null;
    }
}
