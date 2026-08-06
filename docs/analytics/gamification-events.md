# Gamification analytics taxonomy (schema v1)

Pollify uses one vendor-neutral contract. Names and property meanings are stable; breaking changes require a new schema version. Client events describe UI interaction. Events marked **server authoritative** are the source for reward and completion reporting.

| Event | Source | Allowed properties |
|---|---|---|
| `gamification_hub_viewed` | client | `surface`, `challenge_count`, `level` |
| `challenge_started` | server authoritative | `challenge_id`, `challenge_type`, `required_actions` |
| `challenge_progressed` | server authoritative | `challenge_id`, `progress`, `required_actions` |
| `challenge_completed` | server authoritative | `challenge_id`, `reward_xp`, `badge_granted` |
| `streak_changed` | server authoritative | `previous_streak`, `current_streak`, `change_reason` |
| `level_up` | server authoritative | `previous_level`, `current_level` |
| `achievement_unlocked` | server authoritative | `achievement_code`, `reward_xp` |
| `game_round_started` | client | `round_id`, `surface`, `category` |
| `game_round_completed` | client and server | `round_id`, `surface`, `outcome`, `xp_awarded`; server is authoritative for reward metrics |
| `gamification_satisfaction_submitted` | client | integer `score` 1–5, optional fixed `reason_code` |

Every envelope contains `event_id`, `occurred_at`, `schema_version`, `source`, `app_version`, and `platform`; where available it contains `anonymous_id`, pseudonymous `user_id`, `session_id`, `experiment_id`, and `experiment_variant`. IDs are opaque and carry no profile traits. Anonymous IDs are random per installation. On login the anonymous identity is aliased to `usr_<internal-id>`; email, username and display name are never traits. Logout resets vendor identity. Missing or denied consent means no capture. When local and server consent disagree, the stricter value wins.

## Data prohibition

Never send poll question, description or option text; selected option ID; wellness/private/health responses; email; username; display name; JWT/session/access tokens; push or advertising tokens; IP-derived identity; URLs/query strings; arbitrary errors/free text; or undocumented properties. Poll IDs are excluded. Category must be a controlled coarse label. Runtime allowlists reject unknown keys and non-primitive values. Analytics errors are swallowed and do not participate in voting or reward transactions.

Client events are suppressed for five seconds by event name plus semantic key. Server events use unique semantic keys and a durable transactional outbox.
