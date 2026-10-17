using System.Net.Http.Headers;
using System.Text.Json;
using BackendAPI.Models;

namespace BackendAPI.Services;

public interface IAdditionalIngestionSource
{
    string Name { get; }
    Task<IngestionFetchResult> FetchWithResultAsync(CancellationToken token = default);
}

// Limits external calls and never logs request URLs or provider response bodies (which may contain credentials).
public abstract class JsonIngestionSource(IHttpClientFactory http, IConfiguration config) : IAdditionalIngestionSource
{
    protected IConfiguration Config => config;
    public abstract string Name { get; }
    protected abstract bool Configured { get; }
    protected abstract Task<List<TrendingTopic>> FetchAsync(CancellationToken token);
    private int requests, successes, rateLimits;
    private string? error;
    public async Task<IngestionFetchResult> FetchWithResultAsync(CancellationToken token = default)
    {
        if (config.GetValue($"Ingestion:{Name}:Disabled", true)) return IngestionFetchResult.Disabled(Name);
        if (!Configured) return IngestionFetchResult.Misconfigured(Name);
        requests = successes = rateLimits = 0; error = null;
        var started = DateTime.UtcNow;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(90));
        List<TrendingTopic> topics;
        try { topics = await FetchAsync(timeout.Token); }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { topics = []; error = "timeout"; }
        catch (JsonException) { topics = []; error = "invalid_response"; }
        catch (InvalidOperationException) { topics = []; error = "invalid_response"; }
        catch (HttpRequestException) { topics = []; error = "request_failed"; }
        return new(Name, rateLimits > 0 ? IngestionSourceState.CoolingDown : error != null && topics.Count == 0 ? IngestionSourceState.Failed : IngestionSourceState.Enabled,
            topics, requests, successes, rateLimits, DateTime.UtcNow-started, error);
    }
    protected async Task<JsonDocument?> GetAsync(string url, string? bearer, CancellationToken token)
    {
        // Stop this sweep after rate limiting or denied access, including partial multi-request sweeps.
        if (error is "rate_limited" or "access_denied") return null;
        requests++;
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (bearer != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        using var response = await http.CreateClient("ingestion").SendAsync(request, token);
        if (!response.IsSuccessStatusCode)
        {
            error = (int)response.StatusCode switch { 429 => "rate_limited", 401 or 403 => "access_denied", _ => "http_error" };
            if ((int)response.StatusCode == 429) rateLimits++;
            return null;
        }
        var json = await response.Content.ReadAsStringAsync(token);
        var document = JsonDocument.Parse(json);
        if (document.RootElement.TryGetProperty("error", out _)) { document.Dispose(); error = "provider_error"; return null; }
        successes++;
        return document;
    }
    protected static string Text(JsonElement item, string name) => item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
    protected static IEnumerable<JsonElement> Items(JsonElement item, string name) => item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array ? value.EnumerateArray() : [];
    protected static DateTime? Date(string value) => DateTimeOffset.TryParse(value, out var date) ? date.UtcDateTime : null;
    protected static string Limit(string value, int max) => value.Length <= max ? value : value[..max];
}

public sealed class XIngestionService(IHttpClientFactory http, IConfiguration config) : JsonIngestionSource(http, config)
{
    public override string Name => "x";
    protected override bool Configured => !string.IsNullOrWhiteSpace(Config["X:BearerToken"]);
    protected override async Task<List<TrendingTopic>> FetchAsync(CancellationToken token)
    {
        var bearer = Config["X:BearerToken"]!;
        var woeid = Math.Max(1, Config.GetValue("X:Woeid", 23424848)); // India
        var count = Math.Clamp(Config.GetValue("X:MaxTrends", 5), 1, 10);
        using var trends = await GetAsync($"https://api.x.com/2/trends/by/woeid/{woeid}?max_trends={count}", bearer, token);
        var topics = new List<TrendingTopic>();
        if (trends == null) return topics;
        foreach (var trend in Items(trends.RootElement, "data").Take(count))
        {
            var name = Text(trend, "trend_name");
            if (string.IsNullOrWhiteSpace(name)) continue;
            // Quote external trend text so it cannot inject search operators.
            var phrase = name.Replace("\"", " ").Replace("\\", " ");
            var query = Uri.EscapeDataString($"\"{phrase}\" -is:retweet");
            using var posts = await GetAsync($"https://api.x.com/2/tweets/search/recent?query={query}&max_results=10&tweet.fields=created_at,lang", bearer, token);
            if (posts == null) continue; // A hashtag alone is insufficient grounding for a poll.
            topics.AddRange(ParsePosts(posts.RootElement, name, woeid.ToString()));
        }
        return topics.DistinctBy(t => t.SourceItemId).ToList();
    }
    internal static IEnumerable<TrendingTopic> ParsePosts(JsonElement root, string trend, string region)
    {
        foreach (var post in Items(root, "data").Take(10))
        {
            var id = Text(post, "id"); var text = TopicEnrichment.CleanText(Text(post, "text"));
            if (string.IsNullOrWhiteSpace(text) || !id.All(char.IsDigit) || id.Length == 0) continue;
            yield return new TrendingTopic { Title = Limit(text, 200), Summary = text, SourceType = "x", SourceItemId = id,
                SourceUrl = $"https://x.com/i/web/status/{id}", Publisher = "X public post", PublishedAt = Date(Text(post,"created_at")),
                Language = Text(post,"lang"), Region = region, SourceTopic = trend, Category = "General" };
        }
    }
}

