using BackendAPI.Models;
using BackendAPI.Services;
using Xunit;

namespace BackendAPI.Tests;

public class TopicEnrichmentTests
{
    [Theory]
    [InlineData("Google launches a new AI assistant", "Technology")]
    [InlineData("BCCI announces cricket tournament schedule", "Sports")]
    [InlineData("Hospital expands cancer screening access", "Health")]
    [InlineData("Stock market reacts to bank policy", "Work")]
    public void Classify_UsesTopicKeywords(string title, string expected)
    {
        Assert.Equal(expected, TopicEnrichment.Classify(title, null));
    }

    [Fact]
    public void CreateFallbackPoll_ProducesAValidBoundedPoll()
    {
        var topic = new TrendingTopic
        {
            Title = new string('A', 300),
            Summary = "A technology story",
            Category = "Technology",
            SourceUrl = "https://example.com/story"
        };

        var poll = TopicEnrichment.CreateFallbackPoll(topic);

        Assert.InRange(poll.Question.Length, 30, 120);
        Assert.EndsWith("?", poll.Question);
        Assert.InRange(poll.Options.Count, 2, 4);
        Assert.All(poll.Options, option => Assert.True(option.Length <= 40));
        Assert.Equal("Technology", poll.Category);
        Assert.Contains("deterministic fallback", poll.QualityWarnings.Single());
    }

    [Fact]
    public void CleanText_DecodesEntitiesAndCollapsesWhitespace()
    {
        Assert.Equal("Gold & silver rise", TopicEnrichment.CleanText(" Gold &amp;   silver\n rise "));
    }

    [Theory]
    [InlineData("Police investigate a fatal shooting")]
    [InlineData("New suicide prevention programme announced")]
    public void RequiresHumanReview_FlagsSensitiveStories(string title)
    {
        Assert.True(TopicEnrichment.RequiresHumanReview(new TrendingTopic { Title = title }));
    }
}
