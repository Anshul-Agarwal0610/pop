import { NativeEventEmitter, NativeModules, Platform } from 'react-native';
import type { PollTossPayload } from '../types/pollToss';

export const MAX_PAYLOAD_BYTES = 512;
const TOKEN = /^[A-Za-z0-9_-]{43}$/;
export function parsePollTossPayload(bytes: Uint8Array): PollTossPayload {
  if (bytes.byteLength > MAX_PAYLOAD_BYTES) throw new Error('payload_oversize');
  const text = new TextDecoder('utf-8', { fatal:true }).decode(bytes);
  const value: unknown = JSON.parse(text);
  if (!value || typeof value !== 'object' || Array.isArray(value)) throw new Error('payload_schema');
  const keys=Object.keys(value); const record=value as Record<string,unknown>;
  if (keys.length!==2 || !keys.includes('version') || !keys.includes('invitationToken') || record.version!==1 || typeof record.invitationToken!=='string' || !TOKEN.test(record.invitationToken)) throw new Error('payload_schema');
  return value as PollTossPayload;
}

export interface NearbyCapabilities { supported:boolean; playServices:boolean; radiosAvailable:boolean; missingPermissions:string[] }
export interface NearbyAdapter {
  capabilities():Promise<NearbyCapabilities>; requestPermissions():Promise<boolean>;
  startAdvertising(label:string):Promise<void>; startDiscovery():Promise<void>; selectEndpoint(id:string):Promise<void>;
  confirmVerification(id:string, accepted:boolean):Promise<void>; sendPayload(id:string, json:string):Promise<void>; stop():Promise<void>;
}
const module = Platform.OS==='android' ? NativeModules.PollifyNearby : undefined;
export const nearbyPollToss: NearbyAdapter = module ?? {
  capabilities: async()=>({supported:false,playServices:false,radiosAvailable:false,missingPermissions:[]}),
  requestPermissions:async()=>false, startAdvertising:async()=>{throw new Error('unsupported')}, startDiscovery:async()=>{throw new Error('unsupported')},
  selectEndpoint:async()=>{throw new Error('unsupported')}, confirmVerification:async()=>{throw new Error('unsupported')}, sendPayload:async()=>{throw new Error('unsupported')}, stop:async()=>{},
};
export const nearbyEvents = module ? new NativeEventEmitter(module) : undefined;
