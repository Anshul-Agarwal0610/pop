using BackendAPI.Interfaces;
using System.Net;
using System.Text.Json;

namespace BackendAPI.Services.Llm;

internal static class LlmProviderHttp
{
    public static async Task<LlmProviderResult> SendAsync(string provider, HttpClient client,
        HttpRequestMessage request, Func<string, string?> extract, ILogger logger,
        TimeSpan maximumCooldown, CancellationToken ct)
    {
        try
        {
            using var response = await client.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                var kind = LlmHttpFailureClassifier.Classify(response.StatusCode, body);
                var retryAt = LlmHttpFailureClassifier.GetRetryAt(response, DateTimeOffset.UtcNow, maximumCooldown);
                logger.LogWarning("[{Provider}] HTTP {Status}; class={Class}", provider, (int)response.StatusCode, kind);
                return LlmProviderResult.Failure(provider, kind, (int)response.StatusCode, retryAt);
            }
            try
            {
                var payload = extract(body);
                return string.IsNullOrWhiteSpace(payload)
                    ? LlmProviderResult.Failure(provider, LlmFailureClass.InvalidResponse)
                    : LlmProviderResult.Success(provider, payload);
            }
            catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
            {
                return LlmProviderResult.Failure(provider, LlmFailureClass.InvalidResponse);
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return LlmProviderResult.Failure(provider, LlmFailureClass.Timeout);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "[{Provider}] transient network failure", provider);
            return LlmProviderResult.Failure(provider, LlmFailureClass.TransientServer);
        }
    }
}
