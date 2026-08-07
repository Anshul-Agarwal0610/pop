using BackendAPI.Interfaces;
using BackendAPI.Models;
using System.Text.Json;

namespace BackendAPI.Services
{
    /// <summary>
    /// US-04 — Fetches trending YouTube videos (India) via YouTube Data API v3.
    /// Free tier: 10,000 units/day; one videos.list call costs 1 unit.
    /// Config key: YouTube:ApiKey  (set in appsettings.json or Azure App Settings)
    /// </summary>
    public class YouTubeIngestionService : IYouTubeIngestionService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<YouTubeIngestionService> _logger;

        // YouTube Data API v3 — most popular videos in India (videoCategoryId=25 = News & Politics)
        private const string BaseUrl =
            "https://www.googleapis.com/youtube/v3/videos" +
            "?part=snippet,statistics" +
            "&chart=mostPopular" +
            "&regionCode=IN" +
            "&videoCategoryId=25" +
            "&maxResults=10" +
            "&key={0}";

        public YouTubeIngestionService(
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            ILogger<YouTubeIngestionService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _config            = config;
            _logger            = logger;
        }

        public async Task<IEnumerable<TrendingTopic>> FetchAsync()
            => (await FetchWithResultAsync()).Topics;

        public async Task<IngestionFetchResult> FetchWithResultAsync(CancellationToken token = default)
        {
            var started=DateTime.UtcNow;
            var apiKey = _config["YouTube:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("YouTube:ApiKey not configured — skipping YouTube ingestion");
                return IngestionFetchResult.Misconfigured("youtube");
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var url    = string.Format(BaseUrl, apiKey);

                using var response = await client.GetAsync(url, token);
                if ((int)response.StatusCode==429) return new("youtube",IngestionSourceState.CoolingDown,[],1,0,1,DateTime.UtcNow-started,"rate_limited");
                if (!response.IsSuccessStatusCode) return new("youtube",IngestionSourceState.Failed,[],1,0,0,DateTime.UtcNow-started,"http_error");

                var json = await response.Content.ReadAsStringAsync();
                return new("youtube",IngestionSourceState.Enabled,ParseResponse(json),1,1,0,DateTime.UtcNow-started);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "YouTube ingestion failed");
                return new("youtube",IngestionSourceState.Failed,[],1,0,0,DateTime.UtcNow-started,ex is OperationCanceledException?"timeout":"request_failed");
            }
        }

        private static List<TrendingTopic> ParseResponse(string json)
        {
            var topics = new List<TrendingTopic>();

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("items", out var items)) return topics;

            foreach (var item in items.EnumerateArray())
            {
                var videoId  = item.GetProperty("id").GetString() ?? "";
                var snippet  = item.GetProperty("snippet");
                var title    = TopicEnrichment.CleanText(snippet.GetProperty("title").GetString());
                var desc     = TopicEnrichment.CleanText(snippet.TryGetProperty("description", out var d) ? d.GetString() : "");
                var publisher = snippet.TryGetProperty("channelTitle", out var channel) ? channel.GetString() : null;
                DateTime? publishedAt = snippet.TryGetProperty("publishedAt", out var published) && DateTime.TryParse(published.GetString(), out var date) ? date.ToUniversalTime() : null;

                if (desc.Length > 400) desc = desc[..400];

                // Thumbnail: prefer maxres, then high, then default
                string? thumbnail = null;
                if (snippet.TryGetProperty("thumbnails", out var thumbs))
                {
                    foreach (var quality in new[] { "maxres", "high", "medium", "default" })
                    {
                        if (thumbs.TryGetProperty(quality, out var t)
                            && t.TryGetProperty("url", out var tu))
                        {
                            thumbnail = tu.GetString();
                            break;
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(title)) continue;

                topics.Add(new TrendingTopic
                {
                    Title        = title,
                    Summary      = desc,
                    SourceType   = "youtube",
                    SourceUrl    = $"https://www.youtube.com/watch?v={videoId}",
                    ThumbnailUrl = thumbnail,
                    Publisher    = publisher,
                    PublishedAt  = publishedAt,
                    Category     = TopicEnrichment.Classify(title, desc)
                });
            }

            return topics;
        }
    }
}
