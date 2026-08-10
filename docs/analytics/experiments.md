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

## PoP Live native shortcut spikes

- Owner: Product / PoP Live, with Mobile, Backend, Privacy, QA, and Release Engineering sign-off.
- Dates: set only after canonical PoP Live sessions, secure invitations, HTTPS fallback, and power analysis exist; observe at least 28 days.
- Flags/variants: `pop_live_shareplay_spike_v1` and `pop_live_nfc_spike_v1`; each has independent server, build, and deterministic user controls, default off.
- Hypothesis: a supported native entry shortcut improves backend-validated joins over an offered QR/copied-link baseline without changing game semantics.
- Eligibility: consent-eligible users for whom the installed spike build reports platform support. Report eligible/offered users separately from all mobile users.
- Primary metric: backend-validated join conversion per eligible offer, attributed only by low-cardinality channel.
- Guardrails: native failure below 2%, fallback success at least 95%, no session-completion regression, zero scoring/reward/vote semantic mismatch, privacy incidents, or sample-ratio mismatch.
- Minimum: 500 eligible offers per shortcut and 28 days; go requires at least 10% relative conversion improvement with a confidence interval excluding zero and at least 15% coverage of eligible mobile join opportunities.
- Cost gate: no more than five recurring engineer-days per quarter per shortcut; measure actual engineering, QA, release, and support hours.
- Events: `pop_live_shortcut_offered`, `pop_live_shortcut_started`, `pop_live_shortcut_fallback`, `pop_live_invitation_resolved`, `pop_live_join_completed`. Allowed properties are `channel`, `platform`, `support_state`, `outcome`, `reason_code`, and `app_experience`; tokens, URLs, session IDs, identity, FaceTime participants, and poll content are forbidden.
- Stop/rollback: disable the affected server flag and assignment independently; builds without its entitlement remain the core release. Retain QR/HTTPS recovery and authoritative backend state.
- Full decision record: `docs/spikes/pop-live-ios-shareplay-nfc.md`.
