import { API_BASE_URL } from "@/lib/config"

const TOKEN_KEY = "pollify_token"
const USER_KEY  = "pollify_user"

export interface AuthUser {
  id: number
  username: string
  displayName: string
  email?: string
  avatarUrl?: string
  authProvider: "local" | "google"
  xp: number
  streak: number
  totalVotes: number
  pollsCreated: number
  lastVoteDate?: string | null
  createdAt: string
}

export interface AuthResponse {
  token: string
  user: AuthUser
}

// ── Token helpers ─────────────────────────────────────────────────────────

export function getToken(): string | null {
  if (typeof window === "undefined") return null
  return localStorage.getItem(TOKEN_KEY)
}

export function getStoredUser(): AuthUser | null {
  if (typeof window === "undefined") return null
  try {
    const raw = localStorage.getItem(USER_KEY)
    return raw ? JSON.parse(raw) : null
  } catch {
    return null
  }
}

export function saveSession(data: AuthResponse) {
  localStorage.setItem(TOKEN_KEY, data.token)
  localStorage.setItem(USER_KEY, JSON.stringify(data.user))
}

export function clearSession() {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(USER_KEY)
}

// ── API calls ─────────────────────────────────────────────────────────────

async function post<T>(path: string, body: unknown): Promise<T> {
  const res = await fetch(`${API_BASE_URL}${path}`, {
    method:  "POST",
    headers: { "Content-Type": "application/json" },
    body:    JSON.stringify(body),
  })
  const data = await res.json()
  if (!res.ok) throw new Error(data.message ?? "Request failed")
  return data as T
}

export async function apiRegister(
  username: string,
  displayName: string,
  password: string,
  confirmPassword: string,
): Promise<AuthResponse> {
  return post<AuthResponse>("/api/auth/register", {
    username,
    displayName,
    password,
    confirmPassword,
  })
}

export async function apiLogin(
  username: string,
  password: string,
): Promise<AuthResponse> {
  return post<AuthResponse>("/api/auth/login", { username, password })
}

export async function apiGoogleLogin(idToken: string): Promise<AuthResponse> {
  return post<AuthResponse>("/api/auth/google", { idToken })
}
