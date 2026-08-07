export { colors, radius } from './colors';
export type { ColorScheme, ThemeColors, RadiusScale } from './colors';

import { colors } from './colors';

/**
 * Default (light) theme. The app has no dark-mode screens yet
 * (see docs/design-system/US194-restyle-mobile-screens-with-shared-theme.md),
 * so screens should import `theme` directly until dark mode is scoped.
 */
export const theme = colors.light;
