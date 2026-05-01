using BackendAPI.Interfaces;
using BackendAPI.Models;
using System.Text;
using System.Text.Json;

namespace BackendAPI.Services
{
    /// <summary>
    /// US-07 — Calls the deployed Llama/Mistral VM endpoint to generate a poll
    /// from a TrendingTopic.
    ///
    /// Config keys (appsettings.json / Azure App Settings):
    ///   PollGen:BaseUrl  — e.g. http://your-vm-ip:8000   (placeholder until real URL is provided)
    ///   PollGen:ApiKey   — optional bearer token (leave empty if VM has no auth)
    ///
    /// Expected request  POST /generate
    ///   { "title": "...", "summary": "...", "category": "..." }
    ///
    /// Expected response (model must return valid JSON):
    ///   {
    ///     "question": "...",
    ///     "options":  ["...", "...", "...", "..."],
    ///     "category": "..."
    ///   }
    /// </summary>
    public class PollGenerationService : IPollGenerationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<PollGenerationService> _logger;

        public PollGenerationService(
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            ILogger<PollGenerationService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _config            = config;
            _logger            = logger;
        }

        public async Task<GeneratedPoll?> GenerateAsync(TrendingTopic topic)
        {
            var baseUrl = _config["PollGen:BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                _logger.LogWarning("PollGen:BaseUrl not configured — skipping poll generation");
                return null;
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                // Attach bearer token if configured
                var apiKey = _config["PollGen:ApiKey"];
                if (!string.IsNullOrWhiteSpace(apiKey))
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                var payload = new
                {
                    title    = topic.Title,
                    summary  = topic.Summary,
                    category = topic.Category
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                using var response = await client.PostAsync($"{baseUrl.TrimEnd('/')}/generate", content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "PollGen VM returned {Status} for topic '{Title}'",
                        (int)response.StatusCode, topic.Title);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                return ParseResponse(json, topic.Category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PollGen failed for topic '{Title}'", topic.Title);
                return null;
            }
        }

        private static GeneratedPoll? ParseResponse(string json, string fallbackCategory)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var question = root.TryGetProperty("question", out var q) ? q.GetString() : null;
                var category = root.TryGetProperty("category", out var c) ? c.GetString() : fallbackCategory;

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

                // Need at least 2 options to form a valid poll
                if (options.Count < 2) return null;

                return new GeneratedPoll
                {
                    Question = question!,
                    Options  = options,
                    Category = category ?? fallbackCategory
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
