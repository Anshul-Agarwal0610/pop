import { StatusBar } from 'expo-status-bar';
import * as Google from 'expo-auth-session/providers/google';
import * as WebBrowser from 'expo-web-browser';
import { useEffect, useMemo, useState } from 'react';
import {
  ActivityIndicator,
  Image,
  KeyboardAvoidingView,
  Linking,
  Platform,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native';

import { SafeAreaProvider, SafeAreaView, useSafeAreaInsets } from 'react-native-safe-area-context';
import { PollExperience } from './src/screens/PollExperience';
import { PollShareProvider } from './src/components/PollShareProvider';
import { BusinessExperience } from './src/screens/BusinessExperience';
import { AuthProvider, useAuth } from './src/context/AuthContext';
import { usersApi } from './src/lib/api';

import { hasCompletedOnboarding, markOnboardingComplete } from './src/lib/session';
import type { AuthUser } from './src/types/auth';


import { ThemeProvider, useTheme, typography, spacing, radius, type ThemeColors } from './src/theme';

type AuthMode = 'login' | 'register';

WebBrowser.maybeCompleteAuthSession();


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
    <SafeAreaProvider>
      <ThemeProvider><AuthProvider><PollShareProvider>
        <PollifyApp />
      </PollShareProvider></AuthProvider></ThemeProvider>
    </SafeAreaProvider>
  );
}

function PollifyApp() {
  const insets = useSafeAreaInsets();
  const { colors: theme, scheme } = useTheme(); const styles = useMemo(() => createStyles(theme), [theme]);

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
  }, [user?.id]);

  return (
    <View style={{ flex: 1, backgroundColor: "#000000" }}>
    <SafeAreaView edges={Platform.OS === "android" ? ["top", "left", "right"] : undefined} style={styles.safeArea}>
      <StatusBar style={scheme === "dark" ? "light" : "dark"} />
      {isLoading || isCheckingOnboarding ? (
        <LoadingScreen />
      ) : isAuthenticated && user?.accountType === 'Business' ? (
        <BusinessExperience />
      ) : isAuthenticated && user && !onboardingComplete ? (
        <OnboardingScreen
          onComplete={() => setOnboardingComplete(true)}
          user={user}
        />
      ) : isAuthenticated ? (
        <PollExperience />
      ) : (
        <AuthScreen />
      )}
    </SafeAreaView>
    {Platform.OS === "android" && <View pointerEvents="none" style={{ height: insets.bottom, backgroundColor: "#000000" }} />}
    </View>
  );
}

function LoadingScreen() {
  const { colors: theme, scheme } = useTheme(); const styles = useMemo(() => createStyles(theme), [theme]);

  return (
    <View style={styles.loading}>
      <ActivityIndicator color={theme.destructive} size="large" />
      <Text style={styles.loadingText}>Checking your Pollify session</Text>
    </View>
  );
}

