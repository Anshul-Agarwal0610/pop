import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Alert, Animated, AppState, PanResponder, Pressable, Switch, Text, useWindowDimensions, Vibration, View } from 'react-native';
import { Action, Card, Chips, Field, Glyph, Progress, Screen, useUI, useAction, useResource } from '../components/ScreenKit';
import { featureApi as api } from '../lib/featuresApi';
import { useAuth } from '../context/AuthContext';
import type { ApiGameSession, ApiLeaderboardResponse, ApiPredictionOpportunity } from '../types/features';
import { useTheme } from '../theme';

type PredictionMode = 'Winner' | 'Split';
function optionSides(options: ApiPredictionOpportunity['poll']['options']) {
  const support = options.find(x => /support|up|yes|for/i.test(x.side || x.text)) ?? options[1];
  const against = options.find(x => /against|down|no|oppose/i.test(x.side || x.text)) ?? options[0];
  return { support, against };
}

function SwipePredictionGame({ mode, opportunities, back, refreshUser }: { mode: PredictionMode; opportunities: ApiPredictionOpportunity[]; back: () => void; refreshUser: () => Promise<unknown> }) {
  const { colors: c } = useTheme(); const ui = useUI(); const screen = useWindowDimensions();
  const [index, setIndex] = useState(0); const [percentage, setPercentage] = useState(65); const [busy, setBusy] = useState(false); const [error, setError] = useState('');
  const position = useRef(new Animated.ValueXY()).current;
  const current = opportunities[index];
  const rotation = position.x.interpolate({ inputRange: [-screen.width, 0, screen.width], outputRange: ['-10deg', '0deg', '10deg'], extrapolate: 'clamp' });
  const submit = useCallback(async (direction: 'left' | 'right') => {
    if (!current || busy) return;
    const sides = optionSides(current.poll.options); const option = direction === 'right' ? sides.support : sides.against;
    if (!option) return;
    setBusy(true); setError(''); Vibration.vibrate(28);
    try {
      await new Promise<void>((resolve, reject) => Animated.timing(position, { toValue: { x: direction === 'right' ? screen.width * 1.2 : -screen.width * 1.2, y: 0 }, duration: 220, useNativeDriver: true }).start(({ finished }) => finished ? resolve() : reject(new Error('Animation interrupted'))));
      await api.predict(current.poll.id, option.id, mode, mode === 'Split' ? percentage : undefined);
      position.setValue({ x: 0, y: 0 }); setIndex(value => value + 1); setPercentage(65); void refreshUser();
    } catch (e) { position.setValue({ x: 0, y: 0 }); setError(e instanceof Error ? e.message : 'Could not save this prediction.'); }
    finally { setBusy(false); }
  }, [busy, current, mode, percentage, position, refreshUser, screen.width]);
  const responder = useMemo(() => PanResponder.create({
    onMoveShouldSetPanResponder: (_, gesture) => !busy && Math.abs(gesture.dx) > 10 && Math.abs(gesture.dx) > Math.abs(gesture.dy),
    onPanResponderMove: (_, gesture) => position.setValue({ x: gesture.dx, y: gesture.dy * 0.12 }),
    onPanResponderRelease: (_, gesture) => Math.abs(gesture.dx) > 85 ? void submit(gesture.dx > 0 ? 'right' : 'left') : Animated.spring(position, { toValue: { x: 0, y: 0 }, useNativeDriver: true }).start(),
    onPanResponderTerminate: () => Animated.spring(position, { toValue: { x: 0, y: 0 }, useNativeDriver: true }).start(),
  }), [busy, position, submit]);
  if (!current) return <Screen title={mode === 'Winner' ? 'Predict the Winner' : 'Predict the Split'} back={back}><Card><Glyph name="checkmark-circle" /><Text style={ui.title}>Round complete</Text><Text style={ui.body}>You made {index} predictions. Results and XP appear after each poll closes.</Text><Action title="Back to games" onPress={back} /></Card></Screen>;
  const sides = optionSides(current.poll.options);
  return <Screen title={mode === 'Winner' ? 'Predict the Winner' : 'Predict the Split'} subtitle={`${index + 1} of ${opportunities.length} · Swipe left or right`} back={back} busy={busy} error={error}>
    <Animated.View {...responder.panHandlers} style={{ transform: [...position.getTranslateTransform(), { rotate: rotation }] }}>
      <View style={{ minHeight: Math.max(430, screen.height * .58), borderRadius: 30, backgroundColor: c.card, borderWidth: 1, borderColor: c.border, padding: 22, justifyContent: 'space-between', gap: 18 }}>
        <View style={ui.spread}><Text style={ui.tag}>{current.poll.category}</Text><Text style={ui.meta}>Locks {new Date(current.locksAt).toLocaleDateString()}</Text></View>
        <View style={{ gap: 12 }}><Text style={[ui.title, { fontSize: 27, lineHeight: 34 }]}>{current.poll.question}</Text><Text numberOfLines={3} style={ui.body}>{current.poll.description}</Text></View>
        {mode === 'Split' && <View style={{ gap: 14 }}><Text style={[ui.title, { textAlign: 'center', fontSize: 34 }]}>{percentage}%</Text><Text style={[ui.meta, { textAlign: 'center' }]}>Predicted winning share</Text><Pressable accessibilityRole="adjustable" accessibilityValue={{ min: 50, max: 95, now: percentage }} onPress={event => setPercentage(Math.round(50 + Math.max(0, Math.min(1, event.nativeEvent.locationX / Math.max(1, screen.width - 84))) * 45))} style={{ height: 36, justifyContent: 'center' }}><View style={{ height: 10, borderRadius: 5, backgroundColor: c.secondary }}><View style={{ width: `${(percentage - 50) / 45 * 100}%`, height: 10, borderRadius: 5, backgroundColor: c.primary }} /></View><View style={{ position: 'absolute', left: `${(percentage - 50) / 45 * 92}%`, width: 28, height: 28, borderRadius: 14, backgroundColor: c.primary, borderWidth: 3, borderColor: c.card }} /></Pressable><View style={ui.spread}><Action secondary title="− 1%" onPress={() => setPercentage(v => Math.max(50, v - 1))} /><Action secondary title="+ 1%" onPress={() => setPercentage(v => Math.min(95, v + 1))} /></View></View>}
        <View style={{ flexDirection: 'row', gap: 12 }}><View style={{ flex: 1, padding: 14, borderRadius: 20, backgroundColor: `${c.chart3}22`, alignItems: 'center', gap: 5 }}><Text style={{ color: c.chart3, fontSize: 22 }}>←</Text><Text style={ui.label}>Against</Text><Text numberOfLines={2} style={[ui.meta, { textAlign: 'center' }]}>{sides.against?.text}</Text></View><View style={{ flex: 1, padding: 14, borderRadius: 20, backgroundColor: `${c.primary}22`, alignItems: 'center', gap: 5 }}><Text style={{ color: c.primary, fontSize: 22 }}>→</Text><Text style={ui.label}>Support</Text><Text numberOfLines={2} style={[ui.meta, { textAlign: 'center' }]}>{sides.support?.text}</Text></View></View>
      </View>
    </Animated.View>
    <View style={{ flexDirection: 'row', gap: 12 }}><View style={{ flex: 1 }}><Action secondary title="← Against" disabled={busy} onPress={() => void submit('left')} /></View><View style={{ flex: 1 }}><Action title="Support →" disabled={busy} onPress={() => void submit('right')} /></View></View>
  </Screen>;
}