public sealed class InstagramIngestionService(IHttpClientFactory http, IConfiguration config) : JsonIngestionSource(http, config)
{
    public override string Name => "instagram";
    protected override bool Configured => !string.IsNullOrWhiteSpace(Config["Instagram:AccessToken"])
        && System.Text.RegularExpressions.Regex.IsMatch(Config["Instagram:GraphVersion"] ?? "", @"^v\d+\.\d+$")
        && !string.IsNullOrWhiteSpace(Config["Instagram:UserId"]) && Config.GetSection("Instagram:Hashtags").Get<string[]>() is { Length: > 0 and <= 10 };
    protected override async Task<List<TrendingTopic>> FetchAsync(CancellationToken token)
    {
        var root = $"https://graph.facebook.com/{Config["Instagram:GraphVersion"]}";
        var user = Uri.EscapeDataString(Config["Instagram:UserId"]!); var bearer = Config["Instagram:AccessToken"]!;
        var topics = new List<TrendingTopic>();
        foreach (var hashtag in Config.GetSection("Instagram:Hashtags").Get<string[]>()!.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var name = hashtag.Trim().TrimStart('#'); if (name.Length == 0) continue;
            using var search = await GetAsync($"{root}/ig_hashtag_search?user_id={user}&q={Uri.EscapeDataString(name)}", bearer, token);
            if (search == null) continue;
            var id = Items(search.RootElement,"data").Select(x => Text(x,"id")).FirstOrDefault();
            if (string.IsNullOrEmpty(id) || !id.All(char.IsDigit)) continue;
            using var media = await GetAsync($"{root}/{id}/top_media?user_id={user}&fields=id,caption,permalink,timestamp&limit=10", bearer, token);
            if (media != null) topics.AddRange(ParseMedia(media.RootElement, name));
        }
        return topics.DistinctBy(t => t.SourceItemId).ToList();
    }
    internal static IEnumerable<TrendingTopic> ParseMedia(JsonElement root, string hashtag)
    {
        foreach (var item in Items(root,"data").Take(10))
        {
            var caption = TopicEnrichment.CleanText(Text(item,"caption")); var id = Text(item,"id"); var url = Text(item,"permalink");
            if (caption.Length == 0 || id.Length == 0 || !Uri.TryCreate(url,UriKind.Absolute,out var uri) || uri.Scheme != "https") continue;
            yield return new TrendingTopic { Title = Limit(caption,200), Summary = Limit(caption,3000), SourceType = "instagram", SourceItemId = id,
                SourceUrl = url, Publisher = "Instagram public hashtag media", PublishedAt = Date(Text(item,"timestamp")), SourceTopic = hashtag, Category = "General" };
        }
    }
}

public sealed class EventsIngestionService(IHttpClientFactory http, IConfiguration config) : JsonIngestionSource(http, config)
{
    public override string Name => "events";
    protected override bool Configured => !string.IsNullOrWhiteSpace(Config["Events:TicketmasterApiKey"]);
    protected override async Task<List<TrendingTopic>> FetchAsync(CancellationToken token)
    {
        var country = Config["Events:CountryCode"] ?? "IN";
        var end = DateTime.UtcNow.AddDays(Math.Clamp(Config.GetValue("Events:HorizonDays",30),1,90)).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var start = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        using var data = await GetAsync($"https://app.ticketmaster.com/discovery/v2/events.json?countryCode={Uri.EscapeDataString(country)}&size=50&sort=date,asc&startDateTime={start}&endDateTime={end}&apikey={Uri.EscapeDataString(Config["Events:TicketmasterApiKey"]!)}", null, token);
        return data == null ? [] : ParseEvents(data.RootElement,country).ToList();
    }
    internal static IEnumerable<TrendingTopic> ParseEvents(JsonElement root, string country)
    {
        if (!root.TryGetProperty("_embedded",out var embedded)) yield break;
        foreach (var item in Items(embedded,"events").Take(50))
        {
            var id=Text(item,"id"); var title=TopicEnrichment.CleanText(Text(item,"name")); var url=Text(item,"url");
            if (id.Length==0 || title.Length==0 || !Uri.TryCreate(url,UriKind.Absolute,out _)) continue;
            DateTime? starts = null;
            if (item.TryGetProperty("dates",out var dates)) {
                if(dates.TryGetProperty("status",out var status) && Text(status,"code")=="cancelled") continue;
                if(dates.TryGetProperty("start",out var start)) starts=Date(Text(start,"dateTime"));
            }
            var details=Text(item,"info")+" "+Text(item,"pleaseNote");
            if(item.TryGetProperty("_embedded",out var links)) {
                var venue=Items(links,"venues").FirstOrDefault();
                if(venue.ValueKind==JsonValueKind.Object) {
                    details += " Venue: "+Text(venue,"name");
                    if(venue.TryGetProperty("city",out var city)) details += ", "+Text(city,"name");
                }
            }
            yield return new TrendingTopic { Title=Limit(title,200), Summary=Limit(TopicEnrichment.CleanText(details),3000),
                SourceType="events", SourceItemId=id,SourceUrl=url,Publisher="Ticketmaster",Region=country,Language=Text(item,"locale"),EventStartUtc=starts,Category="Culture" };
            // Event start is not a publication date; PublishedAt intentionally stays null.
        }
    }
}
