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
