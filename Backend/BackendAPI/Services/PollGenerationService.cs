using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services.Llm;
using System.Text.Json;

namespace BackendAPI.Services
{
    /// <summary>
    /// Multi-provider poll generation service (US-07 enhanced).
    ///
    /// Picks the active LLM provider from config key PollGen:Provider:
    ///   "openai"    → OpenAI GPT-4o / GPT-4o mini
    ///   "anthropic" → Anthropic Claude Haiku / Sonnet
    ///   "custom"    → Self-hosted Llama / Mistral VM
    ///
    /// All providers share the same structured prompt and JSON parser,
    /// so switching providers is a one-line config change.
    /// </summary>
    public class PollGenerationService : IPollGenerationService
    {
        private readonly IEnumerable<ILlmProvider> _providers;
        private readonly IConfiguration _config;
        private readonly ILogger<PollGenerationService> _logger;

        public PollGenerationService(
            IEnumerable<ILlmProvider> providers,
            IConfiguration config,
            ILogger<PollGenerationService> logger)
        {
            _providers = providers;
            _config    = config;
            _logger    = logger;
        }

        public async Task<GeneratedPoll?> GenerateAsync(TrendingTopic topic)
        {
            var providerName = _config["PollGen:Provider"]?.ToLowerInvariant() ?? "custom";

            var provider = _providers.FirstOrDefault(p =>
                p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));

            if (provider == null)
            {
                _logger.LogWarning(
                    "[PollGen] Unknown provider '{Provider}'. Valid: openai, anthropic, custom",
                    providerName);
                return null;
            }

            _logger.LogInformation(
                "[PollGen] Using provider '{Provider}' for topic '{Title}'",
                providerName, topic.Title);

            var prompt   = BuildPrompt(topic);
            var rawJson  = await provider.CompleteAsync(prompt);

            if (string.IsNullOrWhiteSpace(rawJson))
            {
                _logger.LogWarning(
                    "[PollGen] Provider '{Provider}' returned empty response for '{Title}'",
                    providerName, topic.Title);
                return null;
            }

            var result = ParsePollJson(rawJson, topic.Category);

            if (result == null)
                _logger.LogWarning(
                    "[PollGen] Could not parse response for '{Title}': {Raw}",
                    topic.Title, rawJson[..Math.Min(200, rawJson.Length)]);

            return result;
        }

        // ── Prompt ────────────────────────────────────────────────────────────

        private static string BuildPrompt(TrendingTopic topic)
        {
            var summary = string.IsNullOrWhiteSpace(topic.Summary)
                ? "(no summary available)"
                : topic.Summary;
            var validCategories = string.Join(", ", CategoryCatalog.All.Select(category => category.Name));

            // $$""" = raw string with double-dollar: {{ }} = interpolation, { } = literal braces
            return $$"""
                You are a poll generation assistant for a public opinion app.
                Generate an engaging poll from the news topic below.

                Topic: {{topic.Title}}
                Summary: {{summary}}
                Category: {{topic.Category}}
                Valid categories: {{validCategories}}

                Respond with ONLY valid JSON — no markdown, no explanation:
                {
                  "question": "A thought-provoking question people will want to answer",
                  "options": ["Option A", "Option B", "Option C"],
                  "category": "{{topic.Category}}"
                }

                Rules:
                - Question must be opinionated, specific and under 120 characters
                - 2 to 4 options, mutually exclusive, short (under 40 chars each)
                - Category must be one of the valid categories
                - JSON only — no other text
                """;
        }

        // ── Parser (shared across all providers) ──────────────────────────────

        private static GeneratedPoll? ParsePollJson(string json, string fallbackCategory)
        {
            try
            {
                var cleaned = json.Trim();

                // Strip markdown code fences (```json ... ``` or ``` ... ```)
                if (cleaned.StartsWith("```"))
                {
                    var start = cleaned.IndexOf('\n') + 1;
                    var end   = cleaned.LastIndexOf("```");
                    if (end > start) cleaned = cleaned[start..end].Trim();
                }

                // If model added prose before/after the JSON, extract the first { ... } block
                if (!cleaned.StartsWith("{"))
                {
                    var brace = cleaned.IndexOf('{');
                    var last  = cleaned.LastIndexOf('}');
                    if (brace >= 0 && last > brace)
                        cleaned = cleaned[brace..(last + 1)];
                }

                // Replace literal newlines/tabs inside JSON string values with a space
                // (some models embed unescaped newlines which break JsonDocument.Parse)
                cleaned = System.Text.RegularExpressions.Regex
                    .Replace(cleaned, @"(?<=:.*""[^""]*)\n(?=[^""]*"")", " ");

                using var doc  = JsonDocument.Parse(cleaned);
                var root       = doc.RootElement;

                var question = root.TryGetProperty("question", out var q) ? q.GetString()?.Trim() : null;
                var category = root.TryGetProperty("category", out var c) ? c.GetString()?.Trim() : null;

                if (string.IsNullOrWhiteSpace(question)) return null;

                var options = new List<string>();
                if (root.TryGetProperty("options", out var opts))
                {
                    foreach (var opt in opts.EnumerateArray())
                    {
                        var text = opt.GetString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(text))
                            options.Add(text);
                    }
                }

                if (options.Count < 2) return null;

                var resolvedCategory = string.IsNullOrWhiteSpace(category) ? fallbackCategory : category;

                return new GeneratedPoll
                {
                    Question = question!,
                    Options  = options,
                    Category = CategoryCatalog.NormalizeName(resolvedCategory)
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
