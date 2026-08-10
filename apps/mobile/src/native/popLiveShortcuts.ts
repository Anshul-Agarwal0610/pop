import type { FallbackReason } from '../features/pop-live/invitation';

export type NativeShortcutResult = { outcome: 'started' } | { outcome: 'fallback'; reason: FallbackReason };

/** Native adapters are entry/presence transports only. They deliberately expose no game commands. */
export interface PopLiveShortcutAdapter {
  isEligible(): Promise<boolean>;
  startActivity(invitationUrl: string): Promise<NativeShortcutResult>;
  cancel(): Promise<void>;
}

export const unavailableShortcutAdapter: PopLiveShortcutAdapter = {
  async isEligible() { return false; },
  async startActivity() { return { outcome: 'fallback', reason: 'unsupported' }; },
  async cancel() {},
};
