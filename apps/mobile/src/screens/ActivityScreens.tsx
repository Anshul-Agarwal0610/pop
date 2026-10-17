import { useCallback, useEffect, useState } from 'react';
import { Alert, Image, Linking, Pressable, Switch, Text, View } from 'react-native';
import * as ImagePicker from 'expo-image-picker';
import { Action, Card, Chips, ErrorText, Field, Glyph, Progress, Screen, useUI, useAction, useResource } from '../components/ScreenKit';
import { categories, featureApi as api } from '../lib/featuresApi';
import { usersApi } from '../lib/api';
import { setAnalyticsConsent } from '../lib/analytics/privacy';
import type { ApiPoll } from '../types/poll';
import { useTheme } from '../theme';
import { useAuth } from '../context/AuthContext';
import { UserAvatar } from '../components/UserAvatar';
import { sharePoll } from '../lib/sharePoll';
import { resultPercent } from '../lib/pollPresentation';

type Back = { back: () => void };
type OpenPoll = { openPoll: (poll: ApiPoll) => void };
const date = (value: string) => new Date(value).toLocaleDateString(undefined, { day: 'numeric', month: 'short' });
const loadPollAudiences = async () => ({ friends: await api.friends(), groups: await api.groups() });

export function EditProfileScreen({ back }: Back) {
  const { user, refreshUser } = useAuth();
  const ui = useUI();
  const [displayName, setDisplayName] = useState(user?.displayName ?? '');
  const [avatarUrl, setAvatarUrl] = useState<string | null>(user?.avatarUrl ?? null);
  const [previewUri, setPreviewUri] = useState<string | null>(user?.avatarUrl ?? null);
  const [notice, setNotice] = useState('');
  const action = useAction();

  async function chooseAvatar() {
    const permission = await ImagePicker.requestMediaLibraryPermissionsAsync();
    if (!permission.granted) return Alert.alert('Photo permission needed', 'Allow Pop to choose a profile photo.');
    const result = await ImagePicker.launchImageLibraryAsync({ mediaTypes: ['images'], allowsEditing: true, aspect: [1, 1], quality: .8 });
    if (result.canceled || !result.assets[0]) return;
    const asset = result.assets[0];
    setPreviewUri(asset.uri);
    const uploaded = await api.uploadPollMedia({ uri: asset.uri, name: asset.fileName || `avatar-${Date.now()}.jpg`, type: asset.mimeType || 'image/jpeg' });
    setAvatarUrl(uploaded.url);
  }

  if (!user) return null;
  const previewUser = { ...user, avatarUrl: previewUri };
  return <Screen title="Edit profile" subtitle="Choose how your name and photo appear across Pop." back={back} busy={action.busy} error={action.error}>
    {!!notice && <Text accessibilityLiveRegion="polite" style={ui.tag}>{notice}</Text>}
    <Card><View style={[ui.row, { justifyContent: 'center' }]}><UserAvatar user={previewUser} size={104} /></View><Action title="Choose profile photo" secondary busy={action.busy} onPress={() => void action.run(chooseAvatar)} />{avatarUrl && <Action title="Remove profile photo" secondary disabled={action.busy} onPress={() => { setAvatarUrl(null); setPreviewUri(null); }} />}</Card>
    <Card><Field label="Display name" value={displayName} maxLength={80} onChangeText={setDisplayName} placeholder="Your name" /><Text style={ui.meta}>@{user.username} · Your username cannot be changed yet.</Text></Card>
    <Action title="Save profile" busy={action.busy} disabled={displayName.trim().length < 2} onPress={() => void action.run(async () => { await usersApi.updateProfile(displayName.trim(), avatarUrl); await refreshUser(); setNotice('Profile updated.'); })} />
  </Screen>;
}

