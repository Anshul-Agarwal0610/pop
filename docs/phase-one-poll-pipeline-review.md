# Phase-one poll pipeline review

Reviewed 2026-09-27. Scope: API ingestion without AI, one generation request producing several binary polls per topic, timed publication. This is an audit and proposed implementation contract, not a production deployment.

## Existing foundation

- RSS, GNews, and YouTube adapters fetch without AI. TopicEnrichment cleans text and classifies using keyword rules.
- TrendingTopics already stores Id, Title, Summary, SourceType, SourceUrl, Publisher, PublishedAt, FetchedAt, Category and processing/retry metadata.
- Hangfire runs ingestion and generation. Defaults: ingestion every 30 minutes; generation at minutes 5 and 40 each hour. The latter is not a uniform 35-minute interval.
- OpenAI and Anthropic adapters exist alongside Gemini and Groq. Structured JSON, readiness checks, retry policies and circuit/concurrency controls exist.
- Quality gate supports Accepted, NeedsReview and Rejected; accepted AI polls can publish automatically. Vote controller checks expiry and Published status. Generated poll lifetime is hardcoded to 48 hours.
- Admin pipeline health, controls, retries and operational metrics exist.

## Confirmed gaps and defects

1. PollGenerationService.BuildRequest asks for exactly one question; the result interface is singular. PropositionQualityEvaluator makes a second LLM request. Multiple failover attempts can also occur. This does not meet one successful generation request for 4-5 candidate polls.
2. Duplicate detection rejects a matching source URL, regardless of question. That prevents legitimate different angles from the same topic. CanonicalUrl also discards every query parameter, collapsing distinct YouTube watch?v=... videos into /watch.
3. Two processing state machines coexist: ConversionStatus/NextAttemptAt and GenerationStatus/NextAttemptAtUtc/LeaseId. PollGenerationJob uses GetEligibleAsync and CreateAsync, not ClaimDueAsync and CompleteGeneratedPollAsync. Poll creation and topic completion occur in separate transactions. A crash between them can leave an already-created topic eligible again. Hangfire locking helps but does not make these database operations atomic.
4. US147 introduces a unique SourceTopicId index and completion assumes one poll per source topic. TrendingTopic.GeneratedPollId is singular. The new one-to-many path needs migration and updated completion semantics; do not simply remove the unique index without replacement idempotency keys.
5. Ingestion deduplication uses URL or a lowercased exact title within 48 hours. It does not cluster the same event from different publishers. IF NOT EXISTS is not a concurrency-safe uniqueness guarantee by itself.
6. Ingestion takes the first maxTopics after concatenating RSS, YouTube, GNews; a large first source can crowd out later sources. SaveBatch writes each topic in a separate database round trip. Generation repeats recent-poll duplicate queries.
7. GetEligibleAsync does not apply the 72-hour freshness rule found in the older GetUnprocessedAsync. Old backlog can produce stale polls. Newest-first ordering can starve older eligible work.
8. X, Instagram and a dedicated events API adapter are absent. News about events can already come through news ingestion; event listings require a selected source.
9. Publication lifetime is measured from creation, not approval/publication. A reviewed poll can expire while waiting. Feed queries filter IsActive and Published but do not consistently distinguish open versus expired polls.
10. The vote repository transaction checks option ownership and IsActive, but not expiration/Published status at the point of write. The controller checks earlier; add an atomic check using database UTC time to close the timing gap.
11. Health category currently forces a poll into private Wellness mode. Public news-policy polls about health need explicit mode separation, or phase one should exclude this category.
12. Local config uses LlmWithFallback. Deterministic fallback exists; phase one should explicitly use LlmOnly if all generated poll questions must come from Claude/GPT.

## Live evidence and limits

The hosted /health/llm endpoint returned HTTP 200 with status Degraded: Anthropic Available, Gemini MissingCredential, OpenAI Disabled, Groq Disabled. Available means configuration readiness, not a successful live generation test.

Repository PollQuality:EvaluatorProvider defaults to gemini, and evaluator failure sends candidates to NeedsReview. If production retains this default, this can block automatic publication even when Claude generation works. Production evaluator configuration, job execution history, moderation backlog and DB migration state have NOT been verified. The earlier publication HTTP 500 and missing fresh polls are not yet explained by a confirmed production root cause.

Baseline: dotnet test Backend/BackendAPI.Tests/BackendAPI.Tests.csproj --no-restore --verbosity quiet passed 150 tests. These tests do not establish live API or database health.

## Proposed phase-one contract

### 1. Fetch and store

Run a durable ingestion sweep every five elapsed hours using a persisted NextRunAtUtc cursor and a short scheduler tick. A cron expression with */5 hours resets daily and produces a four-hour overnight gap, so do not describe that as exact five-hour scheduling.

