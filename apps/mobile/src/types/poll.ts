export interface ApiPollOption {
  id: number;
  pollId: number;
  text: string;
  side?: string | null;
  voteCount: number;
  votePercentage: number;
}

export interface ApiPoll {
  id: number;
  question: string;
  description: string;
  category: string;
  isTrending: boolean;
  isActive: boolean;
  moderationStatus?: 'Draft' | 'PendingReview' | 'Published' | 'Rejected' | 'Flagged' | string;
  moderationReason?: string | null;
  expiresAt: string;
  createdAt: string;
  totalVotes: number;
  createdByUserId: number | null;
  createdByUsername: string | null;
  createdByDisplayName: string | null;
  isPrivate?: boolean;
  isWellness?: boolean;
  audienceType?: 'Public' | 'Friend' | 'Group';
  audienceFriendUserId?: number | null;
  audienceGroupId?: number | null;
  sourceType: string | null;
  sourceUrl: string | null;
  thumbnailUrl: string | null;
  mediaType?: 'image' | 'video' | string | null;
  creativeTemplate?: 'classic' | 'pulse' | 'spotlight' | string | null;
  reactionId?: 'big_yes' | 'thinking' | 'no_way' | 'wow' | string | null;
  resultVisibility?: 'Immediate' | 'AfterVote' | 'AfterClose';
  canViewResults?: boolean;
  remixedFromPollId?: number | null;
  remixedFromQuestion?: string | null;
  remixedFromCreatorName?: string | null;
  isAIGenerated: boolean;
  isSponsored?: boolean;
  sponsorName?: string | null;
  options: ApiPollOption[];
  hasVoted: boolean;
  userVotedOptionId: number | null;
}

import type { ProgressionSnapshot } from './auth';

export interface VoteReward {
  xp: number;
  streak: number;
  longestStreak: number;
  totalVotes: number;
  xpAwarded: number;
  streakAdvanced: boolean;
  todayComplete: boolean;
  recoveryEligible: boolean;
  recoveryUsed: boolean;
  nextRecoveryAt: string | null;
  milestoneReached: number | null;
  lastVoteDate: string | null;
  awardedXp: number;
  progression: ProgressionSnapshot;
  previousLevel: number;
  leveledUp: boolean;
  levelsGained: number;
  events: Array<{ type: 'Vote' | 'Challenge' | 'Achievement'; sourceId: string; awardedXp: number; label: string | null }>;
}

export interface CastVoteResponse {
  poll: ApiPoll;
  reward: VoteReward;
}
