# Multiplayer safety, privacy, and retention

Pollify stores invitation, join, reconnect, device, and network credentials only as keyed or cryptographic hashes. Pending invitations and reconnect capabilities expire with their session; leaving immediately revokes the participant's reconnect hash and suppresses future session notifications.

Identity disclosure, individual-vote disclosure, coarse-region participation, and public/result-card sharing are separate, explicit consents and default to off. Host and display views receive participation state and aggregates, not account identity or individual choices. Regions are derived server-side, contain no coordinates or network identifiers, and are suppressed below the configured minimum cohort (five by default).

Ended-session lifecycle metadata may be retained for 90 days. Detailed responses and event payloads should be removed or aggregated after 30 days; account links should be pseudonymized after 90 days. Device/network correlation hashes expire independently after 30 days. Pending invitations are deleted 30 days after expiry. These periods are deployment-configurable and cleanup must be safe to retry.

Safety reports and their audit trail may be retained for up to two years with administrator-only access. Reward-risk decisions may be retained for one year for financial and abuse review. Neither record contains raw IP addresses, credentials, precise location, vote selections, or copied profile data.

Account deletion removes identity links and notification/privacy settings, revokes invitations and capabilities, and pseudonymizes historical participation. Reports and reward decisions retain only the minimum evidence required by policy. De-identified aggregate poll counts may remain. Device/network correlation is a review signal, ages out, and never creates an irreversible device ban.
