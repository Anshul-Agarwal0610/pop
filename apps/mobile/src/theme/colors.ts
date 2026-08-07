import tokens from '../../../../design-tokens/tokens.json';

export type ColorScheme = 'light' | 'dark';

export interface ThemeColors {
  background: string;
  foreground: string;
  card: string;
  cardForeground: string;
  popover: string;
  popoverForeground: string;
  primary: string;
  primaryForeground: string;
  secondary: string;
  secondaryForeground: string;
  muted: string;
  mutedForeground: string;
  accent: string;
  accentForeground: string;
  destructive: string;
  destructiveForeground: string;
  border: string;
  input: string;
  ring: string;
  chart1: string;
  chart2: string;
  chart3: string;
  chart4: string;
  chart5: string;
  sidebar: string;
  sidebarForeground: string;
  sidebarPrimary: string;
  sidebarPrimaryForeground: string;
  sidebarAccent: string;
  sidebarAccentForeground: string;
  sidebarBorder: string;
  sidebarRing: string;
}

type TokenColorEntry = { oklch: string; hex: string };
type TokenColorSet = Record<keyof ThemeColors, TokenColorEntry>;

function toThemeColors(tokenSet: TokenColorSet): ThemeColors {
  const result = {} as ThemeColors;
  for (const key of Object.keys(tokenSet) as (keyof ThemeColors)[]) {
    result[key] = tokenSet[key].hex;
  }
  return result;
}

export const colors: Record<ColorScheme, ThemeColors> = {
  light: toThemeColors(tokens.color.light as TokenColorSet),
  dark: toThemeColors(tokens.color.dark as TokenColorSet),
};

export interface RadiusScale {
  sm: number;
  md: number;
  lg: number;
  xl: number;
}

export const radius: RadiusScale = tokens.radius;
