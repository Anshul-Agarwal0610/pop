import * as Notifications from 'expo-notifications';
import { Platform } from 'react-native';

Notifications.setNotificationHandler({
  handleNotification: async () => ({
    shouldPlaySound: true,
    shouldSetBadge: false,
    shouldShowBanner: true,
    shouldShowList: true,
  }),
});

export async function getExpoPushToken() {
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
