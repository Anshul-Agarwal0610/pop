using BackendAPI.Models;
namespace BackendAPI.Services;
public static class LiveRoomScoring
{
    public static IReadOnlyDictionary<Guid,int> Score(LiveRoomMode mode, LiveRoomRuleConfig rules,
        IReadOnlyDictionary<Guid,(BinaryChoice Choice, BinaryChoice? Prediction)> votes)
    {
        var up = votes.Count(x => x.Value.Choice == BinaryChoice.Up); var against = votes.Count - up;
        if (mode == LiveRoomMode.PredictMajority) {
            if (up == against) return votes.Keys.ToDictionary(x => x, _ => 0); // ties award no prediction points
            var majority = up > against ? BinaryChoice.Up : BinaryChoice.Against;
            return votes.ToDictionary(x => x.Key, x => x.Value.Prediction == majority ? rules.CorrectPredictionPoints : 0);
        }
        var consensus = votes.Count > 0 && Math.Max(up, against) / (double)votes.Count >= rules.ConsensusThreshold;
        return votes.Keys.ToDictionary(x => x, _ => consensus ? rules.ConsensusPoints : 0);
    }
}
