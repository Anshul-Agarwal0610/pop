export type ShortcutChannel = 'qr' | 'link' | 'shareplay' | 'nfc';
export type FallbackReason = 'disabled' | 'unsupported' | 'denied' | 'cancelled' | 'timeout' | 'invalid_invitation' | 'native_error';

export interface InvitationResolution {
  status: 'resolved';
  sessionId: string;
}

export interface InvitationResolver {
  resolve(invitationUrl: string): Promise<InvitationResolution>;
}

export interface ShortcutFallback {
  showQrAndLink(invitationUrl: string, reason: FallbackReason): void | Promise<void>;
}

export function invitationTokenFromUrl(value: string, publicHost: string): string | null {
  try {
    const url = new URL(value);
    const expected = new URL(publicHost);
    if (url.protocol !== 'https:' || url.origin !== expected.origin || url.search || url.hash) return null;
    const match = url.pathname.match(/^\/live\/join\/([A-Za-z0-9_-]{16,256})$/);
    return match?.[1] ?? null;
  } catch {
    return null;
  }
}

export async function resolveShortcut(
  channel: ShortcutChannel,
  invitationUrl: string,
  publicHost: string,
  resolver: InvitationResolver,
  fallback: ShortcutFallback,
): Promise<InvitationResolution | null> {
  if (!invitationTokenFromUrl(invitationUrl, publicHost)) {
    await fallback.showQrAndLink(invitationUrl, 'invalid_invitation');
    return null;
  }
  try {
    return await resolver.resolve(invitationUrl);
  } catch {
    await fallback.showQrAndLink(invitationUrl, 'invalid_invitation');
    return null;
  }
}

export async function recoverNativeShortcut(invitationUrl: string, reason: FallbackReason, fallback: ShortcutFallback) {
  await fallback.showQrAndLink(invitationUrl, reason);
}
