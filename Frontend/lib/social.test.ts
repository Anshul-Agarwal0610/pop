import { afterEach, describe, expect, it, vi } from "vitest"
import { ApiError, socialApi } from "./api"

afterEach(() => vi.unstubAllGlobals())

describe("social API",()=>{
  it("uses the authenticated social endpoint and parses bounded results",async()=>{
    const fetchMock = vi.fn(async (input: string | URL | Request, init?: RequestInit) => {
      expect(String(input)).toContain("/api/social/friends?")
      expect(new Headers(init?.headers).get("content-type")).toBe("application/json")
      return Response.json({items:[],nextCursor:null})
    })
    vi.stubGlobal("fetch", fetchMock)
    await expect(socialApi.friends()).resolves.toEqual({items:[],nextCursor:null})
    expect(fetchMock).toHaveBeenCalledOnce()
  })
  it("preserves conflict and rate limit statuses for UI handling",async()=>{
    vi.stubGlobal("fetch", vi.fn(async () => Response.json({message:"slow down"},{status:429})))
    await expect(socialApi.sendFriendRequest(2)).rejects.toMatchObject<ApiError>({status:429})
  })
  it("accepts empty 204 mutation responses",async()=>{
    vi.stubGlobal("fetch", vi.fn(async () => new Response(null,{status:204})))
    await expect(socialApi.acceptFriendRequest(4)).resolves.toBeUndefined()
  })
})
