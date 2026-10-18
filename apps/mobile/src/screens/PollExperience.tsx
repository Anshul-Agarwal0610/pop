import { BallotVote } from '../components/BallotVote';
import { CreativePollHero } from '../components/CreativePollHero';
import { SplitResultReveal } from '../components/SplitResultReveal';
import { ballotChoices } from '../lib/ballot';
import Ionicons from '@expo/vector-icons/Ionicons';
import { isRunningInExpoGo } from 'expo';
import { useVideoPlayer, VideoView } from 'expo-video';
import { WebView } from 'react-native-webview';
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { ActivityIndicator, Alert, BackHandler, FlatList, Image, Linking, Platform, Pressable, RefreshControl, ScrollView, StyleSheet, Text, TextInput, Vibration, View } from 'react-native';
import type { ComponentProps, ReactNode } from 'react';
import { useAuth } from '../context/AuthContext';
import { ApiRequestError, notificationsApi, votesApi } from '../lib/api';
import { track } from '../lib/analytics/client';
import { getExpoPushToken } from '../lib/pushNotifications';
import { isPollClosed, nextUnvotedPoll, pollDeadline, resultPercent } from '../lib/pollPresentation';
import type { ApiPoll, VoteReward } from '../types/poll';
import { useTheme, radius, spacing, type ThemeColors } from '../theme';
import { UserAvatar } from '../components/UserAvatar';
import { categories as pollCategories, featureApi } from '../lib/featuresApi';
import { CreatePollScreen, NotificationsScreen, ChallengesScreen, AchievementsScreen, HistoryScreen, MyPollsScreen, PreferencesScreen, ReportScreen, EditProfileScreen } from './ActivityScreens';
import { usePollShare } from '../components/PollShareProvider';
import { GamesScreen, WellnessScreen, RankingsScreen } from './PlayScreens';
import { CommunityScreen } from './CommunityScreen';
import { API_BASE_URL } from '../config/api';
import { youtubeVideoId } from '../lib/youtube';
import { useDirectionalShake } from '../lib/directionalShake';

type Tab = 'Home' | 'Explore' | 'Ranks' | 'Profile' | 'More';
export type Feature = 'Create poll' | 'My polls' | 'Notifications' | 'Challenges' | 'Achievements' | 'Voted polls' | 'Preferences' | 'Games' | 'Community' | 'Wellness' | 'Report' | 'Edit profile';
type IconName = ComponentProps<typeof Ionicons>['name'];
const tabIcons: Record<Tab, IconName> = { Home: 'home-outline', Explore: 'search-outline', Ranks: 'trophy-outline', Profile: 'person-outline', More: 'grid-outline' };
const count = (value: number) => value.toLocaleString();
const samplePoll: ApiPoll = {
  id: -1001, question: '[Sample ballot test] Pop should use drag-and-drop ballots.',
  description: 'Development-only sample. Drop your ballot into Support or Against. Votes stay in this session and do not award XP.',
  category: 'General', isTrending: false, isActive: true, expiresAt: '2099-01-01T00:00:00Z', createdAt: '2026-09-26T00:00:00Z',
  totalVotes: 0, createdByUserId: null, createdByUsername: null, createdByDisplayName: null,
  sourceType: 'sample', sourceUrl: null, thumbnailUrl: null, isAIGenerated: false, hasVoted: false, userVotedOptionId: null,
  creativeTemplate: 'pulse', reactionId: 'big_yes', resultVisibility: 'AfterVote',
  options: [{ id: -1002, pollId: -1001, text: 'Use drag-and-drop ballots', side: 'Support', voteCount: 0, votePercentage: 0 }, { id: -1003, pollId: -1001, text: 'Keep standard voting controls', side: 'Against', voteCount: 0, votePercentage: 0 }],
};

function Icon({ name, color, size = 22 }: { name: IconName; color?: string; size?: number }) {
  const { colors: c } = useTheme(); const s = useMemo(() => createStyles(c), [c]);

  return <Ionicons name={name} color={color ?? c.mutedForeground} size={size} />;
}

function Button({ title, onPress, disabled = false, busy = false, secondary = false }: { title: string; onPress: () => void; disabled?: boolean; busy?: boolean; secondary?: boolean }) {
  const { colors: c } = useTheme(); const s = useMemo(() => createStyles(c), [c]);

  return <Pressable accessibilityRole="button" accessibilityState={{ disabled: disabled || busy, busy }} disabled={disabled || busy} onPress={onPress}
    style={({ pressed }) => [s.button, secondary && s.secondaryButton, (disabled || busy) && s.disabled, pressed && s.pressed]}>
    {busy ? <ActivityIndicator color={c.primaryForeground} /> : <Text style={[s.buttonText, secondary && s.secondaryText]}>{title}</Text>}
  </Pressable>;
}

function StateCard({ title, copy, children }: { title: string; copy: string; children?: ReactNode }) {
  const { colors: c } = useTheme(); const s = useMemo(() => createStyles(c), [c]);

  return <View style={s.state}><Icon name="sparkles-outline" color={c.primary} size={32} /><Text style={s.cardTitle}>{title}</Text><Text style={[s.body, s.center]}>{copy}</Text>{children}</View>;
}

function Category({ name }: { name: string }) {
  const { colors: c } = useTheme(); const s = useMemo(() => createStyles(c), [c]);

  return <View style={s.pill}><Icon name="pricetag-outline" size={13} color={c.secondaryForeground} /><Text style={s.pillText}>{name || 'Community'}</Text></View>;
}

function PollArtwork() {
  const { colors: c } = useTheme(); const s = useMemo(() => createStyles(c), [c]);

  return <View style={s.art} accessibilityElementsHidden importantForAccessibility="no-hide-descendants">
    <View style={s.artCircle} /><View style={s.ballot}><View style={s.ballotLine} /><View style={s.artRow}><Icon name="checkmark-circle" color={c.primary} size={22} /><View style={s.ballotLine} /></View><View style={s.artRow}><Icon name="ellipse-outline" color={c.chart3} size={22} /><View style={s.ballotLine} /></View></View>
  </View>;
}