const gameLoad = async () => ({ modes: await api.modes(), active: await api.activeGame(), winnerOpportunities: await api.predictionOpportunities('Winner'), splitOpportunities: await api.predictionOpportunities('Split'), predictions: await api.predictions() });
export function GamesScreen({ back }: { back: () => void }) {
  const { colors: c } = useTheme(); const ui = useUI();

  const resource = useResource(gameLoad); const action = useAction();
  const { refreshUser } = useAuth();
  const [session, setSession] = useState<ApiGameSession | null>(null);
  const [predictionMode, setPredictionMode] = useState<PredictionMode | null>(null);
  const [timed, setTimed] = useState(true);
  const [selected, setSelected] = useState<number | null>(null);
  const [remaining, setRemaining] = useState<number | null>(null);
  const deadline = useRef<number | null>(null);
  const applySession = useCallback((value: ApiGameSession) => {
    setSession(value); setSelected(null);
    deadline.current = value.expiresAt ? Date.now() + Math.max(0, Date.parse(value.expiresAt) - Date.parse(value.serverNow)) : null;
    setRemaining(deadline.current ? Math.max(0, Math.ceil((deadline.current - Date.now()) / 1000)) : null);
  }, []);
  useEffect(() => {
    const timer = setInterval(() => { if (deadline.current) setRemaining(Math.max(0, Math.ceil((deadline.current - Date.now()) / 1000))); }, 1000);
    return () => clearInterval(timer);
  }, []);
  useEffect(() => {
    const subscription = AppState.addEventListener('change', state => { if (state === 'active' && session?.status === 'Active') void action.run(async () => applySession(await api.game(session.id))); });
    return () => subscription.remove();
  }, [session?.id, session?.status]);
  const expired = session?.status === 'Expired' || (session?.status === 'Active' && remaining === 0);
  if (predictionMode && resource.data) return <SwipePredictionGame mode={predictionMode} opportunities={predictionMode === 'Winner' ? resource.data.winnerOpportunities : resource.data.splitOpportunities} refreshUser={refreshUser} back={() => { setPredictionMode(null); void resource.refresh(); }} />;
  return <Screen title={session ? 'Opinion Sprint' : 'Play a little'} subtitle={session ? undefined : 'Quick rounds. Your opinions are never right or wrong.'} back={() => session && !action.busy ? (setSession(null), void resource.refresh()) : back()} busy={action.busy} refresh={session ? undefined : resource.refresh} loading={!session && resource.loading} error={action.error || (!session ? resource.error : null)}>
    {!session ? <>
      {resource.data?.active && <Card><Text style={ui.title}>A round is waiting for you</Text><Text style={ui.body}>{resource.data.active.votesCast} votes cast · {resource.data.active.remainingPolls} remaining</Text><Action title="Resume round" busy={action.busy} onPress={() => void action.run(async () => applySession(await api.game(resource.data!.active!.id)))} /></Card>}
      <Text style={ui.title}>Prediction games</Text>
      <Pressable accessibilityRole="button" onPress={() => setPredictionMode('Winner')} style={({ pressed }) => ({ borderRadius: 28, padding: 22, minHeight: 190, backgroundColor: c.primary, opacity: pressed ? .78 : 1, justifyContent: 'space-between' })}><Glyph name="swap-horizontal" color={c.primaryForeground} /><View><Text style={[ui.title, { color: c.primaryForeground, fontSize: 27 }]}>Predict the Winner</Text><Text style={[ui.body, { color: c.primaryForeground, opacity: .82 }]}>One poll at a time. Swipe right for Support, left for Against.</Text></View><Text style={{ color: c.primaryForeground, fontWeight: '800' }}>{resource.data?.winnerOpportunities.length ?? 0} ready · Play →</Text></Pressable>
      <Pressable accessibilityRole="button" onPress={() => setPredictionMode('Split')} style={({ pressed }) => ({ borderRadius: 28, padding: 22, minHeight: 190, backgroundColor: c.chart3, opacity: pressed ? .78 : 1, justifyContent: 'space-between' })}><Glyph name="analytics-outline" color={c.primaryForeground} /><View><Text style={[ui.title, { color: c.primaryForeground, fontSize: 27 }]}>Predict the Split</Text><Text style={[ui.body, { color: c.primaryForeground, opacity: .86 }]}>Set the winning percentage, then swipe toward the side you expect to lead.</Text></View><Text style={{ color: c.primaryForeground, fontWeight: '800' }}>{resource.data?.splitOpportunities.length ?? 0} ready · Play →</Text></Pressable>
      {!!resource.data?.predictions.length && <><Text style={ui.title}>Your predictions</Text>{resource.data.predictions.map(item => <Card key={`result-${item.id}`}><Text style={ui.tag}>{item.category} · {item.status}</Text><Text style={ui.label}>{item.question}</Text><Text style={ui.body}>Your prediction: {item.predictedOptionText}</Text>{item.status !== 'Pending' && <Text style={ui.body}>{item.winningOptionText ? `Crowd result: ${item.winningOptionText}` : 'No clear winner'}{item.status === 'Correct' ? ` · +${item.xpAwarded} XP` : ''}</Text>}<Text style={ui.meta}>{item.status === 'Pending' ? `Reveals after ${new Date(item.locksAt).toLocaleString()}` : `${item.totalVotes} participant votes`}</Text></Card>)}</>}
      <Card><View style={ui.spread}><Text style={ui.label}>Play against the clock</Text><Switch accessibilityLabel="Timed round" value={timed} onValueChange={setTimed} trackColor={{ true: c.primary }} /></View></Card>
      {resource.data?.modes.map(mode => <Card key={`${mode.mode}-${mode.category}`}><Glyph name="flash-outline" /><Text style={ui.title}>{mode.name}</Text><Text style={ui.body}>{mode.rules}</Text><Text style={ui.tag}>{mode.pollCount} polls · {mode.category} · +{mode.completionXp} completion XP</Text><Action title={mode.available ? 'Start a round' : 'More polls needed'} busy={action.busy} disabled={!mode.available || !!resource.data?.active} onPress={() => void action.run(async () => applySession(await api.startGame(mode.category, timed)))} /></Card>)}
      {!resource.loading && resource.data?.modes.length === 0 && <Text style={ui.body}>New game modes will appear here when available.</Text>}
    </> : session.status === 'Completed' && session.summary ? <>
      <Card><Glyph name="trophy" /><Text style={ui.title}>A round well played.</Text><Text style={ui.body}>{session.summary.votes} perspectives shared</Text><Text style={[ui.title, { fontSize: 36, color: c.primary }]}>+{session.summary.totalXpEarned} XP</Text><Text style={ui.meta}>{session.summary.voteXpEarned} voting XP + {session.summary.completionXpEarned} completion XP</Text></Card>
      {session.summary.achievementsUnlocked.map(badge => <Card key={badge.id}><Text style={ui.tag}>Achievement unlocked</Text><Text style={ui.title}>{badge.name}</Text><Text style={ui.body}>{badge.description}</Text></Card>)}
      {session.summary.challengeProgress.map(challenge => <Card key={challenge.challengeId}><Text style={ui.title}>{challenge.title}</Text><Text style={ui.body}>{challenge.currentVotes}/{challenge.requiredVotes} votes · {challenge.state}</Text></Card>)}
      <Action title="Back to games" onPress={() => { setSession(null); void resource.refresh(); }} />
    </> : expired || session.status === 'Abandoned' ? <Card><Text style={ui.title}>This round has ended</Text><Text style={ui.body}>Your completed votes remain recorded. Start a fresh round when you’re ready.</Text><Action title="Back to games" onPress={() => { setSession(null); void resource.refresh(); }} /></Card> : <>
      <View style={ui.spread}><Text style={ui.tag}>{session.category} · {session.votesCast}/{session.pollCount} votes</Text><Text style={ui.label}>{remaining === null ? 'At your pace' : `${Math.floor(remaining / 60)}:${String(remaining % 60).padStart(2, '0')}`}</Text></View><Progress value={session.pollCount ? session.votesCast / session.pollCount * 100 : 0} />
      {session.currentPoll ? <Card><Text style={ui.title}>{session.currentPoll.question}</Text><Text style={ui.body}>{session.currentPoll.description}</Text>{session.currentPoll.options.map(option => <Action key={option.id} title={`${selected === option.id ? '●' : '○'}  ${option.text}`} secondary disabled={action.busy} onPress={() => setSelected(option.id)} />)}<Action title="Confirm and continue" disabled={selected === null || expired} busy={action.busy} onPress={() => void action.run(async () => {
        try { const response = await api.gameVote(session.id, session.currentPosition, session.currentPoll!.id, selected!); applySession(response.session); await refreshUser(); }
        catch (error) { applySession(await api.game(session.id).catch(() => session)); throw error; }
      })} /></Card> : <Card><Text style={ui.title}>{session.remainingPolls === 0 ? 'Ready to finish' : 'Let’s refresh this round'}</Text><Action title={session.remainingPolls === 0 ? 'Complete round' : 'Reload round'} busy={action.busy} onPress={() => void action.run(async () => { applySession(session.remainingPolls === 0 ? await api.completeGame(session.id) : await api.game(session.id)); await refreshUser(); })} /></Card>}
    </>}
  </Screen>;
}