export function CreatePollScreen({ back, openPoll }: Back & OpenPoll) {
  const { colors: c } = useTheme(); const ui = useUI();

  const [question, setQuestion] = useState('');
  const [description, setDescription] = useState('');
  const [category, setCategory] = useState('General');
  const options = ['Support', 'Against'];
  const [duration, setDuration] = useState('7 days');
  const [source, setSource] = useState('');
  const [media, setMedia] = useState<{ uri: string; url: string; mediaType: 'image' | 'video'; name: string } | null>(null);
  const [mediaBusy, setMediaBusy] = useState<'photo' | 'video' | 'library' | null>(null);
  const [mediaError, setMediaError] = useState<string | null>(null);
  const [review, setReview] = useState(false);
  const [published, setPublished] = useState<ApiPoll | null>(null);
  const audienceResource = useResource(loadPollAudiences);
  const [audience, setAudience] = useState<'Public' | 'Friend' | 'Group'>('Public');
  const [friendId, setFriendId] = useState<number | null>(null);
  const [groupId, setGroupId] = useState<number | null>(null);
  const action = useAction();
  const cleaned = options.map(option => option.trim());
  const sourceValid = !source.trim() || /^https?:\/\/[^\s]+$/i.test(source.trim());
  const validAudience = audience === 'Public' || (audience === 'Friend' ? friendId !== null : groupId !== null);
  const valid = question.trim().length > 0 && cleaned.every(Boolean) && new Set(cleaned.map(option => option.toLowerCase())).size === cleaned.length && sourceValid && validAudience;
  const friends = audienceResource.data?.friends.items.filter(friend => friend.state === 'Accepted') ?? [];
  const groups = audienceResource.data?.groups.items ?? [];
  async function uploadResult(result: ImagePicker.ImagePickerResult) {
    if (result.canceled || !result.assets[0]) return;
    const asset = result.assets[0];
    const extension = asset.type === 'video' ? 'mp4' : 'jpg';
    const name = asset.fileName || `poll-${Date.now()}.${extension}`;
    const type = asset.mimeType || (asset.type === 'video' ? 'video/mp4' : 'image/jpeg');
    const uploaded = await api.uploadPollMedia({ uri: asset.uri, name, type });
    setMedia({ uri: asset.uri, name, ...uploaded });
  }
  async function capture(kind: 'image' | 'video') {
    const permission = await ImagePicker.requestCameraPermissionsAsync();
    if (!permission.granted) {
      Alert.alert('Camera permission needed', 'Allow Pop to use the camera to add a photo or video to your poll.');
      return;
    }
    await uploadResult(await ImagePicker.launchCameraAsync({ mediaTypes: [kind === 'image' ? 'images' : 'videos'], quality: .8, videoMaxDuration: 30 }));
  }
  async function chooseMedia() {
    const permission = await ImagePicker.requestMediaLibraryPermissionsAsync();
    if (!permission.granted) {
      Alert.alert('Photo permission needed', 'Allow Pop to choose a photo or video from your phone.');
      return;
    }
    await uploadResult(await ImagePicker.launchImageLibraryAsync({ mediaTypes: ['images', 'videos'], quality: .8, videoMaxDuration: 30 }));
  }
  async function runMedia(kind: 'photo' | 'video' | 'library', operation: () => Promise<void>) {
    if (mediaBusy) return;
    setMediaBusy(kind); setMediaError(null);
    try { await operation(); }
    catch (error) { setMediaError(error instanceof Error ? error.message : 'Could not attach this media. Please try again.'); }
    finally { setMediaBusy(null); }
  }
  if (published) return <Screen title="You started a conversation" back={back}><Card><Glyph name="checkmark-circle" /><Text style={ui.title}>{published.question}</Text><Text style={ui.body}>{audience === 'Public' ? 'Your poll is live and ready for votes.' : `Your private poll is available to the selected ${audience.toLowerCase()}.`}</Text><Action title="Share poll" onPress={() => sharePoll(published)} /><Action title="Open my poll" secondary onPress={() => openPoll(published)} /><Action title="Back to Pop" secondary onPress={back} /></Card></Screen>;
  return <Screen title={review ? 'Review your poll' : 'Start a conversation'} subtitle="A clear question. Room for every perspective." back={() => review && !action.busy && !mediaBusy ? setReview(false) : back()} busy={action.busy || !!mediaBusy} error={action.error || mediaError}>
    <Progress value={review ? 100 : 50} />
    {review ? <Card><Text style={ui.tag}>{category} · {duration}</Text><Text style={ui.title}>{question.trim()}</Text>{!!description && <Text style={ui.body}>{description}</Text>}{media?.mediaType === 'image' && <Image source={{ uri: media.uri }} resizeMode="cover" style={{ width: '100%', height: 190, borderRadius: 16 }} />}{media && <Text style={ui.meta}>{media.mediaType === 'image' ? 'Photo' : 'Video'} attached · {media.name}</Text>}{cleaned.map(option => <Text key={option} style={ui.label}>○  {option}</Text>)}{!!source && <Text style={ui.meta}>{source}</Text>}<Text style={ui.meta}>{audience === 'Public' ? 'Public poll' : `${audience}-only poll`} · Results appear after voting</Text></Card> : <>
      <Card><Field label="Your question" value={question} onChangeText={setQuestion} maxLength={280} multiline placeholder="What would you like people’s opinion on?" /><Text style={ui.meta}>{question.length}/280</Text><Field label="Context (optional)" value={description} onChangeText={setDescription} maxLength={500} multiline placeholder="Give both sides room to think." /></Card>
      <Card><Text style={ui.title}>The choices</Text><Text style={ui.body}>Support · Against</Text><Text style={ui.meta}>Write a statement people can support or oppose.</Text></Card>
      <Card><View style={ui.spread}><View style={ui.flex}><Text style={ui.title}>Add media</Text><Text style={ui.body}>Capture something now or choose an existing photo or video.</Text></View><Glyph name="camera-outline" /></View>{media?.mediaType === 'image' && <Image source={{ uri: media.uri }} resizeMode="cover" style={{ width: '100%', height: 190, borderRadius: 16 }} />}{media && <Text style={ui.meta}>{media.mediaType === 'image' ? 'Photo' : 'Video'} ready · {media.name}</Text>}<Action title="Take photo" secondary busy={mediaBusy === 'photo'} disabled={mediaBusy !== null} onPress={() => void runMedia('photo', () => capture('image'))} /><Action title="Record video" secondary busy={mediaBusy === 'video'} disabled={mediaBusy !== null} onPress={() => void runMedia('video', () => capture('video'))} /><Action title="Choose from phone" secondary busy={mediaBusy === 'library'} disabled={mediaBusy !== null} onPress={() => void runMedia('library', chooseMedia)} />{media && <Action title="Remove media" secondary disabled={mediaBusy !== null || action.busy} onPress={() => setMedia(null)} />}<Text style={ui.meta}>Photos up to 10 MB · Videos up to 75 MB and 30 seconds</Text></Card>
      <Card><Text style={ui.title}>Who can vote?</Text><Chips items={['Public', 'Friend', 'Group']} selected={audience} onSelect={value => { setAudience(value as typeof audience); setFriendId(null); setGroupId(null); }} />{audience === 'Public' && <Text style={ui.body}>Everyone on Pop can discover and vote on this poll.</Text>}{audience === 'Friend' && <>{friends.map(friend => <Action key={friend.user.id} title={`${friendId === friend.user.id ? '✓ ' : ''}${friend.user.displayName} · @${friend.user.username}`} secondary={friendId !== friend.user.id} onPress={() => setFriendId(friend.user.id)} />)}{!audienceResource.loading && friends.length === 0 && <Text style={ui.body}>Connect with a friend in Community before creating a friend-only poll.</Text>}</>}{audience === 'Group' && <>{groups.map(group => <Action key={group.id} title={`${groupId === group.id ? '✓ ' : ''}${group.name} · ${group.memberCount} members`} secondary={groupId !== group.id} onPress={() => setGroupId(group.id)} />)}{!audienceResource.loading && groups.length === 0 && <Text style={ui.body}>Create or join a group in Community before creating a group poll.</Text>}</>}<Text style={ui.meta}>Private poll links work only for the selected friend or active group members.</Text></Card>
      <Card><Text style={ui.label}>Category</Text><Chips items={categories} selected={category} onSelect={setCategory} /><Text style={ui.label}>Open for</Text><Chips items={['1 day', '3 days', '7 days']} selected={duration} onSelect={setDuration} /><Field label="Source link (optional)" value={source} onChangeText={setSource} autoCapitalize="none" keyboardType="url" placeholder="https://…" />{!sourceValid && <ErrorText message="Use a complete http or https link." />}</Card>
    </>}
    <Action title={review ? 'Publish poll' : 'Review poll'} busy={action.busy} disabled={!valid} onPress={() => review ? void action.run(async () => setPublished(await api.create({ question: question.trim(), description: description.trim(), category, options: cleaned, expiresAt: new Date(Date.now() + parseInt(duration) * 86_400_000).toISOString(), sourceType: 'manual', sourceUrl: source.trim() || undefined, thumbnailUrl: media?.url, mediaType: media?.mediaType, isAIGenerated: false, isPrivate: audience !== 'Public', audienceType: audience, audienceFriendUserId: audience === 'Friend' ? friendId ?? undefined : undefined, audienceGroupId: audience === 'Group' ? groupId ?? undefined : undefined }))) : setReview(true)} />
  </Screen>;
}

