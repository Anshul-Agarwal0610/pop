import type { ApiPoll } from '../types/poll';

export function isPollClosed(poll: ApiPoll, now = Date.now()) {
  return !poll.isActive || (Number.isFinite(Date.parse(poll.expiresAt)) && Date.parse(poll.expiresAt) <= now);
}

export function pollDeadline(poll: ApiPoll, now = Date.now()) {
  if (isPollClosed(poll, now)) return 'Closed';
  const remaining = Date.parse(poll.expiresAt) - now;
  if (!Number.isFinite(remaining)) return 'Open';
  if (remaining < 3_600_000) return `${Math.max(1, Math.ceil(remaining / 60_000))}m left`;
  if (remaining < 86_400_000) return `${Math.ceil(remaining / 3_600_000)}h left`;
  return `${Math.ceil(remaining / 86_400_000)}d left`;
}

export function resultPercent(votes: number, total: number) {
  return total > 0 ? Math.max(0, Math.min(100, Math.round(votes / total * 100))) : 0;
}

export function nextUnvotedPoll(polls: ApiPoll[], currentId: number) {
  const index = polls.findIndex(poll => poll.id === currentId);
  const ordered = [...polls.slice(index + 1), ...polls.slice(0, index)];
  return ordered.find(poll => !poll.hasVoted && !isPollClosed(poll) && poll.options.length > 0);
}
