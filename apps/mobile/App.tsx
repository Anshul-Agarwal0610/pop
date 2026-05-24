import { StatusBar } from 'expo-status-bar';
import { useCallback, useEffect, useState } from 'react';
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
import { pollsApi, votesApi } from './src/lib/api';
import type { ApiPoll, ApiPollOption, VoteReward } from './src/types/poll';

type AuthMode = 'login' | 'register';

export default function App() {
  return (
    <AuthProvider>
      <PollifyApp />
    </AuthProvider>
  );
}

function PollifyApp() {
  const { isAuthenticated, isLoading } = useAuth();

  return (
    <SafeAreaView style={styles.safeArea}>
      <StatusBar style="dark" />
      {isLoading ? <LoadingScreen /> : isAuthenticated ? <SignedInHome /> : <AuthScreen />}
    </SafeAreaView>
  );
}

function LoadingScreen() {
  return (
    <View style={styles.loading}>
      <ActivityIndicator color="#B0413E" size="large" />
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
              <ActivityIndicator color="#1F2B32" />
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
        placeholderTextColor="#9A9285"
        secureTextEntry={secureTextEntry}
        style={styles.input}
        value={value}
      />
    </View>
  );
}

function SignedInHome() {
  const { applyVoteReward, signOut, user } = useAuth();
  const [polls, setPolls] = useState<ApiPoll[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [votingPollId, setVotingPollId] = useState<number | null>(null);
  const [latestReward, setLatestReward] = useState<VoteReward | null>(null);

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
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not record your vote');
    } finally {
      setVotingPollId(null);
    }
  }

  if (!user) return null;

  return (
    <View style={styles.feedShell}>
      <View style={styles.feedHeader}>
        <View>
          <Text style={styles.eyebrow}>Pollify</Text>
          <Text style={styles.feedTitle}>Hi, {user.displayName}</Text>
        </View>
        <Pressable onPress={signOut} style={styles.logoutButton}>
          <Text style={styles.logoutButtonText}>Logout</Text>
        </Pressable>
      </View>

      <View style={styles.compactStats}>
        <Metric label="XP" value={String(user.xp ?? 0)} />
        <Metric label="Streak" value={String(user.streak ?? 0)} />
        <Metric label="Votes" value={String(user.totalVotes ?? 0)} />
      </View>

      {latestReward && (
        <View style={styles.rewardBanner}>
          <Text style={styles.rewardTitle}>+{latestReward.xpAwarded} XP earned</Text>
          <Text style={styles.rewardCopy}>
            {latestReward.streakAdvanced
              ? `Daily streak is now ${latestReward.streak}.`
              : `Streak stays at ${latestReward.streak}.`}
          </Text>
        </View>
      )}

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
          <ActivityIndicator color="#B0413E" size="large" />
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
    </View>
  );
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
          <ActivityIndicator color="#B0413E" />
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

