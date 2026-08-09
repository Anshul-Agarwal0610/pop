# US193: Mobile theme module built from shared design tokens

GitHub issue: https://github.com/Anshul-Agarwal0610/pop/issues/193

## User story

As a mobile app developer,
I want a React Native theme module built from the shared design tokens,
so that mobile screens can reference the same colors/radius as the web app instead of one-off hardcoded hex values.

## Description

Create a theme module (e.g. `apps/mobile/src/theme/colors.ts`) that imports the shared tokens from US192 and exposes named constants mirroring the web's semantic token names (`background`, `foreground`, `primary`, `secondary`, `accent`, `muted`, `destructive`, `border`). This story only creates the module — it does not yet apply it to any screen.

## Acceptance criteria

- [ ] `apps/mobile/src/theme` (or similar) exports semantic color/radius constants sourced from the shared tokens
- [ ] Naming mirrors the web app's semantic token names for easy cross-reference
- [ ] Module is typed and typechecks cleanly
- [ ] No existing mobile screen changes visual behavior yet
- [ ] `npm run typecheck` passes in `apps/mobile`

## Technical notes

- Depends on US192's shared token source
- Follow existing `apps/mobile/src` folder conventions (`config`/`context`/`lib`/`types`)
- Plain exported constants are enough — no theming library needed at this scale

## Out of scope

- Applying the theme to `App.tsx`'s existing styles (see US194)
- Dark mode support on mobile — web has a dark theme; whether mobile needs one is an open question, not assumed here
