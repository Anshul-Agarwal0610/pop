namespace BackendAPI.Services;

public static class TopicLanguagePolicy
{
    public static string? BatchLanguage(string? language)
    {
        if (!IsSupported(language)) return null;
        return language!.Trim().StartsWith("hi", StringComparison.OrdinalIgnoreCase) ? "hi" : "en";
    }

    public static bool IsSupported(string? language)
    {
        if (string.IsNullOrWhiteSpace(language)) return false;
        var code = language.Trim().ToLowerInvariant();
        return code is "en" or "hi" || code.StartsWith("en-") || code.StartsWith("hi-");
    }
}
