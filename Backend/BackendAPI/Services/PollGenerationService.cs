using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services.Llm;
using System.Text.Json;
using Microsoft.Extensions.Options;

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
        private readonly IPollsRepository _pollsRepo;
        private readonly IOptionsMonitor<PollGenerationOptions> _options;
        private readonly LlmProviderReadinessService _readiness;
        private readonly ILogger<PollGenerationService> _logger;

        public PollGenerationService(
            IEnumerable<ILlmProvider> providers,
            IPollsRepository pollsRepo,
            IOptionsMonitor<PollGenerationOptions> options,
            LlmProviderReadinessService readiness,
            ILogger<PollGenerationService> logger)
        {
            _providers = providers;
            _pollsRepo = pollsRepo;
            _options   = options;
            _readiness = readiness;
            _logger    = logger;
        }

        public async Task<GeneratedPoll?> GenerateAsync(TrendingTopic topic)
        {
            if (!_options.CurrentValue.Enabled) return null;
            var available = _readiness.GetStatus().Where(x => x.State == LlmProviderReadinessState.Available).Select(x => x.Provider).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var providers = _providers.ToDictionary(x => x.ProviderName, StringComparer.OrdinalIgnoreCase);
            LlmProviderResult? generation = null;
            var request = new LlmGenerationRequest("You are a poll generation assistant. Always respond with valid JSON only, no markdown.", BuildPrompt(topic));
            foreach (var providerName in _options.CurrentValue.ProviderOrder)
            {
                if (!available.Contains(providerName) || !providers.TryGetValue(providerName, out var provider)) continue;
                generation = await provider.GenerateAsync(request);
                _logger.LogInformation("[PollGen] Attempt completed. Provider={Provider} Model={Model} Outcome={Outcome}", generation.Provider, generation.Model, generation.Outcome);
                if (generation.Outcome == LlmProviderOutcome.TransientFailure) continue;
                if (generation.Outcome == LlmProviderOutcome.PermanentFailure) return null;
                break;
            }
            if (generation?.Outcome != LlmProviderOutcome.Success || string.IsNullOrWhiteSpace(generation.Content)) return null;
            var result = ParsePollJson(generation.Content, topic.Category);

            if (result == null)
            {
                _logger.LogWarning(
                    "[PollGen] Could not parse response for topic {TopicId} '{Title}'. Provider={Provider} Model={Model}",
                    topic.Id, topic.Title, generation.Provider, generation.Model);
                return null;
            }

            result.SourceTitle = topic.Title;
            result.SourceUrl = topic.SourceUrl;
            result.GenerationProvider = generation.Provider;
            result.GenerationModel = generation.Model;
            await ApplyQualityChecksAsync(result, topic);

            if (result.QualityWarnings.Any(warning => warning.StartsWith("Rejected:", StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning(
                    "[PollGen] Rejected generated poll for topic {TopicId} '{Title}'. Warnings={Warnings}. Question={Question}",
                    topic.Id, topic.Title, string.Join(" | ", result.QualityWarnings), result.Question);
                return null;
            }

            if (result.QualityWarnings.Count > 0)
            {
                _logger.LogInformation(
                    "[PollGen] Generated poll for topic {TopicId} needs review. Warnings={Warnings}",
                    topic.Id, string.Join(" | ", result.QualityWarnings));
            }

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
                - Question must be clear, specific, neutral, and 30-120 characters
                - Question must be answerable by ordinary readers without niche expertise
                - Do not use vague questions like "What do you think about this?"
                - Exactly 2 options, mutually exclusive and short
                - This is a binary publication contract: options must be exactly ["Up", "Against"] in that order
                - Options must not overlap in meaning and must not be duplicates
                - Preserve the source topic meaning; do not invent facts beyond the summary
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

        private async Task ApplyQualityChecksAsync(GeneratedPoll poll, TrendingTopic topic)
        {
            if (poll.Question.Length < 30)
                poll.QualityWarnings.Add("Rejected: question is too short to be clear.");

            if (poll.Question.Length > 120)
                poll.QualityWarnings.Add("Rejected: question is longer than 120 characters.");

            if (!poll.Question.EndsWith("?"))
                poll.QualityWarnings.Add("Question should be phrased as a question.");

            var generatedCategory = poll.Category;
            if (!IsKnownCategory(generatedCategory))
                poll.QualityWarnings.Add("Rejected: category is not in the allowed catalog.");
            poll.Category = CategoryCatalog.NormalizeName(generatedCategory);

            poll.Options = poll.Options
                .Select(option => option.Trim())
                .Where(option => !string.IsNullOrWhiteSpace(option))
                .ToList();

            if (poll.Options.Count is < 2 or > 4)
                poll.QualityWarnings.Add("Rejected: poll must have 2 to 4 options.");

            if (!BinaryPublicationValidator.IsPublishable(poll))
                poll.QualityWarnings.Add("Rejected: publication requires exactly the options Up and Against.");

            if (poll.Options.GroupBy(NormalizeText).Any(group => group.Count() > 1))
                poll.QualityWarnings.Add("Rejected: options contain duplicates.");

            if (poll.Options.Any(option => option.Length > 40))
                poll.QualityWarnings.Add("Option text should stay under 40 characters.");

            if (HasOverlappingOptions(poll.Options))
                poll.QualityWarnings.Add("Options may overlap and need human review.");

            var similar = await FindSimilarPollAsync(poll.Question, topic.SourceUrl);
            if (similar != null)
            {
                poll.SimilarPollId = similar.Id;
                poll.QualityWarnings.Add($"Similar generated poll detected: #{similar.Id}.");
            }
        }

        private async Task<Poll?> FindSimilarPollAsync(string question, string? sourceUrl)
        {
            var recent = await _pollsRepo.GetRecentGeneratedAsync();
            var normalizedQuestion = NormalizeText(question);

            return recent.FirstOrDefault(existing =>
                (!string.IsNullOrWhiteSpace(sourceUrl)
                    && !string.IsNullOrWhiteSpace(existing.SourceUrl)
                    && existing.SourceUrl.Equals(sourceUrl, StringComparison.OrdinalIgnoreCase))
                || Similarity(normalizedQuestion, NormalizeText(existing.Question)) >= 0.72);
        }

        private static bool HasOverlappingOptions(IEnumerable<string> options)
        {
            var normalized = options.Select(NormalizeText).ToList();
            for (var i = 0; i < normalized.Count; i++)
            {
                for (var j = i + 1; j < normalized.Count; j++)
                {
                    if (normalized[i].Contains(normalized[j]) || normalized[j].Contains(normalized[i]))
                        return true;
                }
            }

            return false;
        }

        private static double Similarity(string left, string right)
        {
            var leftTokens = left.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
            var rightTokens = right.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
            if (leftTokens.Count == 0 || rightTokens.Count == 0) return 0;

            var intersection = leftTokens.Intersect(rightTokens).Count();
            var union = leftTokens.Union(rightTokens).Count();
            return (double)intersection / union;
        }

        private static bool IsKnownCategory(string? category)
        {
            if (string.IsNullOrWhiteSpace(category)) return false;

            var normalized = category.Trim();
            return CategoryCatalog.All.Any(item =>
                item.Name.Equals(normalized, StringComparison.OrdinalIgnoreCase)
                || item.Slug.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeText(string value)
        {
            var chars = value
                .ToLowerInvariant()
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : ' ')
                .ToArray();

            return string.Join(
                ' ',
                new string(chars)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(token => token.Length > 1));
        }
    }
}
