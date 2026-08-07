using BackendAPI.Models;

namespace BackendAPI.Services;

public static class PollClashRules
{
    public static bool IsValidRoundCount(int count) => count is 1 or 3 or 5;
    public static long? ResolveMajority(long firstOptionId, int firstVotes, long secondOptionId, int secondVotes)
        => firstVotes == secondVotes ? null : firstVotes > secondVotes ? firstOptionId : secondOptionId;
    public static int PredictionPoint(long? predictionOptionId, long? majorityOptionId)
        => majorityOptionId.HasValue && predictionOptionId == majorityOptionId ? 1 : 0;
    public static bool Agreement(long firstOpinionOptionId, long secondOpinionOptionId) => firstOpinionOptionId == secondOpinionOptionId;
    public static bool CanReveal(int responseCount) => responseCount == 2;
    public static bool CanComplete(int revealedRounds, int roundCount) => revealedRounds == roundCount;
    public static bool CanRematch(string status) => status == PollClashStatuses.Completed;
    public static PollClashScore Score(IEnumerable<(long FirstOpinion, long SecondOpinion, long? FirstPrediction, long? SecondPrediction, long? Majority)> rounds)
    {
        var first=0; var second=0; var agreement=0; var completed=0;
        foreach (var round in rounds) { completed++; first += PredictionPoint(round.FirstPrediction, round.Majority); second += PredictionPoint(round.SecondPrediction, round.Majority); if (Agreement(round.FirstOpinion, round.SecondOpinion)) agreement++; }
        return new(first, second, agreement, completed);
    }
}
