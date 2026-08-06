"use client"

import {
  createContext,
  useContext,
  useEffect,
  useState,
  useCallback,
  type ReactNode,
} from "react"
import {
  type AuthUser,
  type AuthResponse,
  getStoredUser,
  saveSession,
  clearSession,
} from "@/lib/auth"
import { usersApi } from "@/lib/api"
import { getAnalyticsConsent, setAnalyticsConsent } from "@/lib/analytics/privacy"

interface AuthContextValue {
  user: AuthUser | null
  isLoading: boolean
  isAuthenticated: boolean
  login:  (data: AuthResponse) => void
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser]         = useState<AuthUser | null>(null)
  const [isLoading, setLoading] = useState(true)

  // Hydrate from localStorage on mount
  useEffect(() => {
    setUser(getStoredUser())
    setLoading(false)
  }, [])

  const login = useCallback((data: AuthResponse) => {
    saveSession(data)
    setUser(data.user)
  }, [])

  const logout = useCallback(() => {
    clearSession()
    setUser(null)
  }, [])

  useEffect(() => {
    if (!user) return
    usersApi.getAnalyticsPrivacy().then(({ consent }) => {
      const local = getAnalyticsConsent()
      if (consent === "denied" || local === "denied") setAnalyticsConsent("denied")
      else if (consent === "granted" && local === "granted") setAnalyticsConsent("granted")
      else setAnalyticsConsent("unknown")
    }).catch(() => undefined)
  }, [user?.id])

  return (
    <AuthContext.Provider
      value={{ user, isLoading, isAuthenticated: !!user, login, logout }}
    >
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error("useAuth must be used inside <AuthProvider>")
  return ctx
}
