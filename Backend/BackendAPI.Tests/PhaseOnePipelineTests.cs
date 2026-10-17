using System.Text.Json;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Repository;
using BackendAPI.Services;
using BackendAPI.Services.Llm;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace BackendAPI.Tests;

public class PhaseOnePipelineTests
{
    private const string Candidate = """{"proposition":"Should Parliament adopt the proposed data privacy law?","category":"Technology","supportStatement":"Adopt the proposed privacy law","againstStatement":"Do not adopt the proposed privacy law","sourceGrounding":{"rationale":"The source describes a proposed law.","evidence":["Parliament is considering a data privacy law."]},"quality":{"isSelfContained":true,"isNeutral":true,"isBinary":true,"isGrounded":true,"confidence":0.9,"isAmbiguous":false,"ambiguityReason":null}}""";
    private static TrendingTopic Topic => new() { Title = "Privacy legislation", Summary = "Parliament is considering a data privacy law." };

    [Fact]
    public async Task Five_candidates_use_one_provider_request_and_survive_persistence()
    {
        var provider = new Provider("{\"polls\":[" + string.Join(',', Enumerable.Repeat(Candidate, 5)) + "]}");
        var result = await Generator(provider).GenerateAsync(Topic);
        Assert.Equal(1, provider.Calls);
        Assert.Equal(5, result.Batch!.Polls.Count);
        var saved = JsonSerializer.Deserialize<TopicPollBatch>(JsonSerializer.Serialize(result.Batch));
        Assert.Equal(5, saved!.Polls.Count);
        Assert.Null(TopicBatchGenerator.Validate(Topic, saved.Polls[0], new PollQualityOptions()));
        Assert.Contains("polls", provider.Request!.ResponseSchema);
    }

