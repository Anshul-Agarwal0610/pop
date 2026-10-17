import Ionicons from '@expo/vector-icons/Ionicons';
import { useCallback, useMemo, useState, type ComponentProps } from 'react';
import { ActivityIndicator, Pressable, RefreshControl, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { useAuth } from '../context/AuthContext';
import { businessApi } from '../lib/businessApi';
import { friendlyError, useResource } from '../components/ScreenKit';
import { radius, spacing, type ThemeColors, useTheme } from '../theme';

type Section = 'Overview' | 'Campaigns' | 'Brand' | 'Account';

export function BusinessExperience() {
  const { colors: c } = useTheme(); const s = useMemo(() => styles(c), [c]);
  const { user, signOut } = useAuth();
  const [section, setSection] = useState<Section>('Overview');
  const load = useCallback(async () => {
    const [accounts, campaigns] = await Promise.all([businessApi.accounts(), businessApi.campaigns()]);
    return { account: accounts[0] ?? null, campaigns };
  }, []);
  const resource = useResource(load);
  const [creating, setCreating] = useState(false); const [name, setName] = useState(''); const [objective, setObjective] = useState(''); const [actionError, setActionError] = useState<string | null>(null);
  const account = resource.data?.account; const campaigns = resource.data?.campaigns ?? [];
  const totals = campaigns.reduce((sum, item) => ({ impressions: sum.impressions + item.impressions, votes: sum.votes + item.votes }), { impressions: 0, votes: 0 });
  async function createCampaign() {
    if (!account || !name.trim() || !objective.trim()) return;
    setCreating(true); setActionError(null);
    try { await businessApi.createCampaign(account.id, name.trim(), objective.trim()); setName(''); setObjective(''); await resource.refresh(); setSection('Campaigns'); }
    catch (error) { setActionError(friendlyError(error)); } finally { setCreating(false); }
  }
  return <View style={s.shell}>
    <View style={s.header}><View><Text style={s.brand}>Pop Business</Text><Text style={s.muted}>{account?.name ?? user?.displayName}</Text></View><View style={s.avatar}><Text style={s.avatarText}>{(account?.name ?? 'B').charAt(0).toUpperCase()}</Text></View></View>
    <ScrollView contentContainerStyle={s.content} refreshControl={<RefreshControl refreshing={resource.loading} onRefresh={resource.refresh} tintColor={c.primary} />}>
      {resource.loading && !resource.data ? <ActivityIndicator color={c.primary} /> : null}
      {(resource.error || actionError) && <Text style={s.error}>{resource.error ?? actionError}</Text>}
      {section === 'Overview' && <>
        <View style={s.hero}><Text style={s.kicker}>BUSINESS WORKSPACE</Text><Text style={s.heroTitle}>Turn questions into decisions.</Text><Text style={s.heroBody}>Create campaigns, sponsor polls and follow audience response from one place.</Text></View>
        <View style={s.metricRow}><Metric label="Impressions" value={totals.impressions} s={s} /><Metric label="Responses" value={totals.votes} s={s} /></View>
        <View style={s.card}><View style={s.spread}><View><Text style={s.cardTitle}>Subscription</Text><Text style={s.muted}>{account?.subscriptionPlan ?? 'Starter'} · {account?.subscriptionStatus ?? 'Trialing'}</Text></View><Ionicons name="card-outline" size={24} color={c.primary} /></View>{account?.trialEndsAt && <Text style={s.small}>Trial ends {new Date(account.trialEndsAt).toLocaleDateString()}</Text>}</View>
        <Text style={s.sectionTitle}>Create a campaign</Text><View style={s.card}><Input label="Campaign name" value={name} onChangeText={setName} s={s} /><Input label="Objective" value={objective} onChangeText={setObjective} s={s} multiline /><Pressable disabled={creating || !name.trim() || !objective.trim()} onPress={createCampaign} style={[s.primary, (creating || !name.trim() || !objective.trim()) && s.disabled]}>{creating ? <ActivityIndicator color={c.primaryForeground} /> : <Text style={s.primaryText}>Create draft campaign</Text>}</Pressable></View>
      </>}
      {section === 'Campaigns' && <><Text style={s.pageTitle}>Campaigns</Text><Text style={s.muted}>Sponsored poll work stays in draft until it is reviewed.</Text>{campaigns.map(item => <View key={item.id} style={s.card}><View style={s.spread}><Text style={s.cardTitle}>{item.name}</Text><Text style={s.status}>{item.status}</Text></View><Text style={s.muted}>{item.objective}</Text><View style={s.metricRow}><Metric compact label="Views" value={item.impressions} s={s} /><Metric compact label="Votes" value={item.votes} s={s} /></View></View>)}{campaigns.length === 0 && <View style={s.card}><Text style={s.cardTitle}>No campaigns yet</Text><Text style={s.muted}>Create your first draft from Overview.</Text></View>}</>}
      {section === 'Brand' && <><Text style={s.pageTitle}>Brand studio</Text><View style={s.card}><Ionicons name="color-palette-outline" size={30} color={c.primary} /><Text style={s.cardTitle}>Your poll identity</Text><Text style={s.muted}>Logo, brand colours and sponsored disclosure settings will appear here. These controls will apply only to approved business polls.</Text><Text style={s.status}>Coming next</Text></View></>}
      {section === 'Account' && <><Text style={s.pageTitle}>Business account</Text><View style={s.card}><Text style={s.cardTitle}>{account?.name}</Text><Text style={s.muted}>@{user?.username}</Text>{account?.websiteUrl && <Text style={s.muted}>{account.websiteUrl}</Text>}<Text style={s.small}>This identity is separate from personal Pop accounts.</Text></View><Pressable onPress={() => void signOut()} style={s.secondary}><Text style={s.secondaryText}>Sign out of business</Text></Pressable></>}
    </ScrollView>
    <View style={s.nav}>{(['Overview', 'Campaigns', 'Brand', 'Account'] as Section[]).map((item, index) => <Pressable key={item} onPress={() => setSection(item)} style={s.navItem}><Ionicons name={(['analytics-outline','megaphone-outline','color-palette-outline','person-circle-outline'] as const)[index]} size={22} color={section === item ? c.primary : c.mutedForeground} /><Text style={[s.navText, section === item && { color: c.primary }]}>{item}</Text></Pressable>)}</View>
  </View>;
}

function Input({ label, s, ...props }: { label: string; s: ReturnType<typeof styles> } & ComponentProps<typeof TextInput>) { return <View style={{ gap: 7 }}><Text style={s.label}>{label}</Text><TextInput placeholderTextColor="#7b7b86" style={[s.input, props.multiline && { minHeight: 84, textAlignVertical: 'top' }]} {...props} /></View>; }
function Metric({ label, value, compact, s }: { label: string; value: number; compact?: boolean; s: ReturnType<typeof styles> }) { return <View style={[s.metric, compact && { backgroundColor: 'transparent', borderWidth: 0, padding: 0 }]}><Text style={s.metricValue}>{value.toLocaleString()}</Text><Text style={s.small}>{label}</Text></View>; }
const styles = (c: ThemeColors) => StyleSheet.create({ shell:{flex:1,backgroundColor:c.background},header:{paddingHorizontal:20,paddingVertical:14,flexDirection:'row',justifyContent:'space-between',alignItems:'center',borderBottomWidth:StyleSheet.hairlineWidth,borderColor:c.border},brand:{fontSize:22,fontWeight:'800',color:c.foreground},avatar:{width:42,height:42,borderRadius:14,backgroundColor:c.primary,alignItems:'center',justifyContent:'center'},avatarText:{color:c.primaryForeground,fontWeight:'800',fontSize:18},content:{padding:20,gap:16,paddingBottom:32},hero:{backgroundColor:c.foreground,borderRadius:radius['3xl'],padding:24,gap:10},kicker:{color:c.primary,fontSize:12,fontWeight:'800',letterSpacing:1.3},heroTitle:{color:c.background,fontSize:31,lineHeight:37,fontWeight:'800'},heroBody:{color:c.muted,fontSize:15,lineHeight:23},metricRow:{flexDirection:'row',gap:12},metric:{flex:1,backgroundColor:c.card,borderWidth:StyleSheet.hairlineWidth,borderColor:c.border,borderRadius:radius.xl,padding:17,gap:3},metricValue:{color:c.foreground,fontSize:24,fontWeight:'800'},small:{color:c.mutedForeground,fontSize:12,lineHeight:18},muted:{color:c.mutedForeground,fontSize:14,lineHeight:21},card:{backgroundColor:c.card,borderWidth:StyleSheet.hairlineWidth,borderColor:c.border,borderRadius:radius.xl,padding:18,gap:13},spread:{flexDirection:'row',justifyContent:'space-between',alignItems:'center',gap:12},cardTitle:{color:c.foreground,fontSize:18,fontWeight:'700'},sectionTitle:{color:c.foreground,fontSize:20,fontWeight:'800',marginTop:4},pageTitle:{color:c.foreground,fontSize:30,fontWeight:'800'},label:{color:c.foreground,fontSize:13,fontWeight:'700'},input:{borderWidth:1,borderColor:c.border,borderRadius:radius.lg,paddingHorizontal:14,minHeight:48,color:c.foreground,backgroundColor:c.background,fontSize:15},primary:{backgroundColor:c.primary,minHeight:50,borderRadius:radius.xl,alignItems:'center',justifyContent:'center'},primaryText:{color:c.primaryForeground,fontWeight:'800'},secondary:{backgroundColor:c.secondary,minHeight:50,borderRadius:radius.xl,alignItems:'center',justifyContent:'center'},secondaryText:{color:c.foreground,fontWeight:'700'},disabled:{opacity:.45},status:{color:c.primary,fontSize:12,fontWeight:'800',backgroundColor:`${c.primary}16`,paddingHorizontal:10,paddingVertical:6,borderRadius:99},error:{color:c.destructive},nav:{flexDirection:'row',borderTopWidth:StyleSheet.hairlineWidth,borderColor:c.border,backgroundColor:c.card,paddingTop:9,paddingBottom:spacing['3']},navItem:{flex:1,alignItems:'center',gap:4},navText:{color:c.mutedForeground,fontSize:10,fontWeight:'600'} });
