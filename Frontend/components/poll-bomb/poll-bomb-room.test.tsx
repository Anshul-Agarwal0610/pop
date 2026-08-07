import { render, screen } from "@testing-library/react"
import { beforeEach, describe, expect, it, vi } from "vitest"
import { PollBombRoom } from "./poll-bomb-room"
import { useLiveSession } from "@/hooks/use-live-session"

vi.mock("@/hooks/use-live-session",()=>({useLiveSession:vi.fn()}))
const mocked=vi.mocked(useLiveSession)
const base={publicId:"bomb",mode:"Bomb" as const,status:"Voting" as const,hostUserId:1,participantId:2,isHost:false,hasLockedVote:true,notificationsEnabled:false,joinedCount:4,lockedCount:2,targetVotes:3,remainingVotes:1,stateVersion:4,serverNow:new Date().toISOString(),expiresAt:new Date(Date.now()+10000).toISOString(),revealedAt:null,terminalReason:null,poll:{id:9,question:"Best snack?",options:[{id:1,text:"Popcorn",voteCount:null},{id:2,text:"Chips",voteCount:null}]}}

describe("PollBombRoom",()=>{
  beforeEach(()=>mocked.mockReturnValue({state:base,error:null,reconcile:vi.fn(),vote:vi.fn(),setNotifications:vi.fn()}))
  it("shows aggregate progress without exposing selections before reveal",()=>{
    render(<PollBombRoom publicId="bomb" />)
    expect(screen.getByText("4")).toBeInTheDocument();expect(screen.getByText("2")).toBeInTheDocument();expect(screen.getByText("3")).toBeInTheDocument()
    expect(screen.queryByText(/votes$/)).not.toBeInTheDocument()
    expect(screen.getByText(/Vote locked/)).toBeInTheDocument()
  })
  it("keeps partial results private after expiry",()=>{
    mocked.mockReturnValue({state:{...base,status:"Expired",terminalReason:"TargetNotReached"},error:null,reconcile:vi.fn(),vote:vi.fn(),setNotifications:vi.fn()})
    render(<PollBombRoom publicId="bomb" />)
    expect(screen.getByText("Expired without reveal")).toBeInTheDocument()
    expect(screen.queryByText("Popcorn")).not.toBeInTheDocument()
  })
})
