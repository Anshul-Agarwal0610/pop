using System.Text.Json;
using Xunit;

namespace BackendAPI.Tests;

public class GeneratedPollQualityFixtureTests
{
    [Fact]
    public void Versioned_fixture_suite_covers_categories_and_all_dispositions()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "generated-poll-quality-v1.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var fixtures = document.RootElement.EnumerateArray().ToArray();
        var categories = fixtures.Select(x => x.GetProperty("category").GetString()).ToHashSet();
        var dispositions = fixtures.Select(x => x.GetProperty("expectedDisposition").GetString()).ToHashSet();

        Assert.True(new[] { "Politics", "Technology", "Health", "Sports", "Entertainment", "Business", "General" }.All(categories.Contains));
        Assert.True(new[] { "Accepted", "NeedsReview", "Rejected" }.All(dispositions.Contains));
        Assert.All(fixtures, fixture =>
        {
            Assert.True(fixture.GetProperty("options").GetArrayLength() == 2);
            Assert.True(fixture.TryGetProperty("source", out _));
            Assert.True(fixture.TryGetProperty("expectedReasons", out _));
        });
    }
}
