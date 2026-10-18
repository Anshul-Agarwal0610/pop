import { createContext, useContext, useMemo, useRef, useState, type ReactNode } from 'react';
import { ActivityIndicator, Alert, Modal, Platform, Pressable, ScrollView, Text, useWindowDimensions, View } from 'react-native';
import Svg, { Path, Rect } from 'react-native-svg';
import QRCode from 'qrcode';
import { captureRef } from 'react-native-view-shot';
import * as Sharing from 'expo-sharing';
import { PoPmoji, type PollReaction } from './CreativePollHero';
import { pollShareUrl, sharePollLink, sharePollQr } from '../lib/sharePoll';
import type { ApiPoll } from '../types/poll';

type ShareablePoll = Pick<ApiPoll, 'id' | 'question'> & Partial<ApiPoll>;
type ShareContextValue = (poll: ShareablePoll) => void;
const ShareContext = createContext<ShareContextValue | null>(null);

export function usePollShare() {
  const open = useContext(ShareContext);
  if (!open) throw new Error('PollShareProvider is missing');
  return open;
}

function PollQr({ url }: { url: string }) {
  const path = useMemo(() => {
    const modules = QRCode.create(url, { errorCorrectionLevel: 'M' }).modules;
    const commands: string[] = [];
    for (let y = 0; y < modules.size; y++) for (let x = 0; x < modules.size; x++) {
      if (modules.get(y, x)) commands.push(`M${x + 4} ${y + 4}h1v1h-1z`);
    }
    return { d: commands.join(''), size: modules.size + 8 };
  }, [url]);
  return <Svg width={112} height={112} viewBox={`0 0 ${path.size} ${path.size}`} accessibilityLabel="QR code for this poll">
    <Rect width={path.size} height={path.size} fill="#FFFFFF" />
    <Path d={path.d} fill="#211B27" />
  </Svg>;
}

function PollCard({ poll, width }: { poll: ShareablePoll; width: number }) {
  const template = poll.creativeTemplate === 'spotlight' ? 'spotlight' : poll.creativeTemplate === 'classic' ? 'classic' : 'pulse';
  const night = template === 'spotlight';
  const background = night ? '#242037' : template === 'classic' ? '#FFFDF9' : '#F8F0E9';
  const ink = night ? '#FFFFFF' : '#24202C';
  const muted = night ? '#DDD4E9' : '#5B515C';
  const reaction = poll.reactionId as PollReaction | undefined;
  const url = pollShareUrl(poll.id);
  return <View style={{ width, backgroundColor: background, padding: 22, overflow: 'hidden' }}>
    <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' }}>
      <Text style={{ color: ink, fontFamily: Platform.OS === 'ios' ? 'Georgia' : 'serif', fontSize: 31, fontWeight: '900', letterSpacing: -2 }}>PoP.</Text>
      <Text style={{ color: night ? '#D6B7FF' : '#DB4B66', fontSize: 10, fontWeight: '900', letterSpacing: 2 }}>PICK A SIDE</Text>
    </View>
    <View style={{ height: 1, backgroundColor: night ? '#665B74' : '#D9CBD2', marginTop: 14 }} />
    <Text style={{ color: night ? '#D6B7FF' : '#BD405E', marginTop: 20, fontSize: 11, fontWeight: '900', letterSpacing: 1.3, textTransform: 'uppercase' }}>{poll.category || 'The public question'}{poll.isPrivate ? '  ·  PRIVATE' : ''}</Text>
    <Text style={{ color: ink, fontFamily: Platform.OS === 'ios' ? 'Georgia' : 'serif', fontSize: 31, lineHeight: 36, fontWeight: '700', letterSpacing: -1.2, marginTop: 12 }}>{poll.question}</Text>
    {!!poll.description && <Text numberOfLines={3} style={{ color: muted, fontSize: 13, lineHeight: 19, marginTop: 12 }}>{poll.description}</Text>}
    {reaction && ['big_yes', 'thinking', 'no_way', 'wow'].includes(reaction) && <View style={{ alignSelf: 'flex-end', marginTop: 4 }}><PoPmoji id={reaction} size={65} /></View>}
    <View style={{ flexDirection: 'row', height: 112, marginTop: 22, borderWidth: 2, borderColor: night ? '#DDD4E9' : '#24202C', borderRadius: 15, overflow: 'hidden' }}>
      <View style={{ flex: 1, backgroundColor: night ? '#71628D' : '#DCCFFA', padding: 15, justifyContent: 'space-between' }}><Text style={{ color: '#372B4E', fontWeight: '900', fontSize: 12 }}>01  ↙</Text><Text style={{ color: '#27213D', fontFamily: Platform.OS === 'ios' ? 'Georgia' : 'serif', fontSize: 24, fontWeight: '700' }}>Against</Text></View>
      <View style={{ width: 3, backgroundColor: '#FFFFFF' }} />
      <View style={{ flex: 1, backgroundColor: night ? '#DE796D' : '#FF9F90', padding: 15, justifyContent: 'space-between' }}><Text style={{ color: '#75382F', fontWeight: '900', fontSize: 12 }}>02  ↗</Text><Text style={{ color: '#3D201F', fontFamily: Platform.OS === 'ios' ? 'Georgia' : 'serif', fontSize: 24, fontWeight: '700' }}>Support</Text></View>
    </View>
    <View style={{ flexDirection: 'row', alignItems: 'center', gap: 13, marginTop: 20 }}>
      <PollQr url={url} />
      <View style={{ flex: 1 }}><Text style={{ color: ink, fontSize: 17, lineHeight: 21, fontWeight: '900' }}>Where do you stand?</Text><Text style={{ color: muted, fontSize: 12, lineHeight: 17, marginTop: 7 }}>Scan to vote on PoP.</Text>{poll.isPrivate && <Text style={{ color: muted, fontSize: 10, marginTop: 6 }}>Only invited people can open this poll.</Text>}</View>
    </View>
    <Text numberOfLines={1} style={{ color: muted, fontSize: 10, marginTop: 15 }}>{url}</Text>
  </View>;
}

