using System.Text.RegularExpressions;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using Microsoft.Extensions.Options;

namespace BackendAPI.Services;

public sealed class GeneratedPollQualityGate(
    IPropositionQualityEvaluator evaluator,
    IGeneratedPollDuplicateDetector duplicates,
    IOptions<PollQualityOptions> options) : IGeneratedPollQualityGate
{
    private readonly PollQualityOptions _options = options.Value;
    private static readonly Regex Leading = new(@"\b(obviously|clearly|reckless|disgraceful|common sense|only an idiot)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Prediction = new(@"\b(who|what) will\b|\bwill (it|the|there)\b|\bwhich\b|\bfavou?rite\b|\bmost important\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Unbalanced = new(@"\b(or (?:face|risk|allow) (?:death|disaster|harm)|save innocent lives|protect children or)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<GeneratedPollQualityDecision> EvaluateAsync(TrendingTopic topic,
        PropositionGenerationResult candidate, IReadOnlyList<string> pollOptions, CancellationToken ct = default)
    {
        var sensitiveCode = SensitivityCode(topic, candidate.Proposition);
        var sensitive = sensitiveCode is not null;
        var publish = sensitive ? _options.SensitiveMinimumPublishScore : _options.MinimumPublishScore;
        var review = sensitive ? _options.SensitiveMinimumReviewScore : _options.MinimumReviewScore;
        var floor = sensitive ? _options.SensitiveMinimumDimensionScore : _options.MinimumDimensionScore;
        var reasons = DeterministicReasons(candidate, pollOptions).ToList();
        var fingerprint = duplicates.Fingerprint(candidate.Proposition);
        var duplicate = await duplicates.FindAsync(candidate.Proposition, topic.SourceUrl, ct);
        if (duplicate is not null)
            reasons.Add(duplicate.MatchType is "exact" or "source_url" ? PollQualityReasonCodes.DuplicateExact : PollQualityReasonCodes.DuplicateNear);

        var hardReject = reasons.Any(r => r is PollQualityReasonCodes.ContractInvalid or PollQualityReasonCodes.SafetyProhibited
            or PollQualityReasonCodes.AnswerabilityInvalid or PollQualityReasonCodes.DuplicateExact);
        PollQualityScores? scores = null;
        if (!hardReject) scores = await evaluator.EvaluateAsync(topic, candidate, ct);
        if (scores is null && !hardReject) reasons.Add(PollQualityReasonCodes.EvaluatorUnavailable);
        scores ??= new PollQualityScores();
        var overall = scores.Values().Average();

        PollQualityDisposition disposition;
        if (hardReject) disposition = PollQualityDisposition.Rejected;
        else if (reasons.Contains(PollQualityReasonCodes.EvaluatorUnavailable)) disposition = PollQualityDisposition.NeedsReview;
        else if (overall < review) disposition = PollQualityDisposition.Rejected;
        else if (reasons.Count > 0 ||
                 reasons.Contains(PollQualityReasonCodes.DuplicateNear) ||
                 scores.Values().Any(v => v < floor) || overall < publish)
            disposition = PollQualityDisposition.NeedsReview;
        else disposition = PollQualityDisposition.Accepted;

        if (scores.Values().Any(v => v < floor) && !hardReject) reasons.Add(PollQualityReasonCodes.DimensionBelowMinimum);
        if (overall < publish && !hardReject && scores.Values().Any(v => v > 0)) reasons.Add(PollQualityReasonCodes.ScoreBelowPublish);
        if (sensitive && disposition == PollQualityDisposition.NeedsReview) reasons.Add(PollQualityReasonCodes.SensitiveReview);

        return new GeneratedPollQualityDecision
        {
            Disposition = disposition, OverallScore = overall, Scores = scores,
            ReasonCodes = reasons.Distinct(StringComparer.Ordinal).ToArray(), IsSensitive = sensitive,
            SensitivityPolicyCode = sensitiveCode, PublishThreshold = publish, ReviewThreshold = review,
            RulesVersion = _options.RulesVersion, EvaluatorPromptVersion = _options.EvaluatorPromptVersion,
            EvaluatorSchemaVersion = _options.EvaluatorSchemaVersion,
            GenerationPromptVersion = PollGenerationService.GenerationPromptVersion,
            GenerationSchemaVersion = PollGenerationService.GenerationSchemaVersion,
            GenerationProvider = candidate.ProviderName, ProviderConfidence = candidate.Quality?.Confidence,
            DuplicatePollId = duplicate?.PollId, DuplicateSimilarity = duplicate?.Similarity,
            DuplicateMatchType = duplicate?.MatchType, ExactFingerprint = fingerprint
        };
    }

    private IEnumerable<string> DeterministicReasons(PropositionGenerationResult c, IReadOnlyList<string> options)
    {
        if (!GeneratedPollContract.TryValidate(options, out _)) yield return PollQualityReasonCodes.ContractInvalid;
        if (string.IsNullOrWhiteSpace(c.Proposition) || c.Proposition.Length is < 20 or > 160 ||
            !c.Proposition.EndsWith('?') || c.Proposition.Count(x => x == '?') != 1)
            yield return PollQualityReasonCodes.AnswerabilityInvalid;
        if (c.Grounding is null || string.IsNullOrWhiteSpace(c.Grounding.Rationale) || c.Grounding.Evidence is null ||
            c.Grounding.Evidence.Count is < 1 or > 3 || c.Grounding.Evidence.Any(string.IsNullOrWhiteSpace))
            yield return PollQualityReasonCodes.GroundingInsufficient;
        if (Prediction.IsMatch(c.Proposition)) yield return PollQualityReasonCodes.AnswerabilityInvalid;
        if (c.Proposition.Contains(" and should ", StringComparison.OrdinalIgnoreCase) ||
            c.Proposition.Contains(" as well as ", StringComparison.OrdinalIgnoreCase)) yield return PollQualityReasonCodes.ClarityCompound;
        if (Leading.IsMatch(c.Proposition)) yield return PollQualityReasonCodes.NeutralityLeading;
        if (Unbalanced.IsMatch(c.Proposition)) yield return PollQualityReasonCodes.AnswerabilityUnbalanced;
        if (_options.ProhibitedTerms.Any(t => c.Proposition.Contains(t, StringComparison.OrdinalIgnoreCase)))
            yield return PollQualityReasonCodes.SafetyProhibited;
    }

    private string? SensitivityCode(TrendingTopic topic, string proposition)
    {
        var text = $"{topic.Title} {topic.Summary} {topic.Category} {proposition}";
        var keyword = _options.SensitiveKeywords.FirstOrDefault(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
        return keyword is null ? null : $"sensitive.keyword.{Regex.Replace(keyword.ToLowerInvariant(), @"\s+", "_")}";
    }
}
