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

        public async Task<LlmCompletionResult> CompleteAsync(LlmGenerationRequest request, CancellationToken ct = default)
        {
            var baseUrl = _config["PollGen:Custom:BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                _logger.LogWarning("[CustomVM] PollGen:Custom:BaseUrl not configured");
                return LlmCompletionResult.Misconfigured(ProviderName);
            }

            var client = _http.CreateClient();
            const string model = "custom";

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
                    var status=(int)response.StatusCode;
                    return new(ProviderName, model, false, null, status, status==429?"rate_limited":"http_error", status==429 || status>=500, status==429, RetryAfter: response.Headers.RetryAfter?.Date);
                }

                // Custom VM is expected to return the JSON poll directly
                return new(ProviderName, model, true, await response.Content.ReadAsStringAsync(ct), (int)response.StatusCode, null, false, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CustomVM] Request failed");
                return new(ProviderName, model, false, null, null, ex is OperationCanceledException ? "timeout" : "transport_error", true, false);
            }
        }
    }
}
