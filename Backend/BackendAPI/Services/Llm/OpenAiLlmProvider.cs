using BackendAPI.Interfaces;
using System.Text;
using System.Text.Json;

namespace BackendAPI.Services.Llm
{
    /// <summary>
    /// Calls the OpenAI Chat Completions API (GPT-4o, GPT-4o mini, etc.).
    /// Also works with any OpenAI-compatible API such as Groq (free).
    ///
    /// Config keys:
    ///   PollGen:OpenAI:ApiKey   — sk-...  (OpenAI) or gsk_... (Groq)
    ///   PollGen:OpenAI:Model    — "gpt-4o-mini" (OpenAI) | "llama-3.3-70b-versatile" (Groq)
    ///   PollGen:OpenAI:BaseUrl  — optional, overrides endpoint (e.g. https://api.groq.com/openai/v1)
    ///
    /// To use Groq (free): set BaseUrl = https://api.groq.com/openai/v1
    ///                           ApiKey = your Groq key (gsk_...)
    ///                           Model  = llama-3.3-70b-versatile
    /// </summary>
    public class OpenAiLlmProvider : ILlmProvider
    {
        public string ProviderName => "openai";

        private const string DefaultEndpoint = "https://api.openai.com/v1/chat/completions";

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

            var model    = _config["PollGen:OpenAI:Model"] ?? "gpt-4o-mini";
            var baseUrl  = _config["PollGen:OpenAI:BaseUrl"];
            var endpoint = string.IsNullOrWhiteSpace(baseUrl)
                ? DefaultEndpoint
                : $"{baseUrl.TrimEnd('/')}/chat/completions";

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
                max_tokens      = 1024,
                response_format = new { type = "json_object" }  // forces clean JSON — works on Gemini OpenAI-compat endpoint
            };

            var content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            try
            {
                using var response = await client.PostAsync(endpoint, content, ct);

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
