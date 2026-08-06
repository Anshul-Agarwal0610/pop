using BackendAPI.Interfaces;
using BackendAPI.Models;

namespace BackendAPI.Services;

public sealed class MultiplayerRewardRiskEvaluator : IMultiplayerRewardRiskEvaluator
{
    public const string PolicyVersion = "multiplayer-risk-v1";
    public MultiplayerRiskDecision Evaluate(MultiplayerRiskContext c, DateTime? evaluatedAt = null)
    {
        var signals = new List<string>();
        void Add(bool condition, string name) { if (condition) signals.Add(name); }
        Add(c.SelfInvite, "self_invite"); Add(c.Replay, "replay"); Add(c.RapidAccountCycling, "rapid_account_cycling");
        Add(c.DuplicateDevice, "duplicate_device"); Add(c.DuplicateNetwork, "duplicate_network");
        Add(c.ImplausibleTiming, "implausible_timing"); Add(c.RepeatedPairing, "repeated_pairing");

        // Replay and self-invite invalidate this reward only. Weak correlation signals never ban an identity.
        var outcome = c.SelfInvite || c.Replay ? RewardRiskOutcome.Suppress
            : signals.Count >= 4 ? RewardRiskOutcome.Suppress
            : signals.Count >= 2 ? RewardRiskOutcome.Hold
            : signals.Count == 1 ? RewardRiskOutcome.Cap : RewardRiskOutcome.Allow;
        return new(outcome, signals.Count, PolicyVersion, signals, evaluatedAt ?? DateTime.UtcNow);
    }
}
