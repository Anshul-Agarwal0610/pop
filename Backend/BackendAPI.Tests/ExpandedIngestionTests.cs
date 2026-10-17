using System.Net;
using System.Text.Json;
using BackendAPI.Models;
using BackendAPI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BackendAPI.Tests;

public class ExpandedIngestionTests
{
    [Fact]
    public async Task X_fetches_post_context_and_stops_after_rate_limit()
    {
        var http=new FakeHttp(
            (HttpStatusCode.OK,"""{"data":[{"trend_name":"#Transit"},{"trend_name":"Other"}]}"""),
            (HttpStatusCode.TooManyRequests,"{}"));
        var result=await new XIngestionService(http,Config(("Ingestion:x:Disabled","false"),("X:BearerToken","secret"))).FetchWithResultAsync();
        Assert.Equal(2,http.Requests.Count);
        Assert.Equal(IngestionSourceState.CoolingDown,result.State);
        Assert.Empty(result.Topics);
        Assert.Contains("tweets/search/recent",http.Requests[1].Url);
        Assert.All(http.Requests,r=> { Assert.DoesNotContain("secret",r.Url); Assert.Equal("Bearer secret",r.Auth); });
    }

    [Fact]
    public async Task Instagram_uses_configured_hashtag_and_skips_captionless_posts()
    {
        var http=new FakeHttp((HttpStatusCode.OK,"""{"data":[{"id":"123"}]}"""),
            (HttpStatusCode.OK,"""{"data":[{"id":"456","caption":"A city proposes electric buses","permalink":"https://www.instagram.com/p/test/","timestamp":"2026-09-27T10:00:00Z"},{"id":"789","permalink":"https://www.instagram.com/p/empty/"}]}"""));
        var result=await new InstagramIngestionService(http,Config(("Ingestion:instagram:Disabled","false"),("Instagram:AccessToken","secret"),
            ("Instagram:UserId","99"),("Instagram:GraphVersion","v25.0"),("Instagram:Hashtags:0","transit"))).FetchWithResultAsync();
        var topic=Assert.Single(result.Topics);
        Assert.Equal("456",topic.SourceItemId);
        Assert.Equal("transit",topic.SourceTopic);
        Assert.Equal(2,result.RequestCount);
        Assert.Contains("/123/top_media",http.Requests[1].Url);
        Assert.All(http.Requests,r=>Assert.DoesNotContain("secret",r.Url));
    }

    [Fact]
    public async Task Missing_credentials_and_disabled_sources_do_not_call_network()
    {
        var http=new FakeHttp();
        Assert.Equal(IngestionSourceState.Disabled,(await new XIngestionService(http,Config()).FetchWithResultAsync()).State);
        Assert.Equal(IngestionSourceState.Misconfigured,(await new EventsIngestionService(http,Config(("Ingestion:events:Disabled","false"))).FetchWithResultAsync()).State);
        Assert.Equal(IngestionSourceState.Misconfigured,(await new InstagramIngestionService(http,Config(("Ingestion:instagram:Disabled","false"))).FetchWithResultAsync()).State);
        Assert.Empty(http.Requests);
    }

    [Fact]
    public void Event_date_is_separate_from_publication_date_and_cancelled_events_are_skipped()
    {
        using var data=JsonDocument.Parse("""{"_embedded":{"events":[{"id":"ev1","name":"City festival","url":"https://ticketmaster.com/ev1","dates":{"start":{"dateTime":"2026-10-10T10:00:00Z"},"status":{"code":"onsale"}}},{"id":"ev2","name":"Cancelled festival","url":"https://ticketmaster.com/ev2","dates":{"status":{"code":"cancelled"}}}]}}""");
        var topic=Assert.Single(EventsIngestionService.ParseEvents(data.RootElement,"IN"));
        Assert.Null(topic.PublishedAt);
        Assert.NotNull(topic.EventStartUtc);
        Assert.Equal("IN",topic.Region);
    }

    [Fact]
    public async Task Empty_event_inventory_is_success_not_a_provider_failure()
    {
        var http=new FakeHttp((HttpStatusCode.OK,"{\"page\":{\"totalElements\":0}}"));
        var result=await new EventsIngestionService(http,Config(("Ingestion:events:Disabled","false"),("Events:TicketmasterApiKey","test"))).FetchWithResultAsync();
        Assert.Equal(1,result.SuccessCount); Assert.Empty(result.Topics); Assert.Null(result.ErrorCode);
        Assert.Contains("countryCode=IN",http.Requests[0].Url);
    }

    [Fact]
    public void Regional_rss_preserves_unicode_and_metadata_and_removes_html()
    {
        const string xml="""<rss version="2.0"><channel><title>News</title><link>https://example.test</link><description>Feed</description><item><title>नगर परिषद की नई योजना</title><link>https://example.test/story</link><description>&lt;p&gt;नई बस सेवा&lt;/p&gt;</description></item></channel></rss>""";
        var topic=Assert.Single(RssIngestionService.Parse(xml,new("Regional","https://example.test/rss",Language:"hi",Region:"IN-BR"),5));
        Assert.Equal("नई बस सेवा",topic.Summary); Assert.Equal("hi",topic.Language); Assert.Equal("IN-BR",topic.Region);
        Assert.Equal("नगर परिषद की नई योजना",topic.Title);
    }

