import { Platform, Pressable, Text, View } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';
import { useTheme } from '../theme';

export type PollTemplate = 'classic' | 'pulse' | 'spotlight';
export type PollReaction = 'big_yes' | 'thinking' | 'no_way' | 'wow';

export const templates: Array<{ id: PollTemplate; label: string; hint: string }> = [
  { id: 'pulse', label: 'PoP Split', hint: 'A bold two-sided debate' },
  { id: 'classic', label: 'Classic', hint: 'A quieter question' },
  { id: 'spotlight', label: 'Night', hint: 'High-contrast focus' },
];

export const reactions: Array<{ id: PollReaction; label: string; word: string; mark: string; color: string; ink: string }> = [
  { id: 'big_yes', label: 'Big yes', word: 'YES!', mark: '✳', color: '#DFFF69', ink: '#232334' },
  { id: 'thinking', label: 'Wait…', word: 'WAIT', mark: '?', color: '#D9C7FA', ink: '#27213D' },
  { id: 'no_way', label: 'No way', word: 'NOPE', mark: '×', color: '#B9EAF0', ink: '#172C3A' },
  { id: 'wow', label: 'Wow', word: 'WOW', mark: '✦', color: '#FFCE78', ink: '#392317' },
];

export function PoPmoji({ id, size = 46 }: { id: PollReaction; size?: number }) {
  const reaction = reactions.find(item => item.id === id)!;
  return <View accessibilityLabel={`PoP sticker: ${reaction.label}`} style={{ width: size, height: size, alignItems: 'center', justifyContent: 'center', transform: [{ rotate: id === 'thinking' ? '9deg' : '-9deg' }] }}>
    <View style={{ width: size * .88, height: size * .88, borderRadius: id === 'wow' ? size * .44 : size * .16, backgroundColor: reaction.color, alignItems: 'center', justifyContent: 'center', borderWidth: 1.5, borderColor: reaction.ink, shadowColor: '#000000', shadowOpacity: .18, shadowRadius: 3, shadowOffset: { width: 2, height: 3 }, elevation: 3 }}>
      <Text style={{ position: 'absolute', top: size * .04, right: size * .09, color: reaction.ink, fontSize: size * .22, fontWeight: '900', lineHeight: size * .23 }}>{reaction.mark}</Text>
      <Text adjustsFontSizeToFit numberOfLines={1} style={{ color: reaction.ink, fontSize: size * .23, fontWeight: '900', letterSpacing: -.7, marginTop: size * .10 }}>{reaction.word}</Text>
    </View>
  </View>;
}