const styles = StyleSheet.create({
  safeArea: {
    flex: 1,
    backgroundColor: '#F7F5EF',
  },
  keyboard: {
    flex: 1,
  },
  container: {
    flexGrow: 1,
    padding: 24,
    gap: 18,
  },
  loading: {
    alignItems: 'center',
    flex: 1,
    gap: 14,
    justifyContent: 'center',
    padding: 24,
  },
  loadingText: {
    color: '#54514A',
    fontSize: 16,
    fontWeight: '700',
    letterSpacing: 0,
  },
  feedShell: {
    flex: 1,
    paddingHorizontal: 18,
    paddingTop: 18,
  },
  feedHeader: {
    alignItems: 'center',
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingBottom: 14,
  },
  feedTitle: {
    color: '#222222',
    fontSize: 28,
    fontWeight: '900',
    letterSpacing: 0,
    lineHeight: 34,
  },
  logoutButton: {
    backgroundColor: '#FFFFFF',
    borderColor: '#E6E0D4',
    borderRadius: 8,
    borderWidth: 1,
    paddingHorizontal: 12,
    paddingVertical: 9,
  },
  logoutButtonText: {
    color: '#B0413E',
    fontSize: 13,
    fontWeight: '800',
    letterSpacing: 0,
  },
  compactStats: {
    flexDirection: 'row',
    gap: 8,
    paddingBottom: 12,
  },
  rewardBanner: {
    backgroundColor: '#233D4D',
    borderRadius: 8,
    gap: 4,
    marginBottom: 12,
    padding: 14,
  },
  rewardTitle: {
    color: '#F4D35E',
    fontSize: 16,
    fontWeight: '900',
    letterSpacing: 0,
  },
  rewardCopy: {
    color: '#E7EFF2',
    fontSize: 14,
    fontWeight: '700',
    letterSpacing: 0,
    lineHeight: 20,
  },
  feedError: {
    alignItems: 'center',
    backgroundColor: '#FBE9E7',
    borderRadius: 8,
    flexDirection: 'row',
    gap: 10,
    marginBottom: 12,
    padding: 12,
  },
  feedErrorText: {
    color: '#9F2E2B',
    flex: 1,
    fontSize: 14,
    fontWeight: '700',
    letterSpacing: 0,
    lineHeight: 20,
  },
  retryButton: {
    backgroundColor: '#FFFFFF',
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 8,
  },
  retryButtonText: {
    color: '#9F2E2B',
    fontSize: 13,
    fontWeight: '900',
    letterSpacing: 0,
  },
  feedState: {
    alignItems: 'center',
    flex: 1,
    gap: 14,
    justifyContent: 'center',
    padding: 24,
  },
  feedList: {
    gap: 14,
    paddingBottom: 28,
  },
  hero: {
    paddingTop: 36,
    paddingBottom: 16,
  },
  heroCompact: {
    paddingTop: 36,
    paddingBottom: 8,
  },
  eyebrow: {
    color: '#B0413E',
    fontSize: 14,
    fontWeight: '700',
    letterSpacing: 0,
    marginBottom: 10,
    textTransform: 'uppercase',
  },
  title: {
    color: '#222222',
    fontSize: 36,
    fontWeight: '800',
    letterSpacing: 0,
    lineHeight: 42,
  },
  titleSmall: {
    color: '#222222',
    fontSize: 32,
    fontWeight: '800',
    letterSpacing: 0,
    lineHeight: 38,
  },
  subtitle: {
    color: '#54514A',
    fontSize: 17,
    lineHeight: 25,
    marginTop: 14,
  },
  segment: {
    backgroundColor: '#EEE8DC',
    borderRadius: 8,
    flexDirection: 'row',
    padding: 4,
  },
  segmentButton: {
    alignItems: 'center',
    borderRadius: 7,
    flex: 1,
    paddingVertical: 12,
  },
  segmentButtonActive: {
    backgroundColor: '#FFFFFF',
  },
  segmentText: {
    color: '#756F63',
    fontSize: 15,
    fontWeight: '800',
    letterSpacing: 0,
  },
  segmentTextActive: {
    color: '#222222',
  },
  formPanel: {
    backgroundColor: '#FFFFFF',
    borderColor: '#E6E0D4',
    borderRadius: 8,
    borderWidth: 1,
    padding: 18,
    gap: 14,
  },
  panelTitle: {
    color: '#222222',
    fontSize: 18,
    fontWeight: '800',
    letterSpacing: 0,
    marginBottom: 2,
  },
  field: {
    gap: 8,
  },
  inputLabel: {
    color: '#4C473F',
    fontSize: 14,
    fontWeight: '800',
    letterSpacing: 0,
  },
  input: {
    backgroundColor: '#F9F7F2',
    borderColor: '#DED6C8',
    borderRadius: 8,
    borderWidth: 1,
    color: '#222222',
    fontSize: 16,
    minHeight: 50,
    paddingHorizontal: 14,
  },
  errorText: {
    backgroundColor: '#FBE9E7',
    borderRadius: 8,
    color: '#9F2E2B',
    fontSize: 14,
    fontWeight: '700',
    letterSpacing: 0,
    lineHeight: 20,
    padding: 12,
  },
  apiPanel: {
    backgroundColor: '#FFFFFF',
    borderColor: '#E6E0D4',
    borderRadius: 8,
    borderWidth: 1,
    gap: 6,
    padding: 16,
  },
  label: {
    color: '#756F63',
    fontSize: 13,
    fontWeight: '700',
    letterSpacing: 0,
    textTransform: 'uppercase',
  },
  value: {
    color: '#26231F',
    fontSize: 16,
    fontWeight: '600',
    lineHeight: 22,
  },
  profilePanel: {
    backgroundColor: '#FFFFFF',
    borderColor: '#E6E0D4',
    borderRadius: 8,
    borderWidth: 1,
    gap: 18,
    padding: 18,
  },
  profileName: {
    color: '#222222',
    fontSize: 22,
    fontWeight: '800',
    letterSpacing: 0,
  },
  profileUsername: {
    color: '#756F63',
    fontSize: 16,
    fontWeight: '700',
    letterSpacing: 0,
    marginTop: 4,
  },
  metricGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 10,
  },
  metric: {
    backgroundColor: '#F7F5EF',
    borderColor: '#E6E0D4',
    borderRadius: 8,
    borderWidth: 1,
    flex: 1,
    minWidth: '47%',
    padding: 14,
  },
  metricValue: {
    color: '#233D4D',
    fontSize: 24,
    fontWeight: '900',
    letterSpacing: 0,
  },
  metricLabel: {
    color: '#756F63',
    fontSize: 13,
    fontWeight: '800',
    letterSpacing: 0,
    marginTop: 2,
    textTransform: 'uppercase',
  },
  actionPanel: {
    backgroundColor: '#233D4D',
    borderRadius: 8,
    padding: 20,
    gap: 14,
  },
  actionTitle: {
    color: '#FFFFFF',
    fontSize: 20,
    fontWeight: '800',
    letterSpacing: 0,
  },
  actionCopy: {
    color: '#DDE7EA',
    fontSize: 16,
    lineHeight: 23,
  },
  primaryButton: {
    alignItems: 'center',
    backgroundColor: '#F4D35E',
    borderRadius: 8,
    minHeight: 50,
    paddingHorizontal: 16,
    paddingVertical: 13,
  },
  primaryButtonDisabled: {
    opacity: 0.72,
  },
  primaryButtonText: {
    color: '#1F2B32',
    fontSize: 16,
    fontWeight: '800',
    letterSpacing: 0,
  },
  secondaryButton: {
    alignItems: 'center',
    backgroundColor: '#F4D35E',
    borderRadius: 8,
    paddingHorizontal: 16,
    paddingVertical: 13,
  },
  secondaryButtonText: {
    color: '#1F2B32',
    fontSize: 16,
    fontWeight: '800',
    letterSpacing: 0,
  },
  emptyState: {
    alignItems: 'center',
    backgroundColor: '#FFFFFF',
    borderColor: '#E6E0D4',
    borderRadius: 8,
    borderWidth: 1,
    gap: 12,
    padding: 22,
  },
  emptyTitle: {
    color: '#222222',
    fontSize: 20,
    fontWeight: '900',
    letterSpacing: 0,
    textAlign: 'center',
  },
  emptyCopy: {
    color: '#54514A',
    fontSize: 15,
    letterSpacing: 0,
    lineHeight: 22,
    textAlign: 'center',
  },
  pollCard: {
    backgroundColor: '#FFFFFF',
    borderColor: '#E6E0D4',
    borderRadius: 8,
    borderWidth: 1,
    gap: 14,
    padding: 16,
  },
  pollMetaRow: {
    alignItems: 'center',
    flexDirection: 'row',
    justifyContent: 'space-between',
  },
  categoryPill: {
    backgroundColor: '#F4D35E',
    borderRadius: 8,
    color: '#1F2B32',
    fontSize: 12,
    fontWeight: '900',
    letterSpacing: 0,
    overflow: 'hidden',
    paddingHorizontal: 10,
    paddingVertical: 6,
    textTransform: 'uppercase',
  },
  pollMeta: {
    color: '#756F63',
    fontSize: 13,
    fontWeight: '800',
    letterSpacing: 0,
  },
  pollQuestion: {
    color: '#222222',
    fontSize: 21,
    fontWeight: '900',
    letterSpacing: 0,
    lineHeight: 27,
  },
  pollDescription: {
    color: '#54514A',
    fontSize: 15,
    letterSpacing: 0,
    lineHeight: 22,
  },
  optionList: {
    gap: 10,
  },
  optionButton: {
    backgroundColor: '#F9F7F2',
    borderColor: '#DED6C8',
    borderRadius: 8,
    borderWidth: 1,
    minHeight: 52,
    overflow: 'hidden',
  },
  optionButtonSelected: {
    borderColor: '#B0413E',
  },
  optionFill: {
    backgroundColor: '#F8E7B1',
    bottom: 0,
    left: 0,
    position: 'absolute',
    top: 0,
  },
  optionContent: {
    alignItems: 'center',
    flexDirection: 'row',
    gap: 10,
    justifyContent: 'space-between',
    minHeight: 52,
    paddingHorizontal: 14,
    paddingVertical: 12,
  },
  optionText: {
    color: '#26231F',
    flex: 1,
    fontSize: 16,
    fontWeight: '800',
    letterSpacing: 0,
    lineHeight: 22,
  },
  optionTextSelected: {
    color: '#9F2E2B',
  },
  optionPercent: {
    color: '#233D4D',
    fontSize: 15,
    fontWeight: '900',
    letterSpacing: 0,
  },
  pollFooter: {
    color: '#756F63',
    fontSize: 13,
    fontWeight: '700',
    letterSpacing: 0,
  },
});
