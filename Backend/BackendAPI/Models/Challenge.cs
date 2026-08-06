namespace BackendAPI.Models;

public static class ChallengeRecurrences
{
    public const string Daily = "Daily";
    public const string Weekly = "Weekly";
    public const string None = "None";
}

public static class ChallengeStates
{
    public const string Available = "Available";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Expired = "Expired";
}

public class Challenge
{
    public long Id { get; set; }
    public long? DefinitionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ChallengeType { get; set; } = "Voting";
    public string Recurrence { get; set; } = ChallengeRecurrences.Daily;
    public string RequirementType { get; set; } = "VoteCount";
    public string RequirementText { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int RequiredVotes { get; set; }
    public int RewardXp { get; set; }
    public string? RewardBadge { get; set; }
    public long? RewardBadgeId { get; set; }
    public bool AllowPrivateVotes { get; set; }
    public bool AllowWellnessVotes { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserChallenge : Challenge
{
    public long ChallengeId { get; set; }
    public int CurrentVotes { get; set; }
    public bool IsCompleted { get; set; }
    public bool RewardGranted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int AwardedXp { get; set; }
    public string State { get; set; } = ChallengeStates.Available;
    public string EligiblePollsUrl { get; set; } = "/polls";
}

public static class ChallengeDomain
{
    public static (DateTime StartAt, DateTime EndAt) Window(string recurrence, DateTime utcNow)
    {
        var now = utcNow.Kind == DateTimeKind.Utc ? utcNow : utcNow.ToUniversalTime();
        var day = now.Date;
        return recurrence switch
        {
            ChallengeRecurrences.Daily => (day, day.AddDays(1)),
            ChallengeRecurrences.Weekly => WeeklyWindow(day),
            _ => throw new ArgumentOutOfRangeException(nameof(recurrence), recurrence, "Unsupported recurrence.")
        };
    }

    private static (DateTime, DateTime) WeeklyWindow(DateTime day)
    {
        var daysSinceMonday = ((int)day.DayOfWeek + 6) % 7;
        var start = day.AddDays(-daysSinceMonday);
        return (start, start.AddDays(7));
    }

    public static string State(bool completed, int progress, DateTime endAt, DateTime utcNow) =>
        completed ? ChallengeStates.Completed :
        endAt <= utcNow ? ChallengeStates.Expired :
        progress > 0 ? ChallengeStates.InProgress : ChallengeStates.Available;

    public static bool IsEligible(Challenge challenge, Poll poll, DateTime utcNow) =>
        challenge.IsActive && challenge.StartAt <= utcNow && utcNow < challenge.EndAt &&
        challenge.RequirementType.Equals("VoteCount", StringComparison.OrdinalIgnoreCase) &&
        (challenge.Category == null || challenge.Category.Equals(poll.Category, StringComparison.OrdinalIgnoreCase)) &&
        (challenge.AllowPrivateVotes || !poll.IsPrivate) &&
        (challenge.AllowWellnessVotes || (!poll.IsWellness && !poll.PollMode.Equals(PollModes.Wellness, StringComparison.OrdinalIgnoreCase))) &&
        poll.IsActive && poll.ModerationStatus.Equals(PollModerationStatus.Published, StringComparison.OrdinalIgnoreCase);
}