export function CreativePollHero({ question, description, category, template, reaction, compact = false, showChoices = true }: {
  question: string; description?: string | null; category?: string | null;
  template?: PollTemplate | string | null; reaction?: PollReaction | string | null; compact?: boolean; showChoices?: boolean;
}) {
  const { colors: c } = useTheme();
  const selected = template === 'pulse' || template === 'spotlight' ? template : 'classic';
  const activeReaction = reactions.find(item => item.id === reaction);
  const split = selected === 'pulse';
  const night = selected === 'spotlight';
  const background = split ? '#F8F0E9' : night ? '#242037' : c.card;
  const ink = split ? '#24202C' : night ? '#FFFFFF' : c.foreground;
  const subdued = split ? '#514951' : night ? '#D6CDE5' : c.mutedForeground;
  const accent = split ? '#E96061' : night ? '#D6B7FF' : c.primary;
  const serif = Platform.OS === 'ios' ? 'Georgia' : 'serif';

  return <View style={{ backgroundColor: background, borderRadius: split ? 18 : 22, overflow: 'hidden', borderWidth: split ? 0 : 1, borderColor: night ? '#9F82D060' : c.border, minHeight: split ? (showChoices ? compact ? 242 : 298 : 196) : compact ? 155 : 190 }}>
    <View style={{ paddingHorizontal: compact ? 17 : 21, paddingTop: compact ? 16 : 19, paddingBottom: showChoices && split ? 14 : 20, flex: 1 }}>
      <View style={{ flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', gap: 8, paddingRight: activeReaction ? (compact ? 45 : 61) : 0 }}>
        <Text style={{ color: accent, fontSize: 10, fontWeight: '900', letterSpacing: 1.5, textTransform: 'uppercase', flexShrink: 1 }} numberOfLines={1}>{category || 'Your perspective'}</Text>
        <Text style={{ color: subdued, fontSize: 9, fontWeight: '800', letterSpacing: 1.2 }}>{split ? 'THE PUBLIC QUESTION' : 'PoP / YOUR VOICE'}</Text>
      </View>
      {activeReaction && <View pointerEvents="none" style={{ position: 'absolute', zIndex: 2, right: compact ? 9 : 12, top: compact ? 10 : 13 }}><PoPmoji id={activeReaction.id} size={compact ? 49 : 66} /></View>}
      <Text accessibilityRole="header" numberOfLines={compact ? 3 : 4} style={{ color: ink, fontFamily: serif, fontSize: compact ? 25 : 33, lineHeight: compact ? 29 : 38, fontWeight: '700', letterSpacing: -1.2, marginTop: compact ? 17 : 24, paddingRight: activeReaction ? (compact ? 23 : 34) : 0 }}>
        {question || 'Your question appears here'}
      </Text>
      {!!description && <Text numberOfLines={compact ? 2 : 3} style={{ color: subdued, fontSize: compact ? 12 : 14, lineHeight: compact ? 17 : 20, marginTop: 8 }}>{description}</Text>}
      {!split && <View style={{ width: 43, height: 4, borderRadius: 3, backgroundColor: accent, marginTop: 16 }} />}
    </View>
    {split && showChoices && <View accessibilityLabel="Poll choices: Against and Support" style={{ flexDirection: 'row', height: compact ? 63 : 82, borderTopWidth: 3, borderColor: '#25202A' }}>
      <View style={{ flex: 1, backgroundColor: '#DCCFFA', paddingHorizontal: compact ? 13 : 17, paddingVertical: compact ? 9 : 13, justifyContent: 'space-between' }}><Text style={{ color: '#463961', fontSize: 9, fontWeight: '900' }}>01  ↙</Text><Text style={{ color: '#28213D', fontFamily: serif, fontSize: compact ? 19 : 24, fontWeight: '700' }}>Against</Text></View>
      <View style={{ width: 3, backgroundColor: '#FFFFFF' }} />
      <View style={{ flex: 1, backgroundColor: '#FF9F90', paddingHorizontal: compact ? 13 : 17, paddingVertical: compact ? 9 : 13, justifyContent: 'space-between' }}><Text style={{ color: '#75382F', fontSize: 9, fontWeight: '900' }}>02  ↗</Text><Text style={{ color: '#3D201F', fontFamily: serif, fontSize: compact ? 19 : 24, fontWeight: '700' }}>Support</Text></View>
    </View>}
  </View>;
}

export function CreativeStylePicker({ template, reaction, onTemplate, onReaction }: {
  template: PollTemplate; reaction: PollReaction | null;
  onTemplate: (value: PollTemplate) => void; onReaction: (value: PollReaction | null) => void;
}) {
  const { colors: c } = useTheme();
  return <View style={{ gap: 18 }}>
    <View style={{ gap: 9 }}><Text style={{ color: c.foreground, fontSize: 15, fontWeight: '800' }}>The look</Text><View style={{ flexDirection: 'row', gap: 8 }}>{templates.map(item => <Pressable key={item.id} accessibilityRole="button" accessibilityLabel={`${item.label}: ${item.hint}`} accessibilityState={{ selected: template === item.id }} onPress={() => onTemplate(item.id)} style={{ flex: 1, minHeight: 99, borderRadius: 13, borderWidth: template === item.id ? 2 : 1, borderColor: template === item.id ? c.primary : c.border, overflow: 'hidden', backgroundColor: item.id === 'pulse' ? '#F8F0E9' : item.id === 'spotlight' ? '#242037' : c.secondary }}>
      <View style={{ flex: 1, flexDirection: 'row', alignItems: 'flex-end', padding: 8 }}><Text style={{ color: item.id === 'spotlight' ? '#FFF' : item.id === 'pulse' ? '#27202C' : c.foreground, fontFamily: Platform.OS === 'ios' ? 'Georgia' : 'serif', fontSize: 17, fontWeight: '700' }}>PoP.</Text></View>
      {item.id === 'pulse' && <View style={{ flexDirection: 'row', height: 9 }}><View style={{ flex: 1, backgroundColor: '#DCCFFA' }} /><View style={{ flex: 1, backgroundColor: '#FF9F90' }} /></View>}
      <Text style={{ color: item.id === 'spotlight' ? '#FFF' : item.id === 'pulse' ? '#27202C' : c.foreground, fontWeight: '800', fontSize: 11, padding: 7 }}>{item.label}</Text>
    </Pressable>)}</View><Text style={{ color: c.mutedForeground, fontSize: 12 }}>{templates.find(item => item.id === template)?.hint}</Text></View>
    <View style={{ gap: 9 }}><Text style={{ color: c.foreground, fontSize: 15, fontWeight: '800' }}>Add a reaction sticker</Text><View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8 }}><Pressable accessibilityRole="button" accessibilityState={{ selected: reaction === null }} onPress={() => onReaction(null)} style={{ minWidth: 58, minHeight: 72, alignItems: 'center', justifyContent: 'center', padding: 7, borderRadius: 12, borderWidth: reaction === null ? 2 : 1, borderColor: reaction === null ? c.primary : c.border }}><Ionicons name="close-outline" size={24} color={c.mutedForeground} /><Text style={{ color: c.mutedForeground, fontSize: 11 }}>None</Text></Pressable>{reactions.map(item => <Pressable key={item.id} accessibilityRole="button" accessibilityLabel={item.label} accessibilityState={{ selected: reaction === item.id }} onPress={() => onReaction(item.id)} style={{ alignItems: 'center', gap: 1, minWidth: 58, minHeight: 72, padding: 5, borderRadius: 12, borderWidth: reaction === item.id ? 2 : 1, borderColor: reaction === item.id ? c.primary : c.border }}><PoPmoji id={item.id} size={44} /><Text style={{ color: c.foreground, fontSize: 11, fontWeight: '700' }}>{item.label}</Text></Pressable>)}</View></View>
  </View>;
}
