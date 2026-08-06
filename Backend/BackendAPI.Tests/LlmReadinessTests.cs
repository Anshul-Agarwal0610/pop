using BackendAPI.Models;
using BackendAPI.Services.Llm;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xunit;

namespace BackendAPI.Tests;

public class LlmReadinessTests
{
    [Fact]
    public void Validator_rejects_duplicate_and_unknown_order_entries()
    {
        var result = new PollGenerationOptionsValidator().Validate(null, new() { ProviderOrder = ["gemini", "gemini", "unknown"] });
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, x => x.Contains("duplicates"));
        Assert.Contains(result.Failures, x => x.Contains("Unknown"));
    }

    [Fact]
    public async Task Missing_key_disables_only_that_provider_and_degrades_health()
    {
        var options = new PollGenerationOptions { Providers = new(StringComparer.OrdinalIgnoreCase) {
            ["gemini"] = new() { Enabled = true, Model = "gemini", Endpoint = "https://example.test", TimeoutSeconds = 10, ApiKey = "" },
            ["openai"] = new() { Enabled = true, Model = "gpt", Endpoint = "https://example.test", TimeoutSeconds = 10, ApiKey = "key" },
            ["anthropic"] = new() { Enabled = false }, ["groq"] = new() { Enabled = false }
        }};
        var service = new LlmProviderReadinessService(new TestMonitor(options));
        Assert.Equal(LlmProviderReadinessState.MissingCredential, service.GetStatus().Single(x => x.Provider == "gemini").State);
        Assert.Equal(LlmProviderReadinessState.Available, service.GetStatus().Single(x => x.Provider == "openai").State);
        Assert.Equal(HealthStatus.Degraded, (await service.CheckHealthAsync(new HealthCheckContext())).Status);
    }
    private sealed class TestMonitor(PollGenerationOptions value) : IOptionsMonitor<PollGenerationOptions> { public PollGenerationOptions CurrentValue => value; public PollGenerationOptions Get(string? name) => value; public IDisposable? OnChange(Action<PollGenerationOptions, string?> listener) => null; }
}
