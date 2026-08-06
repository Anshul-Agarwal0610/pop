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

        public async Task<LlmProviderResult> CompleteAsync(LlmGenerationRequest request, CancellationToken ct = default)
        {
            var apiKey = _config["PollGen:OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("[OpenAI] PollGen:OpenAI:ApiKey not configured");
                return LlmProviderResult.Permanent("PollGen:OpenAI:ApiKey not configured");
            }

            var model = _config["PollGen:OpenAI:Model"];
            if (string.IsNullOrWhiteSpace(model)) return LlmProviderResult.Permanent("PollGen:OpenAI:Model not configured");
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
                    new { role = "system", content = request.SystemInstruction },
                    new { role = "user", content = request.UserPrompt }
                },
                temperature = request.Temperature,
                max_tokens = request.MaxOutputTokens,
                response_format = new { type = "json_schema", json_schema = new { name = "binary_proposition", strict = true, schema = JsonSerializer.Deserialize<JsonElement>(request.ResponseSchema) } }
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
                    return IsTransient(response.StatusCode) ? LlmProviderResult.Transient($"HTTP {(int)response.StatusCode}") : LlmProviderResult.Permanent($"HTTP {(int)response.StatusCode}");
                }

                var json = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);

                // Extract content from choices[0].message.content
                var text = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();
                return string.IsNullOrWhiteSpace(text) ? LlmProviderResult.Permanent("provider returned empty content") : LlmProviderResult.Succeeded(text);
            }
            catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "[OpenAI] Request timed out");
                return LlmProviderResult.Transient("request timed out");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[OpenAI] Request failed");
                return LlmProviderResult.Transient(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OpenAI] Invalid provider response");
                return LlmProviderResult.Permanent(ex.Message);
            }
        }
        private static bool IsTransient(System.Net.HttpStatusCode status) => status is System.Net.HttpStatusCode.RequestTimeout or System.Net.HttpStatusCode.TooManyRequests || (int)status >= 500;
    }
}
