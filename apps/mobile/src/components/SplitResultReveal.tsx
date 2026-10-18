import { useEffect, useRef } from 'react';
import { AccessibilityInfo, Animated, Platform, Text, View } from 'react-native';
import { resultPercent } from '../lib/pollPresentation';
import type { ApiPollOption } from '../types/poll';
import { useTheme } from '../theme';

type Choice = ApiPollOption & { label: string; statement: string };

export function SplitResultReveal({ choices, totalVotes, userVotedOptionId, creatorView, deadline }: {
  choices: Choice[]; totalVotes: number; userVotedOptionId: number | null | undefined; creatorView: boolean; deadline: string;
}) {
  const { colors: c } = useTheme();
  const progress = useRef(new Animated.Value(0)).current;
  const against = choices[0];
  const support = choices[1];
  const againstPercent = resultPercent(against.voteCount, totalVotes);
  const supportPercent = totalVotes > 0 ? 100 - againstPercent : 0;
  const serif = Platform.OS === 'ios' ? 'Georgia' : 'serif';

  useEffect(() => {
    let mounted = true;
    void AccessibilityInfo.isReduceMotionEnabled().then(reduced => {
      if (!mounted) return;
      Animated.timing(progress, { toValue: 1, duration: reduced ? 0 : 650, useNativeDriver: false }).start();
    }).catch(() => progress.setValue(1));
    return () => { mounted = false; progress.stopAnimation(); };
  }, [progress]);

  return <View accessibilityLiveRegion="polite" style={{ borderRadius: 19, overflow: 'hidden', backgroundColor: '#F7F0E9' }}>
    <View style={{ paddingHorizontal: 20, paddingTop: 20, paddingBottom: 16 }}>
      <Text style={{ color: '#E55F60', fontSize: 10, fontWeight: '900', letterSpacing: 1.6 }}>THE SPLIT  ✳</Text>
      <Text style={{ color: '#24202C', fontFamily: serif, fontSize: 29, lineHeight: 34, marginTop: 8, fontWeight: '700' }}>{totalVotes === 0 ? 'The conversation starts here.' : 'The people have spoken.'}</Text>
      <Text style={{ color: '#514951', fontSize: 13, marginTop: 5 }}>{totalVotes} {totalVotes === 1 ? 'vote' : 'votes'} · {deadline}</Text>
    </View>
    <View style={{ flexDirection: 'row', minHeight: 175, borderTopWidth: 3, borderTopColor: '#24202C' }}>
      {[against, support].map((choice, index) => <View key={choice.id} style={{ flex: 1, backgroundColor: index === 0 ? '#DCCFFA' : '#FF9F90', paddingVertical: 18, paddingHorizontal: 15, borderLeftWidth: index === 1 ? 3 : 0, borderLeftColor: '#FFFFFF', justifyContent: 'space-between' }}>
        <Text style={{ color: index === 0 ? '#514069' : '#803E35', fontSize: 10, fontWeight: '900', letterSpacing: 1.3 }}>{index === 0 ? '01  ↙' : '02  ↗'}</Text>
        <View><Text style={{ color: '#24202C', fontFamily: serif, fontSize: 42, lineHeight: 46, fontWeight: '700', letterSpacing: -2 }}>{index === 0 ? againstPercent : supportPercent}%</Text><Text style={{ color: '#24202C', fontFamily: serif, fontSize: 23, fontWeight: '700' }}>{choice.label}</Text></View>
        <Text style={{ color: '#4D4151', fontSize: 11, fontWeight: choice.id === userVotedOptionId ? '900' : '500' }}>{choice.id === userVotedOptionId ? 'YOUR VOTE  ✓' : `${choice.voteCount} ${choice.voteCount === 1 ? 'vote' : 'votes'}`}</Text>
      </View>)}
    </View>
    <View accessibilityRole="progressbar" accessibilityLabel="Support percentage" accessibilityValue={{ min: 0, max: 100, now: supportPercent }} style={{ flexDirection: 'row', height: 9, backgroundColor: '#E1D7DF' }}>
      <Animated.View style={{ width: progress.interpolate({ inputRange: [0, 1], outputRange: ['0%', `${againstPercent}%`] }), backgroundColor: '#B499E6' }} />
      <Animated.View style={{ width: progress.interpolate({ inputRange: [0, 1], outputRange: ['0%', `${supportPercent}%`] }), backgroundColor: '#F17869' }} />
    </View>
    <Text style={{ color: c.mutedForeground, fontSize: 12, lineHeight: 18, paddingHorizontal: 19, paddingVertical: 15, backgroundColor: c.card }}>{creatorView ? 'Results update as people vote. You cannot vote on a poll you created.' : 'Results reflect participants in this poll.'}</Text>
  </View>;
}
