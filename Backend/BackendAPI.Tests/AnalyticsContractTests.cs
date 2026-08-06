using BackendAPI.Analytics;
using Xunit;
namespace BackendAPI.Tests;
public class AnalyticsContractTests
{
    [Theory] [InlineData("email")] [InlineData("token")] [InlineData("wellness_response")] [InlineData("selected_option_id")] [InlineData("free_text")]
    public void RedactorRejectsSensitiveAndUndocumentedProperties(string key) => Assert.Throws<ArgumentException>(() => AnalyticsRedactor.Serialize(new Dictionary<string, object?> { [key] = "secret" }, key));
    [Fact] public void RedactorSerializesOnlyAllowlistedPrimitiveValues() => Assert.Equal("{\"current_level\":2}", AnalyticsRedactor.Serialize(new Dictionary<string, object?> { ["current_level"] = 2 }, "current_level"));
    [Fact] public void FeatureAssignmentIsDeterministic() { var service = new FeatureFlagService(); Assert.Equal(service.Variant("gamification_challenges_v1", "usr_42"), service.Variant("gamification_challenges_v1", "usr_42")); }
    [Fact] public void PopLiveContractContainsTheCompleteFunnel() => Assert.Equal(new[] {
        "pop_live_first_response_locked", "pop_live_invitation_created", "pop_live_invitation_opened",
        "pop_live_relay_handoff", "pop_live_rematch_requested", "pop_live_rematch_started",
        "pop_live_result_shared", "pop_live_session_completed", "pop_live_session_joined", "pop_live_toss_shown"
    }, PopLiveAnalyticsContract.EventNames.OrderBy(x => x));

    [Theory] [InlineData("mode")] [InlineData("platform")] [InlineData("variant")]
    public void PopLiveContractRejectsUnboundedDimensions(string dimension)
    {
        var value = new PopLiveEventProperties("analytics-journey", dimension == "mode" ? "unknown" : "poll_clash",
            dimension == "platform" ? "desktop" : "backend", ExperimentVariant: dimension == "variant" ? "beta" : "control");
        Assert.Throws<ArgumentException>(() => PopLiveAnalyticsContract.Validate(AnalyticsEventNames.PopLiveSessionJoined, value));
    }

    [Fact] public void PopLivePayloadHasRequiredEnvelopeWithoutSensitiveIdentifiers()
    {
        var payload = PopLiveAnalyticsContract.Serialize(new("analytics-journey", "poll_relay", ExperimentVariant: "treatment", HandoffIndex: 2));
        Assert.Contains("\"journey_id\"", payload); Assert.Contains("\"experiment_id\"", payload); Assert.Contains("\"experiment_variant\"", payload);
        Assert.DoesNotContain("token", payload, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("option", payload, StringComparison.OrdinalIgnoreCase);
    }
}
