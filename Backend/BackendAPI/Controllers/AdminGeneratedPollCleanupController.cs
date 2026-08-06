using BackendAPI.Interfaces;
using BackendAPI.Jobs;
using BackendAPI.Models;
using BackendAPI.Services;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendAPI.Controllers;

[ApiController]
[Route("api/admin/generated-poll-cleanup")]
[Authorize(Policy = "Admin")]
public sealed class AdminGeneratedPollCleanupController(
    IGeneratedPollCleanupService service,
    IBackgroundJobClient jobs,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("dry-run")]
    public async Task<ActionResult<GeneratedPollCleanupReport>> DryRun(GeneratedPollCleanupRequest request)
    {
        try { return Ok(await service.DryRunAsync(request.FromPollId, request.ToPollId, request.MaxRecords)); }
        catch (ArgumentOutOfRangeException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("execute")]
    public ActionResult Execute(GeneratedPollCleanupRequest request)
    {
        try { GeneratedPollCleanupService.ValidateBounds(request.FromPollId, request.ToPollId, request.MaxRecords); }
        catch (ArgumentOutOfRangeException ex) { return BadRequest(new { error = ex.Message }); }
        if (request.DryRun) return BadRequest(new { error = "Set dryRun=false for execution. Dry-run is the default." });
        if (!configuration.GetValue<bool>("GeneratedPollCleanup:ExecutionEnabled"))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Cleanup execution is disabled by configuration." });
        var expected = configuration["GeneratedPollCleanup:Confirmation"];
        if (string.IsNullOrWhiteSpace(expected) || !string.Equals(request.Confirmation, expected, StringComparison.Ordinal))
            return BadRequest(new { error = "The execution confirmation value is missing or invalid." });
        var runId = Guid.NewGuid();
        var jobId = jobs.Enqueue<GeneratedPollCleanupJob>(job => job.RunAsync(request.FromPollId, request.ToPollId, request.MaxRecords, runId));
        return Accepted(new { runId, jobId, request.FromPollId, request.ToPollId, request.MaxRecords });
    }
}
