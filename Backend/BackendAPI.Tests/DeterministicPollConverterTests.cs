using BackendAPI.Models;
using BackendAPI.Services;
using Xunit;

namespace BackendAPI.Tests;

public class DeterministicPollConverterTests
{
    private readonly DeterministicPollConverter _converter = new();

    [Fact]
    public void Explicit_supported_action_produces_only_canonical_sides()
    {
        var result = _converter.TryConvert(Topic("Council proposes banning cars from the city centre", "The council proposal would ban cars from the city centre on weekdays."));
        Assert.True(result.Succeeded, result.Reason);
        Assert.Equal("Should Council ban cars from the city centre?", result.Poll!.Proposition);
        Assert.Equal(new[] { "Up", "Against" }, result.Poll.Options);
        Assert.Equal(GenerationMethods.DeterministicFallback, result.Poll.GenerationMethod);
    }

    [Theory]
    [InlineData("Major changes may be coming", "Officials discussed several possible developments but announced no specific action.")]
    [InlineData("You won't believe this shocking council plan", "The meeting summary contains no proposal or concrete policy detail.")]
    [InlineData("Local update", "Brief update.")]
    [InlineData("What should matter most?", "Residents discussed transport, housing, and public spaces at a meeting.")]
    [InlineData("Which option is best?", "The article compares several unrelated options without a proposal.")]
    public void Ambiguous_sensational_insufficient_and_survey_topics_are_rejected(string title, string summary)
        => Assert.False(_converter.TryConvert(Topic(title, summary)).Succeeded);

    [Fact]
    public void Compound_actions_are_rejected()
        => Assert.False(_converter.TryConvert(Topic("Council bans cars and funds new buses", "The council would ban cars and also fund buses.")).Succeeded);

    private static TrendingTopic Topic(string title, string summary) => new() { Title = title, Summary = summary, Category = "General", SourceType = "rss" };
}
