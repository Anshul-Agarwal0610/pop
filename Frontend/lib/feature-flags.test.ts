import { describe, expect, it } from "vitest"
import { rolloutBucket } from "./feature-flags"
it("assigns deterministic rollout buckets", () => { const a = rolloutBucket("gamification_challenges_v1", "usr_42"); expect(rolloutBucket("gamification_challenges_v1", "usr_42")).toBe(a); expect(a).toBeGreaterThanOrEqual(0); expect(a).toBeLessThan(10000) })