export function NotificationsScreen({ back, openPoll }: Back & OpenPoll) {
  const { colors: c } = useTheme(); const ui = useUI();

  const resource = useResource(api.notifications);
  const action = useAction();
  const unread = resource.data?.filter(item => !item.isRead).length ?? 0;
  return <Screen title="Your updates" subtitle={unread ? `${unread} unread updates` : 'You’re all caught up.'} back={back} loading={resource.loading} refresh={resource.refresh} error={resource.error || action.error}>
    {unread > 0 && <Action title="Mark all as read" secondary busy={action.busy} onPress={() => void action.run(async () => { await api.readAll(); await resource.refresh(); })} />}
    {resource.data?.map(item => <Pressable key={item.id} accessibilityRole="button" disabled={action.busy} onPress={() => void action.run(async () => { if (!item.isRead) { await api.read(item.id); resource.setData(current => current?.map(entry => entry.id === item.id ? { ...entry, isRead: true } : entry) ?? null); } if (item.pollId) openPoll(await api.poll(item.pollId)); })}><Card><View style={ui.spread}><Glyph name={item.isRead ? 'notifications-outline' : 'notifications'} /><Text style={ui.meta}>{date(item.createdAt)}</Text></View><Text style={ui.title}>{item.title}</Text><Text style={ui.body}>{item.body}</Text><Text style={ui.tag}>{item.pollId ? 'Open poll →' : item.isRead ? 'Read' : 'Tap to mark read'}</Text></Card></Pressable>)}
    {!resource.loading && resource.data?.length === 0 && <Card><Glyph name="notifications-outline" /><Text style={ui.title}>A quiet moment</Text><Text style={ui.body}>Milestones, reminders, and poll updates will appear here.</Text></Card>}
  </Screen>;
}

