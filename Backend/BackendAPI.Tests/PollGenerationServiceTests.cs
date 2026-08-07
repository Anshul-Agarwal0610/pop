using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using BackendAPI.Services.Llm;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BackendAPI.Tests;

public class PollGenerationServiceTests
{
    private const string Valid = """{"proposition":"Should Parliament adopt the proposed data privacy law?","category":"Technology","sourceGrounding":{"rationale":"The source describes a proposed law.","evidence":["Parliament is considering a data privacy law."]},"quality":{"isSelfContained":true,"isNeutral":true,"isBinary":true,"isGrounded":true,"confidence":0.9,"isAmbiguous":false,"ambiguityReason":null}}""";

    [Theory]
    [InlineData("Politics", "A bill would cap campaign donations")]
    [InlineData("Technology", "A platform proposes labels for AI content")]
    [InlineData("Sports", "The league proposes a new eligibility rule")]
    [InlineData("Entertainment", "A studio proposes a shorter release window")]
    [InlineData("Business", "A company proposes a merger")]
    [InlineData("General", "Officials announced an event")]
    public async Task Prompt_contains_source_and_binary_constraints(string category, string summary)
    {
        var provider = new FakeProvider(Valid);
        var service = Create(provider);
        await service.GenerateAsync(new TrendingTopic { Title = "Source title", Summary = summary, Category = category, SourceType = "rss", Publisher = "Publisher", PublishedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc) });
        Assert.NotNull(provider.Request);
        Assert.Contains("Source title", provider.Request!.UserPrompt);
        Assert.Contains("Publisher", provider.Request.UserPrompt);
        Assert.Contains("publicationDate", provider.Request.UserPrompt);
        Assert.Contains("Up", provider.Request.UserPrompt);
        Assert.Contains("Against", provider.Request.UserPrompt);
        Assert.Contains("predictions", provider.Request.UserPrompt);
        Assert.Equal(.1, provider.Request.Temperature);
        Assert.Contains("additionalProperties", provider.Request.ResponseSchema);
    }

    [Fact]
    public async Task Valid_contract_returns_structured_result()
    {
        var result = await Create(new FakeProvider(Valid)).GenerateAsync(new TrendingTopic { Title = "Law", Summary = "Parliament considers privacy law", Category = "Technology" });
        Assert.Equal(GenerationOutcome.Succeeded, result.Outcome);
        Assert.Equal("Should Parliament adopt the proposed data privacy law?", result.Poll!.Proposition);
        Assert.Equal(GenerationMethods.Llm, result.Poll.GenerationMethod);
        Assert.Equal(new[] { "Up", "Against" }, result.Poll.Options);
    }

    [Theory]
    [InlineData("{\"question\":\"Which option?\",\"options\":[\"A\",\"B\"],\"category\":\"General\"}")]
    [InlineData("```json\n{}\n```")]
    [InlineData("prose {\"proposition\":\"Should this sufficiently described policy be adopted?\"}")]
    [InlineData("{\"proposition\":12}")]
    [InlineData("{\"proposition\":\"Should this sufficiently described policy be adopted?\",\"category\":\"General\",\"sourceGrounding\":{\"rationale\":\"Grounded\",\"evidence\":[]},\"quality\":{\"isSelfContained\":true,\"isNeutral\":true,\"isBinary\":true,\"isGrounded\":true,\"confidence\":2,\"isAmbiguous\":false,\"ambiguityReason\":null}}")]
    public async Task Malformed_legacy_or_invalid_contract_is_rejected(string json)
    {
        var result = await Create(new FakeProvider(json)).GenerateAsync(new TrendingTopic { Title = "Topic", Summary = "Detail", Category = "General" });
        Assert.NotEqual(GenerationOutcome.Succeeded, result.Outcome);
    }

    [Fact]
    public async Task Ambiguous_topic_is_rejected()
    {
        var json = Valid.Replace("false,\"ambiguityReason\":null", "true,\"ambiguityReason\":\"Not enough source detail\"");
        Assert.NotEqual(GenerationOutcome.Succeeded, (await Create(new FakeProvider(json)).GenerateAsync(new TrendingTopic { Title = "Celebrity news", Category = "Entertainment" })).Outcome);
    }

    [Fact]
    public async Task Transient_failure_uses_safe_fallback_only_for_convertible_topic()
    {
        var service = Create(new OutcomeProvider(LlmProviderResult.Transient("timeout")));
        var result = await service.GenerateAsync(new TrendingTopic { Title = "Council bans cars from the city centre", Summary = "The council plan would ban cars from the city centre on weekdays.", Category = "General" });
        Assert.Equal(GenerationOutcome.Succeeded, result.Outcome);
        Assert.Equal(GenerationMethods.DeterministicFallback, result.Poll!.GenerationMethod);
        Assert.Equal(new[] { "Up", "Against" }, result.Poll.Options);
    }

    [Fact]
    public async Task Transient_failure_retains_ambiguous_topic_for_retry()
    {
        var result = await Create(new OutcomeProvider(LlmProviderResult.Transient("timeout"))).GenerateAsync(
            new TrendingTopic { Title = "Major changes may be coming", Summary = "Officials discussed possibilities without announcing an action.", Category = "General" });
        Assert.Equal(GenerationOutcome.ProviderTransientFailure, result.Outcome);
        Assert.Null(result.Poll);
    }

    private static PollGenerationService Create(FakeProvider provider)
        => Create((ILlmProvider)provider);

    private static PollGenerationService Create(ILlmProvider provider)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var options = new PollGenerationOptions
        {
            ProviderOrder = [LlmProviderNames.OpenAi],
            Providers = new(StringComparer.OrdinalIgnoreCase)
            {
                [LlmProviderNames.OpenAi] = new() { Enabled = true, Model = "fake-model", Endpoint = "https://example.test", TimeoutSeconds = 5, ApiKey = "test-key" }
            }
        };
        var monitor = new TestMonitor(options);
        return new PollGenerationService(new[] { provider }, new FakePollsRepository(), config,
            NullLogger<PollGenerationService>.Instance, new DeterministicPollConverter(), monitor,
            new LlmProviderReadinessService(monitor),
            new ProviderResilienceCoordinator(Microsoft.Extensions.Options.Options.Create(options), TimeProvider.System));
    }

    private sealed class OutcomeProvider(LlmProviderResult result) : ILlmProvider
    {
        public string ProviderName => LlmProviderNames.OpenAi;
        public Task<LlmProviderResult> CompleteAsync(LlmGenerationRequest request, CancellationToken ct = default) => Task.FromResult(result);
    }

    private sealed class FakeProvider(string response) : ILlmProvider
    {
        public string ProviderName => LlmProviderNames.OpenAi;
        public LlmGenerationRequest? Request { get; private set; }
        public Task<LlmProviderResult> CompleteAsync(LlmGenerationRequest request, CancellationToken ct = default) { Request = request; return Task.FromResult(LlmProviderResult.Succeeded(response)); }
    }

    private sealed class TestMonitor(PollGenerationOptions value) : IOptionsMonitor<PollGenerationOptions>
    {
        public PollGenerationOptions CurrentValue => value;
        public PollGenerationOptions Get(string? name) => value;
        public IDisposable? OnChange(Action<PollGenerationOptions, string?> listener) => null;
    }

    private sealed class FakePollsRepository : IPollsRepository
    {
        public Task<IEnumerable<Poll>> GetRecentGeneratedAsync(int count = 100) => Task.FromResult<IEnumerable<Poll>>([]);
        public Task<long> CreateAsync(CreatePollRequest request, long? id = null) => throw new NotImplementedException();
        public Task<bool> DeleteAsync(long id) => throw new NotImplementedException();
        public Task<IEnumerable<Poll>> GetAllAsync(long? userId = null, string? category = null) => throw new NotImplementedException();
        public Task<Poll?> GetByIdAsync(long id, long? userId = null) => throw new NotImplementedException();
        public Task<IEnumerable<Poll>> GetModerationQueueAsync(string? status = null, int count = 50) => throw new NotImplementedException();
        public Task<IEnumerable<Poll>> GetPersonalizedAsync(long? userId = null, int count = 20, string? category = null) => throw new NotImplementedException();
        public Task<IEnumerable<Poll>> GetRecentAsync(int count = 10) => throw new NotImplementedException();
        public Task<IEnumerable<Poll>> GetTrendingAsync(int count = 10, long? userId = null, string? category = null) => throw new NotImplementedException();
        public Task<bool> ModerateAsync(long pollId, string status, string? reason, long id) => throw new NotImplementedException();
        public Task<bool> ReportAsync(long pollId, long id, string reason) => throw new NotImplementedException();
        public Task<IEnumerable<Poll>> SearchAsync(string query, string? category = null, long? userId = null) => throw new NotImplementedException();
        public Task UpdateTrendingAsync() => throw new NotImplementedException();
    }
}
