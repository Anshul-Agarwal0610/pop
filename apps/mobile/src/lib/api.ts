import { API_BASE_URL } from '../config/api';
import { getToken } from './session';
import type {
  AuthResponse,
  AuthUser,
  CategoryPreference,
  LoginPayload,
  RegisterPayload,
  RegisterBusinessPayload,
} from '../types/auth';
import type { ApiPoll, CastVoteResponse } from '../types/poll';

interface ApiErrorShape {
  message?: string;
  title?: string;
  errors?: Record<string, string[]>;
}

export class ApiRequestError extends Error {
  constructor(message: string, public readonly status: number) {
    super(message);
    this.name = 'ApiRequestError';
  }
}

async function readError(response: Response) {
  const text = await response.text().catch(() => '');
  if (!text) return response.statusText || 'Request failed';

  try {
    const data = JSON.parse(text) as ApiErrorShape;
    return data.message ?? (data.errors ? Object.values(data.errors).flat().slice(0, 3).join(' ') : data.title) ?? `Request failed (${response.status}). Please try again.`;
  } catch {
    return `Request failed (${response.status}). Please try again.`;
  }
}

export async function apiRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const token = await getToken();
  const multipart = typeof FormData !== 'undefined' && init?.body instanceof FormData;
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: {
      ...(!multipart ? { 'Content-Type': 'application/json' } : {}),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init?.headers,
    },
  });

  if (!response.ok) {
    throw new ApiRequestError(await readError(response), response.status);
  }

  if (response.status === 204) return undefined as T;
  const body = await response.text();
  return body ? JSON.parse(body) as T : undefined as T;
}

export const authApi = {
  login: (payload: LoginPayload) =>
    apiRequest<AuthResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify(payload),
    }),
  register: (payload: RegisterPayload) =>
    apiRequest<AuthResponse>('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify(payload),
    }),
  me: () => apiRequest<AuthUser>('/api/auth/me'),
  google: (idToken: string, acceptTerms: boolean) => apiRequest<AuthResponse>('/api/auth/google', {
    method: 'POST',
    body: JSON.stringify({ idToken, acceptTerms }),
  }),
  businessLogin: (payload: LoginPayload) => apiRequest<AuthResponse>('/api/auth/business/login', {
    method: 'POST', body: JSON.stringify(payload),
  }),
  businessRegister: (payload: RegisterBusinessPayload) => apiRequest<AuthResponse>('/api/auth/business/register', {
    method: 'POST', body: JSON.stringify(payload),
  }),
};

export const usersApi = {
  deleteAccount: () => apiRequest<void>('/api/users/me', { method: 'DELETE' }),
  updateProfile: (displayName: string, avatarUrl?: string | null) =>
    apiRequest<AuthUser>('/api/users/me/profile', { method: 'PUT', body: JSON.stringify({ displayName, avatarUrl }) }),
  getAnalyticsPrivacy: () => apiRequest<{ consent: 'unknown' | 'granted' | 'denied'; updatedAt: string | null }>('/api/users/me/privacy'),
  updateAnalyticsPrivacy: (consent: 'unknown' | 'granted' | 'denied') => apiRequest('/api/users/me/privacy', { method: 'PUT', body: JSON.stringify({ consent }) }),
  getLeaderboard: (count = 20) => apiRequest<AuthUser[]>(`/api/users/leaderboard?count=${count}`),
  getCategoryPreferences: () =>
    apiRequest<CategoryPreference[]>('/api/users/me/preferences/categories'),
  updateCategoryPreferences: (categories: string[]) =>
    apiRequest<CategoryPreference[]>('/api/users/me/preferences/categories', {
      method: 'PUT',
      body: JSON.stringify({ categories }),
    }),
};

export const notificationsApi = {
  registerDeviceToken: (token: string, platform: 'android' | 'ios' | string) =>
    apiRequest<{ success: boolean }>('/api/notifications/device-tokens', {
      method: 'POST',
      body: JSON.stringify({ token, platform }),
    }),
  disableDeviceToken: (token: string) =>
    apiRequest<{ success: boolean }>(
      `/api/notifications/device-tokens?token=${encodeURIComponent(token)}`,
      { method: 'DELETE' },
    ),
};

export const pollsApi = {
  getTrending: (count = 20) => apiRequest<ApiPoll[]>(`/api/polls/trending?count=${count}`),
};

export const votesApi = {
  cast: (pollId: number, optionId: number) =>
    apiRequest<CastVoteResponse>('/api/votes', {
      method: 'POST',
      body: JSON.stringify({ pollId, optionId }),
    }),
};
