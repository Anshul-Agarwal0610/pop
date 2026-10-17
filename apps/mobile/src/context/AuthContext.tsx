import {
  createContext,
  type PropsWithChildren,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from 'react';

import { authApi, usersApi } from '../lib/api';
import { getAnalyticsConsent, setAnalyticsConsent } from '../lib/analytics/privacy';
import { clearSession, getStoredUser, getToken, saveSession } from '../lib/session';
import type { AuthUser, LoginPayload, RegisterBusinessPayload, RegisterPayload } from '../types/auth';
import type { VoteReward } from '../types/poll';
import { configureHttpAnalytics, identify, resetAnalytics } from '../lib/analytics/client';

interface AuthContextValue {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  signIn: (payload: LoginPayload) => Promise<void>;
  signUp: (payload: RegisterPayload) => Promise<void>;
  signInWithGoogle: (idToken: string, acceptTerms: boolean) => Promise<void>;
  signInBusiness: (payload: LoginPayload) => Promise<void>;
  signUpBusiness: (payload: RegisterBusinessPayload) => Promise<void>;
  signOut: () => Promise<void>;
  refreshUser: () => Promise<void>;
  applyVoteReward: (reward: VoteReward) => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: PropsWithChildren) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => { configureHttpAnalytics(process.env.EXPO_PUBLIC_ANALYTICS_CAPTURE_URL); return () => configureHttpAnalytics(); }, []);
  useEffect(() => { if (!isLoading && user) void identify(user.id); }, [isLoading, user?.id]);
  useEffect(() => { if (!isLoading && user) void usersApi.getAnalyticsPrivacy().then(async ({ consent }) => { const local = await getAnalyticsConsent(); await setAnalyticsConsent(consent === 'denied' || local === 'denied' ? 'denied' : consent === 'granted' && local === 'granted' ? 'granted' : 'unknown'); }).catch(() => undefined); }, [isLoading, user?.id]);

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
    void identify(session.user.id);
  }, []);

  const signUp = useCallback(async (payload: RegisterPayload) => {
    const session = await authApi.register(payload);
    await saveSession(session);
    setUser(session.user);
    void identify(session.user.id);
  }, []);

  const signInWithGoogle = useCallback(async (idToken: string, acceptTerms: boolean) => {
    const session = await authApi.google(idToken, acceptTerms);
    await saveSession(session);
    setUser(session.user);
    void identify(session.user.id);
  }, []);

  const signInBusiness = useCallback(async (payload: LoginPayload) => {
    const session = await authApi.businessLogin(payload); await saveSession(session); setUser(session.user);
  }, []);
  const signUpBusiness = useCallback(async (payload: RegisterBusinessPayload) => {
    const session = await authApi.businessRegister(payload); await saveSession(session); setUser(session.user);
  }, []);

  const signOut = useCallback(async () => {
    await clearSession();
    setUser(null);
    void resetAnalytics();
  }, []);

  const refreshUser = useCallback(async () => {
    const token = await getToken();
    if (!token) return;
    const current = await authApi.me();
    await saveSession({ token, user: current });
    setUser(current);
  }, []);

  const applyVoteReward = useCallback((reward: VoteReward) => {
    setUser((current) =>
      current
        ? {
            ...current,
            xp: reward.xp,
            level: reward.progression.level,
            progression: reward.progression,
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
      signInWithGoogle,
      signInBusiness,
      signUpBusiness,
      signOut,
      refreshUser,
      applyVoteReward,
    }),
    [applyVoteReward, isLoading, refreshUser, signIn, signInBusiness, signInWithGoogle, signOut, signUp, signUpBusiness, user],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used inside AuthProvider');
  return context;
}
