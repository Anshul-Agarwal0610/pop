import type { ApiPoll } from './poll';

export interface NearbyPollTossExperiment { enabled: boolean; discoveryTimeoutSeconds: number; invitationTtlSeconds: number }
export interface MobileExperiments { nearbyPollToss: NearbyPollTossExperiment }
export interface PollTossInvitation { id: string; invitationToken: string; expiresAt: string; shareUrl: string }
export interface PollTossPayload { version: 1; invitationToken: string }
export type RedeemedPollToss = ApiPoll;