    [Fact]
    public void Regional_rss_removes_encoded_styled_paragraph_markup()
    {
        const string xml="""<rss version="2.0"><channel><title>News</title><link>https://example.test</link><description>Feed</description><item><title>हिंदी समाचार</title><link>https://example.test/hindi</link><description>&amp;lt;p style=&amp;quot;text-align:justify;&amp;quot;&amp;gt;यह साफ विवरण है&amp;lt;/p&amp;gt;</description></item></channel></rss>""";
        var topic=Assert.Single(RssIngestionService.Parse(xml,new("Hindi","https://example.test/rss",Language:"hi"),5));
        Assert.Equal("यह साफ विवरण है",topic.Summary);
        Assert.DoesNotContain("<p",topic.Summary,StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Text_cleanup_removes_a_tag_fragment_cut_off_by_legacy_truncation()
        => Assert.Equal("साफ विवरण", TopicEnrichment.CleanText("साफ विवरण <str"));

    [Fact]
    public void Rss_rejects_external_entities_and_stale_items()
    {
        Assert.Throws<System.Xml.XmlException>(()=>RssIngestionService.Parse("<!DOCTYPE rss [<!ENTITY x SYSTEM 'file:///etc/passwd'>]><rss/>",new("x","https://example.test"),5));
        Assert.Empty(RssIngestionService.Parse("""<rss version="2.0"><channel><title>News</title><link>https://example.test</link><description>Feed</description><item><title>Old item</title><link>https://example.test/old</link><pubDate>Mon, 01 Jan 2024 12:00:00 GMT</pubDate></item></channel></rss>""",new("x","https://example.test"),5));
    }

    [Fact]
    public async Task X_success_preserves_post_identity_language_and_trend()
    {
        var http=new FakeHttp((HttpStatusCode.OK,"""{"data":[{"trend_name":"#Metro"}]}"""),
            (HttpStatusCode.OK,"""{"data":[{"id":"1234","text":"Council proposes a new metro line.","lang":"en","created_at":"2026-09-27T10:00:00Z"}]}"""));
        var result=await new XIngestionService(http,Config(("Ingestion:x:Disabled","false"),("X:BearerToken","test"))).FetchWithResultAsync();
        var topic=Assert.Single(result.Topics);
        Assert.Equal("https://x.com/i/web/status/1234",topic.SourceUrl);
        Assert.Equal("#Metro",topic.SourceTopic); Assert.Equal("en",topic.Language); Assert.NotNull(topic.PublishedAt);
    }

    [Fact]
    public async Task Rss_partial_failure_keeps_successful_feed_and_reports_actual_counts()
    {
        var http=new FakeHttp((HttpStatusCode.OK,"""<rss version="2.0"><channel><title>News</title><link>https://example.test</link><description>Feed</description><item><title>Valid story</title><link>https://example.test/story</link></item></channel></rss>"""),
            (HttpStatusCode.ServiceUnavailable,"unavailable"));
        var config=Config(("Rss:Feeds:0:Name","good"),("Rss:Feeds:0:Url","https://example.test/good"),
            ("Rss:Feeds:1:Name","bad"),("Rss:Feeds:1:Url","https://example.test/bad"));
        var result=await new RssIngestionService(http,NullLogger<RssIngestionService>.Instance,config).FetchWithResultAsync();
        Assert.Single(result.Topics); Assert.Equal(1,result.SuccessCount); Assert.Equal(2,result.RequestCount);
        Assert.Equal("partial_feed_failure",result.ErrorCode);
    }

    [Fact]
    public async Task Invalid_api_response_is_reported_without_response_body()
    {
        var result=await new XIngestionService(new FakeHttp((HttpStatusCode.OK,"not-json secret")),Config(("Ingestion:x:Disabled","false"),("X:BearerToken","test"))).FetchWithResultAsync();
        Assert.Equal(IngestionSourceState.Failed,result.State); Assert.Equal("invalid_response",result.ErrorCode);
    }

    private static IConfiguration Config(params (string Key,string Value)[] entries) => new ConfigurationBuilder().AddInMemoryCollection(entries.Select(e=>new KeyValuePair<string,string?>(e.Key,e.Value))).Build();
    private sealed class FakeHttp(params (HttpStatusCode Status,string Body)[] replies) : HttpMessageHandler,IHttpClientFactory
    {
        public List<(string Url,string? Auth)> Requests {get;}=[];
        public HttpClient CreateClient(string name)=>new(this,false);
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var response=replies[Requests.Count]; Requests.Add((request.RequestUri!.ToString(),request.Headers.Authorization?.ToString()));
            return Task.FromResult(new HttpResponseMessage(response.Status){Content=new StringContent(response.Body)});
        }
    }
}
