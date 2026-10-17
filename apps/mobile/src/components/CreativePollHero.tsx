import { Pressable, Text, View } from 'react-native';
import { useTheme } from '../theme';

export type PollTemplate = 'classic' | 'pulse' | 'spotlight';
export type PollReaction = 'big_yes' | 'thinking' | 'no_way' | 'wow';

export const templates: Array<{ id: PollTemplate; label: string; hint: string }> = [
  { id: 'classic', label: 'Classic', hint: 'Clear and calm' },
  { id: 'pulse', label: 'Pulse', hint: 'Bright energy' },
  { id: 'spotlight', label: 'Spotlight', hint: 'Bold contrast' },
];

export const reactions: Array<{ id: PollReaction; label: string; face: string; color: string }> = [
  { id: 'big_yes', label: 'Big yes', face: '⌣', color: '#FF6D7B' },
  { id: 'thinking', label: 'Thinking', face: '〰', color: '#AA89F8' },
  { id: 'no_way', label: 'No way', face: '—', color: '#6DBAD6' },
  { id: 'wow', label: 'Wow', face: '○', color: '#F3B45D' },
];

export function PoPmoji({ id, size = 46 }: { id: PollReaction; size?: number }) {
  const reaction = reactions.find(item => item.id === id)!;
  return <View accessibilityLabel={`PoPmoji: ${reaction.label}`} style={{ width: size, height: size, borderRadius: size / 2, backgroundColor: reaction.color, alignItems: 'center', justifyContent: 'center', transform: [{ rotate: id === 'thinking' ? '-10deg' : '8deg' }], borderWidth: 2, borderColor: '#FFFFFF99' }}>
    <View style={{ flexDirection: 'row', gap: size * .18, marginTop: size * .08 }}><View style={{ width: size * .075, height: size * .11, borderRadius: size, backgroundColor: '#251B32' }} /><View style={{ width: size * .075, height: size * .11, borderRadius: size, backgroundColor: '#251B32' }} /></View>
    <Text style={{ color: '#251B32', fontSize: size * .38, fontWeight: '900', height: size * .43, lineHeight: size * .42, marginTop: -size * .03 }}>{reaction.face}</Text>
  </View>;
}

export function CreativePollHero({ question, description, category, template, reaction, compact = false }: {
  question: string; description?: string | null; category?: string | null;
  template?: PollTemplate | string | null; reaction?: PollReaction | string | null; compact?: boolean;
}) {
  const { colors: c, scheme } = useTheme();
  const selected = template === 'pulse' || template === 'spotlight' ? template : 'classic';
  const backgroundColor = selected === 'pulse' ? scheme === 'dark' ? '#40212F' : '#FFE1E4' : selected === 'spotlight' ? '#252034' : c.card;
  const foreground = selected === 'spotlight' ? '#FFFFFF' : selected === 'pulse' && scheme === 'light' ? '#321D2B' : c.foreground;
  const accent = selected === 'pulse' ? '#EE526F' : selected === 'spotlight' ? '#D4B3FF' : c.primary;
  const activeReaction = reactions.find(item => item.id === reaction);
  return <View style={{ padding: compact ? 17 : 22, borderRadius: 24, backgroundColor, borderWidth: 1, borderColor: selected === 'classic' ? c.border : `${accent}55`, overflow: 'hidden', minHeight: compact ? 120 : 178, justifyContent: 'space-between', gap: 12 }}>
    {selected !== 'classic' && <View accessibilityElementsHidden style={{ position: 'absolute', width: compact ? 125 : 180, height: compact ? 125 : 180, borderRadius: 100, right: -43, top: -55, backgroundColor: `${accent}33` }} />}
    <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 10 }}>
      <Text style={{ color: accent, fontSize: 11, fontWeight: '800', letterSpacing: 1.5, textTransform: 'uppercase' }}>{category || 'Your perspective'}</Text>
      {activeReaction && <PoPmoji id={activeReaction.id} size={compact ? 38 : 50} />}
    </View>
    <View style={{ gap: 6 }}><Text style={{ color: foreground, fontSize: compact ? 22 : 29, lineHeight: compact ? 28 : 35, fontWeight: '800', letterSpacing: -.6 }}>{question || 'Your question appears here'}</Text>{!!description && <Text numberOfLines={compact ? 2 : 3} style={{ color: foreground, opacity: .75, fontSize: compact ? 13 : 15, lineHeight: compact ? 19 : 22 }}>{description}</Text>}</View>
    {selected !== 'classic' && <View style={{ height: 4, width: 42, borderRadius: 3, backgroundColor: accent }} />}
  </View>;
}

export function CreativeStylePicker({ template, reaction, onTemplate, onReaction }: {
  template: PollTemplate; reaction: PollReaction | null;
  onTemplate: (value: PollTemplate) => void; onReaction: (value: PollReaction | null) => void;
}) {
  const { colors: c } = useTheme();
  return <View style={{ gap: 17 }}>
    <View style={{ gap: 9 }}><Text style={{ color: c.foreground, fontSize: 15, fontWeight: '700' }}>Card style</Text><View style={{ flexDirection: 'row', gap: 8 }}>{templates.map(item => <Pressable key={item.id} accessibilityRole="button" accessibilityState={{ selected: template === item.id }} onPress={() => onTemplate(item.id)} style={{ flex: 1, minHeight: 80, padding: 9, borderRadius: 16, backgroundColor: item.id === 'pulse' ? '#FFE1E4' : item.id === 'spotlight' ? '#252034' : c.secondary, borderWidth: template === item.id ? 2 : 1, borderColor: template === item.id ? c.primary : c.border, justifyContent: 'flex-end' }}><Text style={{ color: item.id === 'spotlight' ? '#FFFFFF' : item.id === 'pulse' ? '#321D2B' : c.foreground, fontWeight: '800', fontSize: 13 }}>{item.label}</Text><Text style={{ color: item.id === 'spotlight' ? '#D5CADF' : item.id === 'pulse' ? '#674A58' : c.mutedForeground, fontSize: 10 }}>{item.hint}</Text></Pressable>)}</View></View>
    <View style={{ gap: 9 }}><Text style={{ color: c.foreground, fontSize: 15, fontWeight: '700' }}>PoPmoji reaction</Text><View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8 }}><Pressable accessibilityRole="button" accessibilityState={{ selected: reaction === null }} onPress={() => onReaction(null)} style={{ minWidth: 63, alignItems: 'center', justifyContent: 'center', padding: 7, borderRadius: 14, borderWidth: reaction === null ? 2 : 1, borderColor: reaction === null ? c.primary : c.border }}><Text style={{ color: c.mutedForeground, fontSize: 12 }}>None</Text></Pressable>{reactions.map(item => <Pressable key={item.id} accessibilityRole="button" accessibilityLabel={item.label} accessibilityState={{ selected: reaction === item.id }} onPress={() => onReaction(item.id)} style={{ alignItems: 'center', gap: 3, minWidth: 63, padding: 6, borderRadius: 14, borderWidth: reaction === item.id ? 2 : 1, borderColor: reaction === item.id ? c.primary : c.border }}><PoPmoji id={item.id} size={34} /><Text style={{ color: c.mutedForeground, fontSize: 10 }}>{item.label}</Text></Pressable>)}</View></View>
  </View>;
}
