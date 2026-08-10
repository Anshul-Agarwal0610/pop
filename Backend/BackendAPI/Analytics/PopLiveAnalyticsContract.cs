namespace BackendAPI.Analytics;

public static class PopLiveAnalyticsContract
{
    public const int SchemaVersion = 1;
    public const string ExperimentId = "pop_live_funnel_v1";
    public static readonly IReadOnlySet<string> EventNames = new HashSet<string>(StringComparer.Ordinal)
    {
        AnalyticsEventNames.PopLiveTossShown, AnalyticsEventNames.PopLiveInvitationCreated,
        AnalyticsEventNames.PopLiveInvitationOpened, AnalyticsEventNames.PopLiveSessionJoined,
        AnalyticsEventNames.PopLiveFirstResponseLocked, AnalyticsEventNames.PopLiveSessionCompleted,
        AnalyticsEventNames.PopLiveResultShared, AnalyticsEventNames.PopLiveRematchRequested,
        AnalyticsEventNames.PopLiveRematchStarted, AnalyticsEventNames.PopLiveRelayHandoff
    };
    public static readonly IReadOnlySet<string> Modes = Set("poll_toss", "poll_clash", "poll_relay", "poll_bomb", "live_room");
    public static readonly IReadOnlySet<string> Platforms = Set("web", "ios", "android", "backend");
    public static readonly IReadOnlySet<string> Sources = Set("client", "server");
    public static readonly IReadOnlySet<string> InvitationChannels = Set("link", "room_code", "native_share", "in_app", "none");
    public static readonly IReadOnlySet<string> CompletionReasons = Set("completed", "expired", "cancelled", "target_not_reached", "none");
    public static readonly IReadOnlySet<string> Variants = Set("control", "treatment");

    public static void Validate(string eventName, PopLiveEventProperties properties)
    {
        Require(EventNames, eventName, nameof(eventName));
        if (string.IsNullOrWhiteSpace(properties.JourneyId) || properties.JourneyId.Length > 80) throw new ArgumentException("A bounded opaque journey id is required.");
        Require(Modes, properties.Mode, nameof(properties.Mode));
        Require(Platforms, properties.Platform, nameof(properties.Platform));
        Require(Sources, properties.Source, nameof(properties.Source));
        Require(InvitationChannels, properties.InvitationChannel, nameof(properties.InvitationChannel));
        Require(CompletionReasons, properties.CompletionReason, nameof(properties.CompletionReason));
        Require(Variants, properties.ExperimentVariant, nameof(properties.ExperimentVariant));
        if (properties.HandoffIndex is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(properties.HandoffIndex));
    }

    public static string Serialize(PopLiveEventProperties p) => AnalyticsRedactor.Serialize(new Dictionary<string, object?>
    {
        ["journey_id"] = p.JourneyId, ["mode"] = p.Mode, ["platform"] = p.Platform,
        ["source"] = p.Source, ["invitation_channel"] = p.InvitationChannel,
        ["completion_reason"] = p.CompletionReason, ["experiment_id"] = ExperimentId,
        ["experiment_variant"] = p.ExperimentVariant, ["handoff_index"] = p.HandoffIndex
    }, "journey_id", "mode", "platform", "source", "invitation_channel", "completion_reason", "experiment_id", "experiment_variant", "handoff_index");

    private static HashSet<string> Set(params string[] values) => new(values, StringComparer.Ordinal);
    private static void Require(IReadOnlySet<string> values, string value, string field) { if (!values.Contains(value)) throw new ArgumentException($"Unknown {field}.", field); }
}

public sealed record PopLiveEventProperties(
    string JourneyId, string Mode, string Platform = "backend", string Source = "server",
    string InvitationChannel = "none", string CompletionReason = "none",
    string ExperimentVariant = "control", int? HandoffIndex = null);
