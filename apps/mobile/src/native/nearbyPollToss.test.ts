import { expect, jest, test } from '@jest/globals';

jest.mock('react-native',()=>({ NativeEventEmitter: jest.fn(), NativeModules:{}, Platform:{OS:'android'} }));
import { parsePollTossPayload } from './nearbyPollToss';
const enc=new TextEncoder(); const token='A'.repeat(43);
test('accepts only the opaque v1 token schema',()=>expect(parsePollTossPayload(enc.encode(JSON.stringify({version:1,invitationToken:token})))).toEqual({version:1,invitationToken:token}));
test.each([{version:2,invitationToken:token},{version:1,invitationToken:'poll-12'},{version:1,invitationToken:token,pollId:12},['bad']])('rejects spoofed payload %#',value=>expect(()=>parsePollTossPayload(enc.encode(JSON.stringify(value)))).toThrow());
test('rejects oversized payload',()=>expect(()=>parsePollTossPayload(new Uint8Array(513))).toThrow('payload_oversize'));
