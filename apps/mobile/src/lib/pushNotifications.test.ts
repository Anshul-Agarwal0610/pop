import { expect, jest, test } from '@jest/globals';

const mockIsRunningInExpoGo = jest.fn();
const mockLoadNotifications = jest.fn();
const mockNotifications = {
  setNotificationHandler: jest.fn(),
  setNotificationChannelAsync: jest.fn(),
  AndroidImportance: { DEFAULT: 3 },
  getPermissionsAsync: jest.fn(async () => ({ granted: true, status: 'granted' })),
  requestPermissionsAsync: jest.fn(),
  getExpoPushTokenAsync: jest.fn(async () => ({ data: 'ExponentPushToken[test]' })),
};

jest.mock('expo', () => ({ isRunningInExpoGo: mockIsRunningInExpoGo }));
jest.mock('react-native', () => ({ Platform: { OS: 'android' } }));
jest.mock('expo-notifications', () => {
  mockLoadNotifications();
  return mockNotifications;
});

import { getExpoPushToken } from './pushNotifications';

test('Expo Go skips loading the native notification module entirely', async () => {
  mockIsRunningInExpoGo.mockReturnValue(true);
  expect(mockLoadNotifications).not.toHaveBeenCalled();
  await expect(getExpoPushToken()).resolves.toBeNull();
  expect(mockLoadNotifications).not.toHaveBeenCalled();
});

test('development builds retain Android push registration', async () => {
  mockIsRunningInExpoGo.mockReturnValue(false);
  await expect(getExpoPushToken()).resolves.toBe('ExponentPushToken[test]');
  expect(mockNotifications.setNotificationHandler).toHaveBeenCalled();
  expect(mockNotifications.setNotificationChannelAsync).toHaveBeenCalled();
  expect(mockNotifications.getExpoPushTokenAsync).toHaveBeenCalled();
});
