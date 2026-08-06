using System.Text.Json;
namespace BackendAPI.Analytics;
public static class AnalyticsRedactor
{
    private static readonly string[] Forbidden = ["answer", "option", "question", "description", "wellness", "health", "email", "username", "displayname", "token", "jwt", "url", "error", "text"];
    public static string Serialize(IReadOnlyDictionary<string, object?> properties, params string[] allowed)
    {
        var allow = allowed.ToHashSet(StringComparer.Ordinal);
        foreach (var item in properties)
        {
            if (!allow.Contains(item.Key) || Forbidden.Any(x => item.Key.Replace("_", "").Contains(x, StringComparison.OrdinalIgnoreCase))) throw new ArgumentException($"Analytics property is not allowed: {item.Key}");
            if (item.Value is not null and not string and not bool and not byte and not short and not int and not long and not float and not double and not decimal) throw new ArgumentException($"Analytics property has an invalid type: {item.Key}");
        }
        return JsonSerializer.Serialize(properties);
    }
}
