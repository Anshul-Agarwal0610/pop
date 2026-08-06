# Controlled experiments

No experiment launches until this template is complete: owner; planned start/end; flag and variants; hypothesis; eligibility and privacy exclusions; primary metric; guardrails; minimum sample and duration; stop conditions; rollback procedure.

## Challenge visibility v1

- Owner: Product / Gamification
- Dates: set before launch
- Flag/variants: `gamification_challenges_v1`; control (hidden) and treatment (visible), deterministic assignment
- Hypothesis: showing daily challenges increases challenge completion among activated, consent-eligible users.
- Eligibility: users assigned by pseudonymous user ID (installation ID before login); denied/unknown consent is excluded from measurement.
- Primary metric: server-authoritative challenge completion among eligible activated users.
- Guardrails: vote completion rate, API error rate, reward-delivery failure rate, opt-out rate, and satisfaction. Session length is not a success metric.
- Minimum: set by power analysis; at least 14 days to cover weekly effects.
- Stop: material guardrail regression, privacy incident, sample-ratio mismatch, or reward inconsistency.
- Rollback: set rollout to zero on client and server; preserve assignments and outbox records for audit.
