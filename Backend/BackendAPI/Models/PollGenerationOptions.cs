using Microsoft.Extensions.Options;

namespace BackendAPI.Models;

public static class LlmProviderNames
{
    public const string Gemini = "gemini";
    public const string OpenAi = "openai";
    public const string Anthropic = "anthropic";
    public const string Groq = "groq";
    public static readonly string[] All = [Gemini, OpenAi, Anthropic, Groq];
}

public sealed class PollGenerationOptions
{
    public const string Section = "PollGen";
    public bool Enabled { get; set; } = true;
    public List<string> ProviderOrder { get; set; } = [LlmProviderNames.Gemini, LlmProviderNames.OpenAi, LlmProviderNames.Anthropic, LlmProviderNames.Groq];
    public Dictionary<string, LlmProviderOptions> Providers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int MaxAttemptsPerTopic { get; set; } = 8;
    public int BaseRetryDelaySeconds { get; set; } = 30;
    public int MaxRetryDelaySeconds { get; set; } = 3600;
    public double JitterPercentage { get; set; } = .2;
    public int CircuitFailureThreshold { get; set; } = 3;
    public int CircuitCooldownSeconds { get; set; } = 120;
    public int MaxProviderConcurrency { get; set; } = 2;
    public int TopicLeaseSeconds { get; set; } = 300;
}

public sealed class LlmProviderOptions
{
    public bool Enabled { get; set; }
    public string Model { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public string ApiKey { get; set; } = string.Empty;
}

public sealed class PollGenerationOptionsValidator : IValidateOptions<PollGenerationOptions>
{
    public ValidateOptionsResult Validate(string? name, PollGenerationOptions options)
    {
        var errors = new List<string>();
        var order = options.ProviderOrder.Select(x => x.Trim().ToLowerInvariant()).ToList();
        if (order.Count != order.Distinct(StringComparer.OrdinalIgnoreCase).Count()) errors.Add("ProviderOrder contains duplicates.");
        foreach (var provider in order.Where(x => !LlmProviderNames.All.Contains(x, StringComparer.OrdinalIgnoreCase))) errors.Add($"Unknown provider '{provider}'.");
        foreach (var (nameKey, provider) in options.Providers.Where(x => x.Value.Enabled))
        {
            if (string.IsNullOrWhiteSpace(provider.Model)) errors.Add($"Provider '{nameKey}' has no model.");
            if (!Uri.TryCreate(provider.Endpoint, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)) errors.Add($"Provider '{nameKey}' has an invalid endpoint.");
            if (provider.TimeoutSeconds <= 0) errors.Add($"Provider '{nameKey}' timeout must be positive.");
        }
        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}

public enum LlmProviderReadinessState { Disabled, MissingCredential, Available, InvalidConfiguration }
public sealed record LlmProviderReadiness(string Provider, string Model, LlmProviderReadinessState State, string? Warning);
