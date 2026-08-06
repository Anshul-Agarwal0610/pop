import { render, screen, waitFor } from "@testing-library/react"
import { beforeEach, describe, expect, it, vi } from "vitest"
import { AuthRouteGuard } from "./auth-route-guard"

const navigation = vi.hoisted(() => ({
  pathname: "/",
  query: "",
  replace: vi.fn(),
}))
const auth = vi.hoisted(() => ({ isAuthenticated: false, isLoading: false }))

vi.mock("next/navigation", () => ({
  usePathname: () => navigation.pathname,
  useSearchParams: () => new URLSearchParams(navigation.query),
  useRouter: () => ({ replace: navigation.replace }),
}))
vi.mock("@/contexts/auth-context", () => ({ useAuth: () => auth }))

describe("AuthRouteGuard", () => {
  beforeEach(() => {
    navigation.pathname = "/"
    navigation.query = ""
    navigation.replace.mockReset()
    auth.isAuthenticated = false
    auth.isLoading = false
  })

  it("renders Home for signed-out users, including during hydration", () => {
    auth.isLoading = true
    render(<AuthRouteGuard><div>Public Home</div></AuthRouteGuard>)
    expect(screen.getByText("Public Home")).toBeInTheDocument()
  })

  it("does not render protected content while authentication is unresolved", () => {
    navigation.pathname = "/polls"
    auth.isLoading = true
    render(<AuthRouteGuard><div>Protected polls</div></AuthRouteGuard>)
    expect(screen.queryByText("Protected polls")).not.toBeInTheDocument()
    expect(screen.getByText("Checking your session…")).toBeInTheDocument()
  })

  it("redirects signed-out protected routes and preserves paths and queries", async () => {
    navigation.pathname = "/polls/42"
    navigation.query = "view=results"
    render(<AuthRouteGuard><div>Protected poll</div></AuthRouteGuard>)

    expect(screen.queryByText("Protected poll")).not.toBeInTheDocument()
    expect(screen.getByText("Sign in required")).toBeInTheDocument()
    await waitFor(() => expect(navigation.replace).toHaveBeenCalledOnce())
    const target = navigation.replace.mock.calls[0][0] as string
    const url = new URL(target, "https://pollify.test")
    expect(url.pathname).toBe("/login")
    expect(url.searchParams.get("redirect")).toBe("/polls/42?view=results")
  })

  it("renders protected routes for authenticated users", () => {
    navigation.pathname = "/leaderboard"
    auth.isAuthenticated = true
    render(<AuthRouteGuard><div>Leaderboard</div></AuthRouteGuard>)
    expect(screen.getByText("Leaderboard")).toBeInTheDocument()
  })
})
