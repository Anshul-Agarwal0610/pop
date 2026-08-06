using System.Text.Json.Serialization;

namespace BackendAPI.Models;

public enum PollQualityDisposition { Accepted, NeedsReview, Rejected }

public static class PollQualityReasonCodes
{
    public const string ContractInvalid = "contract.invalid";
    public const string GroundingInsufficient = "grounding.insufficient";
    public const string NeutralityLeading = "neutrality.leading";
    public const string ClarityAmbiguous = "clarity.ambiguous";
    public const string ClarityCompound = "clarity.compound";
    public const string AnswerabilityInvalid = "answerability.invalid";
    public const string AnswerabilityUnbalanced = "answerability.unbalanced";
    public const string SafetyProhibited = "safety.prohibited";
    public const string DuplicateExact = "duplicate.exact";
    public const string DuplicateNear = "duplicate.near";
    public const string EvaluatorUnavailable = "evaluator.unavailable";
    public const string ScoreBelowPublish = "score.below_publish_threshold";
    public const string DimensionBelowMinimum = "score.dimension_below_minimum";
    public const string SensitiveReview = "sensitivity.review_required";
}

public sealed class PollQualityScores
{
    [JsonPropertyName("grounding")] public double Grounding { get; set; }
    [JsonPropertyName("neutrality")] public double Neutrality { get; set; }
    [JsonPropertyName("clarity")] public double Clarity { get; set; }
    [JsonPropertyName("answerability")] public double Answerability { get; set; }
    [JsonPropertyName("balancedSides")] public double BalancedSides { get; set; }
    [JsonPropertyName("duplication")] public double Duplication { get; set; }
    [JsonPropertyName("safety")] public double Safety { get; set; }

    public IEnumerable<double> Values() =>
        [Grounding, Neutrality, Clarity, Answerability, BalancedSides, Duplication, Safety];
}

public sealed class GeneratedPollQualityDecision
{
    public PollQualityDisposition Disposition { get; init; }
    public double OverallScore { get; init; }
    public required PollQualityScores Scores { get; init; }
    public required IReadOnlyList<string> ReasonCodes { get; init; }
    public bool IsSensitive { get; init; }
    public string? SensitivityPolicyCode { get; init; }
    public double PublishThreshold { get; init; }
    public double ReviewThreshold { get; init; }
    public required string RulesVersion { get; init; }
    public required string EvaluatorPromptVersion { get; init; }
    public required string EvaluatorSchemaVersion { get; init; }
    public required string GenerationPromptVersion { get; init; }
    public required string GenerationSchemaVersion { get; init; }
    public string? GenerationProvider { get; init; }
    public double? ProviderConfidence { get; init; }
    public long? DuplicatePollId { get; init; }
    public double? DuplicateSimilarity { get; init; }
    public string? DuplicateMatchType { get; init; }
    public string? ExactFingerprint { get; init; }
}

public sealed class PollQualityOptions
{
    public const string Section = "PollQuality";
    public double MinimumPublishScore { get; set; } = .80;
    public double MinimumReviewScore { get; set; } = .55;
    public double SensitiveMinimumPublishScore { get; set; } = .90;
    public double SensitiveMinimumReviewScore { get; set; } = .70;
    public double MinimumDimensionScore { get; set; } = .60;
    public double SensitiveMinimumDimensionScore { get; set; } = .75;
    public double DuplicateSimilarityThreshold { get; set; } = .78;
    public int DuplicateLookbackCount { get; set; } = 200;
    public string RulesVersion { get; set; } = "quality-rules-v1";
    public string EvaluatorPromptVersion { get; set; } = "quality-evaluator-prompt-v1";
    public string EvaluatorSchemaVersion { get; set; } = "quality-evaluator-schema-v1";
    public string[] SensitiveKeywords { get; set; } = ["election", "abortion", "religion", "war", "suicide", "vaccine", "immigration"];
    public string[] ProhibitedTerms { get; set; } = ["kill all", "exterminate", "racially inferior", "suicide method"];
}

public sealed record DuplicateMatch(long PollId, string MatchType, double Similarity);
