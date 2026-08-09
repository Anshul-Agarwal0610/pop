import { StatusBar } from 'expo-status-bar';
import { useCallback, useEffect, useRef, useState } from 'react';
import {
  ActivityIndicator,
  FlatList,
  KeyboardAvoidingView,
  Platform,
  Pressable,
  RefreshControl,
  SafeAreaView,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native';

import { API_BASE_URL } from './src/config/api';
import { AuthProvider, useAuth } from './src/context/AuthContext';
import { notificationsApi, pollsApi, usersApi, votesApi } from './src/lib/api';
import { getExpoPushToken } from './src/lib/pushNotifications';
import { hasCompletedOnboarding, markOnboardingComplete } from './src/lib/session';
import type { AuthUser } from './src/types/auth';
import type { ApiPoll, ApiPollOption, VoteReward } from './src/types/poll';
import { track } from './src/lib/analytics/client';
import { theme, typography, spacing, radius } from './src/theme';

type AuthMode = 'login' | 'register';
type SignedInTab = 'home' | 'profile' | 'leaderboard';

const onboardingCategories = [
  'Technology',
  'Society',
  'Work',
  'Environment',
  'Culture',
  'Sports',
  'Health',
  'Politics',
];

export default function App() {
  return (
    <AuthProvider>
      <PollifyApp />
    </AuthProvider>
  );
}

function PollifyApp() {
  const { isAuthenticated, isLoading, user } = useAuth();
  const [onboardingComplete, setOnboardingComplete] = useState(false);
  const [isCheckingOnboarding, setIsCheckingOnboarding] = useState(false);

  useEffect(() => {
    let isMounted = true;

    async function loadOnboardingState() {
      if (!user) {
        setOnboardingComplete(false);
        setIsCheckingOnboarding(false);
        return;
      }

      setIsCheckingOnboarding(true);
      try {
        const completed = await hasCompletedOnboarding(user.id);
        if (isMounted) setOnboardingComplete(completed);
      } finally {
        if (isMounted) setIsCheckingOnboarding(false);
      }
    }

    loadOnboardingState();

    return () => {
      isMounted = false;
    };
  }, [user]);

  return (
    <SafeAreaView style={styles.safeArea}>
      <StatusBar style="dark" />
      {isLoading || isCheckingOnboarding ? (
        <LoadingScreen />
      ) : isAuthenticated && user && !onboardingComplete ? (
        <OnboardingScreen
          onComplete={() => setOnboardingComplete(true)}
          user={user}
        />
      ) : isAuthenticated ? (
        <SignedInHome />
      ) : (
        <AuthScreen />
      )}
    </SafeAreaView>
  );
}

function LoadingScreen() {
  return (
    <View style={styles.loading}>
      <ActivityIndicator color={theme.destructive} size="large" />
      <Text style={styles.loadingText}>Checking your Pollify session</Text>
    </View>
  );
}

function AuthScreen() {
  const { signIn, signUp } = useAuth();
  const [mode, setMode] = useState<AuthMode>('login');
  const [username, setUsername] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const isRegister = mode === 'register';

  async function submit() {
    setError(null);
    setIsSubmitting(true);

    try {
      if (isRegister) {
        await signUp({
          username: username.trim().toLowerCase(),
          displayName: displayName.trim(),
          password,
          confirmPassword,
        });
      } else {
        await signIn({
          username: username.trim().toLowerCase(),
          password,
        });
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Authentication failed');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <KeyboardAvoidingView
      behavior={Platform.OS === 'ios' ? 'padding' : undefined}
      style={styles.keyboard}
    >
      <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled">
        <View style={styles.hero}>
          <Text style={styles.eyebrow}>Pollify Mobile</Text>
          <Text style={styles.title}>Vote daily. Keep the streak alive.</Text>
          <Text style={styles.subtitle}>
            Sign in once and Pollify keeps your JWT session in secure native storage.
          </Text>
        </View>

        <View style={styles.segment}>
          <Pressable
            onPress={() => {
              setMode('login');
              setError(null);
            }}
            style={[styles.segmentButton, !isRegister && styles.segmentButtonActive]}
          >
            <Text style={[styles.segmentText, !isRegister && styles.segmentTextActive]}>
              Login
            </Text>
          </Pressable>
          <Pressable
            onPress={() => {
              setMode('register');
              setError(null);
            }}
            style={[styles.segmentButton, isRegister && styles.segmentButtonActive]}
          >
            <Text style={[styles.segmentText, isRegister && styles.segmentTextActive]}>
              Register
            </Text>
          </Pressable>
        </View>

        <View style={styles.formPanel}>
          <Text style={styles.panelTitle}>
            {isRegister ? 'Create your Pollify account' : 'Welcome back'}
          </Text>

          <LabeledInput
            autoCapitalize="none"
            label="Username"
            onChangeText={setUsername}
            placeholder="anshul_01"
            value={username}
          />

          {isRegister && (
            <LabeledInput
              label="Display name"
              onChangeText={setDisplayName}
              placeholder="Anshul"
              value={displayName}
            />
          )}

          <LabeledInput
            label="Password"
            onChangeText={setPassword}
            placeholder="Enter password"
            secureTextEntry
            value={password}
          />

          {isRegister && (
            <LabeledInput
              label="Confirm password"
              onChangeText={setConfirmPassword}
              placeholder="Repeat password"
              secureTextEntry
              value={confirmPassword}
            />
          )}

          {error && <Text style={styles.errorText}>{error}</Text>}

          <Pressable
            disabled={isSubmitting}
            onPress={submit}
            style={[styles.primaryButton, isSubmitting && styles.primaryButtonDisabled]}
          >
            {isSubmitting ? (
              <ActivityIndicator color={theme.primaryForeground} />
            ) : (
              <Text style={styles.primaryButtonText}>
                {isRegister ? 'Create account' : 'Login'}
              </Text>
            )}
          </Pressable>
        </View>

        <View style={styles.apiPanel}>
          <Text style={styles.label}>Backend API</Text>
          <Text style={styles.value}>{API_BASE_URL}</Text>
        </View>
      </ScrollView>
    </KeyboardAvoidingView>
  );
}

interface LabeledInputProps {
  autoCapitalize?: 'none' | 'sentences' | 'words' | 'characters';
  label: string;
  onChangeText: (value: string) => void;
  placeholder: string;
  secureTextEntry?: boolean;
  value: string;
}

function LabeledInput({
  autoCapitalize = 'sentences',
  label,
  onChangeText,
  placeholder,
  secureTextEntry,
  value,
}: LabeledInputProps) {
  return (
    <View style={styles.field}>
      <Text style={styles.inputLabel}>{label}</Text>
      <TextInput
        autoCapitalize={autoCapitalize}
        autoCorrect={false}
        onChangeText={onChangeText}
        placeholder={placeholder}
        placeholderTextColor={theme.mutedForeground}
        secureTextEntry={secureTextEntry}
        style={styles.input}
        value={value}
      />
    </View>
  );
}

function OnboardingScreen({
  onComplete,
  user,
}: {
  onComplete: () => void;
  user: AuthUser;
}) {
  const [step, setStep] = useState(0);
  const [selectedCategories, setSelectedCategories] = useState<string[]>([]);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isPreferenceStep = step === 2;

  function toggleCategory(category: string) {
    setSelectedCategories((current) =>
      current.includes(category)
        ? current.filter((item) => item !== category)
        : [...current, category],
    );
  }

  async function finish(skipPreferences = false) {
    setIsSaving(true);
    setError(null);

    try {
      if (!skipPreferences && selectedCategories.length > 0) {
        await usersApi.updateCategoryPreferences(selectedCategories);
      }

      await markOnboardingComplete(user.id);
      onComplete();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not save onboarding');
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <ScrollView contentContainerStyle={styles.onboardingContainer}>
      <View style={styles.onboardingHeader}>
        <Text style={styles.eyebrow}>Welcome, {user.displayName}</Text>
        <Text style={styles.titleSmall}>
          {step === 0
            ? 'Vote in seconds'
            : step === 1
              ? 'Build your streak'
              : 'Tune your feed'}
        </Text>
        <Text style={styles.subtitle}>
          {step === 0
            ? 'Open Pollify, answer a quick poll, see live results, and keep moving.'
            : step === 1
              ? 'Every vote earns XP. Daily activity keeps your streak alive and helps you climb ranks.'
              : 'Pick a few categories so your first mobile feed feels closer to your interests.'}
        </Text>
      </View>

      <View style={styles.onboardingCard}>
        {step === 0 && (
          <>
            <OnboardingPoint title="Fresh polls" copy="Trending questions are pulled from the backend feed." />
            <OnboardingPoint title="Fast voting" copy="Tap an option, get results, and continue your session." />
            <OnboardingPoint title="Share-worthy results" copy="Public polls can be shared once you want to bring friends in." />
          </>
        )}

        {step === 1 && (
          <>
            <OnboardingPoint title="XP rewards" copy="Votes update your XP and level right away." />
            <OnboardingPoint title="Daily streaks" copy="Come back daily to keep your streak growing." />
            <OnboardingPoint title="Ranks" copy="The leaderboard uses real backend XP data, no mock scores." />
          </>
        )}

        {isPreferenceStep && (
          <>
            <Text style={styles.panelTitle}>Choose categories</Text>
            <View style={styles.categoryGrid}>
              {onboardingCategories.map((category) => {
                const selected = selectedCategories.includes(category);
                return (
                  <Pressable
                    key={category}
                    onPress={() => toggleCategory(category)}
                    style={[styles.categoryChoice, selected && styles.categoryChoiceActive]}
                  >
                    <Text
                      style={[
                        styles.categoryChoiceText,
                        selected && styles.categoryChoiceTextActive,
                      ]}
                    >
                      {category}
                    </Text>
                  </Pressable>
                );
              })}
            </View>
          </>
        )}
      </View>

      {error && <Text style={styles.errorText}>{error}</Text>}

      <View style={styles.onboardingDots}>
        {[0, 1, 2].map((item) => (
          <View
            key={item}
            style={[styles.onboardingDot, step === item && styles.onboardingDotActive]}
          />
        ))}
      </View>

      <View style={styles.onboardingActions}>
        <Pressable
          disabled={isSaving}
          onPress={() => finish(true)}
          style={styles.skipButton}
        >
          <Text style={styles.skipButtonText}>Skip</Text>
        </Pressable>
        <Pressable
          disabled={isSaving}
          onPress={() => {
            if (step < 2) {
              setStep((current) => current + 1);
            } else {
              finish(false);
            }
          }}
          style={[styles.primaryButton, styles.onboardingPrimaryButton, isSaving && styles.primaryButtonDisabled]}
        >
          {isSaving ? (
            <ActivityIndicator color={theme.primaryForeground} />
          ) : (
            <Text style={styles.primaryButtonText}>{step < 2 ? 'Next' : 'Start voting'}</Text>
          )}
        </Pressable>
      </View>
    </ScrollView>
  );
}

function OnboardingPoint({ copy, title }: { copy: string; title: string }) {
  return (
    <View style={styles.onboardingPoint}>
      <View style={styles.onboardingPointBullet} />
      <View style={styles.onboardingPointText}>
        <Text style={styles.onboardingPointTitle}>{title}</Text>
        <Text style={styles.onboardingPointCopy}>{copy}</Text>
      </View>
    </View>
  );
}

function SignedInHome() {
  const { applyVoteReward, signOut, user } = useAuth();
  const [activeTab, setActiveTab] = useState<SignedInTab>('home');
  const [pushToken, setPushToken] = useState<string | null>(null);
  const [pushNotice, setPushNotice] = useState<string | null>(null);
  const [leaderboard, setLeaderboard] = useState<AuthUser[]>([]);
  const [isLeaderboardLoading, setIsLeaderboardLoading] = useState(false);
  const [leaderboardError, setLeaderboardError] = useState<string | null>(null);
  const [polls, setPolls] = useState<ApiPoll[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [votingPollId, setVotingPollId] = useState<number | null>(null);
  const [latestReward, setLatestReward] = useState<VoteReward | null>(null);
  const roundIds = useRef(new Map<number, string>());

  const loadLeaderboard = useCallback(async () => {
    setIsLeaderboardLoading(true);
    setLeaderboardError(null);

    try {
      setLeaderboard(await usersApi.getLeaderboard(25));
    } catch (err) {
      setLeaderboardError(err instanceof Error ? err.message : 'Could not load leaderboard');
    } finally {
      setIsLeaderboardLoading(false);
    }
  }, []);

  const loadPolls = useCallback(async (refreshing = false) => {
    if (refreshing) {
      setIsRefreshing(true);
    } else {
      setIsLoading(true);
    }

    setError(null);

    try {
      setPolls(await pollsApi.getTrending(20));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not load polls');
    } finally {
      setIsLoading(false);
      setIsRefreshing(false);
    }
  }, []);

  useEffect(() => {
    loadPolls();
  }, [loadPolls]);

  useEffect(() => {
    const poll = polls[0]; if (!poll) return;
    let roundId = roundIds.current.get(poll.id); if (!roundId) { roundId = `mobile-${Date.now()}-${poll.id}`; roundIds.current.set(poll.id, roundId); }
    void track('game_round_started', { round_id: roundId, surface: 'feed', category: poll.category }, `feed:${poll.id}`);
  }, [polls[0]?.id, polls[0]?.category]);

  useEffect(() => {
    let isMounted = true;

    async function registerForPush() {
      try {
        const token = await getExpoPushToken();
        if (!token) {
          if (isMounted) setPushNotice('Enable notifications to get streak and daily poll reminders.');
          return;
        }

        await notificationsApi.registerDeviceToken(token, Platform.OS);
        if (isMounted) {
          setPushToken(token);
          setPushNotice(null);
        }
      } catch {
        if (isMounted) setPushNotice('Push reminders could not be enabled on this device.');
      }
    }

    registerForPush();

    return () => {
      isMounted = false;
    };
  }, []);

  useEffect(() => {
    if (activeTab === 'leaderboard' && leaderboard.length === 0) {
      loadLeaderboard();
    }
  }, [activeTab, leaderboard.length, loadLeaderboard]);

  async function vote(pollId: number, optionId: number) {
    setVotingPollId(pollId);
    setError(null);

    try {
      const response = await votesApi.cast(pollId, optionId);
      setPolls((current) =>
        current.map((poll) => (poll.id === response.poll.id ? response.poll : poll)),
      );
      setLatestReward(response.reward);
      applyVoteReward(response.reward);
      let roundId = roundIds.current.get(pollId); if (!roundId) { roundId = `mobile-${Date.now()}-${pollId}`; roundIds.current.set(pollId, roundId); }
      void track('game_round_completed', { round_id: roundId, surface: 'feed', outcome: 'voted', xp_awarded: response.reward.xpAwarded }, `feed:${pollId}:completed`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not record your vote');
    } finally {
      setVotingPollId(null);
    }
  }

  async function logout() {
    if (pushToken) {
      await notificationsApi.disableDeviceToken(pushToken).catch(() => undefined);
    }

    await signOut();
  }

  if (!user) return null;

  const level = user.progression.level;
  const progress = { current: user.progression.xpIntoLevel, percent: user.progression.progressPercent };

  return (
    <View style={styles.feedShell}>
      <View style={styles.feedHeader}>
        <View>
          <Text style={styles.eyebrow}>Pollify</Text>
          <Text style={styles.feedTitle}>Hi, {user.displayName}</Text>
        </View>
        <Pressable onPress={logout} style={styles.logoutButton}>
          <Text style={styles.logoutButtonText}>Logout</Text>
        </Pressable>
      </View>

      <View style={styles.compactStats}>
        <Metric label="XP" value={String(user.xp ?? 0)} />
        <Metric label="Streak" value={String(user.streak ?? 0)} />
        <Metric label="Votes" value={String(user.totalVotes ?? 0)} />
      </View>

      <View style={styles.segment}>
        <TabButton active={activeTab === 'home'} label="Home" onPress={() => setActiveTab('home')} />
        <TabButton
          active={activeTab === 'profile'}
          label="Profile"
          onPress={() => setActiveTab('profile')}
        />
        <TabButton
          active={activeTab === 'leaderboard'}
          label="Ranks"
          onPress={() => setActiveTab('leaderboard')}
        />
      </View>

      {latestReward && (
        <View style={styles.rewardBanner}>
          <Text accessibilityLiveRegion="polite" style={styles.rewardTitle}>
            {latestReward.leveledUp ? `Level up! Level ${latestReward.progression.level}` : `+${latestReward.awardedXp} XP earned`}
          </Text>
          <Text style={styles.rewardCopy}>
            {latestReward.streakAdvanced
              ? `Daily streak is now ${latestReward.streak}.`
              : `Streak stays at ${latestReward.streak}.`}
          </Text>
        </View>
      )}

      {pushNotice && (
        <View style={styles.pushNotice}>
          <Text style={styles.pushNoticeText}>{pushNotice}</Text>
        </View>
      )}

      {activeTab === 'home' && (
        <>
          <View style={styles.progressPanel}>
            <View style={styles.progressHeader}>
              <Text style={styles.panelTitle}>Today's progress</Text>
              <Text style={styles.levelPill}>Level {level}</Text>
            </View>
            <View style={styles.progressTrack}>
              <View style={[styles.progressFill, { width: `${progress.percent}%` }]} />
            </View>
            <Text style={styles.progressCopy}>
              {progress.current} / {user.progression.xpRequiredForNextLevel} XP toward the next level.
            </Text>
          </View>

          {error && (
            <View style={styles.feedError}>
              <Text style={styles.feedErrorText}>{error}</Text>
              <Pressable onPress={() => loadPolls()} style={styles.retryButton}>
                <Text style={styles.retryButtonText}>Retry</Text>
              </Pressable>
            </View>
          )}

          {isLoading ? (
            <View style={styles.feedState}>
              <ActivityIndicator color={theme.destructive} size="large" />
              <Text style={styles.loadingText}>Loading trending polls</Text>
            </View>
          ) : (
            <FlatList
              contentContainerStyle={styles.feedList}
              data={polls}
              keyExtractor={(poll) => String(poll.id)}
              ListEmptyComponent={<EmptyPollState onRetry={() => loadPolls()} />}
              refreshControl={
                <RefreshControl refreshing={isRefreshing} onRefresh={() => loadPolls(true)} />
              }
              renderItem={({ item }) => (
                <PollCard
                  isVoting={votingPollId === item.id}
                  onVote={(optionId) => vote(item.id, optionId)}
                  poll={item}
                />
              )}
              showsVerticalScrollIndicator={false}
            />
          )}
        </>
      )}

      {activeTab === 'profile' && (
        <ScrollView contentContainerStyle={styles.feedList} showsVerticalScrollIndicator={false}>
          <ProfilePanel level={level} user={user} />
          <View style={styles.actionPanel}>
            <Text style={styles.actionTitle}>Gamified mobile profile</Text>
            <Text style={styles.actionCopy}>
              XP, streak, level, profile stats, and rankings are driven by backend user data.
            </Text>
          </View>
        </ScrollView>
      )}

      {activeTab === 'leaderboard' && (
        <ScrollView contentContainerStyle={styles.feedList} showsVerticalScrollIndicator={false}>
          <LeaderboardPanel
            currentUserId={user.id}
            error={leaderboardError}
            isLoading={isLeaderboardLoading}
            onRefresh={loadLeaderboard}
            users={leaderboard}
          />
        </ScrollView>
      )}
    </View>
  );
}

function TabButton({
  active,
  label,
  onPress,
}: {
  active: boolean;
  label: string;
  onPress: () => void;
}) {
  return (
    <Pressable
      onPress={onPress}
      style={[styles.segmentButton, active && styles.segmentButtonActive]}
    >
      <Text style={[styles.segmentText, active && styles.segmentTextActive]}>{label}</Text>
    </Pressable>
  );
}

function ProfilePanel({ level, user }: { level: number; user: AuthUser }) {
  return (
    <View style={styles.profilePanel}>
      <View>
        <Text style={styles.profileName}>{user.displayName}</Text>
        <Text style={styles.profileUsername}>@{user.username}</Text>
      </View>
      <View style={styles.metricGrid}>
        <Metric label="Level" value={String(level)} />
        <Metric label="XP" value={String(user.xp ?? 0)} />
        <Metric label="Streak" value={String(user.streak ?? 0)} />
        <Metric label="Votes" value={String(user.totalVotes ?? 0)} />
        <Metric label="Polls" value={String(user.pollsCreated ?? 0)} />
        <Metric label="Joined" value={formatMonth(user.createdAt)} />
      </View>
    </View>
  );
}

function LeaderboardPanel({
  currentUserId,
  error,
  isLoading,
  onRefresh,
  users,
}: {
  currentUserId: number;
  error: string | null;
  isLoading: boolean;
  onRefresh: () => void;
  users: AuthUser[];
}) {
  return (
    <View style={styles.profilePanel}>
      <View style={styles.progressHeader}>
        <Text style={styles.panelTitle}>Leaderboard</Text>
        <Pressable onPress={onRefresh} style={styles.smallButton}>
          <Text style={styles.smallButtonText}>Refresh</Text>
        </Pressable>
      </View>

      {isLoading ? (
        <View style={styles.inlineState}>
          <ActivityIndicator color={theme.destructive} />
          <Text style={styles.inlineStateText}>Loading real rankings</Text>
        </View>
      ) : error ? (
        <Text style={styles.errorText}>{error}</Text>
      ) : users.length === 0 ? (
        <View style={styles.inlineState}>
          <Text style={styles.inlineStateText}>No ranked users yet.</Text>
        </View>
      ) : (
        users.map((rankedUser, index) => (
          <LeaderboardRow
            currentUserId={currentUserId}
            key={rankedUser.id}
            rank={index + 1}
            user={rankedUser}
          />
        ))
      )}
    </View>
  );
}

function LeaderboardRow({
  currentUserId,
  rank,
  user,
}: {
  currentUserId: number;
  rank: number;
  user: AuthUser;
}) {
  const isCurrentUser = user.id === currentUserId;

  return (
    <View style={[styles.leaderboardRow, isCurrentUser && styles.leaderboardRowActive]}>
      <Text style={styles.rankText}>#{rank}</Text>
      <View style={styles.leaderboardIdentity}>
        <Text style={styles.leaderboardName}>{user.displayName}</Text>
        <Text style={styles.leaderboardUsername}>@{user.username}</Text>
      </View>
      <View style={styles.leaderboardScore}>
        <Text style={styles.leaderboardXp}>{user.xp ?? 0}</Text>
        <Text style={styles.leaderboardLabel}>XP</Text>
      </View>
    </View>
  );
}

function formatMonth(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return 'New';

  return date.toLocaleDateString('en-IN', {
    month: 'short',
    year: '2-digit',
  });
}

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <View style={styles.metric}>
      <Text style={styles.metricValue}>{value}</Text>
      <Text style={styles.metricLabel}>{label}</Text>
    </View>
  );
}

function EmptyPollState({ onRetry }: { onRetry: () => void }) {
  return (
    <View style={styles.emptyState}>
      <Text style={styles.emptyTitle}>No trending polls yet</Text>
      <Text style={styles.emptyCopy}>
        Pull to refresh or retry once the backend has generated fresh polls.
      </Text>
      <Pressable onPress={onRetry} style={styles.secondaryButton}>
        <Text style={styles.secondaryButtonText}>Refresh</Text>
      </Pressable>
    </View>
  );
}

function PollCard({
  isVoting,
  onVote,
  poll,
}: {
  isVoting: boolean;
  onVote: (optionId: number) => void;
  poll: ApiPoll;
}) {
  const expiresAt = new Date(poll.expiresAt);
  const isExpired = !poll.isActive || expiresAt.getTime() < Date.now();
  const canVote = !poll.hasVoted && !isExpired && !isVoting;

  return (
    <View style={styles.pollCard}>
      <View style={styles.pollMetaRow}>
        <Text style={styles.categoryPill}>{poll.category || 'General'}</Text>
        <Text style={styles.pollMeta}>{poll.totalVotes} votes</Text>
      </View>

      <Text style={styles.pollQuestion}>{poll.question}</Text>
      {Boolean(poll.description) && <Text style={styles.pollDescription}>{poll.description}</Text>}

      <View style={styles.optionList}>
        {poll.options.map((option) => (
          <PollOptionButton
            canVote={canVote}
            isSelected={poll.userVotedOptionId === option.id}
            isVoting={isVoting}
            key={option.id}
            onPress={() => onVote(option.id)}
            option={option}
            showResults={poll.hasVoted || isExpired}
          />
        ))}
      </View>

      <Text style={styles.pollFooter}>
        {poll.hasVoted
          ? 'Your vote is counted.'
          : isExpired
            ? 'This poll has ended.'
            : `Ends ${formatRelativeDate(expiresAt)}`}
      </Text>
    </View>
  );
}

function PollOptionButton({
  canVote,
  isSelected,
  isVoting,
  onPress,
  option,
  showResults,
}: {
  canVote: boolean;
  isSelected: boolean;
  isVoting: boolean;
  onPress: () => void;
  option: ApiPollOption;
  showResults: boolean;
}) {
  const percentage = Math.round(option.votePercentage || 0);

  return (
    <Pressable
      disabled={!canVote}
      onPress={onPress}
      style={[styles.optionButton, isSelected && styles.optionButtonSelected]}
    >
      {showResults && <View style={[styles.optionFill, { width: `${percentage}%` }]} />}
      <View style={styles.optionContent}>
        <Text style={[styles.optionText, isSelected && styles.optionTextSelected]}>
          {option.text}
        </Text>
        {isVoting ? (
          <ActivityIndicator color={theme.destructive} />
        ) : showResults ? (
          <Text style={styles.optionPercent}>{percentage}%</Text>
        ) : null}
      </View>
    </Pressable>
  );
}

function formatRelativeDate(date: Date) {
  if (Number.isNaN(date.getTime())) return 'soon';

  const diffMs = date.getTime() - Date.now();
  const diffHours = Math.ceil(diffMs / (1000 * 60 * 60));

  if (diffHours <= 0) return 'soon';
  if (diffHours < 24) return `in ${diffHours}h`;

  const diffDays = Math.ceil(diffHours / 24);
  return `in ${diffDays}d`;
}

/**
 * Status colors for banners/fills that don't have a shared web equivalent
 * yet (Frontend/app/globals.css has no warning/caution tokens either -
 * those are handled ad hoc there too). Kept local and separate from
 * `theme` so it's clear these aren't part of the shared design tokens.
 */
const statusColors = {
  highlightBackground: '#FFF6D8',
  highlightText: '#5C4A12',
  errorBackground: '#FBE9E7',
  errorText: '#9F2E2B',
  voteResultFill: '#F8E7B1',
};

const styles = StyleSheet.create({
  safeArea: {
    flex: 1,
    backgroundColor: theme.background,
  },
  onboardingContainer: {
    flexGrow: 1,
    gap: spacing['5'],
    justifyContent: 'center',
    padding: spacing['6'],
  },
  onboardingHeader: {
    gap: spacing['1'],
  },
  onboardingCard: {
    backgroundColor: theme.card,
    borderColor: theme.border,
    borderRadius: radius.lg,
    borderWidth: 1,
    gap: spacing['3.5'],
    padding: spacing['5'],
  },
  onboardingPoint: {
    alignItems: 'flex-start',
    flexDirection: 'row',
    gap: spacing['3'],
  },
  onboardingPointBullet: {
    backgroundColor: theme.primary,
    borderRadius: radius.full,
    height: 14,
    marginTop: spacing['1'],
    width: 14,
  },
  onboardingPointText: {
    flex: 1,
    gap: spacing['1'],
  },
  onboardingPointTitle: {
    color: theme.foreground,
    fontSize: typography.fontSize.lg,
    fontWeight: typography.fontWeight.black,
    letterSpacing: 0,
  },
  onboardingPointCopy: {
    color: theme.mutedForeground,
    fontSize: typography.fontSize.sm,
    letterSpacing: 0,
    lineHeight: 22,
  },
  categoryGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing['2.5'],
  },
  categoryChoice: {
    backgroundColor: theme.muted,
    borderColor: theme.border,
    borderRadius: radius.full,
    borderWidth: 1,
    paddingHorizontal: spacing['3.5'],
    paddingVertical: spacing['2.5'],
  },
  categoryChoiceActive: {
    backgroundColor: theme.foreground,
    borderColor: theme.foreground,
  },
  categoryChoiceText: {
    color: theme.mutedForeground,
    fontSize: typography.fontSize.sm,
    fontWeight: typography.fontWeight.black,
    letterSpacing: 0,
  },
  categoryChoiceTextActive: {
    color: theme.primaryForeground,
  },
  onboardingDots: {
    flexDirection: 'row',
    gap: spacing['2'],
    justifyContent: 'center',
  },
  onboardingDot: {
    backgroundColor: theme.muted,
    borderRadius: radius.full,
    height: 8,
    width: 8,
  },
  onboardingDotActive: {
    backgroundColor: theme.destructive,
    width: 22,
  },
  onboardingActions: {
    alignItems: 'center',
    flexDirection: 'row',
    gap: spacing['3'],
  },
  onboardingPrimaryButton: {
    flex: 1,
  },
  skipButton: {
    alignItems: 'center',
    backgroundColor: theme.card,
    borderColor: theme.border,
    borderRadius: radius.lg,
    borderWidth: 1,
    minHeight: 50,
    paddingHorizontal: spacing['5'],
    paddingVertical: spacing['3.5'],
  },
  skipButtonText: {
    color: theme.mutedForeground,
    fontSize: typography.fontSize.base,
    fontWeight: typography.fontWeight.black,
    letterSpacing: 0,
  },
  keyboard: {
    flex: 1,
  },
  container: {
    flexGrow: 1,
    padding: spacing['6'],
    gap: spacing['5'],
  },
  loading: {
    alignItems: 'center',
    flex: 1,
    gap: spacing['3.5'],
    justifyContent: 'center',
    padding: spacing['6'],
  },
  loadingText: {
    color: theme.mutedForeground,
    fontSize: typography.fontSize.base,
    fontWeight: typography.fontWeight.bold,
    letterSpacing: 0,
  },
  feedShell: {
    flex: 1,
    paddingHorizontal: spacing['5'],
    paddingTop: spacing['5'],
  },
  feedHeader: {
    alignItems: 'center',
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingBottom: spacing['3.5'],
  },
  feedTitle: {
    color: theme.foreground,
    fontSize: typography.fontSize['3xl'],
    fontWeight: typography.fontWeight.black,
    letterSpacing: 0,
    lineHeight: 34,
  },
  logoutButton: {
    backgroundColor: theme.card,
    borderColor: theme.border,
    borderRadius: radius.md,
    borderWidth: 1,
    paddingHorizontal: spacing['3'],
    paddingVertical: spacing['2'],
  },
  logoutButtonText: {
    color: theme.destructive,
    fontSize: typography.fontSize.xs,
    fontWeight: typography.fontWeight.extrabold,
    letterSpacing: 0,
  },
  compactStats: {
    flexDirection: 'row',
    gap: spacing['2'],
    paddingBottom: spacing['3'],
  },
  rewardBanner: {
    backgroundColor: theme.foreground,
    borderRadius: radius.lg,
    gap: spacing['1'],
    marginBottom: spacing['3'],
    padding: spacing['3.5'],
  },
  rewardTitle: {
    color: theme.primary,
    fontSize: typography.fontSize.base,
    fontWeight: typography.fontWeight.black,
    letterSpacing: 0,
  },
  rewardCopy: {
    color: theme.primaryForeground,
    fontSize: typography.fontSize.sm,
    fontWeight: typography.fontWeight.bold,
    letterSpacing: 0,
    lineHeight: 20,
  },
  pushNotice: {
    backgroundColor: statusColors.highlightBackground,
    borderColor: theme.primary,
    borderRadius: radius.lg,
    borderWidth: 1,
    marginBottom: spacing['3'],
    padding: spacing['3'],
  },
  pushNoticeText: {
    color: statusColors.highlightText,
    fontSize: typography.fontSize.sm,
    fontWeight: typography.fontWeight.extrabold,
    letterSpacing: 0,
    lineHeight: 20,
  },
  feedError: {
    alignItems: 'center',
    backgroundColor: statusColors.errorBackground,
    borderRadius: radius.lg,
    flexDirection: 'row',
    gap: spacing['2.5'],
    marginBottom: spacing['3'],
    padding: spacing['3'],
  },
  feedErrorText: {
    color: statusColors.errorText,
    flex: 1,
    fontSize: typography.fontSize.sm,
    fontWeight: typography.fontWeight.bold,
    letterSpacing: 0,
    lineHeight: 20,
  },
  retryButton: {
    backgroundColor: theme.card,
    borderRadius: radius.md,
    paddingHorizontal: spacing['3'],
    paddingVertical: spacing['2'],
  },
  retryButtonText: {
    color: statusColors.errorText,
    fontSize: typography.fontSize.xs,
    fontWeight: typography.fontWeight.black,
    letterSpacing: 0,
  },
  feedState: {
    alignItems: 'center',
    flex: 1,
    gap: spacing['3.5'],
    justifyContent: 'center',
    padding: spacing['6'],
  },
  feedList: {
    gap: spacing['3.5'],
    paddingBottom: spacing['8'],
  },
  hero: {
    paddingTop: spacing['8'],
    paddingBottom: spacing['4'],
  },
  heroCompact: {
    paddingTop: spacing['8'],
    paddingBottom: spacing['2'],
  },
  eyebrow: {
    color: theme.destructive,
    fontSize: typography.fontSize.sm,
    fontWeight: typography.fontWeight.bold,
    letterSpacing: 0,
    marginBottom: spacing['2.5'],
    textTransform: 'uppercase',
  },
  title: {
    color: theme.foreground,
    fontSize: typography.fontSize['4xl'],
    fontWeight: typography.fontWeight.extrabold,
    letterSpacing: 0,
    lineHeight: 42,
  },
  titleSmall: {
    color: theme.foreground,
    fontSize: typography.fontSize['3xl'],
    fontWeight: typography.fontWeight.extrabold,
    letterSpacing: 0,
    lineHeight: 38,
  },
  subtitle: {
    color: theme.mutedForeground,
    fontSize: typography.fontSize.lg,
    lineHeight: 25,
    marginTop: spacing['3.5'],
  },
  segment: {
    backgroundColor: theme.muted,
    borderRadius: radius.lg,
    flexDirection: 'row',
    padding: spacing['1'],
  },
  segmentButton: {
    alignItems: 'center',
    borderRadius: radius.md,
    flex: 1,
    paddingVertical: spacing['3'],
  },
  segmentButtonActive: {
    backgroundColor: theme.card,
  },
  segmentText: {
    color: theme.mutedForeground,
    fontSize: typography.fontSize.sm,
    fontWeight: typography.fontWeight.extrabold,
    letterSpacing: 0,
  },
  segmentTextActive: {
    color: theme.foreground,
  },
  formPanel: {
    backgroundColor: theme.card,
    borderColor: theme.border,
    borderRadius: radius.lg,
    borderWidth: 1,
    padding: spacing['5'],
    gap: spacing['3.5'],
  },
  panelTitle: {
    color: theme.foreground,
    fontSize: typography.fontSize.lg,
    fontWeight: typography.fontWeight.extrabold,
    letterSpacing: 0,
    marginBottom: spacing['1'],
  },
  field: {
    gap: spacing['2'],
  },
  inputLabel: {
    color: theme.mutedForeground,
    fontSize: typography.fontSize.sm,
    fontWeight: typography.fontWeight.extrabold,
    letterSpacing: 0,
  },
  input: {
    backgroundColor: theme.muted,
    borderColor: theme.border,
    borderRadius: radius.md,
    borderWidth: 1,
    color: theme.foreground,
    fontSize: typography.fontSize.base,
    minHeight: 50,
    paddingHorizontal: spacing['3.5'],
  },
  errorText: {
    backgroundColor: statusColors.errorBackground,
    borderRadius: radius.lg,
    color: statusColors.errorText,
    fontSize: typography.fontSize.sm,
    fontWeight: typography.fontWeight.bold,
    letterSpacing: 0,
    lineHeight: 20,
    padding: spacing['3'],
  },
  apiPanel: {
    backgroundColor: theme.card,
    borderColor: theme.border,
    borderRadius: radius.lg,
    borderWidth: 1,
    gap: spacing['1.5'],
    padding: spacing['4'],
  },
  label: {
    color: theme.mutedForeground,
    fontSize: typography.fontSize.xs,
    fontWeight: typography.fontWeight.bold,
    letterSpacing: 0,
    textTransform: 'uppercase',
  },
  value: {
    color: theme.foreground,
    fontSize: typography.fontSize.base,
    fontWeight: typography.fontWeight.semibold,
    lineHeight: 22,
  },
  profilePanel: {
    backgroundColor: theme.card,
    borderColor: theme.border,
    borderRadius: radius.lg,
    borderWidth: 1,
    gap: spacing['5'],
    padding: spacing['5'],
  },
  profileName: {
    color: theme.foreground,
    fontSize: typography.fontSize['2xl'],
    fontWeight: typography.fontWeight.extrabold,
    letterSpacing: 0,
  },
  profileUsername: {
    color: theme.mutedForeground,
    fontSize: typography.fontSize.base,
    fontWeight: typography.fontWeight.bold,
    letterSpacing: 0,
    marginTop: spacing['1'],
  },
  metricGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing['2.5'],
  },
  metric: {
    backgroundColor: theme.background,
    borderColor: theme.border,
    borderRadius: radius.lg,
    borderWidth: 1,
    flex: 1,
    minWidth: '47%',
    padding: spacing['3.5'],
  },
  metricValue: {
    color: theme.foreground,
    fontSize: typography.fontSize['2xl'],
    fontWeight: typography.fontWeight.black,
    letterSpacing: 0,
  },
  metricLabel: {
    color: theme.mutedForeground,
    fontSize: typography.fontSize.xs,
    fontWeight: typography.fontWeight.extrabold,
    letterSpacing: 0,
    marginTop: spacing['1'],
    textTransform: 'uppercase',
  },
  progressPanel: {
    backgroundColor: theme.card,
    borderColor: theme.border,
    borderRadius: radius.lg,
    borderWidth: 1,
    gap: spacing['3.5'],
    marginBottom: spacing['3.5'],
    padding: spacing['5'],
  },
  progressHeader: {
    alignItems: 'center',
    flexDirection: 'row',
    gap: spacing['3'],
    justifyContent: 'space-between',
  },
  levelPill: {
    backgroundColor: theme.primary,
    borderRadius: radius.full,
    color: theme.primaryForeground,
    fontSize: typography.fontSize.xs,
    fontWeight: typography.fontWeight.black,
    letterSpacing: 0,
    overflow: 'hidden',
    paddingHorizontal: spacing['2.5'],
    paddingVertical: spacing['1.5'],
    textTransform: 'uppercase',
  },
  progressTrack: {
    backgroundColor: theme.muted,
    borderRadius: radius.full,
    height: 12,
    overflow: 'hidden',
  },
  progressFill: {
    backgroundColor: theme.destructive,
    bottom: 0,
    left: 0,
    position: 'absolute',
    top: 0,
  },
  progressCopy: {
    color: theme.mutedForeground,
    fontSize: typography.fontSize.sm,
    letterSpacing: 0,
    lineHeight: 22,
  },
  smallButton: {
    backgroundColor: theme.background,
    borderColor: theme.border,
    borderRadius: radius.md,
    borderWidth: 1,
    paddingHorizontal: spacing['3'],
    paddingVertical: spacing['2'],
  },
  smallButtonText: {
    color: theme.foreground,
    fontSize: typography.fontSize.xs,
    fontWeight: typography.fontWeight.black,
    letterSpacing: 0,
  },
  inlineState: {
    alignItems: 'center',
    gap: spacing['2.5'],
    padding: spacing['5'],
  },
  inlineStateText: {
    color: theme.mutedForeground,
    fontSize: typography.fontSize.sm,
    fontWeight: typography.fontWeight.bold,
    letterSpacing: 0,
  },
  leaderboardRow: {
    alignItems: 'center',
    backgroundColor: theme.muted,
    borderColor: theme.border,
    borderRadius: radius.lg,
    borderWidth: 1,
    flexDirection: 'row',
    gap: spacing['3'],
    padding: spacing['3'],
  },
  leaderboardRowActive: {
    backgroundColor: statusColors.highlightBackground,
    borderColor: theme.primary,
  },
  rankText: {
    color: theme.destructive,
    fontSize: typography.fontSize.base,
    fontWeight: typography.fontWeight.black,
    letterSpacing: 0,
    minWidth: 38,
  },
  leaderboardIdentity: {
    flex: 1,
  },
  leaderboardName: {
    color: theme.foreground,
    fontSize: typography.fontSize.base,
    fontWeight: typography.fontWeight.black,
    letterSpacing: 0,
  },
  leaderboardUsername: {
    color: theme.mutedForeground,
    fontSize: typography.fontSize.xs,
    fontWeight: typography.fontWeight.bold,
    letterSpacing: 0,
    marginTop: spacing['1'],
  },
  leaderboardScore: {
    alignItems: 'flex-end',
  },
  leaderboardXp: {
    color: theme.foreground,
    fontSize: typography.fontSize.lg,
    fontWeight: typography.fontWeight.black,
    letterSpacing: 0,
  },
  leaderboardLabel: {
    color: theme.mutedForeground,
    fontSize: typography.fontSize.xs,
    fontWeight: typography.fontWeight.black,
    letterSpacing: 0,
    textTransform: 'uppercase',
  },
  actionPanel: {
    backgroundColor: theme.foreground,
    borderRadius: radius.lg,
    padding: spacing['5'],
    gap: spacing['3.5'],
  },
  actionTitle: {
    color: theme.primaryForeground,
    fontSize: typography.fontSize.xl,
    fontWeight: typography.fontWeight.extrabold,
    letterSpacing: 0,
  },
  actionCopy: {
    color: theme.primaryForeground,
    fontSize: typography.fontSize.base,
    lineHeight: 23,
  },
  primaryButton: {
    alignItems: 'center',
    backgroundColor: theme.primary,
    borderRadius: radius.lg,
    minHeight: 50,
    paddingHorizontal: spacing['4'],
    paddingVertical: spacing['3.5'],
  },
  primaryButtonDisabled: {
    opacity: 0.72,
  },
  primaryButtonText: {
    color: theme.primaryForeground,
    fontSize: typography.fontSize.base,
    fontWeight: typography.fontWeight.extrabold,
    letterSpacing: 0,
  },
  secondaryButton: {
    alignItems: 'center',
    backgroundColor: theme.primary,
    borderRadius: radius.lg,
    paddingHorizontal: spacing['4'],
    paddingVertical: spacing['3.5'],
  },
  secondaryButtonText: {
    color: theme.primaryForeground,
    fontSize: typography.fontSize.base,
    fontWeight: typography.fontWeight.extrabold,
    letterSpacing: 0,
  },
  emptyState: {
    alignItems: 'center',
    backgroundColor: theme.card,
    borderColor: theme.border,
    borderRadius: radius.lg,
    borderWidth: 1,
    gap: spacing['3'],
    padding: spacing['6'],
  },
  emptyTitle: {
    color: theme.foreground,
    fontSize: typography.fontSize.xl,
    fontWeight: typography.fontWeight.black,
    letterSpacing: 0,
    textAlign: 'center',
  },
  emptyCopy: {
    color: theme.mutedForeground,
    fontSize: typography.fontSize.sm,
    letterSpacing: 0,
    lineHeight: 22,
    textAlign: 'center',
  },
  pollCard: {
    backgroundColor: theme.card,
    borderColor: theme.border,
    borderRadius: radius['3xl'],
    borderWidth: 1,
    gap: spacing['3.5'],
    padding: spacing['4'],
  },
  pollMetaRow: {
    alignItems: 'center',
    flexDirection: 'row',
    justifyContent: 'space-between',
  },
  categoryPill: {
    backgroundColor: theme.primary,
    borderRadius: radius.full,
    color: theme.primaryForeground,
    fontSize: typography.fontSize.xs,
    fontWeight: typography.fontWeight.black,
    letterSpacing: 0,
    overflow: 'hidden',
    paddingHorizontal: spacing['2.5'],
    paddingVertical: spacing['1.5'],
    textTransform: 'uppercase',
  },
  pollMeta: {
    color: theme.mutedForeground,
    fontSize: typography.fontSize.xs,
    fontWeight: typography.fontWeight.extrabold,
    letterSpacing: 0,
  },
  pollQuestion: {
    color: theme.foreground,
    fontSize: typography.fontSize.lg,
    fontWeight: typography.fontWeight.black,
    letterSpacing: 0,
    lineHeight: 27,
  },
  pollDescription: {
    color: theme.mutedForeground,
    fontSize: typography.fontSize.sm,
    letterSpacing: 0,
    lineHeight: 22,
  },
  optionList: {
    gap: spacing['2.5'],
  },
  optionButton: {
    backgroundColor: theme.muted,
    borderColor: theme.border,
    borderRadius: radius.xl,
    borderWidth: 1,
    minHeight: 52,
    overflow: 'hidden',
  },
  optionButtonSelected: {
    borderColor: theme.destructive,
  },
  optionFill: {
    backgroundColor: statusColors.voteResultFill,
    bottom: 0,
    left: 0,
    position: 'absolute',
    top: 0,
  },
  optionContent: {
    alignItems: 'center',
    flexDirection: 'row',
    gap: spacing['2.5'],
    justifyContent: 'space-between',
    minHeight: 52,
    paddingHorizontal: spacing['3.5'],
    paddingVertical: spacing['3'],
  },
  optionText: {
    color: theme.foreground,
    flex: 1,
    fontSize: typography.fontSize.base,
    fontWeight: typography.fontWeight.extrabold,
    letterSpacing: 0,
    lineHeight: 22,
  },
  optionTextSelected: {
    color: statusColors.errorText,
  },
  optionPercent: {
    color: theme.foreground,
    fontSize: typography.fontSize.sm,
    fontWeight: typography.fontWeight.black,
    letterSpacing: 0,
  },
  pollFooter: {
    color: theme.mutedForeground,
    fontSize: typography.fontSize.xs,
    fontWeight: typography.fontWeight.bold,
    letterSpacing: 0,
  },
});
