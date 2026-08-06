import { expect, test } from '@jest/globals';
import { initialPollTossState, reducePollToss } from './stateMachine';

test('visibility begins idle and requires explicit consent',()=>{
  expect(initialPollTossState.phase).toBe('idle');
  expect(reducePollToss(initialPollTossState,{type:'AVAILABLE'})).toEqual(initialPollTossState);
  expect(reducePollToss(initialPollTossState,{type:'CONSENT',mode:'receive'}).phase).toBe('checking');
});
test.each(['UNAVAILABLE','DENIED','RADIO_ERROR','TIMEOUT','FAILED'] as const)('%s falls back',type=>{
  expect(reducePollToss({phase:'discovering',mode:'receive'},{type}).phase).toBe('fallback');
});
test('receiver requires selection, verification and redemption',()=>{
  let s=reducePollToss(initialPollTossState,{type:'CONSENT',mode:'receive'}); s=reducePollToss(s,{type:'AVAILABLE'}); s=reducePollToss(s,{type:'PERMISSION_GRANTED'}); expect(s.phase).toBe('discovering');
  s=reducePollToss(s,{type:'PEER_SELECTED'}); expect(s.phase).toBe('verifying'); s=reducePollToss(s,{type:'VERIFIED'}); s=reducePollToss(s,{type:'PAYLOAD_RECEIVED'}); expect(s.phase).toBe('redeeming'); s=reducePollToss(s,{type:'REDEEMED'}); expect(s.phase).toBe('completed');
});
