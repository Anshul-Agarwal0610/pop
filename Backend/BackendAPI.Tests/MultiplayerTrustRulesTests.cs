using BackendAPI.Models;
using Xunit;

namespace BackendAPI.Tests;

public sealed class MultiplayerTrustRulesTests
{
    [Fact] public void Disclosure_defaults_are_private() { var p=new MultiplayerPrivacySettings(); Assert.False(p.DiscloseIdentity); Assert.False(p.DiscloseIndividualVote); Assert.False(p.ShareCoarseRegion); Assert.False(p.AllowPublicResultCard); }
    [Fact] public void Anonymous_limits_are_stricter() => Assert.True(MultiplayerTrustRules.JoinLimit(false) < MultiplayerTrustRules.JoinLimit(true));
    [Theory, InlineData(false,10,false), InlineData(true,4,false), InlineData(true,5,true)]
    public void Region_requires_consent_and_minimum_cohort(bool consent,int cohort,bool expected) => Assert.Equal(expected,MultiplayerTrustRules.CanDiscloseRegion(consent,cohort));
    [Fact] public void Quiet_hours_work_across_midnight()
    { var s=new MultiplayerNotificationSettings(QuietHoursStart:new(22,0),QuietHoursEnd:new(7,0),TimeZoneId:"UTC"); Assert.True(MultiplayerTrustRules.IsQuietNow(s,new DateTime(2026,1,1,23,0,0,DateTimeKind.Utc))); Assert.True(MultiplayerTrustRules.IsQuietNow(s,new DateTime(2026,1,1,6,0,0,DateTimeKind.Utc))); Assert.False(MultiplayerTrustRules.IsQuietNow(s,new DateTime(2026,1,1,12,0,0,DateTimeKind.Utc))); }
}
