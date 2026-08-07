using BackendAPI.Models;
using BackendAPI.Services.Llm;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    public void Configuration_binding_does_not_duplicate_provider_order()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PollGen:ProviderOrder:0"] = "gemini",
            ["PollGen:ProviderOrder:1"] = "openai"
        }).Build();
        var services = new ServiceCollection();
        services.AddOptions<PollGenerationOptions>()
            .Bind(configuration.GetSection(PollGenerationOptions.Section))
            .PostConfigure(options =>
            {
                if (options.ProviderOrder.Length == 0)
                    options.ProviderOrder = LlmProviderNames.All.ToArray();
            });

        var options = services.BuildServiceProvider().GetRequiredService<IOptions<PollGenerationOptions>>().Value;

        Assert.Equal(["gemini", "openai"], options.ProviderOrder);
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