export function ChallengesScreen({ back, browse }: Back & { browse: (category?: string) => void }) {
  const { colors: c } = useTheme(); const ui = useUI();

  const [filter, setFilter] = useState('Active');
  const load = useCallback(() => api.challenges(filter.toLowerCase()), [filter]);
  const resource = useResource(load);
  return <Screen title="A little challenge" subtitle="Build a habit, discover new topics, earn a little extra." back={back} refresh={resource.refresh} loading={resource.loading} error={resource.error}>
    <Chips items={['Active', 'Completed', 'Expired', 'All']} selected={filter} onSelect={setFilter} />
    {resource.data?.map(item => <Card key={item.challengeId}><View style={ui.spread}><Glyph name="flag-outline" /><Text style={ui.tag}>+{item.rewardXp} XP · {item.recurrence}</Text></View><Text style={ui.title}>{item.title}</Text><Text style={ui.body}>{item.description || item.requirementText}</Text><Progress value={item.requiredVotes > 0 ? item.currentVotes / item.requiredVotes * 100 : 0} /><Text style={ui.meta}>{item.currentVotes}/{item.requiredVotes} votes · {item.state} · Ends {date(item.endAt)}</Text>{item.rewardBadge && <Text style={ui.label}>Badge: {item.rewardBadge}</Text>}{!item.isCompleted && item.state !== 'Expired' && <Action title="Find eligible polls" onPress={() => browse(item.category || undefined)} />}</Card>)}
    {!resource.loading && resource.data?.length === 0 && <Card><Text style={ui.title}>No {filter.toLowerCase()} challenges</Text><Text style={ui.body}>Check back for your next goal.</Text></Card>}
  </Screen>;
}

