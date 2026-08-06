namespace BackendAPI.Interfaces;

public sealed record LlmGenerationRequest(
    string SystemInstruction,
    string UserPrompt,
    string ResponseSchema,
    double Temperature = 0.1,
    int MaxOutputTokens = 700);

/// <summary>Provider-neutral structured generation boundary.</summary>
public interface ILlmProvider
{
    string ProviderName { get; }
    Task<string?> CompleteAsync(LlmGenerationRequest request, CancellationToken ct = default);
}
