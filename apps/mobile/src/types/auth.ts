export interface AuthUser {
  id: number;
  username: string;
  displayName: string;
  email?: string | null;
  avatarUrl?: string | null;
  authProvider: 'local' | 'google' | string;
  accountType: 'Personal' | 'Business' | string;
  xp: number;
  streak: number;
  longestStreak: number;
  totalVotes: number;
  pollsCreated: number;
  lastVoteDate?: string | null;
  createdAt: string;
  level: number;
  progression: ProgressionSnapshot;
}

export interface ProgressionSnapshot {
  totalXp: number;
  level: number;
  currentLevelXp: number;
  nextLevelXp: number;
  xpIntoLevel: number;
  xpRequiredForNextLevel: number;
  progressPercent: number;
}

export interface CategoryPreference {
  category: string;
  isExplicit: boolean;
  voteCount: number;
}

export interface AuthResponse {
  token: string;
  user: AuthUser;
}

export interface LoginPayload {
  username: string;
  password: string;
}

export interface RegisterPayload extends LoginPayload {
  displayName: string;
  confirmPassword: string;
  acceptTerms: boolean;
}

export interface RegisterBusinessPayload extends RegisterPayload {
  businessName: string;
  websiteUrl?: string;
  subscriptionPlan: 'Starter' | 'Growth' | 'Pro';
}