export function AchievementsScreen({ back }: Back) {
  const { colors: c } = useTheme(); const ui = useUI();

  const resource = useResource(api.achievements);
  const action = useAction();
  const [filter, setFilter] = useState('All');
  const items = resource.data?.achievements.filter(item => filter === 'All' || item.status === (filter === 'Earned' ? 'earned' : filter === 'In progress' ? 'in-progress' : 'locked'));
  return <Screen title="Your collection" subtitle="Every badge tells a small part of your story." back={back} refresh={resource.refresh} loading={resource.loading} error={resource.error || action.error}>
    {resource.data && <Card><Glyph name="ribbon-outline" /><Text style={ui.title}>{resource.data.earnedCount} of {resource.data.totalCount} earned</Text><Progress value={resource.data.totalCount ? resource.data.earnedCount / resource.data.totalCount * 100 : 0} />{resource.data.selectedTitle && <><Text style={ui.body}>Your title: {resource.data.selectedTitle}</Text><Action title="Clear selected title" secondary busy={action.busy} onPress={() => void action.run(async () => { await api.title(null); await resource.refresh(); })} /></>}</Card>}
    <Chips items={['All', 'Earned', 'In progress', 'Locked']} selected={filter} onSelect={setFilter} />
    {items?.map(item => <Card key={item.badgeId}><View style={ui.spread}><Glyph name={item.status === 'earned' ? 'ribbon' : 'lock-closed-outline'} color={item.status === 'earned' ? c.primary : c.mutedForeground} /><Text style={ui.tag}>{item.category} · +{item.rewardXp} XP</Text></View><Text style={ui.title}>{item.isSecret && item.status === 'locked' ? 'A secret to discover' : item.name}</Text><Text style={ui.body}>{item.isSecret && item.status === 'locked' ? 'Keep exploring Pop to reveal this achievement.' : item.description}</Text>{item.status !== 'locked' && item.progressPercent !== null && <Progress value={item.progressPercent} />}{item.requirement && !item.isSecret && <Text style={ui.meta}>{item.requirement}</Text>}{item.awardedAt && <Text style={ui.meta}>Earned {date(item.awardedAt)}</Text>}{item.status === 'earned' && item.rewardTitle && <Action title={resource.data?.selectedTitleBadgeId === item.badgeId ? 'Title selected' : `Use title: ${item.rewardTitle}`} disabled={resource.data?.selectedTitleBadgeId === item.badgeId} busy={action.busy} secondary onPress={() => void action.run(async () => { await api.title(item.badgeId); await resource.refresh(); })} />}</Card>)}
    {!resource.loading && items?.length === 0 && <Text style={ui.body}>No achievements in this category yet.</Text>}
  </Screen>;
}

export function HistoryScreen({ back, openPoll }: Back & OpenPoll) {
  const { colors: c } = useTheme(); const ui = useUI();

  const [limit, setLimit] = useState(30);
  const load = useCallback(() => api.history(limit), [limit]);
  const resource = useResource(load); const action = useAction();
  return <Screen title="Voted polls" subtitle="Revisit results, follow performance, or invite others to vote." back={back} refresh={resource.refresh} loading={resource.loading} error={resource.error || action.error}>
    {resource.data?.map(item => <Card key={`${item.pollId}-${item.votedAt}`}><Text style={ui.tag}>{item.category} · Voted {date(item.votedAt)}</Text><Text style={ui.title}>{item.question}</Text><Text style={ui.body}>Your vote: {item.votedOptionText}</Text><Text style={ui.meta}>{item.totalVotes} votes received so far</Text><Action title="View performance" busy={action.busy} onPress={() => void action.run(async () => openPoll(await api.poll(item.pollId)))} /><Action title="Share poll" secondary disabled={action.busy} onPress={() => sharePoll({ id: item.pollId, question: item.question })} /></Card>)}
    {!resource.loading && resource.data?.length === 0 && <Card><Text style={ui.title}>Your story starts with a vote</Text><Text style={ui.body}>Polls you vote on will appear here. Polls you create stay in My polls.</Text></Card>}
    {resource.data?.length === limit && <Action title="Load more voted polls" secondary onPress={() => setLimit(value => value + 30)} />}
  </Screen>;
}

