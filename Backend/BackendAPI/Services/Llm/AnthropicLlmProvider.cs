using BackendAPI.Interfaces;
using System.Text;
using System.Text.Json;

namespace BackendAPI.Services.Llm
{
    /// <summary>
    /// Calls the Anthropic Messages API (Claude Haiku, Sonnet, etc.).
    ///
    /// Config keys:
    ///   PollGen:Anthropic:ApiKey  — sk-ant-...
    ///   PollGen:Anthropic:Model   — "claude-haiku-4-5" (default) | "claude-sonnet-4-5"
    /// </summary>
    public class AnthropicLlmProvider : ILlmProvider
    {
        public string ProviderName => "anthropic";

        private const string Endpoint        = "https://api.anthropic.com/v1/messages";
        private const string AnthropicVersion = "2023-06-01";

        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;
        private readonly ILogger<AnthropicLlmProvider> _logger;

        public AnthropicLlmProvider(
            IHttpClientFactory http,
            IConfiguration config,
            ILogger<AnthropicLlmProvider> logger)
        {
            _http   = http;
            _config = config;
            _logger = logger;
        }

        public async Task<string?> CompleteAsync(string prompt, CancellationToken ct = default)
        {
            var apiKey = _config["PollGen:Anthropic:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("[Anthropic] PollGen:Anthropic:ApiKey not configured");
                return null;
            }

            var model = _config["PollGen:Anthropic:Model"] ?? "claude-haiku-4-5";

            var client = _http.CreateClient();
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", AnthropicVersion);

            var body = new
            {
                model,
                max_tokens = 512,
                system     = "You are a poll generation assistant. Always respond with valid JSON only, no markdown, no explanation.",
                messages   = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            try
            {
                using var response = await client.PostAsync(Endpoint, content, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning("[Anthropic] HTTP {Status}: {Error}", (int)response.StatusCode, err);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);

                // Extract text from content[0].text
                return doc.RootElement
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Anthropic] Request failed");
                return null;
            }
        }
    }
}
