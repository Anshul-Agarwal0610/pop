import { Suspense } from "react"
import { act } from "@testing-library/react"
import { hydrateRoot } from "react-dom/client"
import { renderToString } from "react-dom/server"
import { expect, it, vi } from "vitest"
import { AuthProvider, useAuth } from "./auth-context"

vi.mock("@/lib/auth", () => ({
  getStoredUser: () => null,
  getToken: () => null,
  getCurrentUser: vi.fn(),
  saveSession: vi.fn(),
  clearSession: vi.fn(),
  saveStoredUser: vi.fn(),
}))

it("hydrates a delayed auth consumer without changing the server greeting", async () => {
  let delayHydration = false
  let release!: () => void
  const pending = new Promise<void>((resolve) => { release = resolve })
  function Greeting() {
    const auth = useAuth()
    if (delayHydration) throw pending
    return <h1>Welcome back, {auth.isLoading ? "..." : "there"}!</h1>
  }
  const tree = <AuthProvider><Suspense fallback={null}><Greeting /></Suspense></AuthProvider>
  const container = document.createElement("div")
  container.innerHTML = renderToString(tree)
  expect(container.textContent).toBe("Welcome back, ...!")
  document.body.append(container)
  const onRecoverableError = vi.fn()
  delayHydration = true
  let root!: ReturnType<typeof hydrateRoot>
  try {
    await act(async () => { root = hydrateRoot(container, tree, { onRecoverableError }) })
    await act(async () => { delayHydration = false; release(); await pending })
    expect(container.textContent).toBe("Welcome back, there!")
    expect(onRecoverableError).not.toHaveBeenCalled()
  } finally {
    await act(async () => root?.unmount())
    container.remove()
  }
})
