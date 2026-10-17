# Pollify web frontend

Next.js app for Pollify's web experience.

## Design tokens

Colors and corner radius are defined in [`app/globals.css`](app/globals.css) and consumed via Tailwind's `@theme inline` block — don't hardcode a new color in a component. See [`docs/design-system/token-convention.md`](../docs/design-system/token-convention.md) for the full shared-token convention (this palette is mirrored for `apps/mobile` too) and how to add a new token.
