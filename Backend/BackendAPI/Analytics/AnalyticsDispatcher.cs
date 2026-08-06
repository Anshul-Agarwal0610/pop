using BackendAPI.Data; using Dapper; using Microsoft.Extensions.Options; using System.Net.Http.Json;
namespace BackendAPI.Analytics;
public sealed class AnalyticsDispatcher(DapperContext context, IHttpClientFactory clients, IOptions<AnalyticsOptions> options, ILogger<AnalyticsDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested) { try { await Dispatch(stoppingToken); } catch (Exception ex) { logger.LogWarning(ex, "Analytics dispatch failed; product transactions are unaffected"); } await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken); }
    }
    private async Task Dispatch(CancellationToken ct)
    {
        var config = options.Value; if (!config.Enabled || string.IsNullOrWhiteSpace(config.CaptureUrl)) return;
        using var db = context.CreateConnection();
        var events = await db.QueryAsync<AnalyticsEvent>(@"SELECT TOP 100 EventId, EventName AS Name, ActorKey, PayloadJson, OccurredAt, SemanticKey, SchemaVersion FROM AnalyticsEventOutbox WHERE DeliveredAt IS NULL AND ExpiresAt>GETUTCDATE() AND (NextAttemptAt IS NULL OR NextAttemptAt<=GETUTCDATE()) ORDER BY OccurredAt");
        foreach (var e in events) { try { using var response = await clients.CreateClient().PostAsJsonAsync(config.CaptureUrl, new { api_key=config.ApiKey, @event=e.Name, distinct_id=e.ActorKey, properties=System.Text.Json.JsonSerializer.Deserialize<object>(e.PayloadJson), uuid=e.EventId }, ct); response.EnsureSuccessStatusCode(); await db.ExecuteAsync("UPDATE AnalyticsEventOutbox SET DeliveredAt=GETUTCDATE() WHERE EventId=@EventId AND DeliveredAt IS NULL", e); } catch { await db.ExecuteAsync("UPDATE AnalyticsEventOutbox SET AttemptCount=AttemptCount+1, NextAttemptAt=DATEADD(second,POWER(2,CASE WHEN AttemptCount>8 THEN 8 ELSE AttemptCount END),GETUTCDATE()) WHERE EventId=@EventId", e); } }
    }
}
