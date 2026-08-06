import { beforeEach, describe, expect, it, vi } from "vitest"
import { clearAnalyticsDedupeForTests, configureAnalytics, identify, track } from "./client"
beforeEach(() => { localStorage.clear(); clearAnalyticsDedupeForTests() })
describe("analytics client", () => {
  it("suppresses capture without affirmative consent", () => { const capture=vi.fn(); configureAnalytics({capture}); track("gamification_hub_viewed", {surface:"home",challenge_count:0,level:1}); expect(capture).not.toHaveBeenCalled() })
  it("aliases pseudonymously and deduplicates", () => { localStorage.setItem("pollify_analytics_consent","granted"); const capture=vi.fn(), alias=vi.fn(); configureAnalytics({capture,alias}); identify(42); track("gamification_hub_viewed", {surface:"home",challenge_count:0,level:1}, "home"); track("gamification_hub_viewed", {surface:"home",challenge_count:0,level:1}, "home"); expect(alias.mock.calls[0][1]).toBe("usr_42"); expect(capture).toHaveBeenCalledTimes(1); expect(capture.mock.calls[0][0]).toMatchObject({event:"gamification_hub_viewed",schema_version:1,platform:"web",user_id:"usr_42"}) })
  it("swallows adapter failures", () => { localStorage.setItem("pollify_analytics_consent","granted"); configureAnalytics({capture:()=>{throw new Error("offline")}}); expect(()=>track("gamification_hub_viewed", {surface:"home",challenge_count:0,level:1})).not.toThrow() })
})