function AuthScreen() {
  const { colors: theme, scheme } = useTheme(); const styles = useMemo(() => createStyles(theme), [theme]);

  const { signIn, signUp, signInWithGoogle, signInBusiness, signUpBusiness } = useAuth();
  const [accountType, setAccountType] = useState<'Personal' | 'Business'>('Personal');
  const [mode, setMode] = useState<AuthMode>('login');
  const [username, setUsername] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [businessName, setBusinessName] = useState('');
  const [websiteUrl, setWebsiteUrl] = useState('');
  const [subscriptionPlan, setSubscriptionPlan] = useState<'Starter' | 'Growth' | 'Pro'>('Starter');
  const [acceptedTerms, setAcceptedTerms] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const googleConfigured = Boolean(
    process.env.EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID &&
    (Platform.OS === 'android' ? process.env.EXPO_PUBLIC_GOOGLE_ANDROID_CLIENT_ID :
      Platform.OS === 'ios' ? process.env.EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID : true)
  );

  const isRegister = mode === 'register';

  async function submit() {
    setError(null);
    setIsSubmitting(true);

    try {
      if (isRegister) {
        if (!acceptedTerms) throw new Error('Accept the Terms of Use and Community Guidelines to continue.');
        const base = {
          username: username.trim().toLowerCase(),
          displayName: displayName.trim(),
          password,
          confirmPassword,
          acceptTerms: true,
        };
        if (accountType === 'Business') await signUpBusiness({ ...base, businessName: businessName.trim(), websiteUrl: websiteUrl.trim() || undefined, subscriptionPlan });
        else await signUp(base);
      } else {
        const payload = {
          username: username.trim().toLowerCase(),
          password,
        };
        if (accountType === 'Business') await signInBusiness(payload); else await signIn(payload);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Authentication failed');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <KeyboardAvoidingView
      behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
      style={styles.keyboard}
    >
      <ScrollView
        automaticallyAdjustKeyboardInsets
        contentContainerStyle={styles.container}
        keyboardDismissMode="on-drag"
        keyboardShouldPersistTaps="handled"
        showsVerticalScrollIndicator={false}
      >
        <View style={styles.hero}>
          <View style={styles.authBrand}>
            <View style={styles.authLogoTile}>
              <Image source={require('./assets/pop-mark.png')} resizeMode="contain" style={styles.authLogo} />
            </View>
            <View>
              <Text style={styles.authBrandName}>PoP</Text>
              <Text style={styles.eyebrow}>Public Opinion Poll</Text>
            </View>
          </View>
          <Text style={styles.title}>{accountType === 'Business' ? 'Ask better questions. Grow with every answer.' : 'Vote daily. Keep the streak alive.'}</Text>
          <Text style={styles.subtitle}>
            Your next perspective is waiting. Sign in and join the conversation.
          </Text>
        </View>

        <View style={styles.segment}>
          {(['Personal', 'Business'] as const).map(item => <Pressable key={item} onPress={() => { setAccountType(item); setError(null); }} style={[styles.segmentButton, accountType === item && styles.segmentButtonActive]}><Text style={[styles.segmentText, accountType === item && styles.segmentTextActive]}>{item === 'Personal' ? 'For people' : 'For business'}</Text></Pressable>)}
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
            {isRegister ? `Create your ${accountType.toLowerCase()} account` : `${accountType} sign in`}
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

          {isRegister && <TermsAcceptance accepted={acceptedTerms} onChange={setAcceptedTerms} styles={styles} />}

          {isRegister && accountType === 'Business' && <>
            <LabeledInput label="Business name" onChangeText={setBusinessName} placeholder="Acme India" value={businessName} />
            <LabeledInput autoCapitalize="none" label="Website (optional)" onChangeText={setWebsiteUrl} placeholder="https://acme.in" value={websiteUrl} />
            <Text style={styles.inputLabel}>Choose a plan</Text>
            <View style={styles.segment}>{(['Starter', 'Growth', 'Pro'] as const).map(plan => <Pressable key={plan} onPress={() => setSubscriptionPlan(plan)} style={[styles.segmentButton, subscriptionPlan === plan && styles.segmentButtonActive]}><Text style={[styles.segmentText, subscriptionPlan === plan && styles.segmentTextActive]}>{plan}</Text></Pressable>)}</View>
            <Text style={styles.helperText}>Includes a 14-day trial. Billing activation will be completed before production launch.</Text>
          </>}

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
          {accountType === 'Personal' && googleConfigured && <GoogleSignInButton busy={isSubmitting} setBusy={setIsSubmitting} setError={setError} signIn={signInWithGoogle} styles={styles} />}
        </View>


      </ScrollView>
    </KeyboardAvoidingView>
  );
}

function GoogleSignInButton({ busy, setBusy, setError, signIn, styles }: {
  busy: boolean;
  setBusy: (value: boolean) => void;
  setError: (value: string | null) => void;
  signIn: (idToken: string, acceptTerms: boolean) => Promise<void>;
  styles: ReturnType<typeof createStyles>;
}) {
  const [acceptedTerms, setAcceptedTerms] = useState(false);
  const webClientId = process.env.EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID!;
  const [request, response, prompt] = Google.useIdTokenAuthRequest({
    clientId: webClientId,
    webClientId,
    androidClientId: process.env.EXPO_PUBLIC_GOOGLE_ANDROID_CLIENT_ID,
    iosClientId: process.env.EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID,
  });

  useEffect(() => {
    if (response?.type !== 'success') return;
    const idToken = response.params.id_token;
    if (!idToken) { setError('Google did not return an identity token.'); return; }
    setBusy(true); setError(null);
    void signIn(idToken, acceptedTerms)
      .catch(error => setError(error instanceof Error ? error.message : 'Google sign-in failed'))
      .finally(() => setBusy(false));
  }, [acceptedTerms, response, setBusy, setError, signIn]);

  return <>
    <View style={styles.authDivider}><View style={styles.authDividerLine} /><Text style={styles.authDividerText}>or</Text><View style={styles.authDividerLine} /></View>
    <TermsAcceptance accepted={acceptedTerms} onChange={setAcceptedTerms} styles={styles} />
    <Pressable disabled={busy || !request || !acceptedTerms} onPress={() => { setError(null); void prompt(); }} style={[styles.googleButton, (busy || !request || !acceptedTerms) && styles.primaryButtonDisabled]}>
      <Text style={styles.googleButtonText}>Continue with Google</Text>
    </Pressable>
  </>;
}

function TermsAcceptance({ accepted, onChange, styles }: { accepted: boolean; onChange: (value: boolean) => void; styles: ReturnType<typeof createStyles> }) {
  const webUrl = process.env.EXPO_PUBLIC_WEB_URL ?? 'https://mvp-react-frontend.azurewebsites.net';
  return <View style={styles.termsRow}>
    <Pressable accessibilityRole="checkbox" accessibilityState={{ checked: accepted }} onPress={() => onChange(!accepted)} style={[styles.checkbox, accepted && styles.checkboxChecked]}>
      {accepted && <Text style={styles.checkboxMark}>✓</Text>}
    </Pressable>
    <Text style={styles.termsText}>I agree to the <Text style={styles.termsLink} onPress={() => void Linking.openURL(`${webUrl}/terms`)}>Terms of Use and Community Guidelines</Text> and acknowledge the <Text style={styles.termsLink} onPress={() => void Linking.openURL(`${webUrl}/privacy`)}>Privacy Policy</Text>.</Text>
  </View>;
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
  const { colors: theme, scheme } = useTheme(); const styles = useMemo(() => createStyles(theme), [theme]);

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
  const { colors: theme, scheme } = useTheme(); const styles = useMemo(() => createStyles(theme), [theme]);

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
            <OnboardingPoint title="Fresh polls" copy="Discover the questions everyone is talking about." />
            <OnboardingPoint title="Fast voting" copy="Tap an option, get results, and continue your session." />
            <OnboardingPoint title="Share-worthy results" copy="Public polls can be shared once you want to bring friends in." />
          </>
        )}

        {step === 1 && (
          <>
            <OnboardingPoint title="XP rewards" copy="Votes update your XP and level right away." />
            <OnboardingPoint title="Daily streaks" copy="Come back daily to keep your streak growing." />
            <OnboardingPoint title="Ranks" copy="Add your voice and find your place in the community." />
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
  const { colors: theme, scheme } = useTheme(); const styles = useMemo(() => createStyles(theme), [theme]);

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

const createStyles = (theme: ThemeColors) => StyleSheet.create({
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
  hero: {
    paddingTop: spacing['4'],
    paddingBottom: spacing['4'],
  },
  authBrand: { alignItems: 'center', flexDirection: 'row', gap: spacing['3'], marginBottom: spacing['5'] },
  authLogoTile: { alignItems: 'center', backgroundColor: '#17151F', borderRadius: radius.lg, height: 58, justifyContent: 'center', width: 58 },
  authLogo: { height: 44, width: 44 },
  authBrandName: { color: theme.foreground, fontSize: typography.fontSize['3xl'], fontWeight: typography.fontWeight.extrabold, letterSpacing: -1, lineHeight: 31 },
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
  helperText: { color: theme.mutedForeground, fontSize: typography.fontSize.xs, lineHeight: 18 },
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
    backgroundColor: `${theme.destructive}16`,
    borderRadius: radius.lg,
    color: theme.destructive,
    fontSize: typography.fontSize.sm,
    fontWeight: typography.fontWeight.bold,
    letterSpacing: 0,
    lineHeight: 20,
    padding: spacing['3'],
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
  authDivider: { flexDirection: 'row', alignItems: 'center', gap: spacing['3'] },
  authDividerLine: { flex: 1, height: 1, backgroundColor: theme.border },
  authDividerText: { color: theme.mutedForeground, fontSize: typography.fontSize.sm },
  googleButton: { alignItems: 'center', justifyContent: 'center', backgroundColor: theme.background, borderColor: theme.border, borderWidth: 1, borderRadius: radius.lg, minHeight: 50, paddingHorizontal: spacing['4'] },
  googleButtonText: { color: theme.foreground, fontSize: typography.fontSize.base, fontWeight: typography.fontWeight.extrabold },
  termsRow: { alignItems: 'flex-start', flexDirection: 'row', gap: spacing['3'] },
  checkbox: { alignItems: 'center', borderColor: theme.border, borderRadius: 5, borderWidth: 1, height: 22, justifyContent: 'center', marginTop: 1, width: 22 },
  checkboxChecked: { backgroundColor: theme.primary, borderColor: theme.primary },
  checkboxMark: { color: theme.primaryForeground, fontSize: 15, fontWeight: typography.fontWeight.extrabold },
  termsText: { color: theme.mutedForeground, flex: 1, fontSize: typography.fontSize.xs, lineHeight: 19 },
  termsLink: { color: theme.primary, fontWeight: typography.fontWeight.extrabold },
});