export function WellnessScreen({ back }: { back: () => void }) {
  const { colors: c } = useTheme(); const ui = useUI();

  const resource = useResource(api.wellness); const action = useAction();
  const [notes, setNotes] = useState<Record<number, string>>({});
  const [choices, setChoices] = useState<Record<number, number>>({});
  const [notice, setNotice] = useState('');
  async function exportData() {
    const { API_BASE_URL } = await import('../config/api');
    const { getToken } = await import('../lib/session');
    const response = await fetch(`${API_BASE_URL}/api/wellness/export.csv`, { headers: { Authorization: `Bearer ${await getToken()}` } });
    if (!response.ok) throw new Error('Could not export your check-ins. Please try again.');
    const { File, Paths } = await import('expo-file-system');
    const { isAvailableAsync, shareAsync } = await import('expo-sharing');
    if (!await isAvailableAsync()) throw new Error('File sharing is unavailable on this device.');
    const file = new File(Paths.cache, `pop-check-ins-${Date.now()}.csv`);
    try { file.create(); file.write(await response.text()); await shareAsync(file.uri, { mimeType: 'text/csv', dialogTitle: 'Export your private check-ins' }); }
    finally { if (file.exists) file.delete(); }
  }
  return <Screen title="Today’s check-in" subtitle="A few quick questions about your day." back={back} refresh={resource.refresh} loading={resource.loading} error={resource.error || action.error} busy={action.busy}>
    <Card><Glyph name="shield-checkmark-outline" /><Text style={ui.title}>Private by design</Text><Text style={ui.body}>Your answers stay out of public feeds, rankings, and profiles. A brand may sponsor a question, but it will not see who answered or your private notes. You can export or delete your history anytime.</Text></Card>
    {!!notice && <Text accessibilityLiveRegion="polite" style={ui.tag}>{notice}</Text>}
    {resource.data && <Card><Text style={ui.title}>{resource.data.insight.totalCheckIns} moments of reflection</Text><Text style={ui.body}>{resource.data.insight.mostCommonResponse ? `Most common response: ${resource.data.insight.mostCommonResponse}` : 'Your first check-in is a good place to start.'}</Text></Card>}
    {resource.data?.polls.map(poll => <Card key={poll.id}>{poll.isSponsored && poll.sponsorName && <Text style={ui.tag}>Sponsored by {poll.sponsorName}</Text>}<Text style={ui.title}>{poll.question}</Text><Text style={ui.body}>{poll.description}</Text>{poll.options.map(option => <Action key={option.id} secondary disabled={action.busy} title={`${choices[poll.id] === option.id ? '●' : '○'}  ${option.text}`} onPress={() => setChoices(current => ({ ...current, [poll.id]: option.id }))} />)}<Field label="Private note (optional)" value={notes[poll.id] ?? ''} onChangeText={text => setNotes(current => ({ ...current, [poll.id]: text }))} multiline maxLength={1000} /><Action title="Submit today’s answer" disabled={choices[poll.id] === undefined} busy={action.busy} onPress={() => void action.run(async () => { await api.checkIn(poll.id, choices[poll.id], notes[poll.id] ?? ''); setNotes(current => ({ ...current, [poll.id]: '' })); setChoices(current => { const next = { ...current }; delete next[poll.id]; return next; }); setNotice('Answer saved for today.'); await resource.refresh(); })} /></Card>)}
    {!resource.loading && resource.data?.polls.length === 0 && <Card><Glyph name="checkmark-circle-outline" /><Text style={ui.title}>You’re checked in for today</Text><Text style={ui.body}>New daily questions will be ready tomorrow.</Text></Card>}
    {!!resource.data?.history.length && <><Text style={ui.title}>Your recent reflections</Text>{resource.data.history.map(item => <Card key={item.id}><Text style={ui.meta}>{new Date(item.createdAt).toLocaleDateString()}</Text><Text style={ui.label}>{item.question}</Text><Text style={ui.body}>{item.optionText}</Text>{item.note && <Text style={ui.body}>{item.note}</Text>}</Card>)}<Action title="Export check-ins as CSV" secondary busy={action.busy} onPress={() => void action.run(exportData)} /><Action title="Delete my check-in history" secondary busy={action.busy} onPress={() => Alert.alert('Delete your check-ins?', 'This permanently deletes all your wellness responses and notes.', [{ text: 'Keep them', style: 'cancel' }, { text: 'Delete', style: 'destructive', onPress: () => void action.run(async () => { await api.deleteWellness(); await resource.refresh(); setNotice('Your check-in history has been deleted.'); }) }])} /></>}
  </Screen>;
}

