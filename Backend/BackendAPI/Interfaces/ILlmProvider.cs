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
    string? Content,
    string? Reason,
    string Provider,
    string Model);

public interface ILlmProvider
{
    string ProviderName { get; }
    Task<LlmProviderResult> GenerateAsync(LlmGenerationRequest request, CancellationToken ct = default);
}
