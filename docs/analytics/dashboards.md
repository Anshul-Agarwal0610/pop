# Gamification dashboard definitions

These definitions are provider-neutral and should be reproduced in the production analytics workspace. Filter all panels to consent-eligible actors and schema version 1. Use server events for reward outcomes.

- **Activation:** unique eligible actors with `gamification_hub_viewed` followed by `game_round_completed` within 24 hours, divided by eligible hub viewers.
- **Challenge completion:** unique actors with server `challenge_completed` divided by unique actors with server `challenge_started`, segmented by challenge type and experiment variant.
- **D1/D7/D30 retention:** among actors activated on day 0, the percentage with a meaningful action (`game_round_completed`, server `challenge_progressed`, or server `challenge_completed`) on calendar day 1, 7, or 30. A page open is not a return.
- **Satisfaction:** score distribution and weekly mean for `gamification_satisfaction_submitted`, segmented by variant.
- **Guardrails:** vote completion rate, API error rate, reward delivery failure rate, analytics opt-out rate, and satisfaction.
- **Data quality:** client/server round-completion ratio, duplicate semantic-key rate, outbox p50/p95 delay and terminal failure rate, consent exclusion count, and rejected unknown schema/property count.

Dashboard owners must record the provider URL here after provisioning; credentials and workspace IDs must not be committed.
# PoP Live funnel and health

Filter every panel to `schema_version = 1` and the documented authoritative source. Count distinct `journey_id` in a rolling release cohort unless stated otherwise.

- Invitation-to-join: journeys with `pop_live_session_joined` / journeys with `pop_live_invitation_created`.
- Completion: joined journeys with `pop_live_session_completed` / joined journeys.
- Rematch: completed Poll Clash journeys followed by `pop_live_rematch_started` / eligible completed Poll Clash journeys.
- Relay continuation: eligible Relay stages followed by `pop_live_relay_handoff` or the next join / eligible prior stages.
- Seven-day return: consent-eligible actors joining or completing another PoP Live journey during days 1–7 after their first completed social journey / eligible first completers.

Break down only by bounded `mode`, `platform`, experiment variant, and release cohort. Data-quality panels show consent exclusions, semantic duplicates suppressed, rejected schema/fields, outbox lag, dispatch failures, and expiry. Operational panels show active sessions, SignalR connections, created/completed/expired totals, fixed failure codes, invitation expiry, and reward outcomes. Baselines and targets are **TBD after the initial controlled release**.
