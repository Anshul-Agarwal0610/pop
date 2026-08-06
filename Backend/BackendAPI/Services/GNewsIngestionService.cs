using BackendAPI.Interfaces;
using BackendAPI.Models;
using System.Text.Json;

namespace BackendAPI.Services
{
    /// <summary>
    /// US-05 — Fetches top Indian news headlines via GNews API (free tier: 100 req/day).
    /// Config key: GNews:ApiKey  (set in appsettings.json or Azure App Settings)
    /// Docs: https://gnews.io/docs/v4
    /// </summary>
    public class GNewsIngestionService : IGNewsIngestionService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<GNewsIngestionService> _logger;

        // Fetch top 10 headlines in English for India
        private const string BaseUrl =
            "https://gnews.io/api/v4/top-headlines" +
            "?country=in&lang=en&max=10&token={0}";

        public GNewsIngestionService(
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            ILogger<GNewsIngestionService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _config            = config;
            _logger            = logger;
        }

        public async Task<IEnumerable<TrendingTopic>> FetchAsync()
        {
            var apiKey = _config["GNews:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("GNews:ApiKey not configured — skipping GNews ingestion");
                return Enumerable.Empty<TrendingTopic>();
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var url    = string.Format(BaseUrl, apiKey);

                using var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return ParseResponse(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GNews ingestion failed");
                return Enumerable.Empty<TrendingTopic>();
            }
        }

        private static List<TrendingTopic> ParseResponse(string json)
        {
            var topics = new List<TrendingTopic>();

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("articles", out var articles)) return topics;

            foreach (var article in articles.EnumerateArray())
            {
                var title = article.TryGetProperty("title", out var t) ? t.GetString()?.Trim() ?? "" : "";
                var desc  = article.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
                var url   = article.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "";
                var img   = article.TryGetProperty("image", out var i) ? i.GetString() : null;
                var publisher = article.TryGetProperty("source", out var source) && source.TryGetProperty("name", out var name) ? name.GetString() : null;
                DateTime? publishedAt = article.TryGetProperty("publishedAt", out var published) && DateTime.TryParse(published.GetString(), out var date) ? date.ToUniversalTime() : null;

                if (desc.Length > 500) desc = desc[..500];
                if (string.IsNullOrWhiteSpace(title)) continue;

                topics.Add(new TrendingTopic
                {
                    Title        = title,
                    Summary      = desc,
                    SourceType   = "gnews",
                    SourceUrl    = url,
                    ThumbnailUrl = img,
                    Publisher    = publisher,
                    PublishedAt  = publishedAt,
                    Category     = "General"
                });
            }

            return topics;
        }
    }
}
