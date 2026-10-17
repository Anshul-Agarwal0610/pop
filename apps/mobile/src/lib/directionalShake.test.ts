import { expect, jest, test } from '@jest/globals';
import { directionalShake } from './directionalShake';

jest.mock('expo-sensors', () => ({ Accelerometer: { setUpdateInterval: jest.fn(), addListener: jest.fn() } }));

test('detects deliberate lateral movement in both directions', () => {
  expect(directionalShake(1.9, .2, .8)).toBe('right');
  expect(directionalShake(-1.9, .2, .8)).toBe('left');
});

test('ignores ordinary movement and vertical shocks', () => {
  expect(directionalShake(.7, .2, 1)).toBeNull();
  expect(directionalShake(1.6, 2, .2)).toBeNull();
});
