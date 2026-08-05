export interface AuthUser {
  id: number;
  username: string;
  displayName: string;
  email?: string | null;
  avatarUrl?: string | null;
  authProvider: 'local' | 'google' | string;
  xp: number;
  streak: number;
  longestStreak: number;
  totalVotes: number;
  pollsCreated: number;
  lastVoteDate?: string | null;
  createdAt: string;
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
}
