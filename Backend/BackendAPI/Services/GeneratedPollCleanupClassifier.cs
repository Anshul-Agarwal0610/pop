using System.Text.RegularExpressions;
using BackendAPI.Interfaces;
using BackendAPI.Models;

namespace BackendAPI.Services;

public static partial class PropositionFormRules
{
    [GeneratedRegex(@"\b(which|favorite|favourite|prefer|preference|rank|ranking|most important|who will|what will|will (it|the|there))\b", RegexOptions.IgnoreCase)]
    private static partial Regex SurveyPattern();

    public static bool IsSurveyPreferenceRankingOrPrediction(string question) => SurveyPattern().IsMatch(question ?? string.Empty);
}

public sealed class GeneratedPollCleanupClassifier : IGeneratedPollCleanupClassifier
{
    public const string FallbackPrefix = "What should matter most in this story:";
    private static readonly HashSet<string> FallbackOptions = new(StringComparer.OrdinalIgnoreCase)
    {
        "User benefits", "Privacy and safety", "Cost and access", "Long-term impact",
        "Player performance", "Team strategy", "Coaching decisions", "Future prospects",
        "Prevention", "Access to care", "Public awareness", "Research and policy",
        "Consumer impact", "Market response", "Jobs and livelihoods", "Long-term growth",
        "Immediate protection", "Policy response", "Long-term resilience", "Creative quality",
        "Audience response", "Social impact", "Lasting influence", "Public impact",
        "Official response", "Long-term effects", "More information"
    };

    public GeneratedPollCleanupClassification Classify(GeneratedPollCleanupCandidate candidate)
    {
        if (!candidate.IsAIGenerated)
            return new(false, GeneratedPollCleanupPolicy.DetectionVersion, [], "not-generated", string.Empty);

        var reasons = new List<string>();
        if (candidate.Options.Count != 2) reasons.Add(GeneratedPollCleanupReasons.OptionCardinality);
        if (candidate.Options.Count != 2 ||
            !candidate.Options.Select(x => x.Text).SequenceEqual(GeneratedPollContract.CanonicalOptions, StringComparer.Ordinal))
            reasons.Add(GeneratedPollCleanupReasons.OptionText);

        var nonNullSides = candidate.Options.Where(x => x.Side is not null).Select(x => x.Side!).ToArray();
        if (nonNullSides.Length > 0 && (nonNullSides.Length != candidate.Options.Count ||
            nonNullSides.Distinct(StringComparer.Ordinal).Count() != nonNullSides.Length ||
            candidate.Options.Any(x => x.Side is not null && !x.Side.Equals(x.Text, StringComparison.Ordinal))))
            reasons.Add(GeneratedPollCleanupReasons.OptionSide);

        var historicalFallback = candidate.Question.StartsWith(FallbackPrefix, StringComparison.OrdinalIgnoreCase) ||
            candidate.Options.Any(x => FallbackOptions.Contains(x.Text));
        if (historicalFallback) reasons.Add(GeneratedPollCleanupReasons.HistoricalFallback);
        if (PropositionFormRules.IsSurveyPreferenceRankingOrPrediction(candidate.Question))
            reasons.Add(GeneratedPollCleanupReasons.SurveyFraming);

        var generationSource = !string.IsNullOrWhiteSpace(candidate.GenerationProvider)
            ? candidate.GenerationProvider!
            : historicalFallback ? "historical-fallback" : "legacy-unknown";
        return new(reasons.Count > 0, GeneratedPollCleanupPolicy.DetectionVersion,
            reasons.Distinct(StringComparer.Ordinal).ToArray(), generationSource,
            candidate.VoteCount == 0 ? GeneratedPollCleanupPolicy.DeactivateAndRegenerate : GeneratedPollCleanupPolicy.PreserveAndHide);
    }
}