export function RankingsScreen({ back }: { back: () => void }) {
  const { colors: c } = useTheme(); const ui = useUI();

  const [period, setPeriod] = useState('This week'); const [offset, setOffset] = useState(0);
  const { user } = useAuth();
  const load = useCallback(async (): Promise<ApiLeaderboardResponse> => {
    if (period === 'Friends') {
      const board = await api.friendsBoard();
      return { rows: board.items.map(row => ({ rank: row.rank, id: row.user.id, username: row.user.username, displayName: row.user.displayName, periodXp: row.xp, lifetimeXp: row.xp, level: 0, badges: [] })), currentUser: null, period: 'Weekly', periodStartUtc: board.weekStartUtc, periodEndUtc: board.weekEndUtc, nextResetAtUtc: board.weekEndUtc, limit: 20, offset: 0, hasMore: false };
    }
    return api.rankings(period === 'This week' ? 'weekly' : 'allTime', offset);
  }, [period, offset]);
  const resource = useResource(load);
  const rows = resource.data?.rows ?? [];
  const own = resource.data?.currentUser;
  return <Screen title="Every voice counts" subtitle="A little friendly competition." back={back} refresh={resource.refresh} loading={resource.loading} error={resource.error}>
    <Chips items={['This week', 'All time', 'Friends']} selected={period} onSelect={value => { setPeriod(value); setOffset(0); }} />
    {resource.data?.nextResetAtUtc && <Text style={ui.meta}>Next reset: {new Date(resource.data.nextResetAtUtc).toLocaleString()}</Text>}
    {rows.map(row => <Card key={row.id}><View style={ui.row}><Text style={[ui.title, { color: c.primary, width: 45 }]}>#{row.rank}</Text><View style={ui.flex}><Text style={ui.label}>{row.displayName}{row.id === user?.id ? ' · You' : ''}</Text><Text style={ui.meta}>@{row.username}</Text></View><Text style={ui.label}>{row.periodXp} XP</Text></View></Card>)}
    {own && !rows.some(row => row.id === own.id) && <Card><Text style={ui.tag}>Your position</Text><Text style={ui.title}>#{own.rank} · {own.periodXp} XP</Text></Card>}
    {!resource.loading && rows.length === 0 && <Card><Text style={ui.title}>There’s room for your voice</Text><Text style={ui.body}>Earn eligible XP to appear in these rankings.</Text></Card>}
    <View style={ui.row}>{offset > 0 && <Action title="Previous" secondary onPress={() => setOffset(value => Math.max(0, value - 20))} />}{resource.data?.hasMore && <Action title="Next page" secondary onPress={() => setOffset(value => value + 20)} />}</View>
  </Screen>;
}
