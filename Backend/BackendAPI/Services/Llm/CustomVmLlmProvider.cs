using BackendAPI.Interfaces;
using System.Text;
using System.Text.Json;

namespace BackendAPI.Services.Llm
{
    /// <summary>
    /// Calls a self-hosted Llama / Mistral VM endpoint.
    ///
    /// Config keys:
    ///   PollGen:Custom:BaseUrl  — e.g. http://your-vm-ip:8000
    ///   PollGen:Custom:ApiKey   — optional bearer token (leave empty if no auth)
    ///
    /// Expected: POST {BaseUrl}/generate
    ///   Request:  { "prompt": "..." }
    ///   Response: { "question": "...", "options": [...], "category": "..." }
    /// </summary>
    public class CustomVmLlmProvider : ILlmProvider
    {
        public string ProviderName => "custom";

        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;
        private readonly ILogger<CustomVmLlmProvider> _logger;

        public CustomVmLlmProvider(
            IHttpClientFactory http,
            IConfiguration config,
            ILogger<CustomVmLlmProvider> logger)
        {
            _http   = http;
            _config = config;
            _logger = logger;
        }

        public async Task<LlmProviderResult> CompleteAsync(LlmGenerationRequest request, CancellationToken ct = default)
        {
            var baseUrl = _config["PollGen:Custom:BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                _logger.LogWarning("[CustomVM] PollGen:Custom:BaseUrl not configured");
                return LlmProviderResult.Permanent("PollGen:Custom:BaseUrl not configured");
            }

            var client = _http.CreateClient();

            var apiKey = _config["PollGen:Custom:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey))
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var body = new { systemInstruction = request.SystemInstruction, prompt = request.UserPrompt, responseSchema = JsonSerializer.Deserialize<JsonElement>(request.ResponseSchema), temperature = request.Temperature, maxOutputTokens = request.MaxOutputTokens };
            var content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            try
            {
                using var response = await client.PostAsync(
                    $"{baseUrl.TrimEnd('/')}/generate", content, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[CustomVM] HTTP {Status}", (int)response.StatusCode);
                    return IsTransient(response.StatusCode) ? LlmProviderResult.Transient($"HTTP {(int)response.StatusCode}") : LlmProviderResult.Permanent($"HTTP {(int)response.StatusCode}");
                }

                // Custom VM is expected to return the JSON poll directly
                var text = await response.Content.ReadAsStringAsync(ct);
                return string.IsNullOrWhiteSpace(text) ? LlmProviderResult.Permanent("provider returned empty content") : LlmProviderResult.Succeeded(text);
            }
            catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "[CustomVM] Request timed out");
                return LlmProviderResult.Transient("request timed out");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[CustomVM] Request failed");
                return LlmProviderResult.Transient(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CustomVM] Invalid provider response");
                return LlmProviderResult.Permanent(ex.Message);
            }
        }
        private static bool IsTransient(System.Net.HttpStatusCode status) => status is System.Net.HttpStatusCode.RequestTimeout or System.Net.HttpStatusCode.TooManyRequests || (int)status >= 500;
    }
}
