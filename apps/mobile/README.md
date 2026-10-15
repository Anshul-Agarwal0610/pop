# Pollify Mobile

## Mobile screens

The mobile app has a native Discover → Vote → Results flow, with the same API contracts as the web app. The bottom navigation includes Home, Explore, Ranks, Profile, and More.

- Home uses the personalized feed; Explore searches all polls through the API with category filters.
- The header has Create Poll and Notifications actions. Creation includes editable options, category, expiry, an optional source, and a review step before publishing.
- More opens Games, Challenges, Achievements, Community, Wellness, voting history, and preferences.
- Challenges show progress and eligible category browsing. Achievements include collection filters and earned profile titles.
- Games support starting/resuming Opinion Sprint, optional timing, voting, completion summaries, and expired sessions.
- Community supports people search, friend requests, accept/decline/remove/block actions, groups, invitations, and group rankings.
- Wellness check-ins are private and use separate response endpoints. CSV export uses the native share sheet; deleting history requires confirmation.
- Ranks supports weekly, all-time, and friends scopes. Profile refreshes account statistics and links to history, achievements, interests, notification preferences, and analytics consent.
- Notifications can be marked read individually or together, and poll-related updates open the poll. Poll details also provide reporting.

Business campaign management, moderation administration, and full social pagination/block-list management remain outside this consumer screen expansion. Google login requires configured OAuth client IDs. Expo Go still skips remote push registration; use a development build to test push delivery.

### Verification

```sh
npm run typecheck
npm test
```

Tests mock the backend to avoid publishing polls, voting, or changing a real account. They cover screen rendering, hub navigation, create/review/publish, failed notification updates, request contracts, and voting presentation rules. Native keyboard behavior, accessibility, game timing across device suspension, and CSV sharing should also be checked on a physical Android device.

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
Set `EXPO_PUBLIC_PRIVACY_POLICY_URL` to the deployed web app's public HTTPS `/privacy` page for release builds. The in-app Preferences screen uses this link and also provides permanent account deletion.
Set `EXPO_PUBLIC_WEB_URL` to the deployed web app origin so native poll sharing includes a direct `/polls/{id}` link.

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
Use [`../../docs/google-play-data-safety.md`](../../docs/google-play-data-safety.md) as the Data Safety submission worksheet.
# Google sign-in

Configure Google OAuth clients before starting Expo:

```env
EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID=your-web-client-id.apps.googleusercontent.com
EXPO_PUBLIC_GOOGLE_ANDROID_CLIENT_ID=your-android-client-id.apps.googleusercontent.com
EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID=your-ios-client-id.apps.googleusercontent.com
```

The Android OAuth client must use package `com.pollify.app` and the SHA-1 fingerprint of the signing certificate. Configure the same IDs on the API as `Google:ClientId`, `Google:AndroidClientId`, and `Google:IosClientId`.