    [Fact]
    public async Task Multi_topic_batch_requires_one_result_for_every_topic()
    {
        var result = await Generator(new Provider("{\"topics\":[]}"))
            .GenerateBatchAsync([Topic]);

        Assert.Null(result.Batch);
        Assert.Equal(LlmFailureClass.InvalidResponse, result.Provider.FailureClass);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"polls\":null}")]
    [InlineData("{\"polls\":[null]}")]
    [InlineData("{\"polls\":[],\"extra\":true}")]
    public async Task Invalid_envelopes_are_rejected(string response)
    {
        var result = await Generator(new Provider(response)).GenerateAsync(Topic);
        Assert.Null(result.Batch);
        Assert.Equal(LlmFailureClass.InvalidResponse, result.Provider.FailureClass);
    }

    [Fact]
    public async Task More_than_five_candidates_are_rejected()
    {
        var result = await Generator(new Provider("{\"polls\":[" + string.Join(',', Enumerable.Repeat(Candidate, 6)) + "]}")).GenerateAsync(Topic);
        Assert.Null(result.Batch);
    }

    [Fact]
    public async Task Unconfigured_provider_is_not_called()
    {
        var provider = new Provider("{\"polls\":[]}");
        Assert.Null((await Generator(provider, false).GenerateAsync(Topic)).Batch);
        Assert.Equal(0, provider.Calls);
    }

    [Fact]
    public void Evidence_translation_and_health_category_are_allowed_in_phase_one()
    {
        var poll = JsonSerializer.Deserialize<PropositionGenerationResult>(Candidate)!;
        poll.Grounding.Evidence = ["A claim absent from the source."];
        Assert.Null(TopicBatchGenerator.Validate(Topic, poll, new PollQualityOptions()));
        poll.Category = "Health";
        Assert.Null(TopicBatchGenerator.Validate(Topic, poll, new PollQualityOptions()));
    }

    [Theory]
    [InlineData("A state schedules an execution by firing squad.")]
    [InlineData("Lawmakers debate the death penalty.")]
    [InlineData("Officials respond after a shooting.")]
    public void Sensitive_sources_are_not_skipped_in_phase_one(string summary)
    {
        var topic = new TrendingTopic { Title = "Sensitive report", Summary = summary };
        var poll = JsonSerializer.Deserialize<PropositionGenerationResult>(Candidate)!;
        poll.Grounding.Evidence = [summary];

        Assert.Null(TopicBatchGenerator.Validate(topic, poll, new PollQualityOptions()));
    }

    [Fact]
    public void Provider_ambiguity_flag_does_not_skip_a_complete_phase_one_poll()
    {
        var poll = JsonSerializer.Deserialize<PropositionGenerationResult>(Candidate)!;
        poll.Quality.IsAmbiguous = true;
        poll.Quality.IsNeutral = false;

        Assert.Null(TopicBatchGenerator.Validate(Topic, poll, new PollQualityOptions()));
    }

    [Fact]
    public void Translated_evidence_does_not_block_a_hindi_phase_one_poll()
    {
        var topic = new TrendingTopic { Title = "नगर परिषद ने नई बस सेवा शुरू की", Summary = "नई बस सेवा से शहर के यात्रियों को सुविधा मिलेगी", Language = "hi" };
        var poll = JsonSerializer.Deserialize<PropositionGenerationResult>(Candidate)!;
        poll.Grounding.Evidence = ["The city council launched a new bus service."];

        Assert.Null(TopicBatchGenerator.Validate(topic, poll, new PollQualityOptions()));
    }

    [Fact]
    public void Older_grounding_metadata_does_not_block_a_complete_phase_one_poll()
    {
        var poll = JsonSerializer.Deserialize<PropositionGenerationResult>(Candidate)!;
        poll.Grounding.Rationale = "";
        poll.Grounding.Evidence = [];

        Assert.Null(TopicBatchGenerator.Validate(Topic, poll, new PollQualityOptions()));
    }

    [Theory]
    [InlineData("Economy", "Work")]
    [InlineData("Crime", "Society")]
    [InlineData("International Relations", "Politics")]
    public void Public_category_aliases_are_normalized_before_publication(string supplied, string expected)
    {
        var poll = JsonSerializer.Deserialize<PropositionGenerationResult>(Candidate)!;
        poll.Category = supplied;

        Assert.Null(TopicBatchGenerator.Validate(Topic, poll, new PollQualityOptions()));
        Assert.Equal(expected, poll.Category);
    }

    [Fact]
    public void Source_identity_keeps_video_ids_but_removes_tracking()
    {
        Assert.NotEqual(SourceIdentity.CanonicalUrl("https://youtube.com/watch?v=one"), SourceIdentity.CanonicalUrl("https://youtube.com/watch?v=two"));
        Assert.Equal(SourceIdentity.CanonicalUrl("https://youtube.com/watch?v=one"), SourceIdentity.CanonicalUrl("https://youtube.com/watch?utm_source=test&v=one#fragment"));
    }

    [Theory]
    [InlineData("en", true)]
    [InlineData("en-IN", true)]
    [InlineData("hi", true)]
    [InlineData("hi-IN", true)]
    [InlineData("bn", false)]
    [InlineData("mr", false)]
    [InlineData("ta", false)]
    [InlineData(null, false)]
    public void Topic_language_policy_allows_only_english_and_hindi(string? language, bool expected)
        => Assert.Equal(expected, TopicLanguagePolicy.IsSupported(language));

    [Fact]
    public void Topic_grouping_preserves_negation_and_numbers()
    {
        Assert.True(TrendingTopicRepository.SameTopicTitle("Council adopts plan!", "COUNCIL adopts plan"));
        Assert.False(TrendingTopicRepository.SameTopicTitle("Council adopts plan", "Council does not adopt plan"));
        Assert.False(TrendingTopicRepository.SameTopicTitle("Plan for 2025", "Plan for 2026"));
        Assert.True(PhaseOnePipelineRepository.PhaseOneDuplicate("Should Parliament adopt the privacy law?", "Should parliament adopt the privacy law?"));
        Assert.False(PhaseOnePipelineRepository.PhaseOneDuplicate("Should Parliament adopt the privacy law?", "Should cities provide free public transport?"));
    }

    private static TopicBatchGenerator Generator(Provider provider, bool configured = true)
    {
        var options = new PollGenerationOptions { Providers = new(StringComparer.OrdinalIgnoreCase) {
            ["anthropic"] = new() { Enabled = true, Model = "test", Endpoint = "https://example.test", TimeoutSeconds = 5, ApiKey = configured ? "test" : "" }
        }};
        return new([provider], new LlmProviderReadinessService(new Monitor(options)), new ConfigurationBuilder().Build());
    }
    private sealed class Monitor(PollGenerationOptions value) : IOptionsMonitor<PollGenerationOptions>
    {
        public PollGenerationOptions CurrentValue => value;
        public PollGenerationOptions Get(string? name) => value;
        public IDisposable? OnChange(Action<PollGenerationOptions, string?> listener) => null;
    }
    private sealed class Provider(string response) : ILlmProvider
    {
        public string ProviderName => "anthropic";
        public int Calls { get; private set; }
        public LlmGenerationRequest? Request { get; private set; }
        public Task<LlmProviderResult> CompleteAsync(LlmGenerationRequest request, CancellationToken ct = default)
        {
            Calls++; Request = request;
            return Task.FromResult(LlmProviderResult.Succeeded(response, ProviderName, "test"));
        }
    }
}
