export type PoPmojiPlacement = { x: number; y: number; scale: number };

export const defaultPoPmojiPlacement: PoPmojiPlacement = { x: 0.82, y: 0.05, scale: 1 };

const clamp = (value: number, min: number, max: number) => Math.min(max, Math.max(min, value));

export function normalizePoPmojiPlacement(value?: Partial<PoPmojiPlacement> | null): PoPmojiPlacement {
  return {
    x: clamp(Number.isFinite(value?.x) ? value!.x! : defaultPoPmojiPlacement.x, 0, 1),
    y: clamp(Number.isFinite(value?.y) ? value!.y! : defaultPoPmojiPlacement.y, 0, 1),
    scale: clamp(Number.isFinite(value?.scale) ? value!.scale! : defaultPoPmojiPlacement.scale, 0.7, 1.5),
  };
}

export function movePoPmoji(start: PoPmojiPlacement, dx: number, dy: number, width: number, height: number, stickerSize: number): PoPmojiPlacement {
  return normalizePoPmojiPlacement({
    ...start,
    x: start.x + dx / Math.max(1, width - stickerSize),
    y: start.y + dy / Math.max(1, height - stickerSize),
  });
}
