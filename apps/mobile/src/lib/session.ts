import * as SecureStore from 'expo-secure-store';

import type { AuthResponse, AuthUser } from '../types/auth';

const TOKEN_KEY = 'pollify_token';
const USER_KEY = 'pollify_user';
const ONBOARDING_PREFIX = 'pollify_onboarding_complete_';

async function secureStoreAvailable() {
  return SecureStore.isAvailableAsync().catch(() => false);
}

export async function getToken() {
  if (!(await secureStoreAvailable())) return null;
  return SecureStore.getItemAsync(TOKEN_KEY);
}

export async function getStoredUser(): Promise<AuthUser | null> {
  if (!(await secureStoreAvailable())) return null;

  const raw = await SecureStore.getItemAsync(USER_KEY);
  if (!raw) return null;

  try {
    return JSON.parse(raw) as AuthUser;
  } catch {
    await SecureStore.deleteItemAsync(USER_KEY);
    return null;
  }
}

export async function saveSession(data: AuthResponse) {
  if (!(await secureStoreAvailable())) return;

  await Promise.all([
    SecureStore.setItemAsync(TOKEN_KEY, data.token),
    SecureStore.setItemAsync(USER_KEY, JSON.stringify(data.user)),
  ]);
}

export async function clearSession() {
  if (!(await secureStoreAvailable())) return;

  await Promise.all([
    SecureStore.deleteItemAsync(TOKEN_KEY),
    SecureStore.deleteItemAsync(USER_KEY),
  ]);
}

export async function hasCompletedOnboarding(userId: number) {
  if (!(await secureStoreAvailable())) return false;

  const value = await SecureStore.getItemAsync(`${ONBOARDING_PREFIX}${userId}`);
  return value === 'true';
}

export async function markOnboardingComplete(userId: number) {
  if (!(await secureStoreAvailable())) return;

  await SecureStore.setItemAsync(`${ONBOARDING_PREFIX}${userId}`, 'true');
}
