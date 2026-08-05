using BackendAPI.Services;
using Xunit;

namespace BackendAPI.Tests;

public sealed class GameSessionRulesTests
{
    private static readonly DateTime Now = new(2026, 8, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Timed_round_expires_at_server_deadline()
    {
        Assert.False(GameSessionRules.IsExpired(Now.AddTicks(1), Now));
        Assert.True(GameSessionRules.IsExpired(Now, Now));
        Assert.True(GameSessionRules.IsExpired(Now.AddSeconds(-1), Now));
    }

    [Fact]
    public void Untimed_round_never_expires_from_elapsed_time() =>
        Assert.False(GameSessionRules.IsExpired(null, Now.AddYears(1)));

    [Fact]
    public void Resume_requires_the_persisted_current_position()
    {
        Assert.True(GameSessionRules.IsCurrentPosition(2, 2));
        Assert.False(GameSessionRules.IsCurrentPosition(2, 1));
        Assert.False(GameSessionRules.IsCurrentPosition(2, 3));
    }

    [Fact]
    public void Completion_reward_is_idempotent()
    {
        Assert.True(GameSessionRules.CanGrantCompletionReward("Active", null, 5, 5));
        Assert.False(GameSessionRules.CanGrantCompletionReward("Completed", Now, 5, 5));
        Assert.False(GameSessionRules.CanGrantCompletionReward("Active", Now, 5, 5));
        Assert.False(GameSessionRules.CanGrantCompletionReward("Active", null, 4, 5));
    }
}
