using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using Microsoft.Extensions.Options;

namespace BackendAPI.Services;

public sealed class GeneratedPollDuplicateDetector(IPollsRepository polls, IOptions<PollQualityOptions> options)
    : IGeneratedPollDuplicateDetector
{
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
        { "a", "an", "the", "is", "are", "be", "to", "of", "for", "in", "on", "and", "or", "should", "would" };
    private readonly PollQualityOptions _options = options.Value;

    public async Task<DuplicateMatch?> FindAsync(string proposition, string? sourceUrl, CancellationToken ct = default)
    {
        var normalized = Normalize(proposition);
        var fingerprint = Fingerprint(proposition);
        DuplicateMatch? near = null;
        foreach (var poll in await polls.GetRecentGeneratedAsync(_options.DuplicateLookbackCount))
        {
            if (!string.IsNullOrWhiteSpace(sourceUrl) && CanonicalUrl(sourceUrl) == CanonicalUrl(poll.SourceUrl))
                return new(poll.Id, "source_url", 1);
            if (Fingerprint(poll.Question) == fingerprint) return new(poll.Id, "exact", 1);
            var similarity = Similarity(normalized, Normalize(poll.Question));
            if (similarity >= _options.DuplicateSimilarityThreshold && (near is null || similarity > near.Similarity))
                near = new(poll.Id, "near", similarity);
        }
        return near;
    }

    public string Fingerprint(string proposition) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Normalize(proposition)))).ToLowerInvariant();

    internal static string Normalize(string value)
    {
        var decomposed = (value ?? string.Empty).Normalize(NormalizationForm.FormD);
        var chars = decomposed.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ').ToArray();
        return string.Join(' ', new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(token => !StopWords.Contains(token)));
    }

    private static double Similarity(string a, string b)
    {
        var x = a.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var y = b.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        return x.Count == 0 || y.Count == 0 ? 0 : (double)x.Intersect(y).Count() / x.Union(y).Count();
    }

    private static string? CanonicalUrl(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)) return value?.Trim().TrimEnd('/').ToLowerInvariant();
        return $"{uri.Scheme.ToLowerInvariant()}://{uri.Host.ToLowerInvariant()}{uri.AbsolutePath.TrimEnd('/')}";
    }
}
