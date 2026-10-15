import { useEffect, useRef } from 'react';
import { Accelerometer } from 'expo-sensors';

export type ShakeDirection = 'left' | 'right';

export function directionalShake(x: number, y: number, z: number): ShakeDirection | null {
  const lateral = Math.abs(x);
  if (lateral < 1.55 || lateral < Math.abs(y) * 1.25 || lateral < Math.abs(z) * 1.15) return null;
  return x > 0 ? 'right' : 'left';
}

export function useDirectionalShake(active: boolean, onShake: (direction: ShakeDirection) => void) {
  const callback = useRef(onShake);
  const lastTrigger = useRef(0);
  callback.current = onShake;

  useEffect(() => {
    if (!active) return;
    Accelerometer.setUpdateInterval(90);
    const subscription = Accelerometer.addListener(({ x, y, z }) => {
      const direction = directionalShake(x, y, z);
      const now = Date.now();
      if (!direction || now - lastTrigger.current < 1800) return;
      lastTrigger.current = now;
      callback.current(direction);
    });
    return () => subscription.remove();
  }, [active]);
}
