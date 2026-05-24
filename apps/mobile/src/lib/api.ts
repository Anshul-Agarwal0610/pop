import { API_BASE_URL } from '../config/api';
import { getToken } from './session';
import type { AuthResponse, AuthUser, LoginPayload, RegisterPayload } from '../types/auth';

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
