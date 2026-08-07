# Shared design tokens

`tokens.json` in this folder is the shared source of truth for Pollify's color palette and corner-radius scale, usable by both `Frontend` (Next.js/Tailwind, web) and `apps/mobile` (React Native).

## Source of truth

The actual authored values live in [`Frontend/app/globals.css`](../Frontend/app/globals.css) as OKLCH CSS custom properties (`:root` for light mode, `.dark` for dark mode). That file is still what Tailwind/the web app uses directly — this token file is a generated mirror of those same values, not a replacement.

Each color token in `tokens.json` has two fields:

- `oklch` — the exact value copied from `globals.css`, kept for reference and for any future web-side consumer that wants the original CSS value.
- `hex` — the sRGB hex equivalent, computed from the OKLCH value. React Native's `StyleSheet` cannot parse `oklch()` colors, so this is the value mobile code should use.

`radius` mirrors the `--radius-sm/md/lg/xl` scale computed in `globals.css`'s `@theme inline` block (based on `--radius: 0.75rem` = 12px at a 16px root font size).

## Regenerating hex values

If the palette in `Frontend/app/globals.css` changes, regenerate the `hex` values from the new OKLCH numbers using the standard OKLCH → linear sRGB → sRGB conversion (Björn Ottosson's OKLab formulas: OKLCH → OKLab → linear sRGB via the M1/M2 matrices → gamma-encode → clamp → round to a byte per channel). Do not hand-guess hex values — small OKLCH changes can shift hue non-obviously once gamma-corrected.

## Consuming this file

- **Web**: not consumed yet — Tailwind continues to read `globals.css` directly. Wiring Tailwind to this file instead is explicitly out of scope for now (see `docs/design-system/US192-extract-shared-design-tokens.md`).
- **Mobile**: `apps/mobile/src/theme` imports this JSON directly (see `docs/design-system/US193-mobile-theme-module.md`). Metro is configured via `apps/mobile/metro.config.js` to watch the repo root so it can resolve the `../../../design-tokens/tokens.json` import outside its own project folder.
