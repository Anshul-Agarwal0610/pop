import {
  createContext,
  type PropsWithChildren,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from 'react';

import { authApi } from '../lib/api';
import { clearSession, getStoredUser, getToken, saveSession } from '../lib/session';
import type { AuthUser, LoginPayload, RegisterPayload } from '../types/auth';
import type { VoteReward } from '../types/poll';

interface AuthContextValue {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  signIn: (payload: LoginPayload) => Promise<void>;
  signUp: (payload: RegisterPayload) => Promise<void>;
  signOut: () => Promise<void>;
  applyVoteReward: (reward: VoteReward) => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: PropsWithChildren) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let isMounted = true;

    async function hydrateSession() {
      try {
        const [storedUser, token] = await Promise.all([getStoredUser(), getToken()]);
        if (!isMounted) return;

        if (!token) {
          setUser(null);
          return;
        }

        setUser(storedUser);

        try {
          const freshUser = await authApi.me();
          if (isMounted) setUser(freshUser);
        } catch {
          await clearSession();
          if (isMounted) setUser(null);
        }
      } finally {
        if (isMounted) setIsLoading(false);
      }
    }

    hydrateSession();

    return () => {
      isMounted = false;
    };
  }, []);

  const signIn = useCallback(async (payload: LoginPayload) => {
    const session = await authApi.login(payload);
    await saveSession(session);
    setUser(session.user);
  }, []);

  const signUp = useCallback(async (payload: RegisterPayload) => {
    const session = await authApi.register(payload);
    await saveSession(session);
    setUser(session.user);
  }, []);

  const signOut = useCallback(async () => {
    await clearSession();
    setUser(null);
  }, []);

  const applyVoteReward = useCallback((reward: VoteReward) => {
    setUser((current) =>
      current
        ? {
            ...current,
            xp: reward.xp,
            streak: reward.streak,
            longestStreak: reward.longestStreak,
            totalVotes: reward.totalVotes,
            lastVoteDate: reward.lastVoteDate,
          }
        : current,
    );
  }, []);

  const value = useMemo(
    () => ({
      user,
      isAuthenticated: Boolean(user),
      isLoading,
      signIn,
      signUp,
      signOut,
      applyVoteReward,
    }),
    [applyVoteReward, isLoading, signIn, signOut, signUp, user],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used inside AuthProvider');
  return context;
}
