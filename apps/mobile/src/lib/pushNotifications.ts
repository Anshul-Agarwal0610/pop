import { isRunningInExpoGo } from 'expo';
import { Platform } from 'react-native';

export async function getExpoPushToken() {
  // Importing expo-notifications itself registers native push listeners.
  // Expo Go cannot load those listeners, even before requesting a token.
  if (isRunningInExpoGo()) return null;

  const Notifications = await import('expo-notifications');
  Notifications.setNotificationHandler({
    handleNotification: async () => ({
      shouldPlaySound: true,
      shouldSetBadge: false,
      shouldShowBanner: true,
      shouldShowList: true,
    }),
  });
  if (Platform.OS === 'android') {
    await Notifications.setNotificationChannelAsync('pollify-retention', {
      name: 'Pollify reminders',
      importance: Notifications.AndroidImportance.DEFAULT,
      vibrationPattern: [0, 250, 250, 250],
      lightColor: '#B0413E',
    });
  }

  const existing = await Notifications.getPermissionsAsync();
  const finalStatus = existing.granted
    ? existing.status
    : (await Notifications.requestPermissionsAsync()).status;

  if (finalStatus !== 'granted') return null;

  const token = await Notifications.getExpoPushTokenAsync();
  return token.data;
}
