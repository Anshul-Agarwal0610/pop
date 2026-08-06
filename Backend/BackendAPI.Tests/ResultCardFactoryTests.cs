using BackendAPI.Models;
using BackendAPI.Services;
using Xunit;

namespace BackendAPI.Tests;

public sealed class ResultCardFactoryTests
{
    private readonly ResultCardFactory factory = new();
    private static NormalizedMultiplayerResult Result(string mode = MultiplayerModes.Clash) => new(42, 7, mode, ResultCardState.Completed,
        "Team reached 80% agreement", "5-round chain", new RecordedBadge("In Sync", "★"),
        [new(7,"Owner","https://cdn.example/owner.png",true),new(9,"Private Person","https://cdn.example/private.png",false)]);

    [Fact] public void IdenticalInputProducesIdenticalPayloadAndHash()
    { var a=factory.Create(Result()); var b=factory.Create(Result()); Assert.Equal(factory.Serialize(a),factory.Serialize(b)); Assert.Equal(factory.Hash(a),factory.Hash(b)); }

    [Theory, InlineData("Clash"), InlineData("Relay"), InlineData("Room")]
    public void SupportsEveryMultiplayerMode(string mode)
    { var card=factory.Create(Result(mode)); Assert.Equal(mode,card.Mode); Assert.Equal("5-round chain",card.Milestone); Assert.Equal("In Sync",card.Badge?.Name); }

    [Fact] public void RejectsUnsupportedAndIncompleteResults()
    { Assert.Throws<ResultCardException>(()=>factory.Create(Result("OpinionSprint"))); Assert.Throws<ResultCardException>(()=>factory.Create(Result() with { State=ResultCardState.Expired })); }

    [Fact] public void RedactsIdentityWithoutExplicitConsent()
    { var json=factory.Serialize(factory.Create(Result())); Assert.Contains("Owner",json); Assert.DoesNotContain("Private Person",json); Assert.DoesNotContain("private.png",json); Assert.DoesNotContain("\"userId\"",json); Assert.Contains("Participant 2",json); }

    [Fact] public void PayloadContainsNoVoteLocationOrEmailFields()
    { var json=factory.Serialize(factory.Create(Result())).ToLowerInvariant(); Assert.DoesNotContain("optionid",json); Assert.DoesNotContain("voteid",json); Assert.DoesNotContain("location",json); Assert.DoesNotContain("email",json); }
}
