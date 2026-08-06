import { API_BASE_URL } from '../config/api';
import { getToken } from './session';
import type {
  AuthResponse,
  AuthUser,
  CategoryPreference,
  LoginPayload,
  RegisterPayload,
} from '../types/auth';
import type { ApiPoll, CastVoteResponse } from '../types/poll';
import type { MobileExperiments, PollTossInvitation } from '../types/pollToss';

interface ApiErrorShape {
  message?: string;
}

async function readError(response: Response) {
  const text = await response.text().catch(() => '');
  if (!text) return response.statusText || 'Request failed';

  try {
    const data = JSON.parse(text) as ApiErrorShape;
    return data.message ?? text;
  } catch {
    return text;
  }
}

export async function apiRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const token = await getToken();
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init?.headers,
    },
  });

  if (!response.ok) {
    throw new Error(await readError(response));
  }
  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
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
};

export const usersApi = {
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

export const pollTossApi = {
  experiments: () => apiRequest<MobileExperiments>('/api/mobile/experiments', { cache: 'no-store' }),
  create: (pollId: number) => apiRequest<PollTossInvitation>('/api/poll-toss/invitations', { method: 'POST', body: JSON.stringify({ pollId }) }),
  redeem: (invitationToken: string) => apiRequest<ApiPoll>('/api/poll-toss/invitations/redeem', { method: 'POST', body: JSON.stringify({ invitationToken }) }),
  cancel: (id: string) => apiRequest<void>(`/api/poll-toss/invitations/${encodeURIComponent(id)}`, { method: 'DELETE' }),
};
