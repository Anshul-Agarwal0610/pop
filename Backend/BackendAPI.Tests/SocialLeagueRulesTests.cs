using BackendAPI.Models;
using System.Text.Json;
using Xunit;

namespace BackendAPI.Tests;

public sealed class SocialLeagueRulesTests
{
    [Theory]
    [InlineData("2026-08-03T00:00:00Z", "2026-08-03T00:00:00Z")]
    [InlineData("2026-08-09T23:59:59Z", "2026-08-03T00:00:00Z")]
    [InlineData("2026-08-10T00:00:00Z", "2026-08-10T00:00:00Z")]
    public void Week_starts_at_monday_midnight_utc(string value, string expected) =>
        Assert.Equal(DateTime.Parse(expected).ToUniversalTime(), SocialLeagueRules.WeekStart(DateTime.Parse(value)));

    [Theory]
    [InlineData(-1, 20)] [InlineData(0, 20)] [InlineData(1, 1)] [InlineData(100, 50)]
    public void Page_size_is_bounded(int requested, int expected) => Assert.Equal(expected, SocialLeagueRules.ClampLimit(requested));

    [Fact]
    public void Safe_social_projection_does_not_serialize_private_fields()
    {
        var json = JsonSerializer.Serialize(new SocialUserSummary(1, "alex", "Alex", null));
        Assert.DoesNotContain("email", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("authProvider", json, StringComparison.OrdinalIgnoreCase);
    }
}
