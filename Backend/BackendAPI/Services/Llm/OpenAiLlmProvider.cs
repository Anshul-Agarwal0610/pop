using BackendAPI.Interfaces;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BackendAPI.Services.Llm;

public sealed class OpenAiLlmProvider(IHttpClientFactory http, IConfiguration config,
    IOptions<PollGenerationOptions> options, ILogger<OpenAiLlmProvider> logger) : ILlmProvider
{
    public string ProviderName => "openai";
    public Task<LlmProviderResult> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        var key = config["PollGen:OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(key)) return Task.FromResult(LlmProviderResult.Failure(ProviderName, LlmFailureClass.Configuration));
        var baseUrl = config["PollGen:OpenAI:BaseUrl"];
        var endpoint = string.IsNullOrWhiteSpace(baseUrl) ? "https://api.openai.com/v1/chat/completions" : $"{baseUrl.TrimEnd('/')}/chat/completions";
        var body = new { model = config["PollGen:OpenAI:Model"] ?? "gpt-4o-mini", messages = new[] { new { role = "user", content = prompt } }, temperature = .7, max_tokens = 1024, response_format = new { type = "json_object" } };
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        return LlmProviderHttp.SendAsync(ProviderName, http.CreateClient(ProviderName), request, json =>
        { using var doc = JsonDocument.Parse(json); return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString(); }, logger, TimeSpan.FromSeconds(options.Value.MaxRetryDelaySeconds), ct);
    }
}
