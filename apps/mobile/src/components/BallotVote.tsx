import { useEffect, useMemo, useRef, useState } from 'react';
import { AccessibilityInfo, ActivityIndicator, Animated, PanResponder, Platform, Pressable, Text, useWindowDimensions, View } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';
import Svg, { Defs, LinearGradient, Line, Path, Stop } from 'react-native-svg';
import { useTheme } from '../theme';
import { measuredBallotTarget } from '../lib/ballot';
type Choice = { id: number; label: string; statement: string };
export function BallotVote({ choices, busy, cast, onDragChange, gestureChoiceId }: { choices: Choice[]; busy: boolean; cast: (id: number) => Promise<boolean>; onDragChange?: (value: boolean) => void; gestureChoiceId?: number | null }) {
  const { colors: c } = useTheme();
  const screen = useWindowDimensions();
  const [width, setWidth] = useState(300);
  const panelTop = 116;
  const panelHeight = Math.min(500, Math.max(370, screen.height * 0.52));
  const [selected, setSelected] = useState<number | null>(null);
  const [hover, setHover] = useState<number | null>(null);
  const [dragging, setDragging] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const reduced = useRef(false), locked = useRef(false), mounted = useRef(true);
  const position = useRef(new Animated.ValueXY()).current;
  const boxes = useRef<Array<View | null>>([]);
  const bounds = useRef<Array<{ x: number; y: number; width: number; height: number }>>([]);
  const origin = useRef({ x: 0, y: 0 });
  function measureBoxes() {
    boxes.current.forEach((box, index) => box?.measureInWindow((x, y, width, height) => { bounds.current[index] = { x, y, width, height }; }));
  }
  const opacity = useRef(new Animated.Value(1)).current;
  useEffect(() => {
    mounted.current = true;
    void AccessibilityInfo.isReduceMotionEnabled().then(value => { if (mounted.current) reduced.current = value; }).catch(() => undefined);
    const listener = AccessibilityInfo.addEventListener('reduceMotionChanged', value => { reduced.current = value; });
    return () => { mounted.current = false; listener.remove(); position.stopAnimation(); opacity.stopAnimation(); onDragChange?.(false); };
  }, [position, opacity, onDragChange]);
  function reset() {
    setDragging(false); setHover(null); onDragChange?.(false);
    Animated.parallel([Animated.timing(position, { toValue: { x: 0, y: 0 }, duration: reduced.current ? 0 : 220, useNativeDriver: true }), Animated.timing(opacity, { toValue: 1, duration: reduced.current ? 0 : 150, useNativeDriver: true })]).start();
  }
  async function submit(index: number) {
    if (locked.current || busy) return;
    locked.current = true; setSubmitting(true); setSelected(index); setHover(index); setDragging(false); onDragChange?.(false);
    Animated.sequence([
      Animated.timing(position, { toValue: { x: (index === 0 ? -1 : 1) * width / 4, y: 112 }, duration: reduced.current ? 0 : 280, useNativeDriver: true }),
      Animated.parallel([Animated.timing(position.y, { toValue: 238, duration: reduced.current ? 0 : 360, useNativeDriver: true }), Animated.timing(opacity, { toValue: 0, duration: reduced.current ? 0 : 360, useNativeDriver: true })]),
    ]).start();
    try {
      const success = await cast(choices[index].id);
      if (mounted.current && !success) { position.stopAnimation(); opacity.stopAnimation(); reset(); }
    } catch { if (mounted.current) reset(); }
    finally { locked.current = false; if (mounted.current) setSubmitting(false); }
  }
  const lastGestureChoice = useRef<number | null>(null);
  useEffect(() => {
    if (gestureChoiceId == null || gestureChoiceId === lastGestureChoice.current) return;
    const index = choices.findIndex(choice => choice.id === gestureChoiceId);
    if (index < 0) return;
    lastGestureChoice.current = gestureChoiceId;
    void submit(index);
  }, [gestureChoiceId]);
  // Keep the responder instance stable while hover/selection rerender the component.
  const handlers = useRef({ busy, submit, reset, onDragChange });
  handlers.current = { busy, submit, reset, onDragChange };
  const responder = useMemo(() => PanResponder.create({
    onStartShouldSetPanResponder: () => !handlers.current.busy && !locked.current,
    onMoveShouldSetPanResponder: () => !handlers.current.busy && !locked.current,
    onPanResponderGrant: () => {
      measureBoxes();
      position.stopAnimation(value => { origin.current = value; });
      setDragging(true); handlers.current.onDragChange?.(true);
    },
    onPanResponderMove: (event, gesture) => {
      position.setValue({ x: origin.current.x + gesture.dx, y: origin.current.y + gesture.dy });
      setHover(measuredBallotTarget(event.nativeEvent.pageX, event.nativeEvent.pageY, bounds.current));
    },
    onPanResponderRelease: (event) => {
      const target = measuredBallotTarget(event.nativeEvent.pageX, event.nativeEvent.pageY, bounds.current);
      if (target === null) handlers.current.reset(); else void handlers.current.submit(target);
    },
    onPanResponderTerminate: () => handlers.current.reset(),
    onPanResponderTerminationRequest: () => false,
    onShouldBlockNativeResponder: () => true,
  }), [position]);
  return <View style={{ marginBottom: 0 }}>
    <Text style={{ color: c.mutedForeground, textAlign: 'center', fontSize: 18, marginBottom: 4 }}>Pick it up. Pick a side.</Text>
    <View onLayout={event => setWidth(event.nativeEvent.layout.width)} style={{ height: panelTop + panelHeight, marginHorizontal: -24, overflow: 'hidden' }}>
      <Svg pointerEvents="none" width={width} height={panelHeight} viewBox="0 0 1000 1000" preserveAspectRatio="none" style={{ position: 'absolute', top: panelTop, left: 0, zIndex: 0 }}>
        <Defs>
          <LinearGradient id="againstPanel" x1="0" y1="0" x2="1" y2="1">
            <Stop offset="0" stopColor="#E8DEFF" />
            <Stop offset="1" stopColor="#C6B7EF" />
          </LinearGradient>
          <LinearGradient id="supportPanel" x1="0" y1="0" x2="1" y2="1">
            <Stop offset="0" stopColor="#FFB0A6" />
            <Stop offset="1" stopColor="#FF8075" />
          </LinearGradient>
        </Defs>
        <Path d="M 0 150 Q 0 32 150 32 L 345 32 Q 424 32 500 158 L 500 1000 L 0 1000 Z" fill="url(#againstPanel)" />
        <Path d="M 1000 150 Q 1000 32 850 32 L 655 32 Q 576 32 500 158 L 500 1000 L 1000 1000 Z" fill="url(#supportPanel)" />
        <Line x1="500" y1="158" x2="500" y2="1000" stroke="rgba(255,255,255,0.92)" strokeWidth="8" />
      </Svg>
      {choices.map((choice, index) => {
        const targeted = hover === index || selected === index;
        const panelWidth = width / 2;
        const ink = index === 0 ? '#292238' : '#2F1715';
        return <View ref={node => { boxes.current[index] = node; }} collapsable={false} onLayout={measureBoxes} key={choice.id} style={{ position: 'absolute', top: panelTop, left: index * panelWidth, width: panelWidth, height: panelHeight, zIndex: submitting ? 3 : 1 }}>
          <Pressable accessibilityRole="radio" accessibilityLabel={`${choice.label}: ${choice.statement}`} accessibilityState={{ checked: selected === index, disabled: busy || submitting }} disabled={busy || submitting} onPress={() => setSelected(index)} style={{ flex: 1, alignItems: 'center', paddingHorizontal: 20, paddingTop: 142 }}>
            <View pointerEvents="none" style={{ width: '65%', height: 9, borderRadius: 6, backgroundColor: '#201A27', marginBottom: 30, shadowColor: targeted ? '#FFFFFF' : '#000000', shadowOpacity: targeted ? 0.9 : 0.22, shadowRadius: targeted ? 12 : 3 }} />
            <Ionicons name={index === 0 ? 'business-outline' : 'leaf-outline'} size={29} color={ink} />
            <Text adjustsFontSizeToFit numberOfLines={1} style={{ textAlign: 'center', color: ink, fontFamily: Platform.OS === 'ios' ? 'Georgia' : 'serif', fontSize: 30, fontWeight: '600', marginTop: 8 }}>{choice.label}</Text>
            <Text numberOfLines={4} style={{ color: ink, opacity: 0.72, fontSize: 14, lineHeight: 20, textAlign: 'center', marginTop: 10 }}>{choice.statement}</Text>
          </Pressable>
        </View>;
      })}
      <Animated.View {...responder.panHandlers} accessible={false} style={{ position: 'absolute', top: 20, left: width / 2 - 52, width: 104, height: 138, zIndex: submitting ? 1 : 10, borderRadius: 7, backgroundColor: '#F5EFE5', borderWidth: 1, borderColor: '#FFFFFF', alignItems: 'center', justifyContent: 'center', gap: 10, shadowColor: '#000000', shadowOpacity: 0.28, shadowRadius: 14, shadowOffset: { width: 0, height: 10 }, opacity, transform: [...position.getTranslateTransform(), { rotate: submitting ? '0deg' : '-7deg' }, { scale: dragging && !reduced.current ? 1.07 : 1 }] }}>
        <Ionicons name="checkmark" size={42} color="#29252C" /><Text style={{ color: '#29252C', fontSize: 13 }}>Your vote</Text>
        <View style={{ position: 'absolute', right: 0, bottom: 0, borderTopWidth: 13, borderRightWidth: 13, borderTopColor: '#DDD3C3', borderRightColor: '#FDFBF7' }} />
      </Animated.View>
      {!busy && !submitting && selected !== null && <Pressable accessibilityRole="button" onPress={() => void submit(selected)} style={{ position: 'absolute', zIndex: 20, left: 22, right: 22, bottom: 22, backgroundColor: '#28232C', padding: 18, borderRadius: 24, alignItems: 'center', shadowColor: '#000000', shadowOpacity: 0.24, shadowRadius: 10, shadowOffset: { width: 0, height: 6 } }}><Text style={{ color: '#FFFFFF', fontSize: 16, fontWeight: '700' }}>Cast vote · {choices[selected].label} →</Text></Pressable>}
      {(busy || submitting) && <View accessibilityLiveRegion="polite" style={{ position: 'absolute', zIndex: 20, left: 22, right: 22, bottom: 22, backgroundColor: '#28232C', padding: 18, borderRadius: 24, alignItems: 'center', flexDirection: 'row', justifyContent: 'center', gap: 12 }}><ActivityIndicator color="#FFFFFF" /><Text style={{ color: '#FFFFFF' }}>Submitting your vote…</Text></View>}
    </View>
  </View>;
}
