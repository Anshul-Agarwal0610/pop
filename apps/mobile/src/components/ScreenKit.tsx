import Ionicons from '@expo/vector-icons/Ionicons';
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type { ComponentProps, PropsWithChildren } from 'react';
import { ActivityIndicator, KeyboardAvoidingView, Platform, Pressable, RefreshControl, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { useTheme, radius, type ThemeColors } from '../theme';

export const friendlyError = (error: unknown) => error instanceof Error ? error.message : 'Something went wrong. Please try again.';
export function useResource<T>(load: () => Promise<T>) {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const version = useRef(0);
  const refresh = useCallback(async () => {
    const id = ++version.current; setLoading(true); setError(null);
    try { const next = await load(); if (id === version.current) setData(next); }
    catch (e) { if (id === version.current) setError(friendlyError(e)); }
    finally { if (id === version.current) setLoading(false); }
  }, [load]);
  useEffect(() => { setData(null); void refresh(); return () => { ++version.current; }; }, [refresh]);
  return { data, setData, loading, error, refresh };
}
export function useAction() {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const locked = useRef(false);
  async function run(action: () => Promise<unknown>) {
    if (locked.current) return;
    locked.current = true; setBusy(true); setError(null);
    try { await action(); } catch (e) { setError(friendlyError(e)); }
    finally { locked.current = false; setBusy(false); }
  }
  return { busy, error, run };
}
export function Glyph({ name, color }: { name: ComponentProps<typeof Ionicons>['name']; color?: string }) {
  const { colors: c } = useTheme(); const ui = useUI();
 return <Ionicons name={name} size={24} color={color ?? c.primary} />; }
export function Card({ children }: PropsWithChildren) {
  const { colors: c } = useTheme(); const ui = useUI();
 return <View style={ui.card}>{children}</View>; }
export function Action({ title, onPress, busy = false, disabled = false, secondary = false }: { title: string; onPress: () => void; busy?: boolean; disabled?: boolean; secondary?: boolean }) {
  const { colors: c } = useTheme(); const ui = useUI();

  return <Pressable accessibilityRole="button" accessibilityState={{ busy, disabled: busy || disabled }} disabled={busy || disabled} onPress={onPress} style={({ pressed }) => [ui.button, secondary && ui.secondary, (busy || disabled || pressed) && { opacity: 0.5 }]}>{busy ? <ActivityIndicator color={secondary ? c.primary : c.primaryForeground} /> : <Text style={[ui.buttonText, secondary && { color: c.primary }]}>{title}</Text>}</Pressable>;
}
export function Field({ label, ...props }: ComponentProps<typeof TextInput> & { label: string }) {
  const { colors: c } = useTheme(); const ui = useUI();
 return <View style={ui.gap}><Text style={ui.label}>{label}</Text><TextInput accessibilityLabel={label} placeholderTextColor={c.mutedForeground} style={[ui.input, props.multiline && { minHeight: 92, textAlignVertical: 'top' }]} {...props} /></View>; }
export function Chips({ items, selected, onSelect }: { items: string[]; selected: string; onSelect: (value: string) => void }) {
  const { colors: c } = useTheme(); const ui = useUI();
 return <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={ui.row}>{items.map(item => <Pressable key={item} accessibilityRole="button" accessibilityState={{ selected: item === selected }} onPress={() => onSelect(item)} style={[ui.chip, selected === item && { backgroundColor: c.primary }]}><Text style={{ color: selected === item ? c.primaryForeground : c.secondaryForeground, fontSize: 13 }}>{item}</Text></Pressable>)}</ScrollView>; }
export function Progress({ value }: { value: number }) {
  const { colors: c } = useTheme(); const ui = useUI();
 const percent = Math.max(0, Math.min(100, Number.isFinite(value) ? value : 0)); return <View accessibilityRole="progressbar" accessibilityValue={{ min: 0, max: 100, now: percent }} style={ui.track}><View style={[ui.fill, { width: `${percent}%` }]} /></View>; }
export function ErrorText({ message }: { message?: string | null }) {
  const { colors: c } = useTheme(); const ui = useUI();
 return message ? <Text accessibilityRole="alert" style={ui.error}>{message}</Text> : null; }
export function Screen({ title, subtitle, back, children, loading = false, error, refresh, busy = false }: PropsWithChildren<{ title: string; subtitle?: string; back: () => void; loading?: boolean; error?: string | null; refresh?: () => void; busy?: boolean }>) {
  const { colors: c } = useTheme(); const ui = useUI();

  return <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : undefined} style={ui.shell}>
    <View style={ui.header}><Pressable accessibilityRole="button" accessibilityLabel="Back" disabled={busy} onPress={back} style={ui.back}><Glyph name="arrow-back" color={c.foreground} /></Pressable><Text style={ui.headerTitle}>{title}</Text>{refresh && <Pressable accessibilityRole="button" accessibilityLabel="Refresh" disabled={loading || busy} onPress={refresh} style={ui.back}><Glyph name="refresh-outline" /></Pressable>}</View>
    <ScrollView keyboardShouldPersistTaps="handled" contentContainerStyle={ui.content} refreshControl={refresh ? <RefreshControl refreshing={loading} onRefresh={refresh} tintColor={c.primary} enabled={!busy} /> : undefined}>
      {subtitle && <Text style={ui.subtitle}>{subtitle}</Text>}<ErrorText message={error} />{loading && <ActivityIndicator color={c.primary} />}{children}
    </ScrollView>
  </KeyboardAvoidingView>;
}
const createUI = (c: ThemeColors) => StyleSheet.create({
  shell: { flex: 1, backgroundColor: c.background }, header: { flexDirection: 'row', alignItems: 'center', paddingHorizontal: 8, paddingVertical: 4 }, back: { minHeight: 48, minWidth: 48, alignItems: 'center', justifyContent: 'center' }, headerTitle: { flex: 1, fontSize: 19, fontWeight: '700', color: c.foreground },
  content: { padding: 20, gap: 16, paddingBottom: 32 }, card: { backgroundColor: c.card, padding: 18, borderWidth: StyleSheet.hairlineWidth, borderColor: c.border, borderRadius: radius['3xl'], gap: 12 }, title: { color: c.foreground, fontSize: 21, lineHeight: 28, fontWeight: '700', letterSpacing: -0.4 }, subtitle: { color: c.mutedForeground, fontSize: 16, lineHeight: 25 }, body: { color: c.mutedForeground, fontSize: 14, lineHeight: 22 }, label: { color: c.foreground, fontSize: 14, fontWeight: '600' }, meta: { color: c.mutedForeground, fontSize: 12, lineHeight: 18 },
  row: { flexDirection: 'row', alignItems: 'center', gap: 10 }, spread: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 12 }, gap: { gap: 8 }, flex: { flex: 1 }, button: { padding: 14, minHeight: 48, backgroundColor: c.primary, borderRadius: radius.xl, alignItems: 'center', justifyContent: 'center' }, secondary: { backgroundColor: c.secondary }, buttonText: { color: c.primaryForeground, fontSize: 14, fontWeight: '700' }, input: { backgroundColor: c.background, borderColor: c.border, borderWidth: 1, borderRadius: radius.lg, minHeight: 50, padding: 14, color: c.foreground, fontSize: 16 }, chip: { backgroundColor: c.secondary, paddingHorizontal: 16, minHeight: 44, justifyContent: 'center', borderRadius: radius.full }, track: { height: 10, borderRadius: radius.full, backgroundColor: c.secondary, overflow: 'hidden' }, fill: { height: '100%', borderRadius: radius.full, backgroundColor: c.primary }, error: { color: c.destructive, fontSize: 14, lineHeight: 22 }, tag: { color: c.primary, fontSize: 12, fontWeight: '700' },
});

export function useUI() { const { colors } = useTheme(); return useMemo(() => createUI(colors), [colors]); }
