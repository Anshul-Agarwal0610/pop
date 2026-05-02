using BackendAPI.Interfaces;
using System.Text;
using System.Text.Json;

namespace BackendAPI.Services.Llm
{
    /// <summary>
    /// Calls the OpenAI Chat Completions API (GPT-4o, GPT-4o mini, etc.).
    ///
    /// Config keys:
    ///   PollGen:OpenAI:ApiKey  — sk-...
    ///   PollGen:OpenAI:Model   — "gpt-4o-mini" (default) | "gpt-4o" | "gpt-3.5-turbo"
    /// </summary>
    public class OpenAiLlmProvider : ILlmProvider
    {
        public string ProviderName => "openai";

        private const string Endpoint = "https://api.openai.com/v1/chat/completions";

        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;
        private readonly ILogger<OpenAiLlmProvider> _logger;

        public OpenAiLlmProvider(
            IHttpClientFactory http,
            IConfiguration config,
            ILogger<OpenAiLlmProvider> logger)
        {
            _http   = http;
            _config = config;
            _logger = logger;
        }

        public async Task<string?> CompleteAsync(string prompt, CancellationToken ct = default)
        {
            var apiKey = _config["PollGen:OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("[OpenAI] PollGen:OpenAI:ApiKey not configured");
                return null;
            }

            var model = _config["PollGen:OpenAI:Model"] ?? "gpt-4o-mini";

            var client = _http.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var body = new
            {
                model,
                messages = new[]
                {
                    new { role = "system", content = "You are a poll generation assistant. Always respond with valid JSON only, no markdown." },
                    new { role = "user",   content = prompt }
                },
                temperature     = 0.7,
                max_tokens      = 512,
                response_format = new { type = "json_object" }  // forces JSON output
            };

            var content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            try
            {
                using var response = await client.PostAsync(Endpoint, content, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning("[OpenAI] HTTP {Status}: {Error}", (int)response.StatusCode, err);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);

                // Extract content from choices[0].message.content
                return doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OpenAI] Request failed");
                return null;
            }
        }
    }
}
