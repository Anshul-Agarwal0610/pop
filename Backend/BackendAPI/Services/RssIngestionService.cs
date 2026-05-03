using BackendAPI.Interfaces;
using BackendAPI.Models;
using System.ServiceModel.Syndication;
using System.Xml;

namespace BackendAPI.Services
{
    /// <summary>
    /// US-03 — Fetches trending topics from RSS feeds.
    ///
    /// All feeds are fetched via HttpClient with a browser-like User-Agent so sites
    /// that block default .NET XML requests don't return 403.
    ///
    /// Reuters was removed — they shut down all public RSS feeds in 2020.
    /// Indian Express was removed — behind Cloudflare bot protection (JS challenge).
    /// Replaced both with Hindustan Times, Economic Times, and The Wire.
    /// </summary>
    public class RssIngestionService : IRssIngestionService
    {
        private readonly IHttpClientFactory _http;
        private readonly ILogger<RssIngestionService> _logger;

        // Mimics a real browser so sites don't return 403
        private const string UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
            "AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/124.0.0.0 Safari/537.36";

        // (Name, Feed URL, Category)
        // NOTE: Sites behind Cloudflare JS-challenge (Indian Express) or with dead DNS
        // (Reuters) have been removed — HttpClient cannot bypass JS bot challenges.
        private static readonly (string Name, string Url, string Category)[] Feeds =
        {
            ("Google News India",    "https://news.google.com/rss?hl=en-IN&gl=IN&ceid=IN:en",           "General"),
            ("The Hindu",            "https://www.thehindu.com/news/national/?service=rss",              "India"),
            ("Times of India",       "https://timesofindia.indiatimes.com/rssfeedstopstories.cms",       "India"),
            ("NDTV Top Stories",     "https://feeds.feedburner.com/ndtvnews-top-stories",                "India"),
            ("BBC World",            "https://feeds.bbci.co.uk/news/world/rss.xml",                     "World"),
            ("Hindustan Times",      "https://www.hindustantimes.com/feeds/rss/india-news/rssfeed.xml",  "India"),
            ("Economic Times",       "https://economictimes.indiatimes.com/rssfeedstopstories.cms",      "Business"),
            ("The Wire",             "https://thewire.in/feed",                                          "India"),
        };

        public RssIngestionService(IHttpClientFactory http, ILogger<RssIngestionService> logger)
        {
            _http   = http;
            _logger = logger;
        }

        public async Task<IEnumerable<TrendingTopic>> FetchAsync()
        {
            var results = new List<TrendingTopic>();

            foreach (var (name, url, category) in Feeds)
            {
                try
                {
                    var items = await FetchFeedAsync(name, url, category);
                    results.AddRange(items);
                    _logger.LogInformation("RSS [{Source}]: fetched {Count} items", name, items.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "RSS [{Source}]: failed to fetch", name);
                }
            }

            return results;
        }

        private async Task<List<TrendingTopic>> FetchFeedAsync(
            string sourceName, string url, string category)
        {
            // Download XML with a real browser User-Agent to avoid 403 responses
            var client = _http.CreateClient();
            client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            client.DefaultRequestHeaders.Add("Accept", "application/rss+xml, application/xml, text/xml, */*");
            client.Timeout = TimeSpan.FromSeconds(15);

            using var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();

            return await Task.Run(() =>
            {
                var topics = new List<TrendingTopic>();

                var xmlSettings = new XmlReaderSettings
                {
                    DtdProcessing             = DtdProcessing.Ignore,
                    MaxCharactersFromEntities = 1024,
                    Async                     = false
                };

                using var reader = XmlReader.Create(stream, xmlSettings);
                var feed = SyndicationFeed.Load(reader);

                foreach (var item in feed.Items.Take(10))
                {
                    var link    = item.Links.FirstOrDefault()?.Uri?.ToString() ?? "";
                    var summary = item.Summary?.Text
                        ?? (item.Content as TextSyndicationContent)?.Text
                        ?? "";

                    // Strip HTML tags from summaries
                    summary = System.Text.RegularExpressions.Regex
                        .Replace(summary, "<[^>]+>", "")
                        .Trim();
                    if (summary.Length > 500) summary = summary[..500];

                    var title = item.Title?.Text?.Trim() ?? "";
                    if (string.IsNullOrWhiteSpace(title)) continue;

                    // Try to extract thumbnail from media:thumbnail or media:content extensions
                    string? thumbnail = null;
                    foreach (var ext in item.ElementExtensions)
                    {
                        if (ext.OuterName.Equals("thumbnail", StringComparison.OrdinalIgnoreCase)
                            || ext.OuterName.Equals("content",   StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                var xElem = ext.GetObject<System.Xml.Linq.XElement>();
                                thumbnail = xElem.Attribute("url")?.Value;
                                if (thumbnail != null) break;
                            }
                            catch { /* ignore malformed extensions */ }
                        }
                    }

                    topics.Add(new TrendingTopic
                    {
                        Title        = title,
                        Summary      = summary,
                        SourceType   = "rss",
                        SourceUrl    = link,
                        ThumbnailUrl = thumbnail,
                        Category     = category
                    });
                }

                return topics;
            });
        }
    }
}
