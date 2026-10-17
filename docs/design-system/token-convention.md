# Shared design token convention

Pollify's color palette and corner-radius scale are defined once and shared between the web app (`Frontend`) and the mobile app (`apps/mobile`). This page is the short version for anyone adding a new UI element; the full provenance/derivation notes live in [`design-tokens/README.md`](../../design-tokens/README.md).

## Where tokens live

- **Source of truth**: [`Frontend/app/globals.css`](../../Frontend/app/globals.css) — OKLCH CSS custom properties (`:root` for light mode, `.dark` for dark mode). Tailwind reads this file directly.
- **Shared mirror**: [`design-tokens/tokens.json`](../../design-tokens/tokens.json) — the same values, generated with a plain sRGB `hex` alongside each `oklch` value, because React Native can't render `oklch()` colors. This file lives at the repo root (outside both `Frontend` and `apps/mobile`) since this isn't an npm workspace and there's no other way to import one file from two independent projects.

## How each platform consumes it

| Platform | Consumes | Notes |
| --- | --- | --- |
| Web (`Frontend`) | `globals.css` directly | Unchanged — Tailwind's `@theme inline` block maps CSS variables to utility classes. |
| Mobile (`apps/mobile`) | `design-tokens/tokens.json`, via [`apps/mobile/src/theme`](../../apps/mobile/src/theme) | `apps/mobile/metro.config.js` adds the repo root to Metro's `watchFolders` so the cross-project import resolves. Import `theme` (or `colors.light` / `colors.dark`) from `./src/theme`, not the raw JSON, so you get typed, named constants. |

## The rule for new UI work

**Don't hardcode a new color.** If an existing token fits, use it (`theme.primary`, `theme.mutedForeground`, etc. on mobile; the existing Tailwind color classes on web). If nothing fits, add a new token to the source of truth first, rather than picking a one-off hex value in a component.

## Worked example: adding a new token

Say a new feature needs a "success" green that doesn't exist yet in either the web or mobile palette.

1. Add it to `Frontend/app/globals.css` as a new OKLCH custom property (both `:root` and `.dark`), e.g. `--success: oklch(0.6 0.15 145);`, and wire it into the `@theme inline` block the same way the existing tokens are.
2. Compute its sRGB hex equivalent (see `design-tokens/README.md` for the conversion method) and add a matching `success` entry to both the `light` and `dark` objects in `design-tokens/tokens.json`.
3. Add `success: string` to the `ThemeColors` interface in `apps/mobile/src/theme/colors.ts` — TypeScript will now require it to be present in both theme objects.
4. Use `bg-success` (or equivalent) on web and `theme.success` on mobile. Neither platform hardcodes the hex value directly.

## What this convention does not cover yet

- Typography tokens (web uses `next/font` Geist; mobile has no custom font loading configured).
- Dark mode on mobile (the mobile theme currently only exposes the light palette by default — see `apps/mobile/src/theme/index.ts`).
- Status/banner colors with no shared semantic token (warning/error tints) — `apps/mobile/App.tsx` currently keeps a few of these as a local `statusColors` constant because the web app doesn't define shared tokens for them either. If you need one on both platforms, follow the worked example above to add it properly rather than reintroducing a local one-off.
