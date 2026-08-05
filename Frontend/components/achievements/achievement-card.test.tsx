import { render,screen } from "@testing-library/react"
import { describe,expect,it } from "vitest"
import { AchievementCard } from "./achievement-card"
import type { ApiAchievement } from "@/lib/api"
const base:ApiAchievement={badgeId:1,userBadgeId:null,code:"x",name:"Explorer",description:"Explore polls",icon:"Compass",category:"Exploration",status:"locked",requirement:"Vote in 3 categories",rewardXp:75,rewardTitle:null,awardedAt:null,currentProgress:null,targetProgress:null,progressPercent:null,isSecret:false}
describe("AchievementCard",()=>{
 it("explains a locked achievement in text",()=>{render(<AchievementCard achievement={base}/>);expect(screen.getByText(/Unlock:/)).toHaveTextContent("Vote in 3 categories");expect(screen.getByLabelText("Explorer: locked")).toBeInTheDocument()})
 it("presents semantic progress",()=>{render(<AchievementCard achievement={{...base,status:"in-progress",currentProgress:2,targetProgress:3,progressPercent:66}}/>);expect(screen.getByRole("progressbar")).toHaveAttribute("aria-valuenow","2")})
 it("shows earned date and reward",()=>{render(<AchievementCard achievement={{...base,status:"earned",awardedAt:"2026-08-01T00:00:00Z"}}/>);expect(screen.getByText(/Reward: 75 XP/)).toBeInTheDocument();expect(screen.getByText(/Earned/)).toBeInTheDocument()})
 it("does not expose secret requirements",()=>{render(<AchievementCard achievement={{...base,name:"Secret achievement",description:"Keep exploring to discover this achievement.",requirement:null,isSecret:true}}/>);expect(screen.queryByText(/Vote in 3 categories/)).not.toBeInTheDocument()})
})
