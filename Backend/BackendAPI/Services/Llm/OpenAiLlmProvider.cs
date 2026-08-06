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

        public async Task<LlmCompletionResult> CompleteAsync(LlmGenerationRequest request, CancellationToken ct = default)
        {
            var apiKey = _config["PollGen:OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("[OpenAI] PollGen:OpenAI:ApiKey not configured");
                return LlmCompletionResult.Misconfigured(ProviderName);
            }

            var model = _config["PollGen:OpenAI:Model"];
            if (string.IsNullOrWhiteSpace(model)) return LlmCompletionResult.Misconfigured(ProviderName);
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
                    var status=(int)response.StatusCode;
                    _logger.LogWarning("[OpenAI] HTTP {Status}", status);
                    return new(ProviderName, model, false, null, status, status==429?"rate_limited":"http_error", status==429 || status>=500, status==429, RetryAfter: response.Headers.RetryAfter?.Date);
                }

                var json = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);

                // Extract content from choices[0].message.content
                var text = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();
                long? input=null, output=null;
                if (doc.RootElement.TryGetProperty("usage", out var usage)) { if (usage.TryGetProperty("prompt_tokens", out var i)) input=i.GetInt64(); if (usage.TryGetProperty("completion_tokens", out var o)) output=o.GetInt64(); }
                return new(ProviderName, model, true, text, (int)response.StatusCode, null, false, false, input, output);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OpenAI] Request failed");
                return new(ProviderName, model, false, null, null, ex is OperationCanceledException ? "timeout" : "transport_error", true, false);
            }
        }
    }
}
