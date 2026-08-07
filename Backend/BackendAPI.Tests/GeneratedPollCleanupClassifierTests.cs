using BackendAPI.Models;
using BackendAPI.Services;
using Xunit;

namespace BackendAPI.Tests;

public class GeneratedPollCleanupClassifierTests
{
    private readonly GeneratedPollCleanupClassifier _classifier = new();

    [Fact]
    public void Canonical_legacy_null_sides_is_valid()
    {
        var result = _classifier.Classify(Candidate("Should cities add more protected bike lanes?", "Up", "Against"));
        Assert.False(result.IsMalformed);
        Assert.Equal(GeneratedPollCleanupPolicy.DetectionVersion, result.DetectionVersion);
    }

    [Theory]
    [InlineData("Which policy is your favorite?")]
    [InlineData("What do residents prefer for transit?")]
    [InlineData("Rank the most important climate response?")]
    [InlineData("Who will win the election?")]
    public void Survey_preference_ranking_and_prediction_are_malformed(string question)
    {
        var result = _classifier.Classify(Candidate(question, "Up", "Against"));
        Assert.Contains(GeneratedPollCleanupReasons.SurveyFraming, result.Reasons);
    }

    [Fact]
    public void Wrong_shape_and_duplicate_sides_have_stable_reasons()
    {
        var candidate = Candidate("Should the proposal proceed?", "Yes", "No", "Maybe");
        candidate.Options.ForEach(x => x.Side = "Up");
        var result = _classifier.Classify(candidate);
        Assert.Equal(new[] { GeneratedPollCleanupReasons.OptionCardinality, GeneratedPollCleanupReasons.OptionText,
            GeneratedPollCleanupReasons.OptionSide }, result.Reasons);
    }

    [Fact]
    public void Historical_fallback_is_identified_with_source()
    {
        var result = _classifier.Classify(Candidate("What should matter most in this story: A new policy?",
            "Public impact", "Official response", "Long-term effects", "More information"));
        Assert.Contains(GeneratedPollCleanupReasons.HistoricalFallback, result.Reasons);
        Assert.Equal("historical-fallback", result.GenerationSource);
    }

    [Fact]
    public void Manual_poll_is_excluded_even_when_shape_is_bad()
    {
        var candidate = Candidate("Favorite?", "One", "Two", "Three"); candidate.IsAIGenerated = false;
        Assert.False(_classifier.Classify(candidate).IsMalformed);
    }

    [Fact]
    public void Explicit_provider_wins_and_votes_choose_preservation()
    {
        var candidate = Candidate("Which option?", "Yes", "No"); candidate.GenerationProvider = "openai"; candidate.VoteCount = 2;
        var result = _classifier.Classify(candidate);
        Assert.Equal("openai", result.GenerationSource);
        Assert.Equal(GeneratedPollCleanupPolicy.PreserveAndHide, result.ProposedDisposition);
    }

    private static GeneratedPollCleanupCandidate Candidate(string question, params string[] options) => new()
    {
        PollId = 1, Question = question, IsAIGenerated = true, IsActive = true,
        Options = options.Select((x, i) => new PollOption { Id = i + 1, PollId = 1, Text = x }).ToList()
    };
}
