using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace BackendAPI.Services;

public sealed class PollMediaService(IConfiguration configuration)
{
    private const string Container = "poll-media";
    private readonly string? _accountUrl = configuration["PollMedia:AccountUrl"];

    private BlobContainerClient ContainerClient => new(new Uri($"{_accountUrl!.TrimEnd('/')}/{Container}"), new DefaultAzureCredential());

    public bool Enabled => Uri.TryCreate(_accountUrl, UriKind.Absolute, out _);

    public async Task<(string Url, string MediaType)> UploadAsync(IFormFile file, CancellationToken token)
    {
        if (!Enabled) throw new InvalidOperationException("Poll media storage is not configured.");
        var type = Normalize(file);
        var name = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
        var blob = ContainerClient.GetBlobClient(name);
        await using var input = file.OpenReadStream();
        await blob.UploadAsync(input, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType } }, token);
        return ($"/api/poll-media/{name}", type);
    }

    public async Task<(Stream Content, string ContentType)?> OpenAsync(string name, CancellationToken token)
    {
        if (!Enabled || !System.Text.RegularExpressions.Regex.IsMatch(name, "^[a-f0-9]{32}\\.(jpg|jpeg|png|webp|gif|mp4|webm|mov)$")) return null;
        try { var result = await ContainerClient.GetBlobClient(name).DownloadStreamingAsync(cancellationToken: token); return (result.Value.Content, result.Value.Details.ContentType ?? "application/octet-stream"); }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404) { return null; }
    }

    private static string Normalize(IFormFile file)
    {
        var allowed = new Dictionary<string, (string Type, long Max)>(StringComparer.OrdinalIgnoreCase) {
            ["image/jpeg"] = ("image", 10_000_000), ["image/png"] = ("image", 10_000_000), ["image/webp"] = ("image", 10_000_000), ["image/gif"] = ("image", 10_000_000),
            ["video/mp4"] = ("video", 75_000_000), ["video/webm"] = ("video", 75_000_000), ["video/quicktime"] = ("video", 75_000_000)
        };
        if (!allowed.TryGetValue(file.ContentType, out var rule) || file.Length == 0 || file.Length > rule.Max)
            throw new ArgumentException("Use an image up to 10 MB or MP4/WebM/MOV video up to 75 MB.");
        return rule.Type;
    }
}
