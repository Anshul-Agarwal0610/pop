import { pollTossApi } from '../../lib/api';
import type { NearbyAdapter } from '../../native/nearbyPollToss';
import type { NearbyPollTossExperiment } from '../../types/pollToss';

export async function checkNearbyExperiment(): Promise<NearbyPollTossExperiment|null> {
  try { const config=await pollTossApi.experiments(); return config.nearbyPollToss?.enabled ? config.nearbyPollToss : null; }
  catch { return null; }
}

export class PollTossSession {
  private stopped=false;
  constructor(private native:NearbyAdapter) {}
  async stop() { if (this.stopped) return; this.stopped=true; await this.native.stop(); }
  async prepare():Promise<NearbyPollTossExperiment|null> {
    const config=await checkNearbyExperiment(); if (!config) return null;
    const capability=await this.native.capabilities();
    if (!capability.supported || !capability.playServices || !capability.radiosAvailable) return null;
    if (!await this.native.requestPermissions()) return null;
    return config;
  }
}
