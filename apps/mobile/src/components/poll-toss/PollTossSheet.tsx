import React from 'react';
import { Modal, Text, TouchableOpacity, View } from 'react-native';
import type { PollTossState } from '../../features/poll-toss/stateMachine';
import { PollShareFallback } from '../share/PollShareFallback';

export function PollTossSheet({ visible,state,secondsRemaining,verificationCode,shareUrl,onConsent,onConfirm,onCancel }:{
  visible:boolean; state:PollTossState; secondsRemaining:number; verificationCode?:string; shareUrl?:string;
  onConsent:(mode:'send'|'receive')=>void; onConfirm?:(accepted:boolean)=>void; onCancel:()=>void;
}) {
  return <Modal visible={visible} transparent animationType="slide" onRequestClose={onCancel}>
    <View accessibilityViewIsModal><Text>Poll Toss</Text><Text>Nearby visibility is temporary ({secondsRemaining}s remaining).</Text>
      {state.phase==='idle'&&<><TouchableOpacity onPress={()=>onConsent('send')}><Text>Toss nearby</Text></TouchableOpacity><TouchableOpacity onPress={()=>onConsent('receive')}><Text>Find nearby poll</Text></TouchableOpacity></>}
      {state.phase==='verifying'&&<><Text>Confirm this code on both phones</Text><Text selectable>{verificationCode}</Text><TouchableOpacity onPress={()=>onConfirm?.(true)}><Text>Codes match</Text></TouchableOpacity><TouchableOpacity onPress={()=>onConfirm?.(false)}><Text>Reject</Text></TouchableOpacity></>}
      {state.phase==='fallback'&&<PollShareFallback shareUrl={shareUrl}/>} 
      <TouchableOpacity onPress={onCancel}><Text>Cancel and stop nearby</Text></TouchableOpacity>
    </View>
  </Modal>;
}
