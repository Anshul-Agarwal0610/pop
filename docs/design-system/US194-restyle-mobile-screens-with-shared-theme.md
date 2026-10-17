# US194: Restyle existing mobile screens with the shared theme

GitHub issue: https://github.com/Anshul-Agarwal0610/pop/issues/194

## User story

As a user of the Pollify mobile app,
I want the app's colors and visual style to match the web app's brand,
so that the mobile and web experience feels like the same product.

## Description

Replace the hardcoded hex values throughout `apps/mobile/App.tsx`'s `StyleSheet.create` (and inline color props like `ActivityIndicator color="#B0413E"`) with the theme constants from US193. Purely a visual restyle — no screen logic, navigation, or API behavior changes.

## Acceptance criteria

- [ ] All hardcoded hex color literals in `App.tsx` are replaced with theme token references
- [ ] Visual review confirms mobile screens use the web app's color palette
- [ ] No functional regressions in auth, onboarding, feed, voting, leaderboard, or profile screens
- [ ] App still builds and runs on Android (smoke test per `apps/mobile/docs/android-smoke-test.md`)
- [ ] Existing related functionality continues to work

## Technical notes

- Touches `apps/mobile/App.tsx` only (~1652 lines; all screens currently live in this one file)
- Depends on US193's theme module existing first
- This is a styling pass, not a component refactor — reuse existing structure

## Out of scope

- Splitting `App.tsx` into separate screen/component files (real cleanup opportunity, but a separate concern)
- Any new features or screen additions
- iOS visual testing — no iOS test path is set up yet
