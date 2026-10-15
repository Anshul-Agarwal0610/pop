# Phase-one pipeline implementation and rollout

Update: expanded RSS, X, Instagram and events adapters are now implemented locally. See [expanded source setup](expanded-ingestion-sources.md) for current settings, access limitations and the additional US152 migration. The API-only defaults and remaining-scope list below describe the initial phase-one delivery.

Implemented locally; not deployed or verified against Azure SQL. The backend unit suite passes (160 tests). SQL lease, migration, publication concurrency, and end-to-end provider behavior still require staging integration checks.

## Behavior

- Scheduled intake checks every five minutes and atomically advances a persisted due time by five hours. This avoids the uneven overnight gap of a five-hour cron expression. Downtime delays a sweep; missed sweeps are not replayed.
- The phase-one default uses GNews and YouTube APIs, with India/English news and the existing India News & Politics YouTube feed. RSS remains available explicitly, or by disabling `PhaseOne:ApiSourcesOnly`. No AI runs during intake.
- Source items retain a unique hash, canonical URL, title, description, publisher, publication date, first/last seen dates, and topic ID. Canonicalization removes known tracking parameters while preserving meaningful query parameters such as YouTube video IDs.
- Grouping uses source identity or matching normalized titles among up to 1,000 topics from the last 48 hours. This is conservative text grouping, not semantic event clustering. Differently worded coverage can still produce separate topics.
- Workers claim up to five topics per run with a ten-minute lease. Topics must have been fetched and, where known, published within the last 72 hours. Old topics are not automatically regenerated.
- One Claude request per topic requests four to five distinct binary questions and categories. Fewer or zero are valid when the source cannot support more. There is no separate AI evaluator call or provider fallback in this path.
- Persisted output is reused if publication fails. Provider failures permit up to three attempts with at least a 15-minute delay. A crash after the provider responds but before output is saved can cause another charged request; exactly-once external calls are not guaranteed.
- Deterministic checks validate form, category, evidence excerpts, self-reported confidence, sensitive/prohibited terms, and recent question similarity. These checks are not independent fact verification. Public Health questions and sensitive topics are held for review. Raw candidates and rejection reasons remain on the topic; no artificial independent-evaluator scores are recorded.
- Accepted questions, server-owned Up/Against options, and topic completion commit together. A transaction lock serializes publication; a unique topic/angle index guards duplicate writes. Multiple distinct angles from one source are allowed.
- Polls expire 48 hours after publication by default (`PhaseOne:LifetimeHours`, bounded to 1–168). Voting checks expiry and publication status inside its transaction. Expired polls can remain in history.

## Rollout order

1. Back up and verify the target schema includes migrations through US150. Apply `Backend/BackendAPI/Migrations/US151_PhaseOnePollBatches.sql` using a tool that recognizes `GO`. Do this before deploying the new ingestion repository: it requires `TopicSourceItems` even when phase-one generation is disabled.
2. Configure secrets through the deployment environment: `GNews__ApiKey`, `YouTube__ApiKey`, and the existing `PollGen__Providers__anthropic__ApiKey`. Verify provider model, endpoint, enabled state, and hosted overrides. Do not put credentials in source control.
3. Deploy first to staging. Confirm `PhaseOne__Enabled=true`, `PhaseOne__Provider=anthropic`, `PhaseOne__ApiSourcesOnly=true`, and `PhaseOne__LifetimeHours=48`. Existing generation pause controls remain supported.
4. Run one ingestion sweep and generate one fresh topic. Inspect its saved source item and candidate payload, resulting categories, two options per poll, and publication/expiry timestamps. Confirm the app sees the new polls.
5. Test duplicate intake, two concurrent claims, expired lease recovery, a publication rollback followed by retry, and rejection of votes after expiry. Unit tests do not substitute for these SQL checks.
6. Enable normal scheduled operation after those checks. Inspect Hangfire failures and topic statuses/reasons. Pausing generation stops new claims but lets an already running topic finish.

Example read-only staging checks:

```sql
SELECT Name, NextRunAtUtc FROM PipelineSchedule;
SELECT TOP(20) Id, ConversionStatus, GenerationStatus, ProcessingStatus,
  AttemptCount, LastFailureReason, PhaseOneProvider, PhaseOneModel
FROM TrendingTopics ORDER BY Id DESC;
SELECT TOP(30) Id, TrendingTopicId, PhaseOneAngle, Question, Category,
  PublishedAtUtc, ExpiresAt FROM Polls WHERE PhaseOneAngle IS NOT NULL ORDER BY Id DESC;
```

## Remaining scope

X trends/tweets and Instagram integrations are not implemented. They need confirmed access and a supported source contract; there is no claimed general Instagram trending feed. A dedicated events API is also not connected. The current sources cover news and a restricted YouTube popular-video feed.

Semantic cross-source deduplication, a richer candidate-review screen, provider-wide shared cooldowns in the batch path, and comprehensive pipeline metrics are follow-ups. Do not interpret local tests as proof that hosted publication failures are resolved.
