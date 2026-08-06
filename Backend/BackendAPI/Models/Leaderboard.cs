using System.Text.Json.Serialization;

namespace BackendAPI.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LeaderboardPeriod
{
    Weekly,
    AllTime
}

public class LeaderboardRow
{
    public long Rank { get; set; }
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public long PeriodXp { get; set; }
    public int LifetimeXp { get; set; }
    public int Level => LifetimeXp / 1000 + 1;
    public List<UserBadge> Badges { get; set; } = new();
}

public sealed class LeaderboardResponse
{
    public IEnumerable<LeaderboardRow> Rows { get; set; } = Enumerable.Empty<LeaderboardRow>();
    public LeaderboardRow? CurrentUser { get; set; }
    public LeaderboardPeriod Period { get; set; }
    public DateTime? PeriodStartUtc { get; set; }
    public DateTime? PeriodEndUtc { get; set; }
    public DateTime? NextResetAtUtc { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
    public bool HasMore { get; set; }
}

public readonly record struct LeaderboardWindow(DateTime? StartUtc, DateTime? EndUtc)
{
    /// <summary>Weekly rankings use [Monday 00:00 UTC, next Monday 00:00 UTC).</summary>
    public static LeaderboardWindow For(LeaderboardPeriod period, DateTime utcNow)
    {
        if (period == LeaderboardPeriod.AllTime) return new(null, null);
        var utc = utcNow.Kind == DateTimeKind.Utc ? utcNow : utcNow.ToUniversalTime();
        var daysSinceMonday = ((int)utc.DayOfWeek + 6) % 7;
        var start = utc.Date.AddDays(-daysSinceMonday);
        return new(DateTime.SpecifyKind(start, DateTimeKind.Utc), DateTime.SpecifyKind(start.AddDays(7), DateTimeKind.Utc));
    }
}
