# Nearby Poll Toss experiment

Nearby is an Android development/EAS-build experiment and is unavailable in Expo Go. It is off by default. A session starts only after an explicit sender or receiver action, refetches `/api/mobile/experiments`, requests OS permissions, and lasts at most the server-configured discovery timeout. Denial, missing Play Services, radio failure, timeout, cancellation, or backgrounding must call `stop()` and leave QR/copy-link sharing available.

Peers advertise only an ephemeral “Pollify player” label. Users compare Nearby's authentication digits and both explicitly accept. The sole bytes payload is `{"version":1,"invitationToken":"<43 character opaque token>"}`. The receiver redeems it over HTTPS; poll data, voting, XP, and rewards never come from the peer transport. The database stores no receiver, endpoint, device, Bluetooth, Wi-Fi, distance, or location data, and expired invitation rows should be purged shortly after expiry.

Two-device smoke test: verify flag-off, Android 30/31/33+ permissions, deny/permanently deny, Bluetooth and Wi-Fi off, mismatched verification, sender/receiver cancellation at each step, backgrounding, timeout, replay, malformed and oversized payloads, disconnect/reconnect, QR/link fallback, and a remote disable before redemption. Confirm the final vote still uses `POST /api/votes`.
