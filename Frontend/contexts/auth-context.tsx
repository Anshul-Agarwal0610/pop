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
  saveStoredUser,
} from "@/lib/auth"
import type { ApiProgression } from "@/lib/api"

interface AuthContextValue {
  user: AuthUser | null
  isLoading: boolean
  isAuthenticated: boolean
  login:  (data: AuthResponse) => void
  logout: () => void
  applyProgression: (progression: ApiProgression) => void
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

  const applyProgression = useCallback((progression: ApiProgression) => {
    setUser((current) => {
      if (!current) return current
      const next = { ...current, xp: progression.totalXp, level: progression.level, progression }
      saveStoredUser(next)
      return next
    })
  }, [])

  return (
    <AuthContext.Provider
      value={{ user, isLoading, isAuthenticated: !!user, login, logout, applyProgression }}
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
