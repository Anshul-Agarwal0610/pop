# US192: Extract shared design tokens into a single source of truth

GitHub issue: https://github.com/Anshul-Agarwal0610/pop/issues/192

## User story

As a developer maintaining both the web and mobile Pollify apps,
I want a single, framework-agnostic source of truth for design tokens (colors, radius),
so that both apps pull from the same values instead of maintaining separate, drifting palettes.

## Description

`Frontend/app/globals.css` defines the app's palette as OKLCH CSS variables (coral/lavender/teal theme). `apps/mobile/App.tsx` currently hardcodes an unrelated earthy hex palette (`#F7F5EF`, `#B0413E`, `#233D4D`, etc.) directly in `StyleSheet.create`. React Native cannot consume `oklch()` values directly, so extract the current light/dark palette into a shared token module with both the OKLCH value (for web) and a plain hex/rgb equivalent (for mobile).

## Acceptance criteria

- [ ] A shared token source exists covering color and radius values matching Frontend's current light and dark theme
- [ ] Each token has both a CSS-var-compatible value and a plain hex/rgb equivalent usable in React Native
- [ ] Token file lives somewhere both `Frontend` and `apps/mobile` can import from without hand-duplicating values
- [ ] Web app is visually unchanged after the refactor (no regression)
- [ ] Frontend build/lint and mobile typecheck pass

## Technical notes

- Source values live in `Frontend/app/globals.css` — note a duplicate exists at `Frontend/styles/globals.css`; confirm which is authoritative before extracting
- A plain `.ts`/`.json` token module is sufficient — no need for a new package/workspace since this isn't a monorepo with shared tooling today
- No build-tool changes required

## Out of scope

- Migrating Tailwind config to reference the new token file directly (web already uses the source CSS variables as-is; this story only mirrors them for mobile)
- Building the React Native theme consumer (see US193)
- Typography/font tokens — web uses `next/font` Geist; mobile has no custom font loading yet, and fonts differ enough to warrant their own story later
