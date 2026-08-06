import { render,screen } from "@testing-library/react"
import { describe,expect,it } from "vitest"
import { ClashReveal } from "./clash-reveal"
describe("ClashReveal",()=>{it("presents tied public totals as unresolved with no point",()=>{render(<ClashReveal round={{id:1,position:0,pollId:1,question:"Q",status:"Revealed",options:[{id:1,text:"Up",publicVotes:4},{id:2,text:"Against",publicVotes:4}],resolvedMajorityOptionId:null,agreed:false,predictionPointsAwarded:0,revealedOpinions:[]}}/>);expect(screen.getByText(/unresolved/i)).toBeInTheDocument();expect(screen.getByText("+0")).toBeInTheDocument();expect(screen.getByText(/does not award XP/i)).toBeInTheDocument()})})
