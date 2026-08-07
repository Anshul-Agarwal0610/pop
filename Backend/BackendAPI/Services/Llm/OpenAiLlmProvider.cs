using BackendAPI.Interfaces;
using BackendAPI.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BackendAPI.Services.Llm;

internal static class LlmHttpFailure
{
    public static LlmProviderOutcome Classify(HttpStatusCode status) =>
        status is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests || (int)status >= 500
            ? LlmProviderOutcome.TransientFailure : LlmProviderOutcome.PermanentFailure;
}

public abstract class OpenAiCompatibleLlmProvider : ILlmProvider
{
    private readonly IHttpClientFactory _http;
    private readonly IOptionsMonitor<PollGenerationOptions> _options;
    private readonly ILogger _logger;
    public abstract string ProviderName { get; }

    protected OpenAiCompatibleLlmProvider(IHttpClientFactory http, IOptionsMonitor<PollGenerationOptions> options, ILogger logger)
        => (_http, _options, _logger) = (http, options, logger);

    public async Task<LlmProviderResult> GenerateAsync(LlmGenerationRequest request, CancellationToken ct = default)
    {
        var config = _options.CurrentValue.Providers[ProviderName];
        using var message = new HttpRequestMessage(HttpMethod.Post, config.Endpoint);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
        message.Content = new StringContent(JsonSerializer.Serialize(new
        {
            model = config.Model,
            messages = new[] { new { role = "system", content = request.SystemInstruction }, new { role = "user", content = request.UserPrompt } },
            temperature = request.Temperature,
            max_tokens = request.MaxTokens,
            response_format = new { type = "json_object" }
        }), Encoding.UTF8, "application/json");
        return await SendAsync(message, config, request, ct);
    }

    private async Task<LlmProviderResult> SendAsync(HttpRequestMessage message, LlmProviderOptions config, LlmGenerationRequest request, CancellationToken ct)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(config.TimeoutSeconds));
            using var response = await _http.CreateClient(ProviderName).SendAsync(message, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                var outcome = LlmHttpFailure.Classify(response.StatusCode);
                _logger.LogWarning("LLM request failed. Provider={Provider} Model={Model} Status={Status} Outcome={Outcome}", ProviderName, config.Model, (int)response.StatusCode, outcome);
                return new(outcome, null, $"HTTP {(int)response.StatusCode}", ProviderName, config.Model);
            }
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            return string.IsNullOrWhiteSpace(content)
                ? new(LlmProviderOutcome.PermanentFailure, null, "Provider returned empty content.", ProviderName, config.Model)
                : new(LlmProviderOutcome.Success, content, null, ProviderName, config.Model);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { return new(LlmProviderOutcome.TransientFailure, null, "Provider request timed out.", ProviderName, config.Model); }
        catch (HttpRequestException ex)
        { _logger.LogWarning("LLM network failure. Provider={Provider} Model={Model} Error={ErrorType}", ProviderName, config.Model, ex.GetType().Name); return new(LlmProviderOutcome.TransientFailure, null, "Provider network failure.", ProviderName, config.Model); }
        catch (JsonException)
        { return new(LlmProviderOutcome.PermanentFailure, null, "Malformed provider response envelope.", ProviderName, config.Model); }
    }
}

public sealed class OpenAiLlmProvider : OpenAiCompatibleLlmProvider
{
    public override string ProviderName => LlmProviderNames.OpenAi;
    public OpenAiLlmProvider(IHttpClientFactory h, IOptionsMonitor<PollGenerationOptions> o, ILogger<OpenAiLlmProvider> l) : base(h, o, l) { }
}

public sealed class GroqLlmProvider : OpenAiCompatibleLlmProvider
{
    public override string ProviderName => LlmProviderNames.Groq;
    public GroqLlmProvider(IHttpClientFactory h, IOptionsMonitor<PollGenerationOptions> o, ILogger<GroqLlmProvider> l) : base(h, o, l) { }
}