function mediaUrl(value: string) {
  if (/^https?:\/\//i.test(value)) return value;
  return `${API_BASE_URL}${value.startsWith('/') ? '' : '/'}${value}`;
}

function PollVideo({ uri, detail = false }: { uri: string; detail?: boolean }) {
  const player = useVideoPlayer(uri, instance => { instance.loop = false; });
  return <VideoView
    accessibilityLabel="Poll video"
    nativeControls
    contentFit="cover"
    player={player}
    style={detail ? { width: '100%', aspectRatio: 16 / 9, borderRadius: 20 } : { width: '100%', aspectRatio: 16 / 9, borderRadius: 16 }}
  />;
}

function PollMedia({ poll, detail = false }: { poll: ApiPoll; detail?: boolean }) {
  const [failed, setFailed] = useState(false);
  const youtubeId = youtubeVideoId(poll.sourceUrl);
  const source = poll.thumbnailUrl || (youtubeId ? `https://i.ytimg.com/vi/${youtubeId}/hqdefault.jpg` : null);
  if (!source || failed) return null;
  const uri = mediaUrl(source);
  if (detail && youtubeId) return <YouTubeMedia videoId={youtubeId} thumbnail={uri} />;
  if (poll.mediaType?.toLowerCase() === 'video') return <PollVideo uri={uri} detail={detail} />;
  return <Image accessibilityLabel="Poll image" source={{ uri }} resizeMode="cover" onError={() => setFailed(true)} style={detail ? { width: '100%', aspectRatio: 16 / 9, borderRadius: 20 } : { width: '100%', aspectRatio: 16 / 9, borderRadius: 16 }} />;
}

function YouTubeMedia({ videoId, thumbnail }: { videoId: string; thumbnail: string }) {
  const { colors: c } = useTheme();
  const [playing, setPlaying] = useState(false);
  const frame = { width: '100%' as const, aspectRatio: 16 / 9, borderRadius: 20, overflow: 'hidden' as const, backgroundColor: '#000' };
  if (playing) return <View style={frame}><WebView
    accessibilityLabel="YouTube video player"
    allowsFullscreenVideo
    allowsInlineMediaPlayback
    mediaPlaybackRequiresUserAction
    originWhitelist={['https://*']}
    source={{ uri: `https://www.youtube.com/embed/${videoId}?playsinline=1&rel=0&autoplay=1` }}
    style={{ flex: 1, backgroundColor: '#000' }}
  /></View>;
  return <Pressable accessibilityRole="button" accessibilityLabel="Play YouTube video" onPress={() => setPlaying(true)} style={frame}>
    <Image source={{ uri: thumbnail }} resizeMode="cover" style={{ width: '100%', height: '100%' }} />
    <View style={{ position: 'absolute', inset: 0, alignItems: 'center', justifyContent: 'center', backgroundColor: '#00000026' }}>
      <View style={{ width: 68, height: 48, borderRadius: 14, backgroundColor: '#FF0033', alignItems: 'center', justifyContent: 'center' }}><Icon name="play" color={c.primaryForeground} size={28} /></View>
    </View>
  </Pressable>;
}

function PollPreview({ poll, onOpen }: { poll: ApiPoll; onOpen: () => void }) {
  const { colors: c } = useTheme(); const s = useMemo(() => createStyles(c), [c]);
  const creative = !!(poll.creativeTemplate || poll.reactionId);
  return <View style={s.card}>
    <View style={s.between}><Category name={poll.category} /><Text style={s.meta}>{pollDeadline(poll)}</Text></View>
    {poll.remixedFromPollId && <Text style={s.meta}>↗ Remixed conversation</Text>}
    {creative ? <CreativePollHero question={poll.question} description={poll.description} category={poll.category} template={poll.creativeTemplate} reaction={poll.reactionId} reactionPlacement={{ x: poll.reactionX ?? undefined, y: poll.reactionY ?? undefined, scale: poll.reactionScale ?? undefined }} compact showChoices={!!ballotChoices(poll.options)} /> : <Text style={s.cardTitle}>{poll.question}</Text>}
    <PollMedia poll={poll} />
    {!creative && <View style={s.previewBody}><Text numberOfLines={3} style={[s.body, s.flex]}>{poll.description || 'A fresh perspective starts with you. Where do you stand?'}</Text>{!poll.thumbnailUrl && !youtubeVideoId(poll.sourceUrl) && <PollArtwork />}</View>}
    <View style={s.row}><Icon name="people-outline" size={17} /><Text style={s.meta}>{poll.canViewResults === false ? 'Results hidden' : `${count(poll.totalVotes)} votes`}</Text>{poll.hasVoted && <Text style={s.votedTag}>Voted ✓</Text>}</View>
    <Button title={poll.hasVoted || isPollClosed(poll) ? poll.canViewResults === false ? 'View vote' : 'View results' : 'Vote now  →'} onPress={onOpen} />
  </View>;
}

export function PollExperience() {
  const sharePoll = usePollShare();
  const { colors: c } = useTheme(); const s = useMemo(() => createStyles(c), [c]);

  const { user, signOut, applyVoteReward, refreshUser } = useAuth();
  const [tab, setTab] = useState<Tab>('Home');
  const [feature, setFeature] = useState<Feature | null>(null);
  const feedRequest = useRef(0);
  const [polls, setPolls] = useState<ApiPoll[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [feedError, setFeedError] = useState<string | null>(null);
  const [category, setCategory] = useState('For you');
  const [query, setQuery] = useState('');
  const [collection, setCollection] = useState<'All' | 'Trending' | 'Latest' | 'Closed'>('All');
  const [detail, setDetail] = useState<ApiPoll | null>(null);
  const [remixSource, setRemixSource] = useState<ApiPoll | null>(null);
  const [selected, setSelected] = useState<number | null>(null);
  const [reward, setReward] = useState<VoteReward | null>(null);
  const [voting, setVoting] = useState(false);
  const [ballotDragging, setBallotDragging] = useState(false);
  const voteLock = useRef(false);
  const [voteError, setVoteError] = useState<string | null>(null);
  const [shakeChoiceId, setShakeChoiceId] = useState<number | null>(null);
  const shakePromptOpen = useRef(false);
  const pushToken = useRef<string | null>(null);
  const roundId = useRef('');
  const [, tick] = useState(0);

  const loadPolls = useCallback(async (refresh = false) => {
    refresh ? setRefreshing(true) : setLoading(true);
    setFeedError(null);
    const requestId = ++feedRequest.current;
    try {
      const data = tab === 'Explore' ? await featureApi.browse(query, category === 'For you' ? '' : category) : await featureApi.personalized();
      if (requestId === feedRequest.current) setPolls(typeof __DEV__ !== 'undefined' && __DEV__ ? [samplePoll, ...data] : data);
    }
    catch (error) { if (requestId === feedRequest.current) setFeedError(error instanceof Error ? error.message : 'Could not load polls. Please try again.'); }
    finally { if (requestId === feedRequest.current) { setLoading(false); setRefreshing(false); } }
  }, [tab, query, category]);

  useEffect(() => {
    if (tab !== 'Home' && tab !== 'Explore') return;
    const timer = setTimeout(() => { void loadPolls(); }, tab === 'Explore' ? 300 : 0);
    return () => { clearTimeout(timer); ++feedRequest.current; };
  }, [loadPolls, tab]);
  useEffect(() => { if (tab === 'Profile') void refreshUser().catch(() => undefined); }, [tab, refreshUser]);
  useEffect(() => { const timer = setInterval(() => tick(value => value + 1), 30_000); return () => clearInterval(timer); }, []);
  useEffect(() => {
    if (isRunningInExpoGo()) return;
    let active = true;
    void getExpoPushToken().then(async token => {
      if (!token || !active) return;
      pushToken.current = token;
      await notificationsApi.registerDeviceToken(token, Platform.OS);
    }).catch(() => undefined);
    return () => { active = false; };
  }, []);

  const back = useCallback(() => {
    if (voteLock.current) return true;
    if (feature === 'Report') { setFeature(null); return true; }
    if (detail) { setDetail(null); setReward(null); setVoteError(null); return true; }
    if (feature) { setFeature(null); setRemixSource(null); return true; }
    if (tab !== 'Home') { setTab('Home'); return true; }
    return false;
  }, [detail, feature, tab]);
  useEffect(() => { const listener = BackHandler.addEventListener('hardwareBackPress', back); return () => listener.remove(); }, [back]);

  function openPoll(poll: ApiPoll) {
    setDetail(poll); setSelected(null); setReward(null); setVoteError(null); setShakeChoiceId(null); shakePromptOpen.current = false;
    if (poll.id !== samplePoll.id) void featureApi.poll(poll.id).then(fresh => {
      if (!voteLock.current) setDetail(current => current?.id === poll.id ? fresh : current);
    }).catch(() => undefined);
    roundId.current = `mobile-${Date.now()}-${poll.id}`;
    if (poll.id !== samplePoll.id && !poll.hasVoted && !isPollClosed(poll)) void track('game_round_started', { round_id: roundId.current, surface: 'feed', category: poll.category }, `feed:${poll.id}`);
  }

  async function confirmVote(optionId = selected): Promise<boolean> {
    if (!detail || optionId === null || voteLock.current || detail.hasVoted || isPollClosed(detail)) return false;
    voteLock.current = true; setVoting(true); setVoteError(null);
    try {
      if (detail.id === samplePoll.id && typeof __DEV__ !== 'undefined' && __DEV__) {
        await new Promise(resolve => setTimeout(resolve, 700));
        const updated = { ...detail, hasVoted: true, userVotedOptionId: optionId, canViewResults: true, totalVotes: 1,
          options: detail.options.map(option => ({ ...option, voteCount: option.id === optionId ? 1 : 0, votePercentage: option.id === optionId ? 100 : 0 })) };
        setDetail(updated);
        setPolls(current => current.map(poll => poll.id === updated.id ? updated : poll));
        return true;
      }
      const [response] = await Promise.all([votesApi.cast(detail.id, optionId), new Promise(resolve => setTimeout(resolve, 700))]);
      const updated = { ...response.poll, hasVoted: true, userVotedOptionId: optionId };
      setDetail(updated); setPolls(current => current.map(poll => poll.id === updated.id ? updated : poll));
      setReward(response.reward); applyVoteReward(response.reward);
      void track('game_round_completed', { round_id: roundId.current, surface: 'feed', outcome: 'voted', xp_awarded: response.reward.xpAwarded }, `feed:${detail.id}:completed`);
      return true;
    } catch (error) {
      if (error instanceof ApiRequestError && error.status === 409) {
        try {
          const updated = await featureApi.poll(detail.id);
          setDetail(updated);
          setPolls(current => current.map(poll => poll.id === updated.id ? updated : poll));
          return true;
        } catch {
          setVoteError('Your vote was already recorded. Refresh to view the results.');
          return false;
        }
      }
      setVoteError(error instanceof Error ? error.message : 'Your vote could not be sent. Please try again.');
      return false;
    }
    finally { voteLock.current = false; setVoting(false); }
  }

  async function logout() {
    try {
      if (pushToken.current) await notificationsApi.disableDeviceToken(pushToken.current).catch(() => undefined);
      await signOut();
    } catch { Alert.alert('Could not sign out', 'Please try again.'); }
  }

  const motionChoices = detail ? ballotChoices(detail.options) : null;
  const motionVotingActive = Boolean(user && detail && motionChoices && detail.createdByUserId !== user.id && !detail.hasVoted && !isPollClosed(detail) && !voting && !ballotDragging);
  useDirectionalShake(motionVotingActive, direction => {
    if (!motionChoices || shakePromptOpen.current) return;
    const choice = direction === 'right' ? motionChoices[1] : motionChoices[0];
    shakePromptOpen.current = true;
    Vibration.vibrate(45);
    Alert.alert(`${choice.label}?`, `Cast your vote for ${choice.label}?`, [
      { text: 'Cancel', style: 'cancel', onPress: () => { shakePromptOpen.current = false; } },
      { text: 'Cast vote', onPress: () => { shakePromptOpen.current = false; setShakeChoiceId(choice.id); } },
    ], { cancelable: true, onDismiss: () => { shakePromptOpen.current = false; } });
  });

  if (!user) return null;
  const categories = ['For you', ...pollCategories];
  const visible = polls.filter(poll => {
    if (category !== 'For you' && poll.category !== category) return false;
    if (tab !== 'Explore') return true;
    if (!`${poll.question} ${poll.category}`.toLowerCase().includes(query.toLowerCase())) return false;
    if (collection === 'Trending') return poll.isTrending && !isPollClosed(poll);
    if (collection === 'Latest') return !isPollClosed(poll) && Date.now() - Date.parse(poll.createdAt) <= 7 * 86_400_000;
    if (collection === 'Closed') return isPollClosed(poll);
    return true;
  }).sort((a, b) => tab === 'Home'
    ? Number(a.hasVoted) - Number(b.hasVoted)
    : collection === 'Latest' ? Date.parse(b.createdAt) - Date.parse(a.createdAt) : 0);

  if (feature === 'Report' && detail) return <ReportScreen pollId={detail.id} back={() => setFeature(null)} />;
  if (detail) {
    const creatorView = detail.createdByUserId === user.id;
    const canVote = !creatorView && !detail.hasVoted && !isPollClosed(detail);
    const results = (detail.canViewResults ?? (creatorView || detail.hasVoted || isPollClosed(detail))) && !canVote;
    const next = nextUnvotedPoll(polls, detail.id);
    const choices = ballotChoices(detail.options);
    const activeBallot = canVote && Boolean(choices);
    return <View style={s.shell}>
      <View style={s.detailHeader}>
        <Pressable accessibilityRole="button" accessibilityLabel="Back to feed" disabled={voting} onPress={back} style={s.iconButton}><Icon name="arrow-back" color={c.foreground} /></Pressable>
        <Text style={s.headerTitle}>{creatorView ? 'Poll performance' : results ? 'Results' : canVote ? 'Today’s debate' : 'Vote counted'}</Text>
        <Pressable accessibilityRole="button" accessibilityLabel="Share poll question" style={s.iconButton} onPress={() => sharePoll(detail)}><Icon name="share-outline" color={c.foreground} /></Pressable>
      </View>
      <ScrollView scrollEnabled={!ballotDragging && !voting} contentContainerStyle={[s.detailContent, activeBallot && s.ballotDetailContent]} showsVerticalScrollIndicator={false}>
        {results && !choices && !creatorView && <View style={s.success}><Icon name="checkmark-circle" color={c.accent} /><Text style={s.successText}>{detail.hasVoted ? 'Your vote is in' : 'This poll has closed'}</Text></View>}
        <View style={s.between}><Category name={detail.category} /><Text style={s.meta}>{pollDeadline(detail)}</Text></View>
        {detail.creativeTemplate || detail.reactionId ? <CreativePollHero question={detail.question} description={detail.description} category={detail.category} template={detail.creativeTemplate} reaction={detail.reactionId} reactionPlacement={{ x: detail.reactionX ?? undefined, y: detail.reactionY ?? undefined, scale: detail.reactionScale ?? undefined }} showChoices={false} /> : <Text accessibilityRole="header" style={[s.question, choices && { fontFamily: Platform.OS === "ios" ? "Georgia" : "serif", fontWeight: "400" }]}>{detail.question}</Text>}
        <PollMedia poll={detail} detail />
        {detail.id === samplePoll.id && <Text style={s.caption}>LOCAL DEMO · No real votes or XP · Refresh the feed to try again</Text>}
        {(!results || creatorView) && !detail.creativeTemplate && !detail.reactionId && <Text style={s.context}>{detail.description || 'Every perspective counts. Where do you stand?'}</Text>}
        {!!detail.sourceUrl && <Pressable accessibilityRole="link" onPress={() => void Linking.openURL(detail.sourceUrl!)}><Text style={s.sourceLink}>Read the original source ↗</Text></Pressable>}
        {detail.remixedFromPollId && detail.remixedFromQuestion && <Pressable accessibilityRole="link" onPress={() => void featureApi.poll(detail.remixedFromPollId!).then(openPoll).catch(() => Alert.alert('Original poll unavailable', 'This poll is no longer available to view.'))}><Text style={s.sourceLink}>Remixed from {detail.remixedFromCreatorName || 'a PoP poll'}: {detail.remixedFromQuestion} ↗</Text></Pressable>}
        {detail.id !== samplePoll.id && !detail.isPrivate && !detail.isWellness && detail.moderationStatus === 'Published' && <Pressable accessibilityRole="button" accessibilityLabel="Remix this poll" onPress={() => { setRemixSource(detail); setDetail(null); setFeature('Create poll'); }}><Text style={s.sourceLink}>Remix this poll ↗</Text></Pressable>}
        {activeBallot && detail.id !== samplePoll.id && <Pressable accessibilityRole="button" disabled={voting} onPress={() => setFeature('Report')}><Text style={[s.meta, { textAlign: 'right' }]}>Report poll</Text></Pressable>}
        {results && choices && !creatorView && <View accessibilityLiveRegion="polite" style={{ alignItems: 'center', gap: 12, paddingVertical: 22 }}>
          <View style={{ width: 72, height: 72, borderRadius: 36, backgroundColor: c.primary, alignItems: 'center', justifyContent: 'center' }}><Icon name={detail.hasVoted ? 'checkmark' : 'time-outline'} color={c.primaryForeground} size={40} /></View>
          <Text style={{ color: c.foreground, fontSize: 30, fontFamily: Platform.OS === 'ios' ? 'Georgia' : 'serif' }}>{detail.hasVoted ? 'Vote counted' : 'Poll closed'}</Text>
          {detail.hasVoted && <Text style={s.context}>You chose <Text style={{ color: c.primary }}>{choices.find(option => option.id === detail.userVotedOptionId)?.label}</Text></Text>}
        </View>}
        {creatorView && <View style={s.success}><Icon name="stats-chart" color={c.primary} /><Text style={s.successText}>{count(detail.totalVotes)} votes received · {detail.moderationStatus && detail.moderationStatus !== 'Published' ? detail.moderationStatus.replace(/([a-z])([A-Z])/g, '$1 $2') : isPollClosed(detail) ? 'Closed' : pollDeadline(detail)}</Text></View>}
        {results && choices ? <SplitResultReveal choices={choices} totalVotes={detail.totalVotes} userVotedOptionId={detail.userVotedOptionId} creatorView={creatorView} deadline={pollDeadline(detail)} /> : results ? <View style={s.card}>
          {detail.options.map((option, index) => {
            const percent = resultPercent(option.voteCount, detail.totalVotes);
            const mine = option.id === detail.userVotedOptionId;
            return <View key={option.id} style={s.resultRow}>
              <View style={s.between}><View style={[s.row, s.flex]}><Text style={[s.optionTitle, s.shrink]}>{choices?.find(choice => choice.id === option.id)?.label ?? option.text}</Text>{mine && <Text style={s.votedTag}>Your vote</Text>}</View><Text style={s.optionTitle}>{percent}%</Text></View>
              <View accessibilityRole="progressbar" accessibilityLabel={option.text} accessibilityValue={{ min: 0, max: 100, now: percent }} style={s.track}><View style={[s.fill, { width: `${percent}%`, backgroundColor: choices ? (choices[1].id === option.id ? c.primary : c.chart3) : index === 0 ? c.primary : c.chart3 }]} /></View>
              <Text style={s.meta}>{count(option.voteCount)} votes</Text>
            </View>;
          })}
          <Text style={s.body}>{count(detail.totalVotes)} total votes · {pollDeadline(detail)}</Text>
          <Text style={s.caption}>{creatorView ? 'Results update as people vote. You cannot vote on a poll you created.' : 'Results reflect participants in this poll.'}</Text>
        </View> : !canVote ? <View style={s.card}><Text style={s.cardTitle}>{detail.hasVoted ? 'Your vote is in' : 'Poll closed'}</Text><Text style={s.body}>Results will appear when this poll closes. Come back after {new Date(detail.expiresAt).toLocaleString()}.</Text></View> : choices ? <BallotVote key={detail.id} choices={choices} busy={voting} cast={confirmVote} onDragChange={setBallotDragging} gestureChoiceId={shakeChoiceId} /> : <View style={s.options}>
          {detail.options.map(option => <Pressable key={option.id} accessibilityRole="radio" accessibilityState={{ checked: selected === option.id, disabled: voting }} disabled={voting} onPress={() => setSelected(option.id)} style={({ pressed }) => [s.option, selected === option.id && s.optionSelected, pressed && s.pressed]}>
            <Icon name={selected === option.id ? 'radio-button-on' : 'radio-button-off'} color={selected === option.id ? c.primary : c.chart3} size={28} />
            <View style={s.flex}><Text style={s.optionTitle}>{option.text}</Text>{['up', 'support', 'against'].includes(option.text.toLowerCase()) && <Text style={s.body}>{['up', 'support'].includes(option.text.toLowerCase()) ? 'I support this' : 'I oppose this'}</Text>}</View>
          </Pressable>)}
          {detail.options.length === 0 && <Text style={s.body}>There are no voting options for this poll.</Text>}
        </View>}
        {reward && <View style={[s.card, s.row]}><View style={s.rewardIcon}><Icon name="star" color={c.chart4} size={27} /></View><View style={s.flex}><Text style={s.cardTitle}>+{reward.xpAwarded} XP</Text><Text style={s.body}>{reward.leveledUp ? `You reached level ${reward.progression.level}!` : `${reward.streak} day streak · ${reward.todayComplete ? 'Today’s activity complete' : 'Keep the conversation going'}`}</Text></View></View>}
        {detail.id !== samplePoll.id && !creatorView && !activeBallot && <Button title="Report this poll" secondary disabled={voting} onPress={() => setFeature("Report")} />}
        {voteError && <Text accessibilityRole="alert" style={s.error}>{voteError}</Text>}
        {canVote && detail.resultVisibility === 'Immediate' && detail.canViewResults && <View style={s.card}><Text style={s.cardTitle}>Live results</Text>{detail.options.map(option => <Text key={option.id} style={s.body}>{option.text}: {resultPercent(option.voteCount, detail.totalVotes)}%</Text>)}</View>}
        {canVote && !choices && <Text style={[s.caption, s.center]}>One vote. Your perspective.</Text>}
      </ScrollView>
      <View style={[s.footer, canVote && choices ? { display: 'none' } : undefined]}>
        {creatorView ? <><Button title="Share poll" onPress={() => sharePoll(detail)} /><Button title="Back to my polls" secondary onPress={back} /></> : !canVote ? <><Button title={next ? 'Next poll  →' : 'Back to feed'} onPress={() => next ? openPoll(next) : back()} />{next && <Button title="Back to feed" secondary onPress={back} />}</> : <Button title="Confirm vote" busy={voting} disabled={selected === null || detail.options.length === 0} onPress={() => void confirmVote()} />}
      </View>
    </View>;
  }

  const closeFeature = () => { setFeature(null); setRemixSource(null); void refreshUser().catch(() => undefined); };
  if (feature === 'Create poll') return <CreatePollScreen back={closeFeature} openPoll={openPoll} remixSource={remixSource} />;
  if (feature === 'My polls') return <MyPollsScreen back={closeFeature} openPoll={openPoll} />;
  if (feature === 'Notifications') return <NotificationsScreen back={closeFeature} openPoll={openPoll} />;
  if (feature === 'Achievements') return <AchievementsScreen back={closeFeature} />;
  if (feature === 'Challenges') return <ChallengesScreen back={closeFeature} browse={value => { setCategory(value || 'For you'); setQuery(''); setFeature(null); setTab('Explore'); }} />;
  if (feature === 'Voted polls') return <HistoryScreen back={closeFeature} openPoll={openPoll} />;
  if (feature === 'Preferences') return <PreferencesScreen back={closeFeature} />;
  if (feature === 'Edit profile') return <EditProfileScreen back={closeFeature} />;
  if (feature === 'Games') return <GamesScreen back={closeFeature} />;
  if (feature === 'Community') return <CommunityScreen back={closeFeature} />;
  if (feature === 'Wellness') return <WellnessScreen back={closeFeature} />;
  const bottomNavigation = <View accessibilityRole="tablist" style={s.nav}>{(Object.keys(tabIcons) as Tab[]).map(item => <Pressable key={item} accessibilityRole="tab" accessibilityState={{ selected: tab === item }} onPress={() => setTab(item)} style={s.navItem}><View style={[s.navIcon, tab === item && s.navIconActive]}><Icon name={tabIcons[item]} color={tab === item ? c.primary : c.mutedForeground} size={23} /></View><Text style={[s.navText, tab === item && s.navTextActive]}>{item}</Text></Pressable>)}</View>;
  if (tab === 'Ranks') return <View style={s.shell}><View style={s.flex}><RankingsScreen back={() => setTab('Home')} /></View>{bottomNavigation}</View>;
  return <View style={s.shell}>
    <View style={s.header}><View style={s.row}><View style={s.logoTile}><Image source={require('../../assets/pop-mark.png')} resizeMode="contain" style={s.logoMark} /></View><Text style={s.logo}>PoP</Text></View><View style={s.row}><Pressable accessibilityRole="button" accessibilityLabel="Create a poll" onPress={() => { setRemixSource(null); setFeature("Create poll"); }} style={s.iconButton}><Icon name="add-circle-outline" color={c.primary} /></Pressable><Pressable accessibilityRole="button" accessibilityLabel="Notifications" onPress={() => setFeature("Notifications")} style={s.iconButton}><Icon name="notifications-outline" color={c.foreground} /></Pressable><Pressable accessibilityRole="button" accessibilityLabel="Open profile" onPress={() => setTab('Profile')} style={s.avatar}><UserAvatar user={user} /></Pressable></View></View>
    {(tab === 'Home' || tab === 'Explore') && <FlatList data={loading ? [] : visible} keyExtractor={poll => String(poll.id)} contentContainerStyle={s.feed} showsVerticalScrollIndicator={false}
      refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => void loadPolls(true)} tintColor={c.primary} colors={[c.primary]} />}
      ListHeaderComponent={<View style={s.feedIntro}>
        <View><Text accessibilityRole="header" style={s.heading}>{tab === 'Home' ? 'Your voice matters.' : 'Find your next debate.'}</Text><Text style={s.context}>{tab === 'Home' ? 'A fresh take starts with you.' : 'Explore the conversations that interest you.'}</Text></View>
        {tab === 'Home' ? <Pressable accessibilityRole="button" accessibilityLabel="View your progress" onPress={() => setTab('Profile')} style={s.streak}><Icon name="flame" color={c.primary} size={28} /><View style={s.flex}><Text style={s.strong}>{user.streak} day streak</Text><View style={s.dots}>{Array.from({ length: 7 }, (_, index) => <View key={index} style={[s.dot, index < Math.min(user.streak, 7) && s.dotActive]} />)}</View></View><Text style={s.meta}>Level {user.progression.level} · {count(user.xp)} XP</Text><Icon name="chevron-forward" size={16} /></Pressable> : <><View style={s.search}><Icon name="search-outline" /><TextInput accessibilityLabel="Search all polls" placeholder="Search questions or categories" placeholderTextColor={c.mutedForeground} value={query} onChangeText={setQuery} style={s.searchInput} /></View><ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={s.chips}>{(['All', 'Trending', 'Latest', 'Closed'] as const).map(item => <Pressable accessibilityRole="button" accessibilityState={{ selected: collection === item }} key={item} onPress={() => setCollection(item)} style={[s.chip, collection === item && s.chipActive]}><Text style={[s.chipText, collection === item && s.chipTextActive]}>{item}</Text></Pressable>)}</ScrollView></>}
        <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={s.chips}>{categories.map(item => <Pressable accessibilityRole="button" accessibilityState={{ selected: category === item }} key={item} onPress={() => setCategory(item)} style={[s.chip, category === item && s.chipActive]}><Text style={[s.chipText, category === item && s.chipTextActive]}>{item}</Text></Pressable>)}</ScrollView>
        <View style={s.between}><Text style={s.sectionTitle}>{tab === 'Home' ? 'In the spotlight' : 'Explore conversations'}</Text>{tab === 'Home' && <Pressable accessibilityRole="button" onPress={() => setTab('Explore')} style={s.seeAll}><Text style={s.meta}>Explore all ›</Text></Pressable>}</View>
        {feedError && <View style={s.card}><Text accessibilityRole="alert" style={s.error}>{feedError}</Text><Button secondary title="Try again" onPress={() => void loadPolls()} /></View>}
      </View>}
      ListEmptyComponent={loading ? <View style={s.state}><ActivityIndicator size="large" color={c.primary} /><Text style={s.body}>Finding fresh perspectives…</Text></View> : !feedError ? <StateCard title="No polls here yet" copy={query || category !== 'For you' ? 'Try another category or search.' : 'New conversations will appear here. Pull down to refresh.'} /> : null}
      renderItem={({ item }) => <PollPreview poll={item} onOpen={() => openPoll(item)} />} />}
    {tab === 'Profile' && <ScrollView contentContainerStyle={s.feed}>
      <Text style={s.heading}>Your impact.</Text><Text style={s.context}>Small opinions. A growing perspective.</Text>
      <View style={s.card}><View style={s.row}><UserAvatar user={user} size={60} /><View style={s.flex}><Text style={s.cardTitle}>{user.displayName}</Text><Text style={s.body}>@{user.username}</Text></View></View><Button title="Edit profile" secondary onPress={() => setFeature('Edit profile')} /><View style={s.between}><Text style={s.strong}>Level {user.progression.level}</Text><Text style={s.meta}>{count(user.xp)} XP</Text></View><View style={s.track}><View style={[s.fill, { width: `${Math.max(0, Math.min(100, user.progression.progressPercent))}%` }]} /></View><Text style={s.body}>{Math.max(0, user.progression.nextLevelXp - user.xp)} XP to the next level</Text></View>
      <View style={s.stats}>{[['Votes cast', user.totalVotes], ['Current streak', user.streak], ['Longest streak', user.longestStreak], ['Polls created', user.pollsCreated]].map(([label, value]) => <View key={label} style={s.stat}><Text style={s.heading}>{value ?? 0}</Text><Text style={s.body}>{label}</Text></View>)}</View>
      {(['My polls', 'Voted polls', 'Achievements', 'Preferences', 'Wellness'] as Feature[]).map(item => <Pressable key={item} accessibilityRole="button" onPress={() => setFeature(item)} style={[s.card, s.between]}><Text style={s.strong}>{item}</Text><Icon name="chevron-forward" /></Pressable>)}
      <Button title="Discover your next poll" onPress={() => setTab('Home')} /><Button title="Sign out" secondary onPress={() => void logout()} />
    </ScrollView>}
    {tab === 'More' && <ScrollView contentContainerStyle={s.feed}><Text style={s.heading}>More ways to Pop.</Text><Text style={s.context}>Play, connect, and make it yours.</Text>{([
      ['Create poll', 'add-circle-outline', 'Start a conversation of your own'],
      ['My polls', 'stats-chart-outline', 'Track votes on conversations you started'],
      ['Games', 'flash-outline', 'Quick rounds, fresh perspectives'],
      ['Challenges', 'flag-outline', 'Small goals. Extra XP.'],
      ['Achievements', 'ribbon-outline', 'Your milestones and earned titles'],
      ['Community', 'people-outline', 'Friends, groups, and shared progress'],
      ['Notifications', 'notifications-outline', 'Updates from your community'],
      ['Wellness', 'heart-outline', 'A private moment to reflect'],
      ['Voted polls', 'time-outline', 'Review results and share polls you answered'],
      ['Preferences', 'options-outline', 'Interests, notifications, and privacy'],
    ] as [Feature, IconName, string][]).map(([name, icon, copy]) => <Pressable key={name} accessibilityRole="button" onPress={() => setFeature(name)} style={[s.card, s.row]}><Icon name={icon} color={c.primary} size={27} /><View style={s.flex}><Text style={s.strong}>{name}</Text><Text style={s.body}>{copy}</Text></View><Icon name="chevron-forward" size={18} /></Pressable>)}</ScrollView>}
    {bottomNavigation}
  </View>;
}

