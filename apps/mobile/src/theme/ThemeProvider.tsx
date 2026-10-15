import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type PropsWithChildren } from 'react';
import { useColorScheme } from 'react-native';
import * as SecureStore from 'expo-secure-store';
import { colors, type ColorScheme, type ThemeColors } from './colors';

export type ThemePreference = 'system' | 'light' | 'dark';
const STORAGE_KEY = 'pop_theme_preference';
export function resolveTheme(preference: ThemePreference, systemScheme: string | null | undefined): ColorScheme {
  return preference === 'system' ? systemScheme === 'dark' ? 'dark' : 'light' : preference;
}
const ThemeContext = createContext({
  preference: 'system' as ThemePreference,
  scheme: 'light' as ColorScheme,
  colors: colors.light as ThemeColors,
  setPreference: async (_preference: ThemePreference): Promise<void> => undefined,
});

export function ThemeProvider({ children }: PropsWithChildren) {
  const systemScheme = useColorScheme();
  const [preference, updatePreference] = useState<ThemePreference>('system');
  const changed = useRef(false);
  const writes = useRef<Promise<void>>(Promise.resolve());
  useEffect(() => {
    let active = true;
    void SecureStore.getItemAsync(STORAGE_KEY).then(saved => {
      if (active && !changed.current && (saved === 'system' || saved === 'light' || saved === 'dark')) updatePreference(saved);
    }).catch(() => undefined);
    return () => { active = false; };
  }, []);
  const setPreference = useCallback((next: ThemePreference) => {
    changed.current = true;
    updatePreference(next);
    // Keep rapid changes ordered so the last choice is also the saved choice.
    const write = writes.current.catch(() => undefined).then(() => SecureStore.setItemAsync(STORAGE_KEY, next));
    writes.current = write;
    return write;
  }, []);
  const scheme = resolveTheme(preference, systemScheme);
  const value = useMemo(() => ({ preference, scheme, colors: colors[scheme], setPreference }), [preference, scheme, setPreference]);
  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export function useTheme() { return useContext(ThemeContext); }
