import { describe, expect, it, jest } from '@jest/globals';
import { invitationTokenFromUrl, recoverNativeShortcut, resolveShortcut } from './invitation';
import type { ShortcutFallback } from './invitation';

const host = 'https://pollify.example.com';
const valid = `${host}/live/join/abcdefghijklmnop`;

describe('PoP Live shortcut invitation coordinator', () => {
  it('accepts only canonical opaque HTTPS invitations', () => {
    expect(invitationTokenFromUrl(valid, host)).toBe('abcdefghijklmnop');
    expect(invitationTokenFromUrl('pollify://live/join/abcdefghijklmnop', host)).toBeNull();
    expect(invitationTokenFromUrl('https://evil.example/live/join/abcdefghijklmnop', host)).toBeNull();
    expect(invitationTokenFromUrl(`${valid}?state=vote`, host)).toBeNull();
  });

  it.each(['qr', 'link', 'shareplay', 'nfc'] as const)('uses one backend resolver for %s', async channel => {
    const resolve = jest.fn(async () => ({ status: 'resolved' as const, sessionId: 'server-session' }));
    const showQrAndLink = jest.fn<ShortcutFallback['showQrAndLink']>();
    await expect(resolveShortcut(channel, valid, host, { resolve }, { showQrAndLink })).resolves.toEqual({ status: 'resolved', sessionId: 'server-session' });
    expect(resolve).toHaveBeenCalledWith(valid);
    expect(showQrAndLink).not.toHaveBeenCalled();
  });

  it('falls back when validation fails', async () => {
    const showQrAndLink = jest.fn<ShortcutFallback['showQrAndLink']>();
    await resolveShortcut('nfc', valid, host, { resolve: async () => { throw new Error('expired'); } }, { showQrAndLink });
    expect(showQrAndLink).toHaveBeenCalledWith(valid, 'invalid_invitation');
  });

  it.each(['disabled', 'unsupported', 'denied', 'cancelled', 'timeout', 'native_error'] as const)('recovers %s through QR/link', async reason => {
    const showQrAndLink = jest.fn<ShortcutFallback['showQrAndLink']>();
    await recoverNativeShortcut(valid, reason, { showQrAndLink });
    expect(showQrAndLink).toHaveBeenCalledWith(valid, reason);
  });
});
