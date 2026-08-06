namespace BackendAPI.Interfaces;

public enum LlmFailureClass
{
    None, RateLimited, Timeout, TransientServer, Authentication, InvalidRequest,
    ContentPolicy, InvalidResponse, Configuration, Unknown
}

public sealed record LlmProviderResult(
    string ProviderName,
    string? Payload,
    LlmFailureClass FailureClass = LlmFailureClass.None,
    int? HttpStatus = null,
    DateTimeOffset? RetryAtUtc = null,
    string? ErrorCode = null)
{
    public bool IsSuccess => FailureClass == LlmFailureClass.None && !string.IsNullOrWhiteSpace(Payload);
    public bool IsRetryable => FailureClass is LlmFailureClass.RateLimited
        or LlmFailureClass.Timeout or LlmFailureClass.TransientServer;

    public static LlmProviderResult Success(string provider, string payload) => new(provider, payload);
    public static LlmProviderResult Failure(string provider, LlmFailureClass kind, int? status = null,
        DateTimeOffset? retryAtUtc = null, string? code = null) =>
        new(provider, null, kind, status, retryAtUtc, code);
}

public interface ILlmProvider
{
    string ProviderName { get; }
    Task<LlmProviderResult> CompleteAsync(string prompt, CancellationToken ct = default);
}
