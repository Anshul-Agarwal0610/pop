export interface ApiPollOption {
  id: number;
  pollId: number;
  text: string;
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
  expiresAt: string;
  createdAt: string;
  totalVotes: number;
  createdByUserId: number | null;
  createdByUsername: string | null;
  createdByDisplayName: string | null;
  sourceType: string | null;
  sourceUrl: string | null;
  thumbnailUrl: string | null;
  isAIGenerated: boolean;
  options: ApiPollOption[];
  hasVoted: boolean;
  userVotedOptionId: number | null;
}

import type { ProgressionSnapshot } from './auth';

export interface VoteReward {
  xp: number;
  streak: number;
  totalVotes: number;
  xpAwarded: number;
  streakAdvanced: boolean;
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
