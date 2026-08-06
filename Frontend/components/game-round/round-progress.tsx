export function RoundProgress({ current, total }: { current: number; total: number }) {
  const display = Math.min(current + 1, total); const remaining = Math.max(0, total - current)
  return <div><div className="mb-2 flex justify-between text-sm font-semibold"><span>Poll {display} of {total}</span><span>{remaining} remaining</span></div><div aria-label={`Poll ${display} of ${total}; ${remaining} remaining`} aria-valuemax={total} aria-valuemin={0} aria-valuenow={current} role="progressbar" className="h-2 overflow-hidden rounded-full bg-muted"><div className="h-full bg-primary transition-[width] motion-reduce:transition-none" style={{width:`${current/total*100}%`}}/></div></div>
}
