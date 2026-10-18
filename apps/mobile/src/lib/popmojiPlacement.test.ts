import { expect, test } from '@jest/globals';
import { defaultPoPmojiPlacement, movePoPmoji, normalizePoPmojiPlacement } from './popmojiPlacement';

test('older polls use a safe default placement', () => {
  expect(normalizePoPmojiPlacement(null)).toEqual(defaultPoPmojiPlacement);
});

test('drag coordinates stay on the card even at the edges', () => {
  expect(movePoPmoji({ x: 0.5, y: 0.5, scale: 1 }, 300, -300, 300, 200, 70)).toEqual({ x: 1, y: 0, scale: 1 });
});

test('invalid or oversized sticker values cannot escape the saved range', () => {
  expect(normalizePoPmojiPlacement({ x: Number.NaN, y: 2, scale: 4 })).toEqual({ x: defaultPoPmojiPlacement.x, y: 1, scale: 1.5 });
});
