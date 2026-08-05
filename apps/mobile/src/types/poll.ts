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
}

export interface CastVoteResponse {
  poll: ApiPoll;
  reward: VoteReward;
}
