import React from 'react';
import { jest, test, expect, beforeEach, afterEach } from '@jest/globals';
import { act, create, type ReactTestRenderer } from 'react-test-renderer';
import { Image } from 'react-native';
import * as SecureStore from 'expo-secure-store';
import { ThemeProvider, useTheme, resolveTheme } from './ThemeProvider';
import { UserAvatar } from '../components/UserAvatar';

jest.mock('react-native', () => ({ useColorScheme: () => 'dark', Image: 'Image', View: 'View' }));
jest.mock('@expo/vector-icons/Ionicons', () => 'Icon');
jest.mock('expo-secure-store', () => ({ getItemAsync: jest.fn(), setItemAsync: jest.fn(async () => undefined) }));
let tree: ReactTestRenderer;
let theme: ReturnType<typeof useTheme>;
function Probe() { theme = useTheme(); return null; }
beforeEach(() => {
  (globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true;
  jest.clearAllMocks();
  jest.mocked(SecureStore.getItemAsync).mockResolvedValue(null);
});
afterEach(async () => { if (tree) await act(async () => tree.unmount()); });
test('follows system and honors an explicit appearance', () => {
  expect(resolveTheme('system', 'dark')).toBe('dark');
  expect(resolveTheme('system', null)).toBe('light');
  expect(resolveTheme('light', 'dark')).toBe('light');
});
test('restores the saved choice and applies and saves a new choice', async () => {
  jest.mocked(SecureStore.getItemAsync).mockResolvedValue('light');
  await act(async () => { tree = create(React.createElement(ThemeProvider, null, React.createElement(Probe))); });
  expect(theme.scheme).toBe('light');
  await act(async () => { await theme.setPreference('dark'); });
  expect(theme.scheme).toBe('dark');
  expect(SecureStore.setItemAsync).toHaveBeenLastCalledWith('pop_theme_preference', 'dark');
});
test('a delayed saved preference cannot override a fresh selection', async () => {
  let restore!: (value: string) => void;
  jest.mocked(SecureStore.getItemAsync).mockReturnValue(new Promise(resolve => { restore = resolve; }));
  await act(async () => { tree = create(React.createElement(ThemeProvider, null, React.createElement(Probe))); });
  await act(async () => { await theme.setPreference('light'); restore('dark'); });
  expect(theme.scheme).toBe('light');
});
test('avatar tries the profile photo, illustration, then icon, and resets for another user', async () => {
  await act(async () => { tree = create(React.createElement(UserAvatar, { user: { username: 'a b', avatarUrl: 'https://example.com/avatar.png' } })); });
  expect(tree.root.findByType(Image).props.source.uri).toBe('https://example.com/avatar.png');
  await act(async () => tree.root.findByType(Image).props.onError());
  expect(tree.root.findByType(Image).props.source.uri).toContain('seed=a%20b');
  await act(async () => tree.root.findByType(Image).props.onError());
  expect(tree.root.findAllByType(Image)).toHaveLength(0);
  await act(async () => tree.update(React.createElement(UserAvatar, { user: { username: 'new-user' } })));
  expect(tree.root.findByType(Image).props.source.uri).toContain('seed=new-user');
});
