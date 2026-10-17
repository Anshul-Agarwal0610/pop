import { useState } from 'react';
import { Image, View } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';
import { useTheme } from '../theme';

type AvatarUser = { username: string; displayName?: string; avatarUrl?: string | null };
export function avatarSources(user: AvatarUser) {
  const fallback = `https://api.dicebear.com/9.x/notionists/png?seed=${encodeURIComponent(user.username || 'guest')}&size=128`;
  return user.avatarUrl?.trim() ? [user.avatarUrl.trim(), fallback] : [fallback];
}

export function UserAvatar({ user, size = 40 }: { user: AvatarUser; size?: number }) {
  const { colors } = useTheme();
  const sources = avatarSources(user);
  const identity = sources.join('|');
  const [failure, setFailure] = useState({ identity, index: 0 });
  const index = failure.identity === identity ? failure.index : 0;
  const uri = sources[index];
  return <View accessibilityRole="image" accessibilityLabel={`${user.displayName || user.username}’s avatar`} style={{ width: size, height: size, borderRadius: size / 2, borderWidth: 2, borderColor: `${colors.primary}40`, backgroundColor: colors.secondary, overflow: 'hidden', alignItems: 'center', justifyContent: 'center' }}>
    <Ionicons name="person" color={colors.mutedForeground} size={size * 0.55} />
    {uri && <Image key={uri} source={{ uri }} resizeMode="cover" accessibilityElementsHidden importantForAccessibility="no-hide-descendants" onError={() => setFailure({ identity, index: index + 1 })} style={{ position: 'absolute', width: '100%', height: '100%', backgroundColor: '#FFFFFF' }} />}
  </View>;
}
