using System.Security.Claims;
using BackendAPI.Interfaces;
using BackendAPI.Jobs;
using BackendAPI.Observability;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendAPI.Controllers;

[ApiController]
[Route("api/admin/pipeline")]
[Authorize(Policy="Admin")]
public sealed class AdminPipelineController : ControllerBase
{
    private static readonly HashSet<string> Sources=new(StringComparer.OrdinalIgnoreCase){"rss","youtube","gnews"};
    private readonly ITrendingTopicRepository _topics;
    private readonly IPipelineRuntimeHealth _health;
    private readonly IBackgroundJobClient _jobs;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminPipelineController> _logger;
    private readonly PipelineMetrics _metrics;
    public AdminPipelineController(ITrendingTopicRepository topics,IPipelineRuntimeHealth health,IBackgroundJobClient jobs,IConfiguration configuration,ILogger<AdminPipelineController> logger,PipelineMetrics metrics)
        =>(_topics,_health,_jobs,_configuration,_logger,_metrics)=(topics,health,jobs,configuration,logger,metrics);

    [HttpGet("health")]
    public async Task<IActionResult> Health()
    {
        var control=await _topics.GetControlStateAsync();
        var backlog=await _topics.GetBacklogAsync();
        _metrics.UpdateGenerationState(control.GenerationPaused,backlog);
        var providers=new[]{"gemini","openai","anthropic","groq"}.Select(name=>_health.GetGeneration(name,IsEnabled(name),IsConfigured(name))).ToArray();
        var recordedIngestion=_health.Ingestion.ToDictionary(x=>x.Provider,StringComparer.OrdinalIgnoreCase);
        var ingestionProviders=new[]{"rss","youtube","gnews"}.Select(name=>recordedIngestion.TryGetValue(name,out var value)?value:ConfiguredIngestion(name)).ToArray();
        return Ok(new { ingestion=new { providers=ingestionProviders }, generation=new { paused=control.GenerationPaused,backlog,providers } });
    }

    [HttpPost("generation/pause")]
    public Task<IActionResult> Pause()=>SetPaused(true);
    [HttpPost("generation/resume")]
    public Task<IActionResult> Resume()=>SetPaused(false);

    [HttpPost("ingestion/run")]
    public IActionResult RunIngestion([FromBody] IngestionRunRequest request)
    {
        if(!Sources.Contains(request.Source) || request.MaxTopics is <1) return BadRequest();
        var limit=Math.Clamp(_configuration.GetValue("Pipeline:MaxIngestionBatch",100),1,500);
        if(request.MaxTopics>limit) return BadRequest(new { error="batch_limit_exceeded",limit });
        var correlationId=Guid.NewGuid().ToString("N");
        var jobId=_jobs.Enqueue<IngestionJob>(job=>job.RunAsync(request.Source,request.MaxTopics));
        Audit("ingestion.run",request.MaxTopics,jobId,correlationId);
        return Accepted(new { jobId,correlationId });
    }

    [HttpPost("generation/retry")]
    public async Task<IActionResult> Retry([FromBody] RetryRunRequest request)
    {
        var limit=Math.Clamp(_configuration.GetValue("Pipeline:MaxRetryBatch",25),1,100);
        if(request.MaxTopics is <1 || request.MaxTopics>limit) return BadRequest(new { error="batch_limit_exceeded",limit });
        var requeued=await _topics.RequeueAsync(request.MaxTopics);
        var correlationId=Guid.NewGuid().ToString("N");
        var jobId=_jobs.Enqueue<PollGenerationJob>(job=>job.RunAsync(request.MaxTopics));
        Audit("generation.retry",request.MaxTopics,jobId,correlationId);
        return Accepted(new { jobId,correlationId,requeued });
    }

    private async Task<IActionResult> SetPaused(bool paused) { await _topics.SetGenerationPausedAsync(paused,Actor()); Audit(paused?"generation.pause":"generation.resume",null,null,null); return Ok(new{generationPaused=paused}); }
    private string Actor()=>User.FindFirstValue(ClaimTypes.NameIdentifier)??User.FindFirstValue("sub")??"unknown";
    private void Audit(string action,int? bound,string? jobId,string? correlationId)=>_logger.LogInformation("Pipeline operator action {Action} by {OperatorId}; bound={Bound}; job={JobId}; correlation={CorrelationId}",action,Actor(),bound,jobId,correlationId);
    private bool IsEnabled(string name)=>_configuration.GetValue($"PollGen:Providers:{name}:Enabled",false);
    private bool IsConfigured(string name)=>!string.IsNullOrWhiteSpace(_configuration[$"PollGen:Providers:{name}:ApiKey"])
        && !string.IsNullOrWhiteSpace(_configuration[$"PollGen:Providers:{name}:Model"])
        && Uri.TryCreate(_configuration[$"PollGen:Providers:{name}:Endpoint"],UriKind.Absolute,out _);
    private ProviderHealth ConfiguredIngestion(string name)
    {
        var disabled=_configuration.GetValue($"Ingestion:{name}:Disabled",false);
        var configured=name=="rss" || !string.IsNullOrWhiteSpace(_configuration[$"{(name=="youtube"?"YouTube":"GNews")}:ApiKey"]);
        return new(name,disabled?ProviderOperationalState.Disabled:configured?ProviderOperationalState.Enabled:ProviderOperationalState.Misconfigured,disabled?"disabled":configured?null:"missing_api_key",null,null,null,null);
    }
}

public sealed record IngestionRunRequest(string Source,int MaxTopics=100);
public sealed record RetryRunRequest(int MaxTopics=10);
