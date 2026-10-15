import type { ApiPollOption } from '../types/poll';

export function ballotChoices(options: ApiPollOption[]) {
  if (options.length !== 2) return null;
  const support = options.find(option => ['support', 'up'].includes(option.side?.toLowerCase() ?? '')) ?? options.find(option => ['support', 'for', 'up', 'yes'].includes(option.text.trim().toLowerCase()));
  const against = options.find(option => option.side?.toLowerCase() === 'against') ?? options.find(option => ['against', 'oppose', 'down', 'no'].includes(option.text.trim().toLowerCase()));
  return support && against ? [{ ...against, label: 'Against', statement: against.text }, { ...support, label: 'Support', statement: support.text }] : null;
}
export function ballotTarget(x: number, y: number, width: number) {
  if (y < 180 || y > 320 || x < 0 || x > width) return null;
  if (x < (width - 16) / 2) return 0;
  if (x > (width + 16) / 2) return 1;
  return null;
}

// Coordinates are measured in the same screen space as the released touch.
export function measuredBallotTarget(x: number, y: number, boxes: Array<{ x: number; y: number; width: number; height: number }>) {
  const index = boxes.findIndex(box => box && box.width > 0 && box.height > 0 && x >= box.x - 6 && x <= box.x + box.width + 6 && y >= box.y - 12 && y <= box.y + box.height + 8);
  return index < 0 ? null : index;
}
