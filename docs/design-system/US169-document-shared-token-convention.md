# US169: Document the shared design token convention

## User story

As a developer adding new features to Pollify,
I want a documented convention for using shared design tokens on both web and mobile,
so that new components don't reintroduce hardcoded, divergent styling.

## Description

With shared tokens in place (US166–US168), write a short contributor-facing note describing where tokens live, how each platform consumes them, and the expectation that new UI work pulls from tokens instead of hardcoding new colors.

## Acceptance criteria

- [ ] Documented convention exists describing the shared token source and how web/mobile each consume it
- [ ] Includes one concrete example: adding a new color token and using it on both platforms
- [ ] Linked from `apps/mobile/README.md` and an equivalent Frontend doc location
- [ ] No code behavior changes from this story

## Technical notes

- Pure documentation story — mirrors the existing convention used in `docs/polls/*` and `docs/operations/*`
- Keep it short — a convention note, not a full design-system doc

## Out of scope

- Enforcing the convention via lint rules/CI (a future story if drift becomes a recurring problem)
- Any new component library or framework adoption — that option (full cross-platform convergence via Tamagui/React Native Web) is explicitly deferred
