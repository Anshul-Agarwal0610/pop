namespace BackendAPI.Interfaces;

public enum LlmProviderOutcome
{
    Success,
    TransientFailure,
    PermanentFailure
}

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
    string Model = "")
{
    public static LlmProviderResult Succeeded(string content, string provider = "", string model = "") => new(LlmProviderOutcome.Success, content, null, provider, model);
    public static LlmProviderResult Transient(string reason, string provider = "", string model = "") => new(LlmProviderOutcome.TransientFailure, null, reason, provider, model);
    public static LlmProviderResult Permanent(string reason, string provider = "", string model = "") => new(LlmProviderOutcome.PermanentFailure, null, reason, provider, model);
}

public interface ILlmProvider
{
    string ProviderName { get; }
    Task<LlmProviderResult> GenerateAsync(LlmGenerationRequest request, CancellationToken ct = default) => CompleteAsync(request, ct);
    Task<LlmProviderResult> CompleteAsync(LlmGenerationRequest request, CancellationToken ct = default) => GenerateAsync(request, ct);
}
