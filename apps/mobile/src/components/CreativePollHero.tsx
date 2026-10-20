import { useRef, useState } from 'react';
import { Image, PanResponder, Platform, Pressable, Text, View } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';
import { useTheme } from '../theme';
import { PoPmojiArt, reactions, type PollReaction } from './PoPmojiArt';
import { movePoPmoji, normalizePoPmojiPlacement, type PoPmojiPlacement } from '../lib/popmojiPlacement';

export type PollTemplate = 'classic' | 'pulse' | 'spotlight';
export type { PollReaction } from './PoPmojiArt';

export const templates: Array<{ id: PollTemplate; label: string; hint: string }> = [
  { id: 'pulse', label: 'PoP Split', hint: 'A bold two-sided debate' },
  { id: 'classic', label: 'Classic', hint: 'A quieter question' },
  { id: 'spotlight', label: 'Night', hint: 'High-contrast focus' },
];

export function PoPmoji({ id, size = 46 }: { id: PollReaction; size?: number }) {
  return <View style={{ width: size, height: size, shadowColor: '#000000', shadowOpacity: .18, shadowRadius: 4, shadowOffset: { width: 2, height: 4 }, elevation: 3 }}><PoPmojiArt id={id} size={size} /></View>;
}

export function PositionedPoPmoji({ id, placement, compact = false, editable = false, onChange }: {
  id: PollReaction; placement?: Partial<PoPmojiPlacement> | null; compact?: boolean;
  editable?: boolean; onChange?: (value: PoPmojiPlacement) => void;
}) {
  const [bounds, setBounds] = useState({ width: 0, height: 0 });
  const normalized = normalizePoPmojiPlacement(placement);
  const current = useRef(normalized); current.current = normalized;
  const start = useRef(normalized);
  const boundsRef = useRef(bounds); boundsRef.current = bounds;
  const onChangeRef = useRef(onChange); onChangeRef.current = onChange;
  const sizeRef = useRef(0);
  const responder = useRef(PanResponder.create({
    onStartShouldSetPanResponder: () => editable,
    onMoveShouldSetPanResponder: () => editable,
    onPanResponderGrant: () => { start.current = current.current; },
    onPanResponderMove: (_, gesture) => onChangeRef.current?.(movePoPmoji(start.current, gesture.dx, gesture.dy, boundsRef.current.width, boundsRef.current.height, sizeRef.current)),
    onPanResponderTerminationRequest: () => false,
  })).current;
  const size = (compact ? 56 : 76) * normalized.scale;
  sizeRef.current = size;
  return <View pointerEvents="box-none" onLayout={event => setBounds(event.nativeEvent.layout)} style={{ position: 'absolute', zIndex: 3, top: 0, bottom: 0, left: 0, right: 0 }}>
    <View {...(editable ? responder.panHandlers : {})} accessibilityLabel={`${reactions.find(item => item.id === id)?.label} sticker${editable ? ', drag to move' : ''}`} style={{ position: 'absolute', left: normalized.x * Math.max(0, bounds.width - size), top: normalized.y * Math.max(0, bounds.height - size), width: size, height: size }}>
      <PoPmoji id={id} size={size} />
    </View>
  </View>;
}

export function CreativePollHero({ question, description, category, template, reaction, reactionPlacement, onReactionPlacementChange, mediaUri, mediaType, onMediaLoad, compact = false, showChoices = true }: {
  question: string; description?: string | null; category?: string | null;
  template?: PollTemplate | string | null; reaction?: PollReaction | string | null; reactionPlacement?: Partial<PoPmojiPlacement> | null;
  onReactionPlacementChange?: (value: PoPmojiPlacement) => void; mediaUri?: string | null; mediaType?: string | null;
  onMediaLoad?: () => void; compact?: boolean; showChoices?: boolean;
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
      <View style={{ flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', gap: 8, paddingRight: activeReaction && !mediaUri ? (compact ? 45 : 61) : 0 }}>
        <Text style={{ color: accent, fontSize: 10, fontWeight: '900', letterSpacing: 1.5, textTransform: 'uppercase', flexShrink: 1 }} numberOfLines={1}>{category || 'Your perspective'}</Text>
        <Text style={{ color: subdued, fontSize: 9, fontWeight: '800', letterSpacing: 1.2 }}>{split ? 'THE PUBLIC QUESTION' : 'PoP / YOUR VOICE'}</Text>
      </View>
      {mediaUri && <View style={{ height: compact ? 175 : 215, marginTop: 13, borderRadius: 14, overflow: 'hidden', backgroundColor: night ? '#403650' : '#DDD2DC' }}>
        <Image accessibilityLabel="Poll cover" source={{ uri: mediaUri }} resizeMode="cover" onLoad={onMediaLoad} style={{ width: '100%', height: '100%' }} />
        {mediaType === 'video' && <View pointerEvents="none" style={{ position: 'absolute', bottom: 12, left: 12, borderRadius: 20, backgroundColor: '#211B27DD', paddingHorizontal: 10, paddingVertical: 6, flexDirection: 'row', alignItems: 'center', gap: 4 }}><Ionicons name="play" size={13} color="#FFFFFF" /><Text style={{ color: '#FFFFFF', fontWeight: '800', fontSize: 11 }}>VIDEO</Text></View>}
        {activeReaction && <PositionedPoPmoji id={activeReaction.id} placement={reactionPlacement} compact={compact} editable={!!onReactionPlacementChange} onChange={onReactionPlacementChange} />}
      </View>}
      {activeReaction && !mediaUri && <PositionedPoPmoji id={activeReaction.id} placement={reactionPlacement} compact={compact} editable={!!onReactionPlacementChange} onChange={onReactionPlacementChange} />}
      <Text accessibilityRole="header" numberOfLines={compact ? 3 : 4} style={{ color: ink, fontFamily: serif, fontSize: compact ? 25 : 33, lineHeight: compact ? 29 : 38, fontWeight: '700', letterSpacing: -1.2, marginTop: compact ? 17 : 24, paddingRight: activeReaction && !mediaUri ? (compact ? 23 : 34) : 0 }}>
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
    <View style={{ gap: 9 }}>
      <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' }}><Text style={{ color: c.foreground, fontSize: 15, fontWeight: '800' }}>Choose a PoPmoji</Text><Pressable accessibilityRole="button" accessibilityState={{ selected: reaction === null }} onPress={() => onReaction(null)} style={{ flexDirection: 'row', alignItems: 'center', gap: 3, padding: 5 }}><Ionicons name="close-outline" size={17} color={c.mutedForeground} /><Text style={{ color: c.mutedForeground, fontSize: 12 }}>No sticker</Text></Pressable></View>
      <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8 }}>{reactions.map(item => <Pressable key={item.id} accessibilityRole="button" accessibilityLabel={item.label} accessibilityState={{ selected: reaction === item.id }} onPress={() => onReaction(item.id)} style={{ width: '48%', minHeight: 105, padding: 10, borderRadius: 15, borderWidth: reaction === item.id ? 2 : 1, borderColor: reaction === item.id ? c.primary : c.border, backgroundColor: c.card, flexDirection: 'row', alignItems: 'center', gap: 5 }}><PoPmoji id={item.id} size={58} /><View style={{ flex: 1 }}><Text style={{ color: c.foreground, fontSize: 13, fontWeight: '800' }}>{item.label}</Text><Text style={{ color: c.mutedForeground, fontSize: 11, marginTop: 3 }}>{item.caption}</Text></View></Pressable>)}</View>
    </View>
  </View>;
}
