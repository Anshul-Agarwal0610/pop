# PoP Live iOS SharePlay and NFC technical spike

Status: **no-go for production implementation until #153 and #154 provide a canonical multiplayer session and secure invitation contract**. SharePlay and NFC are optional entry shortcuts; QR and copied HTTPS links remain the baseline.

## Architecture and invariants

Every channel carries one canonical URL:

```text
https://<public-host>/live/join/<opaque-short-lived-token>
```

The token references an existing backend PoP Live session. SharePlay's `GroupSession` is transport/presence context, never a second game session. NFC is an NDEF URI shortcut, never synchronization. QR, link, SharePlay, and NFC all call the same atomic backend resolve/join operations. Voting, reveal, scoring, rewards, authorization, and reconnect hydration remain server-authoritative and channel-independent. Native messages, if later used, may contain only non-authoritative UI hints.

The prerequisite invitation service must generate cryptographically random tokens, store only token hashes, and record purpose, expiry, revocation, and optional use limit. Tokens contain no identity, poll data, votes, rewards, or state. Resolution returns stable `expired`, `revoked`, `session_closed`, `unsupported_mode`, and `use_limit_reached` outcomes. Joining requires authentication (or preserves the pending token through login), validates and consumes atomically, and does not itself confer vote or reward authority.

## Apple and device requirements

- Confirm deployment targets during implementation against current Apple documentation. Group Activities requires a compatible iOS/iPadOS version, Apple account, FaceTime context, and signed physical-device testing. Core NFC availability varies by device and tag operation.
- The organization needs an active Apple Developer Program team, named certificate/profile owners, registered test devices, and two compatible signed-in devices for SharePlay acceptance.
- Enable the managed **Group Activities** capability and its entitlement on a dedicated spike App ID/profile. Enable **NFC Tag Reading**, `NFCReaderUsageDescription`, and the required NDEF tag-format entitlement only in the independent NFC spike build.
- Configure Associated Domains (`applinks:<public-host>`) and serve a valid `apple-app-site-association` for `/live/join/*`, using the real Apple Team ID and `com.pollify.app`.
- HTTPS is canonical. A custom `pollify://` URL is not an acceptable sole recovery path.

## Expo and native ownership

Expo Go cannot load the required custom Swift. Use `expo-dev-client`, a local Expo module around `GroupActivities`, a config plugin for reproducible entitlements/Info.plist settings, generated native projects, EAS iOS development/preview profiles, and signed physical builds. Do not add production entitlements merely to hide UI behind a runtime flag: entitlement presence is itself a build-level experiment.

The iOS module should expose only `isEligible`, `startActivity(invitationUrl)`, `cancel`, and typed session events. It must encode only the canonical URL (or opaque invitation reference), observe incoming group sessions, and hand them to the shared resolver. It must not expose vote, reveal, score, or reward methods. Unsupported OS/hardware, absent entitlement, denied activation, cancellation, timeout, malformed/expired invitation, and native exception map to QR/link recovery.

NFC scope is writing a canonical URL as an NDEF URI record to an explicit physical tag, with optional in-app NDEF reading if later required. Background URL tags may open the universal link. Arbitrary iPhone-to-iPhone tap transfer is not assumed. Never write JWTs, user/session IDs, state, votes, rewards, or long-lived room secrets.

## Isolation and delivery order

Independent controls default off:

| Layer | SharePlay | NFC | Purpose |
| --- | --- | --- | --- |
| Server | invitation issuance/join channel allowlist | same, independently controlled | authoritative kill switch |
| Build | native module + Group Activities entitlement | Core NFC module + entitlements | capability absent from core build |
| Assignment | `pop_live_shareplay_spike_v1` | `pop_live_nfc_spike_v1` | experimental action visibility |

First deliver #153/#154, QR, the HTTPS join page, universal links, and cross-channel contract tests. Only then add a SharePlay development build; add NFC separately after the baseline is proven. None of these flags or failures may block Clash, Toss, Relay, Bomb, Rooms, or ordinary links.

## Maintenance estimate

Initial estimates are planning ranges, not commitments:

| Area | Initial | Recurring per Expo/iOS release |
| --- | ---: | ---: |
| Backend invitation contract and concurrency/security tests | 2–3 engineer-weeks | 1–2 days |
| Web fallback, AASA, login continuation and E2E | 1–2 engineer-weeks | 1 day |
| React Native coordinator, recovery UI and analytics | 1–2 engineer-weeks | 1–2 days |
| Native iOS SharePlay module/config/signing | 2–4 engineer-weeks | 2–5 days |
| NFC NDEF module/config | 1–2 engineer-weeks | 1–3 days |
| QA on device/OS matrix | 1–2 engineer-weeks | 2–4 days |
| Release/signing/monitoring | 3–5 days | 1–2 days |

Native iOS ownership and an upgrade budget are mandatory. The release gate includes Swift payload/config tests and manual tests on two physical devices; Jest or Expo Go alone is insufficient.

## Measurement and go/no-go

Owner: Product lead decides separately for SharePlay and NFC with Mobile, Backend, Security/Privacy, QA, and Release Engineering sign-off. Dates and power analysis must be entered before exposure. Observe for at least 28 days and until at least 500 eligible offers per experiment; if that sample is infeasible, platform coverage itself supports no-go.

Measure among consent-eligible users: eligible/offered coverage, shortcut start rate, backend-validated join conversion, QR/link fallback success, session completion, native failure rate, and actual engineering/QA/support hours. Analytics properties are restricted to low-cardinality `channel`, `platform`, `support_state`, `outcome`, `reason_code`, and `app_experience`; never token, URL, session ID, participant/FaceTime identity, or poll content.

Go only if validated-join conversion improves by at least 10% relative over the QR/link baseline with a confidence interval excluding zero, native failure stays below 2%, session completion/scoring/reward correctness does not regress, the experiment covers at least 15% of eligible mobile join opportunities, and recurring maintenance is at most five engineer-days per quarter per shortcut. Stop immediately for privacy/security incidents, entitlement/signing instability, sample-ratio mismatch, >2% semantics mismatch, or fallback success below 95%. Otherwise no-go or extend only with an explicitly approved new hypothesis.

## Acceptance and open dependencies

Automated coverage must prove identical resolution across QR/link/SharePlay/NFC attribution, atomic expiry/revocation/use limits, no voting authority from resolution, semantics parity, flag-off safety, sanitized logs/analytics, adapter error mapping, and universal-link status behavior. Physical acceptance covers cancellation, permission denial, FaceTime eligibility, reconnect hydration, NFC read/write, and QR/link recovery.

Open prerequisites: canonical PoP Live models/endpoints (#153), secure invitations and HTTPS join (#154), stable TLS host, Apple Team ID, capability approval, threat-model-approved TTL/use limits, native owner, and product decision to reopen #152/#165. The current authenticated single-user Opinion Sprint `GameSessions` implementation is explicitly not a substitute.
