namespace BackendAPI.Interfaces;

public enum LlmProviderOutcome
{
    Success,
    TransientFailure,
    PermanentFailure
}

public enum LlmFailureClass { None, RateLimited, Timeout, TransientServer, Authentication, InvalidRequest, ContentPolicy, InvalidResponse, Configuration, Unknown }

public sealed record LlmGenerationRequest(
    string SystemInstruction,
    string UserPrompt,
    string? ResponseSchema = null,
    double Temperature = 0.7,
    int MaxTokens = 1024);

public sealed record LlmProviderResult(
    LlmProviderOutcome Outcome,
    string? Content = null,
    string? Reason = null,
    string Provider = "",
    string Model = "",
    LlmFailureClass FailureClass = LlmFailureClass.None,
    int? HttpStatus = null,
    DateTimeOffset? RetryAtUtc = null)
{
    public bool IsRetryable => Outcome == LlmProviderOutcome.TransientFailure || FailureClass is LlmFailureClass.RateLimited or LlmFailureClass.Timeout or LlmFailureClass.TransientServer;
    public bool IsSuccess => Outcome == LlmProviderOutcome.Success && !string.IsNullOrWhiteSpace(Content);
    public static LlmProviderResult Succeeded(string content, string provider = "", string model = "") => new(LlmProviderOutcome.Success, content, null, provider, model);
    public static LlmProviderResult Transient(string reason, string provider = "", string model = "") => new(LlmProviderOutcome.TransientFailure, null, reason, provider, model);
    public static LlmProviderResult Permanent(string reason, string provider = "", string model = "") => new(LlmProviderOutcome.PermanentFailure, null, reason, provider, model);
    public static LlmProviderResult Success(string provider, string content, string model = "") => Succeeded(content, provider, model);
    public static LlmProviderResult Failure(string provider, LlmFailureClass kind, int? status = null, DateTimeOffset? retryAtUtc = null, string? code = null) =>
        new(kind is LlmFailureClass.RateLimited or LlmFailureClass.Timeout or LlmFailureClass.TransientServer ? LlmProviderOutcome.TransientFailure : LlmProviderOutcome.PermanentFailure,
            null, code ?? kind.ToString(), provider, "", kind, status, retryAtUtc);
}

public interface ILlmProvider
{
    string ProviderName { get; }
    Task<LlmProviderResult> GenerateAsync(LlmGenerationRequest request, CancellationToken ct = default) => CompleteAsync(request, ct);
    Task<LlmProviderResult> CompleteAsync(LlmGenerationRequest request, CancellationToken ct = default) => GenerateAsync(request, ct);
}
