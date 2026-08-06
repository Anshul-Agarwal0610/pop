import { expect, test, type Page } from "@playwright/test"

const routes = [
  "/", "/login", "/polls", "/polls/1", "/create", "/leaderboard",
  "/notifications", "/profile", "/moderation", "/business", "/wellness",
]

async function expectNoPageOverflow(page: Page) {
  await expect.poll(() => page.evaluate(() =>
    document.documentElement.scrollWidth - document.documentElement.clientWidth
  )).toBeLessThanOrEqual(1)
}

for (const route of routes) {
  test(`${route} stays within the viewport`, async ({ page }) => {
    await page.goto(route)
    await page.waitForLoadState("domcontentloaded")
    await expectNoPageOverflow(page)
  })
}

test("mobile navigation exposes every destination", async ({ page, viewport }) => {
  test.skip(!viewport || viewport.width >= 1024, "mobile-only behavior")
  await page.goto("/")
  const nav = page.getByRole("navigation", { name: "Mobile navigation" })
  await expect(nav).toBeVisible()
  await expect(nav.getByRole("link", { name: "Polls" })).toBeVisible()
  await nav.getByRole("button", { name: "More destinations" }).click()
  for (const label of ["Notifications", "Business", "Wellness", "Profile"]) {
    await expect(page.getByRole("link", { name: label, exact: true })).toBeVisible()
  }
  await expectNoPageOverflow(page)
})

test("desktop uses the sidebar", async ({ page, viewport }) => {
  test.skip(!viewport || viewport.width < 1024, "desktop-only behavior")
  await page.goto("/")
  await expect(page.locator("aside").first()).toBeVisible()
  await expect(page.getByRole("navigation", { name: "Mobile navigation" })).toBeHidden()
})
