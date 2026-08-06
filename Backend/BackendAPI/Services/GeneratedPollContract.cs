using BackendAPI.Models;

namespace BackendAPI.Services;

/// <summary>The sole domain definition of the generated binary-poll invariant.</summary>
public static class GeneratedPollContract
{
    public const string Up = "Up";
    public const string Against = "Against";
    public static IReadOnlyList<string> CanonicalOptions { get; } = new[] { Up, Against };

    // Deliberately strict: provider casing, whitespace, synonyms, order, and cardinality are contract violations.
    public static bool TryValidate(IReadOnlyList<string>? options, out string reason)
    {
        if (options is null) { reason = "options are missing"; return false; }
        if (options.Count != 2) { reason = "exactly two options are required"; return false; }
        if (options[0] != Up || options[1] != Against)
        {
            reason = "options must be ordered exactly as Up, Against";
            return false;
        }
        reason = string.Empty;
        return true;
    }

    public static void EnsureValid(IReadOnlyList<string>? options)
    {
        if (!TryValidate(options, out var reason))
            throw new ArgumentException($"Invalid generated poll: {reason}.", nameof(options));
    }
}
