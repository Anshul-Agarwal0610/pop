# Android Smoke Test Checklist

Use this checklist before every Play Store internal testing upload. It is designed for a preview APK first, then the production AAB.

## Local Preflight

```powershell
cd apps/mobile
npm install
npm run verify:android:release
```

Expected result:

- TypeScript passes with no errors.
- `npx expo config --type public` prints `android.package` as `com.pollify.app`.
- Android permissions stay limited to the permissions intentionally configured in `app.json`.
- The Expo config output uses the expected `EXPO_PUBLIC_API_URL` for the target environment.

## Backend URL Setup

The mobile app reads `EXPO_PUBLIC_API_URL` at build/start time.

Android emulator:

```powershell
$env:EXPO_PUBLIC_API_URL = "http://10.0.2.2:5177"
npm run android
```

Physical Android device:

```powershell
$env:EXPO_PUBLIC_API_URL = "http://<your-lan-ip>:5177"
npm run android
```

Preview/production EAS builds:

- Replace the placeholder in `eas.json`, or preferably set `EXPO_PUBLIC_API_URL` in EAS environment settings.
- The API URL is public in the app bundle, so never put secrets in this variable.
- The backend must be reachable from the device, not just from the developer machine.

## Preview APK Build

```powershell
cd apps/mobile
npx eas login
npm run build:android:preview
```

Install the generated APK on a real Android phone from the EAS build page.

## Real-Device Smoke Test

Record the device model, Android version, build profile, API URL, and test account used.

- App launches to login/register without a crash.
- Register a new account.
- Log out and log back in with the same account.
- First-run onboarding appears for a new user.
- Category preference selection completes and the app opens the signed-in home.
- Feed loads trending polls from the configured backend.
- Pull-to-refresh reloads the feed.
- Vote on a poll and confirm result bars appear.
- XP, streak, and total vote counters update after voting.
- Duplicate vote or expired poll errors show a readable message.
- Leaderboard loads real backend users.
- Profile shows display name, username, XP, streak, votes, polls created, and joined date.
- Push notification permission prompt appears after sign-in on Android 13+.
- Logout clears the session and disables the current push token.
- Relaunching the app restores the signed-in session when not logged out.
- Relaunching after logout stays on the auth screen.

## Production AAB Check

Only run this after the preview APK smoke test passes.

```powershell
cd apps/mobile
npm run build:android:production
```

Before uploading to Play Console:

- `android.versionCode` increments from the previous Play upload.
- App package remains `com.pollify.app`.
- No `.env`, service account JSON, keystore, or Play credential file is staged.
- Privacy policy URL is live.
- Store listing screenshots reflect the current mobile UI.
- Play Console internal testing track has a tester group configured.

## Troubleshooting

- Emulator cannot reach backend: use `http://10.0.2.2:<port>` instead of `localhost`.
- Physical device cannot reach backend: use the machine LAN IP and allow the backend port through firewall.
- HTTPS/dev certificate errors: use a reachable HTTPS deployment for EAS builds, or use HTTP only for local development.
- Push token registration fails: confirm the user is signed in, backend URL is reachable, and the `US97_Expo_Push_Notifications.sql` migration has run.
- EAS build asks for credentials: let EAS manage Android credentials unless there is a deliberate release-key migration plan.