Fetch enabled providers independently; isolate outages. Persist source ID, provider item ID, canonical URL, title, available description, publisher, source publication time, fetched/last-seen time, region, language, source metrics where available, ingestion run ID and payload hash. Preserve only provider-permitted content. Upsert using unique (provider, external item ID) or a source-specific canonical identity.

Normalize and group repeat coverage into TopicClusterId before AI. Retain links to all supporting source items. Use deterministic exact matching and conservative title/entity/time similarity initially; ambiguous clusters remain separate. This is approximate event deduplication, not a claim of perfect semantic matching.

### 2. Generate once per unique topic

Working interpretation: one normal Claude OR GPT request per unique topic cluster, returning up to five distinct candidates together, not one enormous request containing all five hours of content. Retries are separately tracked and budgeted; failed network attempts cannot be guaranteed to incur zero provider work.

Use one configured primary provider. Prompt with bounded source evidence and a fixed category catalog. Output contains topic ID and an array of candidates: angleKey, question, category, brief context, and source evidence references. Binary sides are server-owned constants, not generated options. Preserve internal Up/Against compatibility; display Support/Against in the app.

Aim for 4-5 different defensible angles. Allow fewer or zero if source evidence cannot support them; do not force repetitive or invented questions. Increase token budget from the existing single-question 700-token limit.

Fold categorization and evidence references into that one response. Use deterministic schema/category/length/framing/evidence-reference/duplicate checks afterward. Model self-scores are not independent factual verification. Uncertain candidates go to review. Remove the extra AI evaluator only for this explicitly configured phase-one path; keep legacy behavior isolated until migration is verified.

Store validated generation output before publication. Deduplicate candidates against each other and recent questions. Same source is allowed for different angles; exact/near-identical questions are not. Preserve meaningful URL parameters such as YouTube v; discard tracking parameters only.

### 3. Publish and expire

Suggested default: configurable 48-hour lifetime from PublishedAtUtc, with override per batch. Generate 4-5 candidates but cap visible questions per topic to avoid a repetitive feed; publish all accepted candidates or stagger them as an explicit product setting.

Atomically store polls and options plus generation completion. Unique keys on (TopicClusterId, generation version) and (GenerationBatchId, angleKey), with fingerprint checks across the output set. Use Pending -> Processing (leased) -> Generated -> Completed or RetryPending/NeedsReview/Rejected, and separate candidate publication status Draft/Published/Rejected. Reuse one consistent lease/retry implementation.

A poll is votable only when Published, enabled, PublishedAtUtc <= database UTC now < ExpiresAtUtc. Enforce inside vote transaction. Expose open/closed status in API; keep closed results accessible. Push means publish into the feed here, not send a device notification for every generated question.

## Integration feasibility

- News: reuse GNews; keep RSS as an optional non-AI source, disable it if phase one requires strictly API-only intake.
- YouTube: existing videos.list mostPopular integration supports region/category charts; it is not a guarantee of all trending topics across YouTube. https://developers.google.com/youtube/v3/docs/videos/list
- X: official location-specific trends endpoint exists. Trend names need supporting post/news context before question generation; a hashtag alone is insufficient. Confirm account access and budget before enabling. https://docs.x.com/x-api/trends/trends-by-woeid/introduction
- Instagram: do not promise a platform-wide trending feed. Official Meta documentation was inaccessible during this review; supported discovery scope, account permissions and any licensed provider must be verified before implementation. No scraper substitute selected.

## Implementation order and acceptance checks

1. Diagnose production first: admin pipeline health, scheduler last success, source credentials, Claude request outcome, quality evaluator readiness, pending-review reasons, and migration/application version alignment. Capture sanitized stack trace for the creation 500.
2. Add one-to-many generation schema and a single state machine with atomic completion/idempotency. Migrate existing rows safely; preserve votes and polls.
3. Implement single-call candidate generation plus deterministic validation and correct URL/event/question deduplication.
4. Add exact five-hour intake scheduling, source quotas/fairness, configurable publication lifetime, and transactional expiry checks.
5. Add X after access verification; add Instagram/events only after source capability is established. Enable sources independently.
6. Verify duplicate source intake, distinct YouTube IDs, same-event grouping, distinct angles from same source, malformed/partial AI output, provider timeout/429, crash/retry without extra polls, lost leases, review delay, expiry boundary and closed-results retrieval. Track generated/accepted/rejected/published counts and tokens per run. Alert on no published polls, exhausted open feed, source failure and backlog age.

Defaults to confirm before implementation: India/English versus another audience; one request per topic versus bounded multi-topic batches; Claude versus GPT primary provider; 48-hour lifetime; simultaneous versus staggered publication.
