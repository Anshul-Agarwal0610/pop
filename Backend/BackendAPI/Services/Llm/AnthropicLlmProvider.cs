using BackendAPI.Interfaces;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace BackendAPI.Services.Llm;

public sealed class AnthropicLlmProvider(IHttpClientFactory http, IConfiguration config,
    IOptions<PollGenerationOptions> options, ILogger<AnthropicLlmProvider> logger) : ILlmProvider
{
    public string ProviderName => "anthropic";
    public Task<LlmProviderResult> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        var key = config["PollGen:Anthropic:ApiKey"];
        if (string.IsNullOrWhiteSpace(key)) return Task.FromResult(LlmProviderResult.Failure(ProviderName, LlmFailureClass.Configuration));
        var body = new { model = config["PollGen:Anthropic:Model"] ?? "claude-haiku-4-5", max_tokens = 512, messages = new[] { new { role = "user", content = prompt } } };
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages") { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };
        request.Headers.Add("x-api-key", key); request.Headers.Add("anthropic-version", "2023-06-01");
        return LlmProviderHttp.SendAsync(ProviderName, http.CreateClient(ProviderName), request, json =>
        { using var doc = JsonDocument.Parse(json); return doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString(); }, logger, TimeSpan.FromSeconds(options.Value.MaxRetryDelaySeconds), ct);
    }
}
