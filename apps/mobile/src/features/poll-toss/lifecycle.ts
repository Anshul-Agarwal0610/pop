import type { NearbyAdapter } from '../../native/nearbyPollToss';

export function boundedCleanup(native: NearbyAdapter, timeoutMs:number, subscribeBackground:(callback:()=>void)=>()=>void, onTimeout:()=>void) {
  let stopped=false;
  const stop=async()=>{ if(stopped)return; stopped=true; clearTimeout(timer); unsubscribe(); await native.stop(); };
  const timer=setTimeout(()=>{ void stop().finally(onTimeout); },timeoutMs);
  const unsubscribe=subscribeBackground(()=>{ void stop(); });
  return stop;
}
