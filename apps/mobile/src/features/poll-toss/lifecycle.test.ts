import { expect, jest, test } from '@jest/globals';
import { boundedCleanup } from './lifecycle';
test('timeout, background and cancel clean native exactly once',async()=>{
 jest.useFakeTimers(); const stop=jest.fn(async()=>{}); let background=()=>{}; const cleanup=boundedCleanup({stop} as never,100,cb=>{background=cb;return jest.fn()},jest.fn());
 background(); await cleanup(); jest.advanceTimersByTime(100); await Promise.resolve(); expect(stop).toHaveBeenCalledTimes(1); jest.useRealTimers();
});
