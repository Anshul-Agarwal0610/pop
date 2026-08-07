using System.Diagnostics.Metrics;
using BackendAPI.Observability;
using Xunit;

namespace BackendAPI.Tests;

public sealed class PipelineMetricsTests
{
    [Fact]
    public void Emits_all_required_lifecycle_metrics_with_bounded_tags()
    {
        var seen=new List<(string Name,long Value,Dictionary<string,object?> Tags)>();
        using var listener=new MeterListener();
        listener.InstrumentPublished=(instrument,l)=> { if(instrument.Meter.Name==PipelineMetrics.MeterName) l.EnableMeasurementEvents(instrument); };
        listener.SetMeasurementEventCallback<long>((instrument,value,tags,state)=> { var copy=new Dictionary<string,object?>(); foreach(var tag in tags) copy[tag.Key]=tag.Value; seen.Add((instrument.Name,value,copy)); });
        listener.Start();
        using var metrics=new PipelineMetrics();
        foreach(var stage in new[]{"fetched","deduplicated","queued"}) metrics.Ingestion(stage,1,"rss");
        foreach(var stage in new[]{"converted","published","retried","rejected","review"}) metrics.Generation(stage);
        metrics.LlmRequest("openai","success"); metrics.LlmRequest("openai","rate_limited"); metrics.Failover("openai","anthropic"); metrics.Tokens("openai","input",3); metrics.Tokens("openai","output",4);

        Assert.Equal(new[]{"converted","published","rejected","retried","review"},seen.Where(x=>x.Name=="pollify.generation.topics").Select(x=>(string)x.Tags["stage"]!).Order().ToArray());
        Assert.Equal(new[]{"deduplicated","fetched","queued"},seen.Where(x=>x.Name=="pollify.ingestion.topics").Select(x=>(string)x.Tags["stage"]!).Order().ToArray());
        var allowed=new HashSet<string>{"stage","source","provider","outcome","from_provider","to_provider","type"};
        Assert.All(seen.SelectMany(x=>x.Tags.Keys),key=>Assert.Contains(key,allowed));
        Assert.DoesNotContain(seen.SelectMany(x=>x.Tags.Keys),key=>key.Contains("correlation",StringComparison.OrdinalIgnoreCase)||key.Contains("url",StringComparison.OrdinalIgnoreCase)||key.Contains("error",StringComparison.OrdinalIgnoreCase));
    }
}
