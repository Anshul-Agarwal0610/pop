using BackendAPI.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace BackendAPI.Services.Llm;

public sealed class LlmProviderReadinessService : IHealthCheck
{
    private readonly IOptionsMonitor<PollGenerationOptions> _options;
    public LlmProviderReadinessService(IOptionsMonitor<PollGenerationOptions> options) => _options = options;
    public IReadOnlyList<LlmProviderReadiness> GetStatus() => LlmProviderNames.All.Select(name =>
    {
        if (!_options.CurrentValue.Providers.TryGetValue(name, out var p)) return new LlmProviderReadiness(name, "", LlmProviderReadinessState.InvalidConfiguration, "Provider configuration is missing.");
        if (!p.Enabled) return new(name, p.Model, LlmProviderReadinessState.Disabled, null);
        if (string.IsNullOrWhiteSpace(p.Model) || !Uri.TryCreate(p.Endpoint, UriKind.Absolute, out _) || p.TimeoutSeconds <= 0) return new(name, p.Model, LlmProviderReadinessState.InvalidConfiguration, "Model, endpoint, or timeout is invalid.");
        if (string.IsNullOrWhiteSpace(p.ApiKey)) return new(name, p.Model, LlmProviderReadinessState.MissingCredential, "Credential is missing; provider will be skipped.");
        return new(name, p.Model, LlmProviderReadinessState.Available, null);
    }).ToList();
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var s = GetStatus(); var data = s.ToDictionary(x => x.Provider, x => (object)new { x.Model, State = x.State.ToString(), x.Warning });
        if (!_options.CurrentValue.Enabled) return Task.FromResult(HealthCheckResult.Healthy("LLM generation is disabled.", data));
        if (!s.Any(x => x.State == LlmProviderReadinessState.Available)) return Task.FromResult(HealthCheckResult.Unhealthy("No LLM provider is available.", data: data));
        return Task.FromResult(s.Any(x => x.State is LlmProviderReadinessState.MissingCredential or LlmProviderReadinessState.InvalidConfiguration) ? HealthCheckResult.Degraded("Some LLM providers are unavailable.", data: data) : HealthCheckResult.Healthy("Configured LLM providers are available.", data));
    }
}

public sealed class LlmReadinessStartupReporter : IHostedService
{
    private readonly LlmProviderReadinessService _readiness; private readonly ILogger<LlmReadinessStartupReporter> _logger;
    public LlmReadinessStartupReporter(LlmProviderReadinessService readiness, ILogger<LlmReadinessStartupReporter> logger) => (_readiness, _logger) = (readiness, logger);
    public Task StartAsync(CancellationToken cancellationToken) { foreach (var x in _readiness.GetStatus()) { if (x.State == LlmProviderReadinessState.Available) _logger.LogInformation("LLM provider available. Provider={Provider} Model={Model}", x.Provider, x.Model); else if (x.State == LlmProviderReadinessState.Disabled) _logger.LogInformation("LLM provider disabled. Provider={Provider}", x.Provider); else _logger.LogWarning("LLM provider unavailable. Provider={Provider} Model={Model} State={State} Warning={Warning}", x.Provider, x.Model, x.State, x.Warning); } return Task.CompletedTask; }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
