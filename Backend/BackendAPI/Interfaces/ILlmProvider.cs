namespace BackendAPI.Interfaces;

public sealed record LlmGenerationRequest(
    string SystemInstruction,
    string UserPrompt,
    string ResponseSchema,
    double Temperature = 0.1,
    int MaxOutputTokens = 700);

public sealed record LlmCompletionResult(string Provider, string Model, bool Success, string? ResponseText, int? HttpStatus, string? ErrorCode, bool Retryable, bool RateLimited, long? InputTokens = null, long? OutputTokens = null, DateTimeOffset? RetryAfter = null)
{
    public static LlmCompletionResult Misconfigured(string provider) => new(provider, "configured", false, null, null, "missing_configuration", false, false);
};

/// <summary>Provider-neutral structured generation boundary.</summary>
public interface ILlmProvider
{
    string ProviderName { get; }
    Task<LlmCompletionResult> CompleteAsync(LlmGenerationRequest request, CancellationToken ct = default);
}
