using BackendAPI.Interfaces;
using BackendAPI.Models;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace BackendAPI.Services.Llm;

public sealed class AnthropicLlmProvider : ILlmProvider
{
    public string ProviderName => LlmProviderNames.Anthropic;
    private readonly IHttpClientFactory _http; private readonly IOptionsMonitor<PollGenerationOptions> _options;
    public AnthropicLlmProvider(IHttpClientFactory h, IOptionsMonitor<PollGenerationOptions> o) => (_http, _options) = (h, o);
    public async Task<LlmProviderResult> GenerateAsync(LlmGenerationRequest request, CancellationToken ct = default)
    {
        var c = _options.CurrentValue.Providers[ProviderName];
        using var msg = new HttpRequestMessage(HttpMethod.Post, c.Endpoint);
        msg.Headers.Add("x-api-key", c.ApiKey); msg.Headers.Add("anthropic-version", "2023-06-01");
        msg.Content = new StringContent(JsonSerializer.Serialize(new { model = c.Model, max_tokens = request.MaxTokens, system = request.SystemInstruction, messages = new[] { new { role = "user", content = request.UserPrompt } } }), Encoding.UTF8, "application/json");
        return await Send(msg, c, ct);
    }
    private async Task<LlmProviderResult> Send(HttpRequestMessage msg, LlmProviderOptions c, CancellationToken ct)
    {
        try { using var t = CancellationTokenSource.CreateLinkedTokenSource(ct); t.CancelAfter(TimeSpan.FromSeconds(c.TimeoutSeconds)); using var r = await _http.CreateClient(ProviderName).SendAsync(msg, t.Token); if (!r.IsSuccessStatusCode) return new(LlmHttpFailure.Classify(r.StatusCode), null, $"HTTP {(int)r.StatusCode}", ProviderName, c.Model); using var d = JsonDocument.Parse(await r.Content.ReadAsStringAsync(ct)); var x = d.RootElement.GetProperty("content")[0].GetProperty("text").GetString(); return string.IsNullOrWhiteSpace(x) ? new(LlmProviderOutcome.PermanentFailure, null, "Provider returned empty content.", ProviderName, c.Model) : new(LlmProviderOutcome.Success, x, null, ProviderName, c.Model); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return new(LlmProviderOutcome.TransientFailure, null, "Provider request timed out.", ProviderName, c.Model); }
        catch (HttpRequestException) { return new(LlmProviderOutcome.TransientFailure, null, "Provider network failure.", ProviderName, c.Model); }
        catch (JsonException) { return new(LlmProviderOutcome.PermanentFailure, null, "Malformed provider response envelope.", ProviderName, c.Model); }
    }
}
