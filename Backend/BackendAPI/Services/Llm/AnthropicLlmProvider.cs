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

        public async Task<LlmProviderResult> CompleteAsync(LlmGenerationRequest request, CancellationToken ct = default)
        {
            var apiKey = _config["PollGen:Anthropic:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("[Anthropic] PollGen:Anthropic:ApiKey not configured");
                return LlmProviderResult.Permanent("PollGen:Anthropic:ApiKey not configured");
            }

            var model = _config["PollGen:Anthropic:Model"];
            if (string.IsNullOrWhiteSpace(model)) return LlmProviderResult.Permanent("PollGen:Anthropic:Model not configured");

            var client = _http.CreateClient();
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", AnthropicVersion);

            var body = new
            {
                model,
                max_tokens = request.MaxOutputTokens,
                temperature = request.Temperature,
                system = request.SystemInstruction + " JSON schema: " + request.ResponseSchema,
                messages   = new[]
                {
                    new { role = "user", content = request.UserPrompt }
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
                    return IsTransient(response.StatusCode) ? LlmProviderResult.Transient($"HTTP {(int)response.StatusCode}") : LlmProviderResult.Permanent($"HTTP {(int)response.StatusCode}");
                }

                var json = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);

                // Extract text from content[0].text
                var text = doc.RootElement
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString();
                return string.IsNullOrWhiteSpace(text) ? LlmProviderResult.Permanent("provider returned empty content") : LlmProviderResult.Succeeded(text);
            }
            catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "[Anthropic] Request timed out");
                return LlmProviderResult.Transient("request timed out");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[Anthropic] Request failed");
                return LlmProviderResult.Transient(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Anthropic] Invalid provider response");
                return LlmProviderResult.Permanent(ex.Message);
            }
        }
        private static bool IsTransient(System.Net.HttpStatusCode status) => status is System.Net.HttpStatusCode.RequestTimeout or System.Net.HttpStatusCode.TooManyRequests || (int)status >= 500;
    }
}
