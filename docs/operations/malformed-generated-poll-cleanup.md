# Malformed generated-poll cleanup runbook

This process hides malformed AI-generated polls without deleting or rewriting polls, options, votes, or quality history. Voted polls use `PreserveAndHide`; unvoted polls use `DeactivateAndRegenerate`. Replacements are new polls and votes are never transferred or deleted. Inactive originals remain available through direct ID access for audit.

## Prerequisite and safety controls

Deploy and apply `US148_GeneratedPollQualityGate.sql` and its binary publication gate before applying `US150_MalformedGeneratedPollCleanup.sql`. Confirm newly generated polls cannot bypass the gate. Cleanup has no recurring or startup registration.

Production execution requires all of the following:

- an authenticated user with the `Admin` role;
- explicit positive `fromPollId`, `toPollId`, and `maxRecords` (maximum 500);
- `GeneratedPollCleanup:ExecutionEnabled=true` in protected production configuration;
- the protected `GeneratedPollCleanup:Confirmation` value;
- `dryRun=false` on the execute request.

Keep the switch disabled except during an approved cleanup window. Pause normal generation only if operators need a quiet inspection window; cleanup never resets `TrendingTopics.IsProcessed`.

## Dry run and inspection

```http
POST /api/admin/generated-poll-cleanup/dry-run
Authorization: Bearer <admin-token>
Content-Type: application/json

{"fromPollId":1000,"toPollId":1100,"maxRecords":100}
```

Dry run performs no writes. Capture `scannedCount`, `malformedCount`, every grouped reason/vote/generation/ingestion source, and inspect samples from every group. Provider metadata comes from the US148 decision; historical fingerprints report `historical-fallback`; other legacy rows honestly report `legacy-unknown`.

Baseline and reconciliation queries:

```sql
SELECT COUNT(*) Polls, SUM(CASE WHEN IsActive=1 THEN 1 ELSE 0 END) Active,
       SUM(CASE WHEN IsAIGenerated=1 THEN 1 ELSE 0 END) Generated
FROM Polls WHERE Id BETWEEN @FromPollId AND @ToPollId;

SELECT p.Id, COUNT_BIG(v.Id) VoteCount, p.IsActive, p.IsTrending
FROM Polls p LEFT JOIN Votes v ON v.PollId=p.Id
WHERE p.Id BETWEEN @FromPollId AND @ToPollId
GROUP BY p.Id,p.IsActive,p.IsTrending ORDER BY p.Id;
```

## Execute a bounded batch

```http
POST /api/admin/generated-poll-cleanup/execute
Authorization: Bearer <admin-token>
Content-Type: application/json

{"fromPollId":1000,"toPollId":1024,"maxRecords":25,"dryRun":false,"confirmation":"<protected-value>"}
```

The endpoint queues exactly one bounded Hangfire cleanup job. Start with a small staging range, inspect it, and repeat the same range to prove idempotency before expanding. Each candidate is independently transactional, so one failure does not stop later candidates.

Run `GeneratedPollRegenerationJob.RunAsync(maxRecords)` as a bounded one-off Hangfire job after reviewing queued unvoted records. It resolves the recorded topic first, or exactly one non-empty matching source URL. Missing or ambiguous provenance fails to manual review and never guesses context.

## Recovery and audit tracing

Failed cleanup candidates can be rerun with the same bounds. Failed regeneration rows become retryable after their availability delay; fix the provider/provenance issue and run another bounded regeneration batch. The original remains inactive throughout.

```sql
SELECT p.Id OriginalPollId, c.Id CleanupRecordId, c.Disposition, c.Status,
       c.ReasonCode, c.VoteCountAtCleanup, c.DetectedAt, c.CleanedAt,
       c.ReplacementPollId, q.Status QueueStatus, c.LastError
FROM Polls p
JOIN GeneratedPollCleanupRecords c ON c.PollId=p.Id
LEFT JOIN GeneratedPollRegenerationQueue q ON q.CleanupRecordId=c.Id
WHERE p.Id BETWEEN @FromPollId AND @ToPollId ORDER BY p.Id;
```

After execution, repeat the dry run and baseline queries. Reconcile hidden voted rows, queued/completed unvoted rows, replacement IDs, and unchanged authoritative vote counts. Escalate any original whose question, option IDs/text/sides, or vote rows changed; the workflow does not authorize such changes.
