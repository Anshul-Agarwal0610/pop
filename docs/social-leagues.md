# Social leagues policy

Social features are opt-in. There is no public group discovery endpoint; every group is private and membership begins only after the targeted user accepts an invite. Public discovery can be added only after a moderation review hook approves it.

- Weeks run from Monday 00:00 UTC (inclusive) until the following Monday (exclusive).
- A friends league contains the viewer and accepted, unblocked friends. A group league contains active, unblocked members.
- Group XP is prospective: the effective score window begins at the later of the week start and `JoinedAt`. Leaving removes access and visibility immediately while retaining audit rows.
- Rank order is eligible XP descending, eligible activity count descending, then user ID ascending.
- Only server-authored `XpEvents` marked `IsSociallyEligible` count. Private polls, wellness activity, and Health-category activity are never eligible. Achievement bonuses are excluded because they cannot reliably inherit activity privacy.
- Blocks are directional records with symmetric social effects. Either direction hides lookup, relationship, invitations, group social visibility, and league visibility. Blocking cancels pending invitations and removes pending or accepted friendship state. Unblocking does not restore either.
- Friend requests: 10 per inviter per rolling hour. Group invites: 20 per inviter per rolling hour, one active invite per group/recipient. Invites expire after seven days and are targeted, single-use tokens.
- Groups have at most 50 active members. Owners cannot leave until ownership is transferred; transfer management is intentionally reserved for a later owner-management endpoint.
- Lists use stable cursors, default to 20 items, and are capped at 50.
