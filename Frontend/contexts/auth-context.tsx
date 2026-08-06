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
  getToken,
  getCurrentUser,
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

  useEffect(() => {
    let active = true

    async function hydrate() {
      const storedUser = getStoredUser()
      if (!getToken() || !storedUser) {
        clearSession()
        if (active) setLoading(false)
        return
      }

      try {
        const currentUser = await getCurrentUser()
        if (active) setUser(currentUser)
      } catch {
        clearSession()
        if (active) setUser(null)
      } finally {
        if (active) setLoading(false)
      }
    }

    hydrate()
    return () => { active = false }
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
