using BackendAPI.Interfaces;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BackendAPI.Services.Llm;

public sealed class CustomVmLlmProvider(IHttpClientFactory http, IConfiguration config,
    IOptions<PollGenerationOptions> options, ILogger<CustomVmLlmProvider> logger) : ILlmProvider
{
    public string ProviderName => "custom";
    public Task<LlmProviderResult> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        var url = config["PollGen:Custom:BaseUrl"];
        if (string.IsNullOrWhiteSpace(url)) return Task.FromResult(LlmProviderResult.Failure(ProviderName, LlmFailureClass.Configuration));
        var request = new HttpRequestMessage(HttpMethod.Post, $"{url.TrimEnd('/')}/generate") { Content = new StringContent(JsonSerializer.Serialize(new { prompt }), Encoding.UTF8, "application/json") };
        var key = config["PollGen:Custom:ApiKey"]; if (!string.IsNullOrWhiteSpace(key)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        return LlmProviderHttp.SendAsync(ProviderName, http.CreateClient(ProviderName), request, json => json, logger, TimeSpan.FromSeconds(options.Value.MaxRetryDelaySeconds), ct);
    }
}
