using BackendAPI.Analytics;
namespace BackendAPI.Tests;
public class AnalyticsContractTests
{
    [Theory] [InlineData("email")] [InlineData("token")] [InlineData("wellness_response")] [InlineData("selected_option_id")] [InlineData("free_text")]
    public void RedactorRejectsSensitiveAndUndocumentedProperties(string key) => Assert.Throws<ArgumentException>(() => AnalyticsRedactor.Serialize(new Dictionary<string, object?> { [key] = "secret" }, key));
    [Fact] public void RedactorSerializesOnlyAllowlistedPrimitiveValues() => Assert.Equal("{\"current_level\":2}", AnalyticsRedactor.Serialize(new Dictionary<string, object?> { ["current_level"] = 2 }, "current_level"));
    [Fact] public void FeatureAssignmentIsDeterministic() { var service = new FeatureFlagService(); Assert.Equal(service.Variant("gamification_challenges_v1", "usr_42"), service.Variant("gamification_challenges_v1", "usr_42")); }
}
