import { StatusBar } from 'expo-status-bar';
import { useState } from 'react';
import {
  ActivityIndicator,
  KeyboardAvoidingView,
  Platform,
  Pressable,
  SafeAreaView,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native';

import { API_BASE_URL } from './src/config/api';
import { AuthProvider, useAuth } from './src/context/AuthContext';

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
  const { signOut, user } = useAuth();

  if (!user) return null;

  return (
    <ScrollView contentContainerStyle={styles.container}>
      <View style={styles.heroCompact}>
        <Text style={styles.eyebrow}>Pollify</Text>
        <Text style={styles.titleSmall}>Hi, {user.displayName}</Text>
        <Text style={styles.subtitle}>
          Your mobile session is active and ready for poll feed integration.
        </Text>
      </View>

      <View style={styles.profilePanel}>
        <View>
          <Text style={styles.profileName}>{user.displayName}</Text>
          <Text style={styles.profileUsername}>@{user.username}</Text>
        </View>
        <View style={styles.metricGrid}>
          <Metric label="XP" value={String(user.xp ?? 0)} />
          <Metric label="Streak" value={String(user.streak ?? 0)} />
          <Metric label="Votes" value={String(user.totalVotes ?? 0)} />
          <Metric label="Polls" value={String(user.pollsCreated ?? 0)} />
        </View>
      </View>

      <View style={styles.actionPanel}>
        <Text style={styles.actionTitle}>Session stored securely</Text>
        <Text style={styles.actionCopy}>
          The JWT is saved with Expo SecureStore and refreshed from `/api/auth/me`
          when the app starts.
        </Text>
        <Pressable onPress={signOut} style={styles.secondaryButton}>
          <Text style={styles.secondaryButtonText}>Logout</Text>
        </Pressable>
      </View>
    </ScrollView>
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
});
