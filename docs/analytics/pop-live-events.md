# PoP Live analytics contract

PoP Live product analytics uses schema version `1`. It is consent-gated and separate from security audit logs and aggregate operational metrics. Analytics failures are dropped and never participate in a voting or session transaction.

## Events and ownership

| Event | Authoritative source | Meaning |
|---|---|---|
| `pop_live_toss_shown` | client | Toss affordance rendered |
| `pop_live_invitation_created` | server | invitation transaction committed |
| `pop_live_invitation_opened` | server; client only for anonymous preview | first successful preview |
| `pop_live_session_joined` | server | participant join committed |
| `pop_live_first_response_locked` | server | first immutable response in a journey |
| `pop_live_session_completed` | server | first successful terminal transition |
| `pop_live_result_shared` | client | native share/copy action succeeded |
| `pop_live_rematch_requested` | server | Clash rematch request committed |
| `pop_live_rematch_started` | server | accepted rematch created |
| `pop_live_relay_handoff` | server | next Relay stage created |

Every event requires `journey_id`, `mode`, `platform`, `source`, `invitation_channel`, `completion_reason`, `experiment_id`, and `experiment_variant`. Relay handoff also requires `handoff_index`. `journey_id` is a random analytics identifier stored with the domain object; it must never be a public invite token or derived from one. Semantic keys are unique per committed transition and suppress retries.

Bounded values are defined in `PopLiveAnalyticsContract`: five modes, `web | ios | android | backend`, `client | server`, controlled invitation/completion values, and `control | treatment`. Raw poll/vote content, poll/option/response identifiers, invitation tokens, URLs, names, IP/device fingerprints, arbitrary errors, and location are prohibited.

## Operational metrics

`Pollify.PopLive` emits aggregate session transitions, SignalR connection transitions, fixed failure codes, and bounded reward outcomes (`allow | cap | hold | suppress`). Metrics have no actor, journey, poll, token, IP, or location labels and are not product analytics.
