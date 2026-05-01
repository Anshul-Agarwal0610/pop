using BackendAPI.Interfaces;
using BackendAPI.Models;
using System.ServiceModel.Syndication;
using System.Xml;

namespace BackendAPI.Services
{
    /// <summary>
    /// US-03 — Fetches trending topics from RSS feeds.
    /// Sources: Google News (India), The Hindu, Times of India, NDTV, BBC, Reuters, Indian Express.
    /// Uses only built-in .NET System.ServiceModel.Syndication — no extra NuGet required.
    /// </summary>
    public class RssIngestionService : IRssIngestionService
    {
        private readonly ILogger<RssIngestionService> _logger;

        // Source → (feed URL, category)
        private static readonly (string Name, string Url, string Category)[] Feeds =
        {
            ("Google News India",   "https://news.google.com/rss?hl=en-IN&gl=IN&ceid=IN:en",             "General"),
            ("The Hindu",           "https://www.thehindu.com/news/national/?service=rss",               "India"),
            ("Times of India",      "https://timesofindia.indiatimes.com/rssfeedstopstories.cms",        "India"),
            ("NDTV Top Stories",    "https://feeds.feedburner.com/ndtvnews-top-stories",                 "India"),
            ("BBC World",           "https://feeds.bbci.co.uk/news/world/rss.xml",                      "World"),
            ("Reuters Top News",    "https://feeds.reuters.com/reuters/topNews",                         "World"),
            ("Indian Express",      "https://indianexpress.com/feed/",                                   "India"),
        };

        public RssIngestionService(ILogger<RssIngestionService> logger)
        {
            _logger = logger;
        }

        public async Task<IEnumerable<TrendingTopic>> FetchAsync()
        {
            var results = new List<TrendingTopic>();

            foreach (var (name, url, category) in Feeds)
            {
                try
                {
                    var items = await FetchFeedAsync(url, name, category);
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

        private static Task<List<TrendingTopic>> FetchFeedAsync(
            string url, string sourceName, string category)
        {
            return Task.Run(() =>
            {
                var topics = new List<TrendingTopic>();

                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Ignore,
                    MaxCharactersFromEntities = 1024
                };

                using var reader = XmlReader.Create(url, settings);
                var feed = SyndicationFeed.Load(reader);

                foreach (var item in feed.Items.Take(10))   // max 10 per feed
                {
                    var link = item.Links.FirstOrDefault()?.Uri?.ToString() ?? "";
                    var summary = item.Summary?.Text
                        ?? (item.Content as TextSyndicationContent)?.Text
                        ?? "";

                    // Strip basic HTML tags from summaries
                    summary = System.Text.RegularExpressions.Regex
                        .Replace(summary, "<[^>]+>", "")
                        .Trim();
                    if (summary.Length > 500) summary = summary[..500];

                    var title = item.Title?.Text?.Trim() ?? "";
                    if (string.IsNullOrWhiteSpace(title)) continue;

                    // Try to extract a thumbnail from media:thumbnail or enclosure
                    string? thumbnail = null;
                    if (item.ElementExtensions != null)
                    {
                        foreach (var ext in item.ElementExtensions)
                        {
                            if (ext.OuterName.Equals("thumbnail", StringComparison.OrdinalIgnoreCase)
                                || ext.OuterName.Equals("content", StringComparison.OrdinalIgnoreCase))
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