export function MyPollsScreen({ back, openPoll }: Back & OpenPoll) {
  const ui = useUI();
  const [filter, setFilter] = useState('Active');
  const load = useCallback(() => api.myPolls(), []);
  const resource = useResource(load);
  const now = Date.now();
  const polls = (resource.data ?? []).filter(poll => {
    const pending = poll.moderationStatus && poll.moderationStatus !== 'Published';
    const active = poll.isActive && new Date(poll.expiresAt).getTime() > now && !pending;
    return filter === 'Pending' ? pending : filter === 'Active' ? active : !active && !pending;
  });
  const totalVotes = (resource.data ?? []).reduce((sum, poll) => sum + poll.totalVotes, 0);

  return <Screen title="My polls" subtitle="Track the conversations you started." back={back} refresh={resource.refresh} loading={resource.loading} error={resource.error}>
    <Card><View style={ui.spread}><View><Text style={ui.title}>{resource.data?.length ?? 0}</Text><Text style={ui.meta}>Polls created</Text></View><View><Text style={ui.title}>{totalVotes}</Text><Text style={ui.meta}>Votes received</Text></View></View></Card>
    <Chips items={['Active', 'Closed', 'Pending']} selected={filter} onSelect={setFilter} />
    {polls.map(poll => <Pressable key={poll.id} accessibilityRole="button" onPress={() => openPoll(poll)}><Card>
      <View style={ui.spread}><Text style={ui.tag}>{poll.category}{poll.isPrivate ? ' · Private' : ' · Public'}</Text><Text style={ui.meta}>{poll.moderationStatus && poll.moderationStatus !== 'Published' ? poll.moderationStatus.replace(/([a-z])([A-Z])/g, '$1 $2') : poll.isActive && new Date(poll.expiresAt).getTime() > now ? `Ends ${date(poll.expiresAt)}` : 'Closed'}</Text></View>
      <Text style={ui.title}>{poll.question}</Text>
      <Text style={ui.body}>{poll.totalVotes} total votes</Text>
      {poll.options.map(option => <View key={option.id}><View style={ui.spread}><Text style={ui.label}>{option.text}</Text><Text style={ui.meta}>{resultPercent(option.voteCount, poll.totalVotes)}% · {option.voteCount}</Text></View><Progress value={resultPercent(option.voteCount, poll.totalVotes)} /></View>)}
      {poll.moderationReason && <ErrorText message={poll.moderationReason} />}
      <Text style={ui.tag}>View performance →</Text>
    </Card></Pressable>)}
    {!resource.loading && polls.length === 0 && <Card><Text style={ui.title}>No {filter.toLowerCase()} polls</Text><Text style={ui.body}>Polls you create will appear here with live voting totals.</Text></Card>}
  </Screen>;
}

