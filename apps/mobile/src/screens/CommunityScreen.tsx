import { useTheme } from '../theme';
import { useState } from 'react';
import { Alert, Image, Text, View } from 'react-native';
import { Action, Card, Chips, Field, Glyph, Screen, useUI, useAction, useResource } from '../components/ScreenKit';
import { featureApi as api } from '../lib/featuresApi';
import type { SocialGroup, SocialUser, WeeklyLeaderboard } from '../types/features';
import { useAuth } from '../context/AuthContext';

const loadCommunity = async () => ({ friends: await api.friends(), groups: await api.groups() });
export function CommunityScreen({ back }: { back: () => void }) {
  const { colors: c } = useTheme(); const ui = useUI();

  const resource = useResource(loadCommunity); const action = useAction(); const { user } = useAuth();
  const [tab, setTab] = useState('Friends');
  const [query, setQuery] = useState(''); const [results, setResults] = useState<SocialUser[]>([]);
  const [searched, setSearched] = useState(false); const [searchCursor, setSearchCursor] = useState<string | null>(null);
  const [groupName, setGroupName] = useState(''); const [inviteToken, setInviteToken] = useState('');
  const [group, setGroup] = useState<SocialGroup | null>(null); const [board, setBoard] = useState<WeeklyLeaderboard | null>(null);
  const [notice, setNotice] = useState('');
  const [createdInvite, setCreatedInvite] = useState('');
  const accepted = resource.data?.friends.items.filter(item => item.state === 'Accepted') ?? [];
  function confirm(title: string, copy: string, task: () => Promise<unknown>) { Alert.alert(title, copy, [{ text: 'Cancel', style: 'cancel' }, { text: 'Confirm', onPress: () => void action.run(task) }]); }
  async function search(more = false) { const response = await api.searchUsers(query.trim(), more ? searchCursor ?? '' : ''); setResults(current => more ? [...current, ...response.items] : response.items); setSearchCursor(response.nextCursor ?? null); setSearched(true); }
  return <Screen title={group ? group.name : 'Public Pulse'} subtitle={group ? `${group.memberCount} members · ${group.role}` : 'Find familiar voices and start a small community.'} back={() => group ? (setGroup(null), setBoard(null), setCreatedInvite('')) : back()} busy={action.busy} refresh={group ? undefined : resource.refresh} loading={resource.loading} error={resource.error || action.error}>
    {!group && <Card><View style={{ flexDirection: 'row', alignItems: 'center', gap: 14 }}><Image source={require('../../assets/public-pulse-mark.png')} resizeMode="contain" style={{ width: 58, height: 58 }} /><View style={{ flex: 1 }}><Text style={ui.title}>Your people. Your pulse.</Text><Text style={ui.body}>Connect with friends, create private groups, and follow shared activity.</Text></View></View></Card>}
    {!!notice && <Text accessibilityLiveRegion="polite" style={ui.tag}>{notice}</Text>}
    {group ? <>
      <Card><Glyph name="people-outline" /><Text style={ui.title}>This week’s voices</Text>{board?.items.map(entry => <View key={entry.user.id} style={ui.spread}><Text style={ui.label}>#{entry.rank} {entry.user.displayName}</Text><Text style={ui.body}>{entry.xp} XP</Text></View>)}{board?.items.length === 0 && <Text style={ui.body}>No activity in this group yet.</Text>}</Card>
      <Card><Text style={ui.title}>Invite a friend</Text>{accepted.map(friend => <Action key={friend.id} secondary busy={action.busy} title={`Invite ${friend.user.displayName}`} onPress={() => void action.run(async () => { const response = await api.invite(group.id, friend.user.id); setCreatedInvite(response.token); setNotice('Invitation created for your friend.'); })} />)}{accepted.length === 0 && <Text style={ui.body}>Connect with a friend first to invite them.</Text>}{!!createdInvite && <><Text style={ui.meta}>Your friend can enter this invitation code in Community → Groups.</Text><Text selectable style={ui.label}>{createdInvite}</Text></>}</Card>
      <Action secondary title="Leave group" busy={action.busy} onPress={() => confirm('Leave this group?', 'You’ll no longer appear in this group’s membership.', async () => { await api.leaveGroup(group.id); setGroup(null); await resource.refresh(); })} />
    </> : <>
      <Chips items={['Friends', 'Find people', 'Groups']} selected={tab} onSelect={value => { setTab(value); setNotice(''); }} />
      {tab === 'Friends' && <>
        {resource.data?.friends.items.filter(item => item.state === 'Accepted' || item.state === 'Pending').map(item => <Card key={item.id}><Text style={ui.title}>{item.user.displayName}</Text><Text style={ui.body}>@{item.user.username} · {item.state === 'Accepted' ? 'Connected' : item.incoming ? 'Wants to connect with you' : 'Request sent'}</Text>{item.state === 'Pending' && item.incoming ? <><Action busy={action.busy} title="Accept request" onPress={() => void action.run(async () => { await api.acceptFriend(item.id); await resource.refresh(); })} /><Action secondary busy={action.busy} title="Decline" onPress={() => void action.run(async () => { await api.declineFriend(item.id); await resource.refresh(); })} /></> : item.state === 'Accepted' ? <Action secondary busy={action.busy} title="Remove friend" onPress={() => confirm('Remove this friend?', 'You can send a new request later.', async () => { await api.removeFriend(item.id); await resource.refresh(); })} /> : null}<Action secondary busy={action.busy} title="Block user" onPress={() => confirm('Block this user?', 'This restricts future social interactions with this person.', async () => { await api.block(item.user.id); await resource.refresh(); setNotice('User blocked.'); })} /></Card>)}
        {!resource.loading && !resource.data?.friends.items.some(item => item.state === 'Accepted' || item.state === 'Pending') && <Card><Glyph name="people-outline" /><Text style={ui.title}>Bring a familiar voice</Text><Text style={ui.body}>Find someone by name or username to start connecting.</Text><Action title="Find people" onPress={() => setTab('Find people')} /></Card>}
      </>}
      {tab === 'Find people' && <><Card><Field label="Name or username" value={query} onChangeText={value => { setQuery(value); setSearched(false); setResults([]); setSearchCursor(null); }} autoCapitalize="none" /><Action title="Search people" disabled={query.trim().length < 2} busy={action.busy} onPress={() => void action.run(() => search())} /></Card>{results.filter(person => person.id !== user?.id).map(person => <Card key={person.id}><Text style={ui.title}>{person.displayName}</Text><Text style={ui.body}>@{person.username}</Text><Action title="Send friend request" busy={action.busy} disabled={!!resource.data?.friends.items.some(item => item.user.id === person.id && ['Pending', 'Accepted'].includes(item.state))} onPress={() => void action.run(async () => { await api.friendRequest(person.id); setNotice(`Request sent to ${person.displayName}.`); await resource.refresh(); })} /></Card>)}{searched && results.length === 0 && <Text style={ui.body}>No matching people found.</Text>}{searchCursor && <Action title="More people" secondary busy={action.busy} onPress={() => void action.run(() => search(true))} />}</>}
      {tab === 'Groups' && <>
        <Card><Text style={ui.title}>Create a circle</Text><Field label="Group name" maxLength={80} value={groupName} onChangeText={setGroupName} /><Action title="Create group" busy={action.busy} disabled={!groupName.trim()} onPress={() => void action.run(async () => { await api.createGroup(groupName.trim()); setGroupName(''); setNotice('Your group is ready.'); await resource.refresh(); })} /></Card>
        <Card><Text style={ui.title}>Have an invitation?</Text><Field label="Invitation code" value={inviteToken} onChangeText={setInviteToken} autoCapitalize="none" /><Action title="Join group" busy={action.busy} disabled={!inviteToken.trim()} onPress={() => void action.run(async () => { await api.acceptInvite(inviteToken.trim()); setInviteToken(''); await resource.refresh(); setNotice('You joined the group.'); })} /></Card>
        {resource.data?.groups.items.map(item => <Card key={item.id}><Text style={ui.title}>{item.name}</Text><Text style={ui.body}>{item.memberCount} members · {item.role}</Text><Action title="Open group" secondary busy={action.busy} onPress={() => void action.run(async () => { setBoard(await api.groupBoard(item.id)); setGroup(item); })} /></Card>)}
      </>}
    </>}
  </Screen>;
}
