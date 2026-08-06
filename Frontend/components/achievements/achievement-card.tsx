import { Award, CheckCircle2, LockKeyhole } from "lucide-react"
import type { ApiAchievement } from "@/lib/api"

export function AchievementCard({ achievement }: { achievement: ApiAchievement }) {
  const earned = achievement.status === "earned"
  const StatusIcon = earned ? CheckCircle2 : achievement.isSecret ? LockKeyhole : Award
  return <article className="rounded-2xl bg-card p-5 ring-1 ring-border/50" aria-label={`${achievement.name}: ${achievement.status}`}>
    <div className="flex gap-3"><StatusIcon className="mt-0.5 h-6 w-6 shrink-0" aria-hidden="true" />
      <div className="min-w-0"><p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">{achievement.status}</p>
        <h2 className="font-semibold">{achievement.name}</h2><p className="mt-1 text-sm text-muted-foreground">{achievement.description}</p>
      </div></div>
    {!earned && achievement.requirement && <p className="mt-3 text-sm"><span className="font-medium">Unlock:</span> {achievement.requirement}</p>}
    {achievement.currentProgress !== null && achievement.targetProgress !== null && <div className="mt-3">
      <div className="flex justify-between text-sm"><span>Progress</span><span>{achievement.currentProgress}/{achievement.targetProgress}</span></div>
      <div className="mt-1 h-2 overflow-hidden rounded-full bg-secondary" role="progressbar" aria-label={`${achievement.name} progress`} aria-valuemin={0} aria-valuemax={achievement.targetProgress} aria-valuenow={achievement.currentProgress}>
        <div className="h-full bg-primary" style={{ width: `${achievement.progressPercent ?? 0}%` }} /></div></div>}
    <div className="mt-3 flex flex-wrap gap-2 text-xs text-muted-foreground">
      <span>Reward: {achievement.rewardXp} XP{achievement.rewardTitle ? ` + “${achievement.rewardTitle}” title` : ""}</span>
      {achievement.awardedAt && <span>Earned {new Date(achievement.awardedAt).toLocaleDateString()}</span>}
    </div>
  </article>
}
