# Pollify Mobile

Expo React Native app for Pollify's Android-first mobile experience, with iOS support kept in the same codebase.

The app uses the existing Pollify backend auth endpoints:

- `POST /api/auth/login`
- `POST /api/auth/register`
- `GET /api/auth/me`

JWT sessions are stored with Expo SecureStore on device.

The app registers an Expo push token after sign-in so retention notifications can reach Android users. Logout disables the current device token through the backend before clearing the local session.

Signed-in users can switch between mobile gamification surfaces:

- Home: current XP, streak, level progress, and native trending poll feed.
- Profile: votes, polls created, XP, streak, level, and joined date.
- Ranks: real backend leaderboard data from `GET /api/users/leaderboard`.

First-time signed-in users see a short native onboarding flow before the tabs:

- Core voting loop: open the app, answer a poll, and see results.
- Gamification: XP, daily streaks, levels, and rankings.
- Category preferences: selected categories are saved to `PUT /api/users/me/preferences/categories`.

Onboarding completion is stored per user in Expo SecureStore so future launches go straight to the app. Users can skip onboarding and enter the app without saving preferences.

The feed is backed by:

- `GET /api/polls/trending`
- `POST /api/votes`

Vote responses update the selected poll, XP, streak, and total vote count in the mobile UI.

Push reminders use:

- `POST /api/notifications/device-tokens`
- `DELETE /api/notifications/device-tokens?token=...`

Expo push does not require a committed secret for this MVP path. Production EAS builds should still define a stable Expo project and keep any future service credentials in EAS environment settings, not in the repo.

## Design tokens

Colors and corner radius come from the shared design tokens, not hardcoded hex values. See [`docs/design-system/token-convention.md`](../../docs/design-system/token-convention.md) for where tokens live, how to consume `./src/theme`, and how to add a new one.

## Requirements

- Node.js 20.19.4 or newer is recommended for the current Expo package set.
- Android Studio and an Android emulator, or a physical Android device with Expo Go.
- The ASP.NET Core backend running locally or on a reachable development URL.

## Run Locally

```bash
cd apps/mobile
npm install
npm run android
```

The app reads the backend URL from `EXPO_PUBLIC_API_URL`.

For the Android emulator, use:

```powershell
$env:EXPO_PUBLIC_API_URL = "http://10.0.2.2:5177"
npm run android
```

For a physical device, use your machine's LAN IP address:

```powershell
$env:EXPO_PUBLIC_API_URL = "http://192.168.1.10:5177"
npm run android
```

You can also copy `.env.example` for a local default API URL.

The web app remains in `Frontend` and can continue to use `NEXT_PUBLIC_API_URL`.

## Auth Notes

Email/password login and registration are wired for the mobile MVP. Google sign-in still needs native Android/iOS OAuth client IDs before it should be exposed in the app store build.

## Android Release

Android release builds use EAS profiles in `eas.json`.

```powershell
cd apps/mobile
npm run verify:android:release
npm run build:android:preview
npm run build:android:production
```

See `docs/android-release.md` for Play Store checklist, privacy policy requirements, credential handling, and submit instructions. Use `docs/android-smoke-test.md` before uploading an internal testing build.
