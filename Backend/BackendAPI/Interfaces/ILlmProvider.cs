namespace BackendAPI.Interfaces;

public sealed record LlmGenerationRequest(string SystemInstruction, string UserPrompt, string ResponseSchema,
    double Temperature = 0.1, int MaxOutputTokens = 700);

public enum LlmProviderOutcome { Success, TransientFailure, PermanentFailure }

public sealed record LlmProviderResult(LlmProviderOutcome Outcome, string? Content = null, string? Reason = null)
{
    public static LlmProviderResult Succeeded(string content) => new(LlmProviderOutcome.Success, content);
    public static LlmProviderResult Transient(string reason) => new(LlmProviderOutcome.TransientFailure, null, reason);
    public static LlmProviderResult Permanent(string reason) => new(LlmProviderOutcome.PermanentFailure, null, reason);
}

public interface ILlmProvider
{
    string ProviderName { get; }
    Task<LlmProviderResult> CompleteAsync(LlmGenerationRequest request, CancellationToken ct = default);
}
