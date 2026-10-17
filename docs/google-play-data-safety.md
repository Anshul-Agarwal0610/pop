# Google Play Data Safety declaration

Last reviewed against the production Android App Bundle: 11 October 2026

This is the submission worksheet for the current Pop Android app and backend. Recheck it against the exact production build, enabled SDKs, and Google Play's current form before submission.

## Verified release artifact

- **EAS build:** `27f484e5-7320-4a77-8980-019935b5ce2d`
- **Application ID:** `com.PublicOpinionPoll.com`
- **Version:** `1.0.0` (`versionCode` 6)
- **Android SDK:** minimum 24, target 36
- **AAB:** `https://expo.dev/artifacts/eas/9x-fD7uumr3yjZS7Az4rCA9YTJgM6sibnCVrbHwoqBU.aab`
- **Validation:** Google bundletool 1.18.3 passed.
- **Production analytics configuration:** no `EXPO_PUBLIC_ANALYTICS_CAPTURE_URL` is configured, so optional client event analytics are not transmitted by this release. Account activity required for polls, votes, rewards, preferences, moderation, and security is still processed by the Pop API as described below.

## Form overview

- **Does the app collect or share user data?** Yes.
- **Is all collected user data encrypted in transit?** Yes. Production app traffic must remain HTTPS-only.
- **Can users request deletion?** Yes. The app provides **More → Preferences → Your data → Delete my account** and the public web deletion flow at `https://mvp-react-frontend.azurewebsites.net/delete-account`.
- **Is data collection optional?** Account and core activity data are required for signed-in features. Media uploads, notifications, Google sign-in, wellness check-ins, and usage analytics are optional. Analytics remains off until the user consents.
- **Independent security review?** Answer **No** unless a qualifying review has been completed before submission.

## Play Console URLs

- **Privacy policy:** `https://mvp-react-frontend.azurewebsites.net/privacy`
- **Account deletion:** `https://mvp-react-frontend.azurewebsites.net/delete-account`

## Data-type answers

| Google Play category | Collected | Shared | Purpose | Optional / deletion behavior |
| --- | --- | --- | --- | --- |
| Name | Yes | No* | Account management, app functionality, social profile | Display name is required; removed on account deletion |
| Email address | Yes, for Google sign-in | No* | Authentication and account management | Optional when password sign-in is used; removed on deletion |
| User IDs | Yes | No* | Authentication, security, app functionality | Required for an account; credentials and identifying values removed on deletion |
| Photos and videos | Yes, when uploaded | No* | User-generated poll media and profile avatar | Optional; account avatar is removed on deletion. Public poll media may remain with an anonymized poll |
| Health info | Yes | No | Private wellness check-ins and notes | Optional; deleted with the account |
| Other user-generated content | Yes | No | Poll questions, options, sources, reports, groups, and other submitted content | Optional; public poll content may remain anonymized after deletion |
| App interactions | Yes | No* | Votes, results, rewards, streaks, challenges, games, personalization, and consented analytics | Core interactions are required for those features; public votes may remain anonymized |
| Other actions | Yes | No | Category, notification, privacy, and social preferences | Deleted with the account |
| Device or other IDs | Yes | No* | Push notifications, security, and consented analytics | Push and analytics are optional; tokens and stored identifiers are deleted or cleared |

`*` The app sends limited data to contracted service providers that process it on Pop's behalf, including cloud hosting/storage, Google authentication, and Expo notification delivery. Google Play may exclude qualifying service-provider transfers from “sharing.” Confirm current contracts and Play's definition before choosing **Not shared**. When a user plays embedded YouTube content, YouTube may receive network/device information; disclose that transfer if the production implementation or current Play guidance classifies it as sharing.

## Processing purposes to select

- **App functionality:** accounts, polls, votes, results, uploads, wellness, notifications, groups, games, XP, achievements, and preferences.
- **Analytics:** only for optional analytics data collected after consent.
- **Developer communications:** push notifications the user enables.
- **Fraud prevention, security, and compliance:** authentication, abuse prevention, reports, and operational logs.
- **Personalization:** feed/category preferences and relevant app experiences.
- Do not select **Advertising or marketing** for the current build unless an advertising SDK or personalized sponsor targeting is added.

## Required release checks

1. Deploy the web privacy page at a stable HTTPS `/privacy` URL.
2. Set `EXPO_PUBLIC_PRIVACY_POLICY_URL` to that URL in every EAS build profile.
3. Enter `https://mvp-react-frontend.azurewebsites.net/privacy` as the privacy-policy URL and `https://mvp-react-frontend.azurewebsites.net/delete-account` as the account-deletion URL in Play Console.
4. Production deletion verified on 11 October 2026: authenticated deletion returned HTTP 204 and the deleted credentials subsequently returned HTTP 401.
5. Final AAB review: bundletool validation passed. The release uses internet/network access, notifications, camera/media selection, microphone, vibration, secure-storage biometrics, notification delivery/background wake, and launcher-badge permissions. The unused system-overlay and physical-activity permissions are explicitly blocked. SDKs provide Google sign-in, Expo notifications, secure storage, accelerometer-based shake input, sharing, video/WebView and optional first-party analytics. No advertising, payment, location, contacts, SMS/call-log, or crash-reporting SDK is present.
