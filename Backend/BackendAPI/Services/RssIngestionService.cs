using BackendAPI.Interfaces;
using BackendAPI.Models;
using System.ServiceModel.Syndication;
using System.Xml;

namespace BackendAPI.Services;

public sealed record RssFeedDefinition(string Name, string Url, string Category = "General", string Language = "en", string Region = "IN", bool Enabled = true);

public class RssIngestionService(IHttpClientFactory http, ILogger<RssIngestionService> logger, IConfiguration config) : IRssIngestionService
{
    public static readonly RssFeedDefinition[] DefaultFeeds = [
        new("Google News India", "https://news.google.com/rss?hl=en-IN&gl=IN&ceid=IN:en"),
        new("BBC World", "https://feeds.bbci.co.uk/news/world/rss.xml", Region:"World"),
        new("The Hindu", "https://www.thehindu.com/news/national/?service=rss"),
        new("Times of India", "https://timesofindia.indiatimes.com/rssfeedstopstories.cms"),
        new("NDTV", "https://feeds.feedburner.com/ndtvnews-top-stories"),
        new("Hindustan Times", "https://www.hindustantimes.com/feeds/rss/india-news/rssfeed.xml"),
        new("Economic Times", "https://economictimes.indiatimes.com/rssfeedstopstories.cms", "Work"),
        new("India Today", "https://www.indiatoday.in/rss/home"),
        new("ABP English States", "https://news.abplive.com/states/feed"),
        new("ABP Hindi States", "https://www.abplive.com/states/feed", Language:"hi"),
        new("ABP Bihar", "https://www.abplive.com/news/states/bihar/feed", Language:"hi", Region:"IN-BR"),
        new("ABP Rajasthan", "https://www.abplive.com/news/states/rajasthan/feed", Language:"hi", Region:"IN-RJ"),
    ];

    public async Task<IEnumerable<TrendingTopic>> FetchAsync() => (await FetchWithResultAsync()).Topics;

    public async Task<IngestionFetchResult> FetchWithResultAsync(CancellationToken token = default)
    {
        if (config.GetValue("Ingestion:rss:Disabled",false)) return IngestionFetchResult.Disabled("rss");
        var started=DateTime.UtcNow;
        var feeds=(config.GetSection("Rss:Feeds").Get<RssFeedDefinition[]>() ?? DefaultFeeds).Where(f=>f.Enabled).Take(40).ToArray();
        if (feeds.Length==0) return IngestionFetchResult.Disabled("rss");
        using var concurrency=new SemaphoreSlim(4);
        var results=await Task.WhenAll(feeds.Select(async feed=> {
            await concurrency.WaitAsync(token);
            try {
                using var request=new HttpRequestMessage(HttpMethod.Get,feed.Url);
                request.Headers.UserAgent.ParseAdd("PopTopicIngestion/1.0");
                request.Headers.Accept.ParseAdd("application/rss+xml, application/atom+xml, application/xml");
                using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token);
                timeout.CancelAfter(TimeSpan.FromSeconds(15));
                using var response=await http.CreateClient("ingestion").SendAsync(request,timeout.Token);
                if(!response.IsSuccessStatusCode) {
                    logger.LogWarning("RSS {Source} returned HTTP {Status}",feed.Name,(int)response.StatusCode);
                    return (Topics:new List<TrendingTopic>(),Success:0,Rate:(int)response.StatusCode==429?1:0);
                }
                var xml=await response.Content.ReadAsStringAsync(timeout.Token);
                var topics=Parse(xml,feed,Math.Clamp(config.GetValue("Rss:ItemsPerFeed",5),1,20));
                return (Topics:topics,Success:1,Rate:0);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception ex) when (ex is HttpRequestException or XmlException or FormatException or ArgumentException or OperationCanceledException) {
                logger.LogWarning("RSS {Source} unavailable: {FailureType}",feed.Name,ex.GetType().Name);
                return (Topics:new List<TrendingTopic>(),Success:0,Rate:0);
            }
            finally { concurrency.Release(); }
        }));
        var topics=results.SelectMany(r=>r.Topics.Select((topic,rank)=>new{topic,rank})).OrderBy(x=>x.rank).Select(x=>x.topic).ToList();
        var success=results.Sum(r=>r.Success); var rate=results.Sum(r=>r.Rate);
        return new("rss",success>0?IngestionSourceState.Enabled:rate>0?IngestionSourceState.CoolingDown:IngestionSourceState.Failed,
            topics,feeds.Length,success,rate,DateTime.UtcNow-started,success==feeds.Length?null:success>0?"partial_feed_failure":"all_feeds_failed");
    }

    internal static List<TrendingTopic> Parse(string xml,RssFeedDefinition definition,int limit)
    {
        using var reader=XmlReader.Create(new StringReader(xml),new XmlReaderSettings { DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=2_000_000 });
        var feed=SyndicationFeed.Load(reader) ?? throw new XmlException("Not a syndication feed");
        var topics=new List<TrendingTopic>();
        foreach(var item in feed.Items.Take(limit)) {
            var title=TopicEnrichment.CleanText(item.Title?.Text);
            var link=item.Links.FirstOrDefault(l=>l.RelationshipType=="alternate")?.Uri?.ToString() ?? item.Links.FirstOrDefault()?.Uri?.ToString() ?? item.Id;
            if(string.IsNullOrWhiteSpace(title) || !Uri.TryCreate(link,UriKind.Absolute,out var uri) || uri.Scheme is not ("https" or "http")) continue;
            var summary=TopicEnrichment.CleanText(item.Summary?.Text ?? (item.Content as TextSyndicationContent)?.Text ?? "");
            var published=item.PublishDate!=DateTimeOffset.MinValue?item.PublishDate:item.LastUpdatedTime;
            if(published!=DateTimeOffset.MinValue && (published.UtcDateTime<DateTime.UtcNow.AddHours(-72) || published.UtcDateTime>DateTime.UtcNow.AddMinutes(15))) continue;
            var thumbnail=item.ElementExtensions.Where(e=>e.OuterName is "thumbnail" or "content")
                .Select(e=>e.GetObject<System.Xml.Linq.XElement>().Attribute("url")?.Value).FirstOrDefault(u=>Uri.TryCreate(u,UriKind.Absolute,out var imageUri) && imageUri.Scheme=="https");
            topics.Add(new TrendingTopic { Title=title.Length>200?title[..200]:title,Summary=summary.Length>1000?summary[..1000]:summary,
                SourceType="rss",SourceUrl=link!,SourceItemId=item.Id is {Length:<=256}?item.Id:null,Publisher=definition.Name,
                ThumbnailUrl=thumbnail,
                PublishedAt=published==DateTimeOffset.MinValue?null:published.UtcDateTime,Language=definition.Language,Region=definition.Region,
                Category=TopicEnrichment.Classify(title,summary,definition.Category) });
        }
        return topics;
    }
}
