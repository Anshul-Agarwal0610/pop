import { afterAll, afterEach, beforeAll, describe, expect, it } from "vitest"
import { http, HttpResponse } from "msw"
import { setupServer } from "msw/node"
import { ApiError, socialApi } from "./api"

const server=setupServer()
beforeAll(()=>server.listen({onUnhandledRequest:"error"}))
afterEach(()=>server.resetHandlers())
afterAll(()=>server.close())

describe("social API",()=>{
  it("uses the authenticated social endpoint and parses bounded results",async()=>{
    server.use(http.get("*/api/social/friends",({request})=>{expect(request.headers.get("content-type")).toBe("application/json");return HttpResponse.json({items:[],nextCursor:null})}))
    await expect(socialApi.friends()).resolves.toEqual({items:[],nextCursor:null})
  })
  it("preserves conflict and rate limit statuses for UI handling",async()=>{
    server.use(http.post("*/api/social/friends/requests",()=>HttpResponse.json({message:"slow down"},{status:429})))
    await expect(socialApi.sendFriendRequest(2)).rejects.toMatchObject<ApiError>({status:429})
  })
  it("accepts empty 204 mutation responses",async()=>{
    server.use(http.post("*/api/social/friends/requests/4/accept",()=>new HttpResponse(null,{status:204})))
    await expect(socialApi.acceptFriendRequest(4)).resolves.toBeUndefined()
  })
})
