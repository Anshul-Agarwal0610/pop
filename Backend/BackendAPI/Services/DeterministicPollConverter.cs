using BackendAPI.Interfaces;
using BackendAPI.Models;
using System.Text.RegularExpressions;

namespace BackendAPI.Services;

public sealed record DeterministicConversionResult(PropositionGenerationResult? Poll, string? Reason)
{
    public bool Succeeded => Poll is not null;
}

public interface IDeterministicPollConverter { DeterministicConversionResult TryConvert(TrendingTopic topic); }

public sealed partial class DeterministicPollConverter : IDeterministicPollConverter
{
    private static readonly string[] Forbidden = ["what should", "which ", "best", "favorite", "favourite", "most important", "who will", "what will", "you won't believe", "shocking"];

    public DeterministicConversionResult TryConvert(TrendingTopic topic)
    {
        var title = Normalize(topic.Title);
        var summary = Normalize(topic.Summary);
        if (title.Length < 15 || summary.Length < 25) return Reject("insufficient source detail");
        if (Forbidden.Any(x => title.Contains(x, StringComparison.OrdinalIgnoreCase))) return Reject("survey, prediction, or sensational framing");
        if (Compound().IsMatch(title)) return Reject("compound proposition");

        var match = Action().Match(title);
        if (!match.Success) return Reject("no explicit actor, action, and object");
        var actor = match.Groups["actor"].Value.Trim(' ', ',', ':', '-');
        var action = match.Groups["action"].Value.ToLowerInvariant();
        var target = match.Groups["target"].Value.Trim(' ', '.', '?', '!');
        if (action == "proposes" && target.StartsWith("banning ", StringComparison.OrdinalIgnoreCase))
        {
            action = "bans";
            target = target[8..];
        }
        if (actor.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > 8 || target.Length < 4 || target.Length > 100)
            return Reject("action is not sufficiently specific");
        if (!summary.Contains(CoreToken(target), StringComparison.OrdinalIgnoreCase)) return Reject("title claim is not supported by summary");

        var proposition = $"Should {actor} {ToBaseForm(action)} {target}?";
        var result = new PropositionGenerationResult
        {
            Proposition = proposition, Category = CategoryCatalog.NormalizeName(topic.Category),
            Grounding = new SourceGrounding { Rationale = "The source explicitly identifies a named actor and proposed action.", Evidence = [title, summary[..Math.Min(summary.Length, 180)]] },
            Quality = new PropositionQuality { IsSelfContained = true, IsNeutral = true, IsBinary = true, IsGrounded = true, Confidence = .85, IsAmbiguous = false, AmbiguityReason = null },
            GenerationMethod = GenerationMethods.DeterministicFallback, SourceTitle = topic.Title, SourceUrl = topic.SourceUrl
        };
        return BinaryPublicationValidator.Validate(result, topic, out var reason) ? new(result, null) : Reject(reason);
    }

    private static DeterministicConversionResult Reject(string reason) => new(null, reason);
    private static string Normalize(string? value) => Regex.Replace(value?.Trim() ?? "", @"\s+", " ");
    private static string CoreToken(string target) => target.Split(' ', StringSplitOptions.RemoveEmptyEntries).OrderByDescending(x => x.Length).FirstOrDefault() ?? target;
    private static string ToBaseForm(string action) => action switch { "proposes" => "adopt", "approves" => "approve", "bans" => "ban", "requires" => "require", "adopts" => "adopt", "repeals" => "repeal", "funds" => "fund", "merges" => "merge", "changes" => "change", _ => action };

    [GeneratedRegex(@"^(?<actor>[A-Z][A-Za-z0-9 .'-]{1,70}?)\s+(?<action>proposes|approves|bans|requires|adopts|repeals|funds|merges|changes)\s+(?<target>.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex Action();
    [GeneratedRegex(@"\b(and|as well as)\b.*\b(proposes|approves|bans|requires|adopts|repeals|funds|merges|changes)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Compound();
}

public static class BinaryPublicationValidator
{
    public static bool Validate(PropositionGenerationResult result, TrendingTopic topic, out string reason)
    {
        if (!GeneratedPollContract.TryValidate(result.Options, out reason)) return false;
        if (!PollGenerationService.Validate(result, out reason)) return false;
        if (string.IsNullOrWhiteSpace(topic.Title) || (string.IsNullOrWhiteSpace(topic.Summary) && string.IsNullOrWhiteSpace(result.Grounding?.Evidence.FirstOrDefault()))) { reason = "insufficient source evidence"; return false; }
        if (!CategoryCatalog.All.Any(c => c.Name.Equals(result.Category, StringComparison.OrdinalIgnoreCase) || c.Slug.Equals(result.Category, StringComparison.OrdinalIgnoreCase))) { reason = "unknown category"; return false; }
        return true;
    }
}
