export type PollTossPhase = 'idle'|'checking'|'permission'|'advertising'|'discovering'|'verifying'|'connected'|'redeeming'|'completed'|'fallback'|'cancelled';
export interface PollTossState { phase: PollTossPhase; mode?: 'send'|'receive'; reason?: string }
export type PollTossEvent =
  | { type:'CONSENT'; mode:'send'|'receive' } | { type:'AVAILABLE' } | { type:'PERMISSION_GRANTED' }
  | { type:'PEER_SELECTED' } | { type:'VERIFIED' } | { type:'PAYLOAD_RECEIVED' } | { type:'REDEEMED' }
  | { type:'UNAVAILABLE'|'DENIED'|'RADIO_ERROR'|'TIMEOUT'|'FAILED'; reason?:string } | { type:'CANCEL' };

export const initialPollTossState: PollTossState = { phase:'idle' };
export function reducePollToss(state: PollTossState, event: PollTossEvent): PollTossState {
  if (event.type === 'CANCEL') return { phase:'cancelled', mode:state.mode };
  if (['UNAVAILABLE','DENIED','RADIO_ERROR','TIMEOUT','FAILED'].includes(event.type)) return { phase:'fallback', mode:state.mode, reason:'reason' in event ? event.reason : undefined };
  switch (`${state.phase}:${event.type}`) {
    case 'idle:CONSENT': return { phase:'checking', mode:(event as {mode:'send'|'receive'}).mode };
    case 'checking:AVAILABLE': return { ...state, phase:'permission' };
    case 'permission:PERMISSION_GRANTED': return { ...state, phase:state.mode==='send'?'advertising':'discovering' };
    case 'advertising:PEER_SELECTED': case 'discovering:PEER_SELECTED': return { ...state, phase:'verifying' };
    case 'verifying:VERIFIED': return { ...state, phase:'connected' };
    case 'connected:PAYLOAD_RECEIVED': return { ...state, phase:'redeeming' };
    case 'connected:REDEEMED': case 'redeeming:REDEEMED': return { ...state, phase:'completed' };
    default: return state;
  }
}
