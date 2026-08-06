namespace BackendAPI.Models;

public enum SafetyTargetType { Session, Poll, Participant }
public enum SafetyReason { Harassment, HateSpeech, Threats, SexualContent, Spam, Cheating, Impersonation, Other }
public enum RewardRiskOutcome { Allow, Cap, Hold, Suppress }

public sealed record MultiplayerPrivacySettings(bool DiscloseIdentity = false, bool DiscloseIndividualVote = false,
    bool ShareCoarseRegion = false, bool AllowPublicResultCard = false);
public sealed record MultiplayerNotificationSettings(bool Invitations = true, bool SessionActivity = true,
    bool Reminders = true, bool Results = true, TimeOnly? QuietHoursStart = null, TimeOnly? QuietHoursEnd = null,
    string TimeZoneId = "UTC", bool AllowCritical = false);
public sealed record CreateSafetyReportRequest(SafetyTargetType TargetType, Guid SessionId, Guid? ParticipantId,
    long? PollId, SafetyReason Reason, string? Comment);
public sealed record SafetyReportReceipt(Guid ReceiptId, string Status, DateTime CreatedAt);

public sealed record MultiplayerRiskContext(Guid SessionId, Guid ParticipantId, string Rule, bool SelfInvite,
    bool Replay, bool RapidAccountCycling, bool DuplicateDevice, bool DuplicateNetwork,
    bool ImplausibleTiming, bool RepeatedPairing);
public sealed record MultiplayerRiskDecision(RewardRiskOutcome Outcome, int Score, string PolicyVersion,
    IReadOnlyList<string> Signals, DateTime EvaluatedAt)
{
    public bool IsPermanentBan => false;
}

public static class MultiplayerTrustRules
{
    public const int RegionalMinimumCohort = 5;
    public const int AnonymousJoinLimit = 6;
    public const int AuthenticatedJoinLimit = 30;
    public static int JoinLimit(bool authenticated) => authenticated ? AuthenticatedJoinLimit : AnonymousJoinLimit;
    public static bool CanDiscloseRegion(bool consented, int cohortSize, int minimum = RegionalMinimumCohort) =>
        consented && cohortSize >= Math.Max(2, minimum);

    public static bool IsQuietNow(MultiplayerNotificationSettings settings, DateTime utcNow)
    {
        if (settings.QuietHoursStart is null || settings.QuietHoursEnd is null) return false;
        TimeZoneInfo zone;
        try { zone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId); }
        catch (TimeZoneNotFoundException) { zone = TimeZoneInfo.Utc; }
        catch (InvalidTimeZoneException) { zone = TimeZoneInfo.Utc; }
        var now = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), zone));
        var start = settings.QuietHoursStart.Value;
        var end = settings.QuietHoursEnd.Value;
        return start == end || (start < end ? now >= start && now < end : now >= start || now < end);
    }
}
