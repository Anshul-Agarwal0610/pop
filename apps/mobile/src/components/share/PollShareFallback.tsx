import React from 'react';
import { Share, Text, TouchableOpacity, View } from 'react-native';

export function PollShareFallback({ shareUrl }: { shareUrl?: string }) {
  return <View accessibilityLabel="QR or link sharing fallback">
    <Text>Nearby isn’t available. Share this invitation link or show its QR code instead.</Text>
    {shareUrl ? <>
      <View accessibilityLabel={`QR value ${shareUrl}`}><Text selectable>{shareUrl}</Text></View>
      <TouchableOpacity accessibilityRole="button" onPress={()=>Share.share({message:shareUrl})}><Text>Share link</Text></TouchableOpacity>
    </> : <Text>Create a new invitation to share.</Text>}
  </View>;
}
