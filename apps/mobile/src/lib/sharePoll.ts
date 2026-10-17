import { Alert, Share } from 'react-native';
import type { ApiPoll } from '../types/poll';

const DEFAULT_PUBLIC_WEB_URL = 'https://mvp-react-frontend.azurewebsites.net';

export function pollShareUrl(pollId: number) {
  const webUrl = (process.env.EXPO_PUBLIC_WEB_URL || DEFAULT_PUBLIC_WEB_URL).replace(/\/$/, '');
  return `${webUrl}/polls/${pollId}`;
}

export function pollShareMessage(poll: Pick<ApiPoll, 'id' | 'question'>) {
  const url = pollShareUrl(poll.id);
  const invitation = 'What do you think? Join the conversation on Pop.';
  return url
    ? `${poll.question}\n\n${invitation}\n${url}`
    : `${poll.question}\n\n${invitation}`;
}

export async function sharePollLink(poll: Pick<ApiPoll, 'id' | 'question'>) {
  await Share.share({ message: pollShareMessage(poll), title: poll.question });
}

export async function sharePollQr(poll: Pick<ApiPoll, 'id' | 'question'>) {
  const url = pollShareUrl(poll.id);
  const [{ default: QRCode }, FileSystem, Sharing] = await Promise.all([
    import('qrcode'), import('expo-file-system/legacy'), import('expo-sharing'),
  ]);
  if (!await Sharing.isAvailableAsync()) throw new Error('File sharing is unavailable on this device.');
  const svg = await QRCode.toString(url, { type: 'svg', width: 1024, margin: 4, errorCorrectionLevel: 'H', color: { dark: '#17121F', light: '#FFFDF9' } });
  const uri = `${FileSystem.cacheDirectory}pop-poll-${poll.id}-${Date.now()}.svg`;
  await FileSystem.writeAsStringAsync(uri, svg, { encoding: FileSystem.EncodingType.UTF8 });
  try { await Sharing.shareAsync(uri, { mimeType: 'image/svg+xml', dialogTitle: `Share QR code for ${poll.question}`, UTI: 'public.svg-image' }); }
  finally { await FileSystem.deleteAsync(uri, { idempotent: true }); }
}

export function sharePoll(poll: Pick<ApiPoll, 'id' | 'question'>) {
  Alert.alert('Share poll', 'Choose how you want to share this poll.', [
    { text: 'Share link', onPress: () => { void sharePollLink(poll).catch(() => Alert.alert('Could not share', 'Please try again.')); } },
    { text: 'Share QR code', onPress: () => { void sharePollQr(poll).catch(error => Alert.alert('Could not share QR code', error instanceof Error ? error.message : 'Please try again.')); } },
    { text: 'Cancel', style: 'cancel' },
  ]);
}
