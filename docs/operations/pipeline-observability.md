# Pipeline observability and operator controls

Pollify emits .NET metrics from the `Pollify.Pipeline` meter. Export this meter with the deployment's Azure Monitor/OpenTelemetry configuration and the standard `APPLICATIONINSIGHTS_CONNECTION_STRING`; structured `ILogger` fields and `ActivitySource` traces use `CorrelationId` to join ingestion, topic generation, and persistence logs. No secrets, prompts, response bodies, URLs, topic IDs, or correlation IDs are metric dimensions.

| Metric | Fixed dimensions |
| --- | --- |
| `pollify.ingestion.topics` | `source=rss|youtube|gnews|all`, `stage=fetched|deduplicated|queued` |
| `pollify.generation.topics` | `stage=converted|published|retried|rejected|review` |
| `pollify.ingestion.provider.duration` | `source` (milliseconds) |
| `pollify.llm.requests` | `provider=openai|anthropic|custom`, `outcome=success|failure|rate_limited` |
| `pollify.llm.request.duration` | `provider` (milliseconds) |
| `pollify.llm.tokens` | `provider`, `type=input|output` |
| `pollify.llm.failovers` | `from_provider`, `to_provider` |

All endpoints require a JWT satisfying the existing `Admin` policy:

- `GET /api/admin/pipeline/health` returns separate `ingestion` and `generation` snapshots, sanitized provider states, pause state, and backlog.
- `POST /api/admin/pipeline/generation/pause` and `/resume` update shared SQL state.
- `POST /api/admin/pipeline/ingestion/run` with `{"source":"rss","maxTopics":50}` queues a bounded run.
- `POST /api/admin/pipeline/generation/retry` with `{"maxTopics":10}` requeues only retry/rejected records and queues a bounded run.

The server rejects unknown sources and values exceeding `Pipeline:MaxIngestionBatch` or `Pipeline:MaxRetryBatch`.

## Recommended Azure Monitor alerts

Configure thresholds from `PollGeneration:Alerts` and evaluate over at least two consecutive windows to limit noise.

- Zero ingestion: alert when the sum of `pollify.ingestion.topics{stage=queued}` is zero for `ZeroIngestionMinutes` (recommended 60 minutes), while at least one ingestion source is enabled.
- High fallback: alert when `sum(pollify.llm.failovers) / max(sum(pollify.llm.requests), 1)` exceeds `HighFallbackRatio` (recommended 0.25) for 15 minutes.
- Sustained 429: alert when `sum(pollify.llm.requests{outcome=rate_limited})` exceeds `Sustained429Count` (recommended 5) in each of three five-minute windows. Group only by provider.
- Growing backlog: poll the protected health endpoint from the operator monitor or export its SQL-backed backlog gauge; alert when queued plus retry-pending exceeds `BacklogCount` (recommended 100), or `OldestEligibleAt` is older than 60 minutes.

Apply `US149_Pipeline_Observability.sql` before enabling the jobs. Pause state is SQL-backed and survives restarts; provider cooldown snapshots are process-local and intentionally sanitized.
