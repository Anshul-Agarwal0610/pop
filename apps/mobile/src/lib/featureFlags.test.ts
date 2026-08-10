import { describe, expect, it } from '@jest/globals';
import { getFeatureFlag } from './featureFlags';

describe('PoP Live experiment flags', () => {
  it('are independently assignable and default off when rollout is zero', () => {
    expect(getFeatureFlag('pop_live_shareplay_spike_v1', 'device-1', 0)).toBe(false);
    expect(getFeatureFlag('pop_live_nfc_spike_v1', 'device-1', 0)).toBe(false);
    expect(getFeatureFlag('pop_live_shareplay_spike_v1', 'device-1', 100)).toBe(true);
    expect(getFeatureFlag('pop_live_nfc_spike_v1', 'device-1', 0)).toBe(false);
  });
});