const settingsLoad = async () => {
  const [preferences, notifications, privacy] = await Promise.all([usersApi.getCategoryPreferences(), api.notificationPreferences(), usersApi.getAnalyticsPrivacy()]);
  return { preferences, notifications, privacy };
};
export function PreferencesScreen({ back }: Back) {
  const { colors: c, preference, setPreference } = useTheme(); const ui = useUI();

  const resource = useResource(settingsLoad); const action = useAction();
  const [selected, setSelected] = useState<string[]>([]);
  const [notice, setNotice] = useState('');
  const { signOut } = useAuth();
  const privacyUrl = process.env.EXPO_PUBLIC_PRIVACY_POLICY_URL;
  useEffect(() => { if (resource.data) setSelected(resource.data.preferences.filter(item => item.isExplicit).map(item => item.category)); }, [resource.data]);
  return <Screen title="Make Pop yours" back={back} refresh={resource.refresh} loading={resource.loading} error={resource.error || action.error}>
    {!!notice && <Text accessibilityLiveRegion="polite" style={ui.tag}>{notice}</Text>}
    <Card><Text style={ui.title}>Appearance</Text><Text style={ui.body}>Choose your look, or follow your phone’s settings.</Text><View style={ui.row}>{(['system', 'light', 'dark'] as const).map(mode => <Pressable key={mode} accessibilityRole="radio" accessibilityState={{ checked: preference === mode }} onPress={() => void action.run(async () => { await setPreference(mode); setNotice('Appearance saved on this device.'); })} style={[ui.chip, preference === mode && { backgroundColor: c.primary }]}><Text style={{ color: preference === mode ? c.primaryForeground : c.foreground }}>{mode === 'system' ? 'System' : mode === 'light' ? 'Light' : 'Dark'}</Text></Pressable>)}</View></Card>
    {resource.data && <>
      <Card><Text style={ui.title}>Your interests</Text><Text style={ui.body}>Choose the topics you want more of.</Text><View style={[ui.row, { flexWrap: 'wrap' }]}>{categories.map(item => <Pressable accessibilityRole="checkbox" accessibilityState={{ checked: selected.includes(item) }} key={item} disabled={action.busy} onPress={() => setSelected(current => current.includes(item) ? current.filter(value => value !== item) : [...current, item])} style={[ui.chip, selected.includes(item) && { backgroundColor: c.primary }]}><Text style={{ color: selected.includes(item) ? c.primaryForeground : c.foreground }}>{item}</Text></Pressable>)}</View><Action title="Save interests" busy={action.busy} onPress={() => void action.run(async () => { await usersApi.updateCategoryPreferences(selected); setNotice('Your interests have been saved.'); })} /><Action title="Reset personalization" secondary busy={action.busy} onPress={() => void action.run(async () => { await api.resetCategories(); await resource.refresh(); setNotice('Personalization reset.'); })} /></Card>
      <Card><Text style={ui.title}>Notifications</Text>{resource.data.notifications.map(item => <View key={item.type} style={ui.spread}><Text style={[ui.body, ui.flex]}>{item.type.replace(/([a-z])([A-Z])/g, '$1 $2')}</Text><Switch accessibilityLabel={item.type} disabled={action.busy} value={item.isEnabled} trackColor={{ true: c.primary }} onValueChange={enabled => void action.run(async () => { const prefs = resource.data!.notifications.map(entry => entry.type === item.type ? { ...entry, isEnabled: enabled } : entry); const saved = await api.saveNotificationPreferences(prefs.filter(entry => !entry.isEnabled).map(entry => entry.type)); resource.setData(current => current ? { ...current, notifications: saved } : current); })} /></View>)}<Text style={ui.meta}>Push delivery needs a Pop development or release build. In-app updates also appear in your inbox.</Text></Card>
      <Card><Text style={ui.title}>Privacy</Text><Text style={ui.body}>Allow optional usage analytics to help improve Pop. You can change this anytime.</Text><View style={ui.spread}><Text style={ui.label}>Usage analytics</Text><Switch accessibilityLabel="Allow usage analytics" disabled={action.busy} value={resource.data.privacy.consent === 'granted'} trackColor={{ true: c.primary }} onValueChange={enabled => void action.run(async () => { const consent = enabled ? 'granted' : 'denied'; if (!enabled) await setAnalyticsConsent('denied'); await usersApi.updateAnalyticsPrivacy(consent); await setAnalyticsConsent(consent); resource.setData(current => current ? { ...current, privacy: { ...current.privacy, consent } } : current); })} /></View></Card>
      <Card><Text style={ui.title}>Your data</Text><Text style={ui.body}>Read how Pop handles account, poll, wellness, media, and analytics data.</Text><Action title="Read privacy policy" secondary onPress={() => privacyUrl ? void Linking.openURL(privacyUrl) : Alert.alert('Privacy link not configured', 'Set EXPO_PUBLIC_PRIVACY_POLICY_URL to the deployed /privacy page.')} /><Text style={ui.label}>Delete account</Text><Text style={ui.body}>This permanently removes your profile, credentials, private wellness history, preferences, notifications, and social connections. Public polls and votes are retained without your identity so aggregate results remain accurate.</Text><Action title="Delete my account" secondary busy={action.busy} onPress={() => Alert.alert('Permanently delete your account?', 'You will be signed out immediately. This cannot be undone.', [{ text: 'Cancel', style: 'cancel' }, { text: 'Delete account', style: 'destructive', onPress: () => void action.run(async () => { await usersApi.deleteAccount(); await signOut(); }) }])} /></Card>
    </>}
  </Screen>;
}

export function ReportScreen({ back, pollId }: Back & { pollId: number }) {
  const { colors: c } = useTheme(); const ui = useUI();

  const [reason, setReason] = useState(''); const action = useAction(); const [sent, setSent] = useState(false);
  return <Screen title="Report a poll" back={back} error={action.error}><Card><Glyph name="flag-outline" /><Text style={ui.title}>{sent ? 'Thanks for speaking up' : 'Help keep Pop thoughtful'}</Text><Text style={ui.body}>{sent ? 'Your report has been sent for review.' : 'Tell us what seems misleading, harmful, or inappropriate.'}</Text>{!sent && <><Field label="Reason" multiline value={reason} maxLength={1000} onChangeText={setReason} /><Action title="Submit report" disabled={!reason.trim()} busy={action.busy} onPress={() => void action.run(async () => { await api.report(pollId, reason.trim()); setSent(true); })} /></>}<Action title="Back to poll" secondary onPress={back} /></Card></Screen>;
}
