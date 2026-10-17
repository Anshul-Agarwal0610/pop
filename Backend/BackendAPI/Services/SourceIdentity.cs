using System.Security.Cryptography;
using System.Text;

namespace BackendAPI.Services;

public static class SourceIdentity
{
    public static string CanonicalUrl(string? value)
    {
        if (!Uri.TryCreate(value,UriKind.Absolute,out var uri)) return value?.Trim() ?? "";
        var query = uri.Query.TrimStart('?').Split('&',StringSplitOptions.RemoveEmptyEntries)
            .Where(part => { var key=part.Split('=')[0]; return !key.StartsWith("utm_",StringComparison.OrdinalIgnoreCase) && !new[]{"fbclid","gclid","tracking"}.Contains(key,StringComparer.OrdinalIgnoreCase); })
            .OrderBy(part=>part,StringComparer.Ordinal).ToArray();
        return $"{uri.Scheme.ToLowerInvariant()}://{uri.Host.ToLowerInvariant()}{(uri.IsDefaultPort ? "" : ":"+uri.Port)}{uri.AbsolutePath.TrimEnd('/')}" + (query.Length==0?"":"?"+string.Join('&',query));
    }
    public static string Key(string source,string url,string title) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source+":"+(string.IsNullOrWhiteSpace(url)?title.Trim().ToLowerInvariant():CanonicalUrl(url))))).ToLowerInvariant();
}
