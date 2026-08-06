# Gamification dashboard definitions

These definitions are provider-neutral and should be reproduced in the production analytics workspace. Filter all panels to consent-eligible actors and schema version 1. Use server events for reward outcomes.

- **Activation:** unique eligible actors with `gamification_hub_viewed` followed by `game_round_completed` within 24 hours, divided by eligible hub viewers.
- **Challenge completion:** unique actors with server `challenge_completed` divided by unique actors with server `challenge_started`, segmented by challenge type and experiment variant.
- **D1/D7/D30 retention:** among actors activated on day 0, the percentage with a meaningful action (`game_round_completed`, server `challenge_progressed`, or server `challenge_completed`) on calendar day 1, 7, or 30. A page open is not a return.
- **Satisfaction:** score distribution and weekly mean for `gamification_satisfaction_submitted`, segmented by variant.
- **Guardrails:** vote completion rate, API error rate, reward delivery failure rate, analytics opt-out rate, and satisfaction.
- **Data quality:** client/server round-completion ratio, duplicate semantic-key rate, outbox p50/p95 delay and terminal failure rate, consent exclusion count, and rejected unknown schema/property count.

Dashboard owners must record the provider URL here after provisioning; credentials and workspace IDs must not be committed.
