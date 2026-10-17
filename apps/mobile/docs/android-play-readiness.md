# PoP Android production readiness

Last reviewed: 2026-10-14

## Current status

PoP is configured for Android package `com.PublicOpinionPoll.com` on Expo SDK 57. The production EAS profile creates an Android App Bundle and increments the Android version code. The resolved Expo configuration, TypeScript check, Expo Doctor, and mobile tests pass.

The deployed web app, poll feed, privacy page, and account-deletion page are reachable. The API health check reports the configured Claude provider as available, and the public poll endpoint returns current polls.

The newest store AAB is version code 6 from 2026-10-11. Preview APK version code 8 is newer, but it predates some current workspace changes. Build and test a fresh release candidate before Play submission.

## Remaining release work

| Priority | Work | Completion evidence |
| --- | --- | --- |
| Blocker | Stabilize and commit the current release candidate | Reviewed commit with no unintended files or secrets |
| Blocker | Build a new production AAB from the reviewed commit | Successful EAS production build with a version code above the existing store build |
| Blocker | Run physical-device regression testing | Registration, password and Google login, restored session, feed, media, vote and duplicate-vote handling, prediction games, private polls, sharing and QR, profile editing/deletion, notifications, dark mode, keyboard, offline and error states pass |
| Blocker | Reconcile Data Safety with the final AAB | Final dependency/permission/data-flow review recorded in `docs/google-play-data-safety.md` and entered in Play Console |
| Blocker | Complete Play Console policy setup | Privacy URL, account-deletion URL, ads declaration, content rating, target audience, app access instructions, and Data Safety form accepted |
| Blocker | Create store listing material | Final title, short/full description, support email, category, 1024x500 feature graphic, phone screenshots, and app icon uploaded |
| Conditional | Complete required closed testing | For qualifying new personal accounts: at least 12 testers continuously opted in for 14 days, followed by production-access approval |
| Release | Submit with a staged rollout | Internal test succeeds, then production release is reviewed and rolled out gradually |

## Completed engineering checks

- Expo SDK 57 dependencies are aligned with the installed SDK.
- `npx expo-doctor`: 21/21 checks pass.
- `npm run verify:android:release`: TypeScript and resolved Expo configuration pass.
- Mobile Jest suite: 10 suites and 54 tests pass.
- Production profile uses the deployed HTTPS API and web/privacy URLs.
- Privacy policy: `https://mvp-react-frontend.azurewebsites.net/privacy`.
- Web deletion resource: `https://mvp-react-frontend.azurewebsites.net/delete-account`.
- In-app deletion, Terms acceptance, media capture/display, Google login configuration, sharing, private polls, business flows, and prediction games are implemented.
- Android permissions resolved by Expo are `POST_NOTIFICATIONS` and `RECORD_AUDIO`; restricted activity-recognition and overlay permissions are blocked.

## Build and validation sequence

1. Review and commit the intended release changes.
2. Run `npm run verify:android:release`, `npx expo-doctor`, and `npm test` in `apps/mobile`.
3. Build a preview APK and complete `android-smoke-test.md` on representative physical Android devices.
4. Build the production AAB with `npm run build:android:production`.
5. Review the exact AAB permissions and data behavior, then update the Data Safety declaration.
6. Upload to Play internal testing, complete policy and listing fields, then proceed through any required closed test.

## References

- [Google Play target API requirements](https://support.google.com/googleplay/android-developer/answer/11926878?hl=en)
- [Google Play Data Safety form guidance](https://support.google.com/googleplay/android-developer/answer/10787469?hl=en)
- [Google Play account deletion requirements](https://support.google.com/googleplay/android-developer/answer/13327111?hl=en)
- [Google Play testing requirements for new personal accounts](https://support.google.com/googleplay/android-developer/answer/14151465?hl=en)
