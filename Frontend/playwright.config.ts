import { defineConfig } from "@playwright/test"

const viewports = [
  ["mobile-320", 320, 640],
  ["mobile-375", 375, 667],
  ["tablet-768", 768, 1024],
  ["laptop-1024", 1024, 768],
  ["desktop-1440", 1440, 900],
] as const

export default defineConfig({
  testDir: "./tests",
  fullyParallel: true,
  retries: process.env.CI ? 2 : 0,
  reporter: "list",
  use: {
    baseURL: "http://127.0.0.1:3000",
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
  },
  projects: viewports.map(([name, width, height]) => ({ name, use: { viewport: { width, height } } })),
  webServer: {
    command: "npm run dev",
    url: "http://127.0.0.1:3000",
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
})