export function PollShareProvider({ children }: { children: ReactNode }) {
  const { width } = useWindowDimensions();
  const [poll, setPoll] = useState<ShareablePoll | null>(null);
  const [busy, setBusy] = useState(false);
  const cardRef = useRef<View>(null);
  const open = (next: ShareablePoll) => {
    if (next.id <= 0) return Alert.alert('Sample poll', 'This demo poll has no public link to share.');
    setPoll(next);
  };
  const perform = async (action: () => Promise<unknown>) => {
    if (busy) return;
    setBusy(true);
    try { await action(); }
    catch (error) { Alert.alert('Could not share', error instanceof Error ? error.message : 'Please try again.'); }
    finally { setBusy(false); }
  };
  const shareImage = () => perform(async () => {
    if (!cardRef.current) throw new Error('The poll card is still loading. Please try again.');
    if (!await Sharing.isAvailableAsync()) throw new Error('File sharing is unavailable on this device.');
    const uri = await captureRef(cardRef, { format: 'png', quality: 1, result: 'tmpfile' });
    await Sharing.shareAsync(uri, { mimeType: 'image/png', UTI: 'public.png', dialogTitle: 'Share PoP poll card' });
  });
  return <ShareContext.Provider value={open}>{children}<Modal visible={poll !== null} transparent animationType="slide" onRequestClose={() => !busy && setPoll(null)}>
    <View style={{ flex: 1, backgroundColor: '#000000CC', justifyContent: 'flex-end' }}>
      <View style={{ maxHeight: '95%', backgroundColor: '#171420', borderTopLeftRadius: 24, borderTopRightRadius: 24, paddingTop: 16, paddingBottom: 30 }}>
        <View style={{ flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', paddingHorizontal: 20, marginBottom: 14 }}><Text style={{ color: '#FFFFFF', fontSize: 18, fontWeight: '800' }}>Share this poll</Text><Pressable accessibilityRole="button" accessibilityLabel="Close share preview" onPress={() => setPoll(null)} disabled={busy}><Text style={{ color: '#FFFFFF', fontSize: 25 }}>×</Text></Pressable></View>
        <ScrollView style={{ flexShrink: 1 }} contentContainerStyle={{ alignItems: 'center', paddingBottom: 12 }}><View ref={cardRef} collapsable={false}>{poll && <PollCard poll={poll} width={Math.min(340, width - 32)} />}</View></ScrollView>
        <View style={{ paddingHorizontal: 20, gap: 9 }}>
          <Pressable accessibilityRole="button" accessibilityLabel="Share poll card image" disabled={busy} onPress={() => void shareImage()} style={{ backgroundColor: '#F76271', borderRadius: 13, padding: 14, alignItems: 'center' }}>{busy ? <ActivityIndicator color="#241923" /> : <Text style={{ color: '#241923', fontWeight: '900' }}>Share card image</Text>}</Pressable>
          <View style={{ flexDirection: 'row', gap: 9 }}><Pressable accessibilityRole="button" disabled={busy} onPress={() => poll && void perform(() => sharePollLink(poll))} style={{ flex: 1, borderColor: '#665B74', borderWidth: 1, borderRadius: 13, padding: 12, alignItems: 'center' }}><Text style={{ color: '#FFFFFF', fontWeight: '700' }}>Share link</Text></Pressable><Pressable accessibilityRole="button" disabled={busy} onPress={() => poll && void perform(() => sharePollQr(poll))} style={{ flex: 1, borderColor: '#665B74', borderWidth: 1, borderRadius: 13, padding: 12, alignItems: 'center' }}><Text style={{ color: '#FFFFFF', fontWeight: '700' }}>Share QR</Text></Pressable></View>
        </View>
      </View>
    </View>
  </Modal></ShareContext.Provider>;
}
