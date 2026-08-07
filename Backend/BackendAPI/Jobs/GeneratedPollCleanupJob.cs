using BackendAPI.Interfaces;
using Hangfire;

namespace BackendAPI.Jobs;

public sealed class GeneratedPollCleanupJob(IGeneratedPollCleanupService service)
{
    [DisableConcurrentExecution(600)]
    public Task RunAsync(long fromPollId, long toPollId, int maxRecords, Guid runId) =>
        service.ExecuteAsync(fromPollId, toPollId, maxRecords, runId);
}

