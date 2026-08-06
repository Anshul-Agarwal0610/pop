import { fireEvent, render, screen, waitFor } from "@testing-library/react"
import { afterEach, describe, expect, it, vi } from "vitest"
import { ResultCard } from "./result-card"
import { ResultCardActions } from "./result-card-actions"
import type { ApiResultCard } from "@/lib/api"

const card:ApiResultCard={id:1,publicToken:"abc",publicUrl:"https://pop.test/live/cards/abc",imageUrl:"https://api.pop.test/api/result-cards/public/abc/image",createdAt:"2026-01-01",expiresAt:"2027-01-01",payload:{schemaVersion:1,mode:"Clash",state:"Completed",aggregateResult:"80% agreement",milestone:"5-round chain",badge:{name:"In Sync",icon:"★"},participantCount:2,participants:[{label:"Asha",isAnonymous:false},{label:"Participant 2",isAnonymous:true}],accessibleSummary:"Clash with 2 participants: 80% agreement"}}
afterEach(()=>vi.unstubAllGlobals())
describe("ResultCard",()=>{
  it("renders result, badge, redacted participant and accessible summary",()=>{render(<ResultCard card={card}/>);expect(screen.getByRole("article",{name:card.payload.accessibleSummary})).toBeInTheDocument();expect(screen.getByText("Participant 2")).toBeInTheDocument();expect(screen.getByText(/In Sync/)).toBeInTheDocument()})
  it("always offers copy and public image download when native share is unsupported",()=>{vi.stubGlobal("navigator",{clipboard:{writeText:vi.fn()}});render(<ResultCardActions card={card}/>);expect(screen.queryByRole("button",{name:"Share"})).not.toBeInTheDocument();expect(screen.getByRole("button",{name:"Copy link"})).toBeInTheDocument();expect(screen.getByRole("link",{name:"Download image"})).toHaveAttribute("href",card.imageUrl)})
  it("keeps fallbacks after native share fails",async()=>{vi.stubGlobal("navigator",{share:vi.fn().mockRejectedValue(new Error("no")),canShare:()=>true,clipboard:{writeText:vi.fn()}});render(<ResultCardActions card={card}/>);fireEvent.click(screen.getByRole("button",{name:"Share"}));await waitFor(()=>expect(screen.getByRole("status")).toHaveTextContent("Sharing failed"));expect(screen.getByRole("button",{name:"Copy link"})).toBeInTheDocument()})
  it("shows a manual link when clipboard is unavailable",async()=>{vi.stubGlobal("navigator",{});render(<ResultCardActions card={card}/>);fireEvent.click(screen.getByRole("button",{name:"Copy link"}));await waitFor(()=>expect(screen.getByRole("status")).toHaveTextContent(card.publicUrl))})
})
