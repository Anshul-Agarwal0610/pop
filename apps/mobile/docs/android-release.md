# Android Release Guide

Pollify's Android build is configured for Expo Application Services (EAS). The app package is `com.pollify.app`.

## Build Profiles

- `development`: internal APK with a development client.
- `preview`: internal APK for QA and stakeholder testing.
- `production`: Android App Bundle (`.aab`) for Play Store upload.

## Required Secrets and Environment

Do not commit credentials or private keys. Store these in EAS or local developer machines:

- `EXPO_PUBLIC_API_URL`: production API base URL. This value is public in the app bundle, so it must not contain secrets.
- Google Play service account JSON: configure through `eas submit:configure` or EAS secrets, never as a committed file.
- Android signing key: let EAS manage credentials, or store keystores outside the repo.

Before production builds, replace the placeholder API URL in `eas.json` with the deployed backend URL or set it through EAS environment variables.

## Release Commands

```powershell
cd apps/mobile
npm install
npm run typecheck
npx eas login
npx eas build:configure
npm run build:android:preview
npm run build:android:production
```

Submit a draft internal-track release after the Play Console service account is configured:

```powershell
npm run submit:android:production
```

## Store Listing Checklist

- App name: Pollify
- Short description: Mobile-first polls with streaks, XP, and leaderboards.
- Full description: Explain public opinion polls, voting, streaks, XP, leaderboards, and future health/business poll categories.
- Category: Social or Entertainment, depending on final positioning.
- Content rating questionnaire completed in Play Console.
- Data safety form completed for account, auth, voting, analytics, and future ads.
- Privacy policy URL published and added in Play Console.
- Screenshots for common Android phone sizes.
- Feature graphic and app icon uploaded.
- Test account credentials available for Play review if login is required.

## Privacy Policy Requirements

The privacy policy must cover:

- Account identifiers such as username, display name, email, and auth provider.
- Poll votes, XP, streaks, leaderboard ranking, and profile statistics.
- Device/app diagnostics if analytics or crash reporting are added.
- Future ad poll and health poll data handling before those features launch.
- Data deletion or support contact process.

## Pre-Release Checks

- Confirm `android.versionCode` increments for each Play Store upload.
- Confirm `expo.android.permissions` stays minimal.
- Confirm no `.env`, service account JSON, keystore, or secret file is staged.
- Run `npm run typecheck`.
- Build preview APK first, install on Android, then build production AAB.
