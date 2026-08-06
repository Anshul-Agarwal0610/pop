# Generated binary poll contract

Generated polls have exactly two ordered choices with stable, case-sensitive side values: `Up`, then `Against`. Missing, duplicate, extra, reordered, whitespace-padded, differently-cased, or renamed values are rejected. Provider output is validated after parsing, again by the generation job, and immediately before persistence; invalid output is logged with a reason and is not persisted.

`PollOptions.Side` stores `Up` or `Against` for generated polls. Its value is nullable for manual, sponsored, wellness, and legacy polls. Generated rows also retain canonical `Text` for backward compatibility. A filtered unique index prevents duplicate non-null sides per poll. The domain/service layer enforces cardinality because a row constraint cannot enforce exactly two children.

API option responses add nullable `side` without removing `id`, `pollId`, or `text`:

```json
{"id":123,"pollId":45,"side":"Up","text":"Up","voteCount":10,"votePercentage":62.5}
```

Votes continue to submit `{ "pollId": 45, "optionId": 123 }`. Option IDs are relational identity; side values are semantic identity; labels are presentation. The API and database require the option to belong to the submitted poll before any vote, XP, streak, counter, reward, or analytics mutation.

Clients render generated labels from a local side-label map so labels can later be localized without changing stored values. They must require exactly one option for each canonical side and never infer semantics from provider text or array position. Custom polls continue to display stored text and may have their existing option shapes.

Existing noncanonical generated rows are not relabeled because their meaning cannot be inferred safely. They remain legacy data for an explicit moderation/regeneration rollout. Public poll creation is always custom; only the internal generation path may set `IsAIGenerated`.

## Quality and safety publication gate

New generated polls also require a structured server-owned quality decision. Deterministic contract, grounding, form, framing, safety, sensitivity, and duplicate rules run before the versioned provider-neutral evaluator. Provider confidence is retained only as audit metadata and never contributes to the authoritative score.

The gate returns `Accepted`, `NeedsReview`, or `Rejected`. Accepted candidates must meet the configured overall and per-dimension thresholds and are published. Uncertain, near-duplicate, evaluator-unavailable, and sensitive borderline candidates enter `PendingReview`. Hard contract, prohibited-content, invalid-answerability, and exact-duplicate failures are rejected without creating a poll. Sensitive topics use the stricter configured thresholds.

`GeneratedPollQualityDecisions` records scores, stable reason codes, sensitivity policy code, duplicate metadata, provider metadata, and generation/evaluator prompt, schema, and rules versions. It deliberately excludes source text, URLs, prompts, raw model output, and user-sensitive data. The repository rechecks the canonical sides, accepted-score threshold, disposition, and version metadata in the same transaction that creates the poll and audit row.