const createStyles = (c: ThemeColors) => StyleSheet.create({
  shell: { flex: 1, backgroundColor: c.background },
  flex: { flex: 1 }, shrink: { flexShrink: 1 }, center: { textAlign: 'center' },
  row: { flexDirection: 'row', alignItems: 'center', gap: spacing['2'] },
  between: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: spacing['2'], flexWrap: 'wrap' },
  header: { paddingHorizontal: spacing['6'], paddingVertical: spacing['3'], flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  logo: { color: c.foreground, fontSize: 30, fontWeight: '800', letterSpacing: -1 },
  logoTile: { width: 38, height: 38, borderRadius: 12, backgroundColor: '#17151F', alignItems: 'center', justifyContent: 'center' },
  logoMark: { width: 28, height: 28 },
  avatar: { height: 44, width: 44, borderRadius: radius.full, backgroundColor: c.secondary, alignItems: 'center', justifyContent: 'center' },
  avatarText: { fontSize: 21, color: c.secondaryForeground, fontWeight: '700' },
  feed: { padding: spacing['5'], paddingTop: spacing['2'], gap: spacing['4'], paddingBottom: spacing['8'] },
  feedIntro: { gap: spacing['5'] },
  heading: { fontSize: 29, lineHeight: 36, letterSpacing: -0.8, fontWeight: '800', color: c.foreground },
  context: { fontSize: 16, lineHeight: 25, color: c.mutedForeground, marginTop: 5 },
  sourceLink: { color: c.primary, fontSize: 14, fontWeight: '700', marginTop: 12 },
  body: { color: c.mutedForeground, fontSize: 14, lineHeight: 22 },
  meta: { color: c.mutedForeground, fontSize: 12, lineHeight: 18 },
  caption: { color: c.mutedForeground, fontSize: 12, lineHeight: 19 },
  strong: { color: c.foreground, fontSize: 14, fontWeight: '700' },
  streak: { backgroundColor: c.card, padding: spacing['3'], borderRadius: radius.xl, flexDirection: 'row', alignItems: 'center', gap: spacing['2'], borderColor: c.border, borderWidth: StyleSheet.hairlineWidth },
  dots: { flexDirection: 'row', gap: 4, marginTop: 6 }, dot: { width: 8, height: 8, borderRadius: 4, backgroundColor: c.secondary }, dotActive: { backgroundColor: c.primary },
  chips: { gap: spacing['2'] }, chip: { minHeight: 44, paddingHorizontal: spacing['4'], justifyContent: 'center', borderRadius: radius.full, backgroundColor: c.secondary }, chipActive: { backgroundColor: c.primary }, chipText: { fontSize: 13, color: c.secondaryForeground }, chipTextActive: { color: c.primaryForeground, fontWeight: '700' },
  sectionTitle: { fontSize: 19, fontWeight: '800', color: c.foreground, letterSpacing: -0.3 }, seeAll: { minHeight: 44, justifyContent: 'center' },
  card: { backgroundColor: c.card, borderRadius: radius['3xl'], padding: spacing['4'], gap: spacing['3'], borderWidth: StyleSheet.hairlineWidth, borderColor: c.border },
  cardTitle: { fontSize: 20, lineHeight: 27, fontWeight: '700', letterSpacing: -0.5, color: c.foreground },
  pill: { backgroundColor: c.secondary, borderRadius: radius.full, paddingHorizontal: 10, paddingVertical: 6, flexDirection: 'row', gap: 5, alignItems: 'center' }, pillText: { fontSize: 11, color: c.secondaryForeground, fontWeight: '500' },
  previewBody: { flexDirection: 'row', gap: 8, alignItems: 'center' }, art: { width: 88, height: 77, justifyContent: 'center', alignItems: 'center' }, artCircle: { position: 'absolute', width: 77, height: 65, backgroundColor: c.secondary, borderTopLeftRadius: 50, borderTopRightRadius: 50, bottom: 0 }, ballot: { backgroundColor: c.card, borderWidth: 1, borderColor: c.border, width: 62, padding: 7, gap: 4, transform: [{ rotate: '-8deg' }], borderRadius: 7 }, ballotLine: { height: 4, backgroundColor: c.secondary, borderRadius: 2, flex: 1, minWidth: 14 }, artRow: { flexDirection: 'row', alignItems: 'center', gap: 3 },
  button: { minHeight: 52, backgroundColor: c.primary, borderRadius: radius.xl, padding: spacing['3.5'], alignItems: 'center', justifyContent: 'center' }, buttonText: { fontSize: 16, fontWeight: '700', color: c.primaryForeground }, secondaryButton: { backgroundColor: 'transparent' }, secondaryText: { color: c.mutedForeground }, disabled: { opacity: 0.45 }, pressed: { opacity: 0.75 },
  nav: { flexDirection: 'row', paddingVertical: 8, borderTopWidth: StyleSheet.hairlineWidth, borderColor: c.border, backgroundColor: c.card }, navItem: { flex: 1, alignItems: 'center', gap: 2, minHeight: 54 }, navIcon: { borderRadius: radius.full, paddingHorizontal: 16, paddingVertical: 4 }, navIconActive: { backgroundColor: `${c.primary}10` }, navText: { color: c.mutedForeground, fontSize: 11 }, navTextActive: { color: c.primary, fontWeight: '700' },
  detailHeader: { paddingHorizontal: 12, paddingVertical: 8, flexDirection: 'row', alignItems: 'center' }, iconButton: { width: 48, height: 48, alignItems: 'center', justifyContent: 'center' }, headerTitle: { flex: 1, fontSize: 17, fontWeight: '700', color: c.foreground, paddingLeft: 8 },
  detailContent: { padding: spacing['6'], gap: spacing['5'], paddingBottom: spacing['8'] }, question: { fontSize: 30, lineHeight: 39, fontWeight: '800', letterSpacing: -0.7, color: c.foreground }, options: { gap: spacing['4'], marginVertical: spacing['3'] }, option: { minHeight: 94, borderWidth: 1.5, borderColor: c.border, backgroundColor: c.card, borderRadius: radius.xl, padding: spacing['5'], flexDirection: 'row', gap: spacing['4'], alignItems: 'center' }, optionSelected: { borderColor: c.primary, backgroundColor: `${c.primary}08` }, optionTitle: { fontSize: 19, lineHeight: 26, fontWeight: '700', color: c.foreground },
  ballotDetailContent: { paddingBottom: 0 },
  footer: { paddingHorizontal: spacing['6'], paddingTop: spacing['3'], paddingBottom: spacing['4'], backgroundColor: c.background, gap: 4 }, success: { flexDirection: 'row', gap: 10, padding: 16, backgroundColor: `${c.accent}15`, borderRadius: radius.xl, justifyContent: 'center', alignItems: 'center' }, successText: { color: c.foreground, fontSize: 16, fontWeight: '700' },
  resultRow: { gap: 10, paddingBottom: 10 }, track: { height: 14, borderRadius: radius.full, backgroundColor: c.secondary, overflow: 'hidden' }, fill: { height: '100%', borderRadius: radius.full, backgroundColor: c.primary }, votedTag: { color: c.primary, backgroundColor: `${c.primary}10`, paddingHorizontal: 8, paddingVertical: 4, borderRadius: radius.full, fontSize: 11 }, rewardIcon: { width: 48, height: 48, borderRadius: radius.full, backgroundColor: `${c.chart4}20`, justifyContent: 'center', alignItems: 'center' },
  error: { color: c.destructive, fontSize: 14, lineHeight: 22 }, state: { padding: 28, alignItems: 'center', gap: 14 }, search: { flexDirection: 'row', alignItems: 'center', paddingHorizontal: 14, borderRadius: radius.xl, borderWidth: 1, borderColor: c.border, backgroundColor: c.card, gap: 10 }, searchInput: { flex: 1, minHeight: 50, color: c.foreground, fontSize: 14 }, rank: { fontSize: 18, fontWeight: '700', color: c.primary, minWidth: 40 }, stats: { flexDirection: 'row', flexWrap: 'wrap', gap: 12 }, stat: { flexGrow: 1, flexBasis: '45%', backgroundColor: c.card, borderWidth: StyleSheet.hairlineWidth, borderColor: c.border, borderRadius: radius.xl, padding: 18, gap: 4 },
});
