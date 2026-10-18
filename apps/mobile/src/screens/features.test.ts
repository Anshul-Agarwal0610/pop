import React from 'react';
import { FlatList, Pressable, Text } from 'react-native';
import { act, create, type ReactTestRenderer } from 'react-test-renderer';
import { afterEach, beforeEach, describe, expect, jest, test } from '@jest/globals';
import { Action, Field } from '../components/ScreenKit';
import { CreatePollScreen, NotificationsScreen, ChallengesScreen, AchievementsScreen, HistoryScreen, MyPollsScreen, PreferencesScreen } from './ActivityScreens';
import { CreativeStylePicker } from '../components/CreativePollHero';
import { GamesScreen, RankingsScreen, WellnessScreen } from './PlayScreens';
import { CommunityScreen } from './CommunityScreen';
import { PollExperience } from './PollExperience';
import { featureApi as api } from '../lib/featuresApi';

jest.mock('react-native', () => ({
  Image: 'Image', View: 'View', Text: 'Text', TextInput: 'TextInput', Pressable: 'Pressable', ScrollView: 'ScrollView', ActivityIndicator: 'ActivityIndicator', RefreshControl: 'RefreshControl', Switch: 'Switch', KeyboardAvoidingView: 'KeyboardAvoidingView',
  Platform: { OS: 'android' }, StyleSheet: { create: (value: unknown) => value, hairlineWidth: 1 },
  Alert: { alert: jest.fn() }, AppState: { addEventListener: () => ({ remove: jest.fn() }) },
  BackHandler: { addEventListener: () => ({ remove: jest.fn() }) },
  FlatList: 'FlatList', Share: { share: jest.fn() },
}));
jest.mock('expo', () => ({ isRunningInExpoGo: () => true }));
jest.mock('expo-secure-store', () => ({ getItemAsync: async () => null, setItemAsync: async () => undefined }));
jest.mock('../lib/analytics/client', () => ({ track: jest.fn() }));
jest.mock('@expo/vector-icons/Ionicons', () => 'Icon');
jest.mock('../components/PollShareProvider', () => ({ usePollShare: () => jest.fn() }));
jest.mock('../context/AuthContext', () => ({ useAuth: () => ({ user: { id: 1, displayName: 'Tester', username: 'tester', xp: 10, streak: 1, progression: { level: 1, progressPercent: 1, nextLevelXp: 1000 } }, refreshUser: async () => undefined }) }));
jest.mock('../lib/analytics/privacy', () => ({ setAnalyticsConsent: jest.fn() }));
jest.mock('../lib/api', () => ({
  usersApi: { getCategoryPreferences: async () => [], getAnalyticsPrivacy: async () => ({ consent: 'unknown' }), updateCategoryPreferences: jest.fn(), updateAnalyticsPrivacy: jest.fn() },
}));
jest.mock('../lib/featuresApi', () => ({
  categories: ['General', 'Technology'],
  featureApi: {
    personalized: jest.fn(), browse: jest.fn(),
    create: jest.fn(), myPolls: jest.fn(), read: jest.fn(), readAll: jest.fn(), poll: jest.fn(),
    notifications: jest.fn(), notificationPreferences: async () => [],
    challenges: async () => [], achievements: async () => ({ achievements: [], earnedCount: 0, totalCount: 0 }), history: async () => [],
    modes: async () => [], activeGame: async () => null,
    rankings: async () => ({ rows: [], currentUser: null, hasMore: false }),
    wellness: async () => ({ polls: [], history: [], insight: { totalCheckIns: 0 } }),
    friends: async () => ({ items: [] }), groups: async () => ({ items: [] }),
  },
}));

let tree: ReactTestRenderer | undefined;
beforeEach(() => {
  (globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true;
  jest.clearAllMocks();
  jest.mocked(api.notifications).mockResolvedValue([]);
  jest.mocked(api.personalized).mockResolvedValue([]);
  jest.mocked(api.browse).mockResolvedValue([]);
  jest.mocked(api.myPolls).mockResolvedValue([]);
});
afterEach(async () => { await act(async () => tree?.unmount()); tree = undefined; jest.useRealTimers(); });
async function mount(element: React.ReactElement) { await act(async () => { tree = create(element); }); return tree!; }
const back = () => undefined;
const openPoll = () => undefined;

describe('consumer mobile screens', () => {
  test('Home and Explore retain closed polls and polls with older answer labels', async () => {
    jest.useFakeTimers();
    const polls = [
      { id: 1, question: 'An older poll', category: 'General', isActive: false, options: [{ id: 10, text: 'Yes' }, { id: 11, text: 'No' }] },
      { id: 2, question: 'Choose a drink', category: 'General', isActive: true, options: [{ id: 20, text: 'Tea' }, { id: 21, text: 'Coffee' }] },
    ] as Awaited<ReturnType<typeof api.personalized>>;
    jest.mocked(api.personalized).mockResolvedValue(polls);
    jest.mocked(api.browse).mockResolvedValue(polls);
    const rendered = await mount(React.createElement(PollExperience));
    await act(async () => { await jest.advanceTimersByTimeAsync(300); });
    expect(rendered.root.findByType(FlatList).props.data).toEqual(polls);
    const explore = rendered.root.findAll(node => node.type === Pressable && node.props.accessibilityRole === 'tab').find(node => node.findAll(child => child.type === Text && child.children.includes('Explore')).length > 0)!;
    await act(async () => explore.props.onPress());
    await act(async () => { await jest.advanceTimersByTimeAsync(300); });
    expect(rendered.root.findByType(FlatList).props.data).toEqual(polls);
  });

  test('More navigation opens notifications and returns to the hub', async () => {
    const rendered = await mount(React.createElement(PollExperience));
    const more = rendered.root.findAll(node => node.type === Pressable && node.props.accessibilityRole === 'tab').find(node => node.findAll(child => child.type === Text && child.children.includes('More')).length > 0)!;
    await act(async () => more.props.onPress());
    expect(JSON.stringify(rendered.toJSON())).toContain('More ways to Pop.');
    const notifications = rendered.root.findAll(node => node.type === Pressable && node.props.accessibilityRole === 'button').find(node => node.findAll(child => child.type === Text && child.children.includes('Notifications')).length > 0)!;
    await act(async () => notifications.props.onPress());
    expect(rendered.root.findByType(NotificationsScreen)).toBeDefined();
    await act(async () => rendered.root.findByType(NotificationsScreen).props.back());
    expect(JSON.stringify(rendered.toJSON())).toContain('More ways to Pop.');
  });
  test.each([
    ['Create', () => React.createElement(CreatePollScreen, { back, openPoll })],
    ['Notifications', () => React.createElement(NotificationsScreen, { back, openPoll })],
    ['Challenges', () => React.createElement(ChallengesScreen, { back, browse: () => undefined })],
    ['Achievements', () => React.createElement(AchievementsScreen, { back })],
    ['History', () => React.createElement(HistoryScreen, { back, openPoll })],
    ['My polls', () => React.createElement(MyPollsScreen, { back, openPoll })],
    ['Preferences', () => React.createElement(PreferencesScreen, { back })],
    ['Games', () => React.createElement(GamesScreen, { back })],
    ['Rankings', () => React.createElement(RankingsScreen, { back })],
    ['Wellness', () => React.createElement(WellnessScreen, { back })],
    ['Community', () => React.createElement(CommunityScreen, { back })],
  ] as Array<[string, () => React.ReactElement]>)('%s renders its empty state without crashing', async (_name, screen) => {
    const rendered = await mount(screen()); expect(rendered.toJSON()).not.toBeNull();
  });
  test('creation requires review before publishing and sends the configured poll', async () => {
    const created = { id: 91, question: 'Should work be flexible?' } as Awaited<ReturnType<typeof api.create>>;
    jest.mocked(api.create).mockResolvedValue(created);
    const rendered = await mount(React.createElement(CreatePollScreen, { back, openPoll }));
    expect(rendered.root.findAllByType(Action).find(button => button.props.title === 'Review poll')?.props.disabled).toBe(true);
    await act(async () => rendered.root.findAllByType(Field).find(field => field.props.label === 'Your question')!.props.onChangeText('Should work be flexible?'));
    await act(async () => rendered.root.findByType(CreativeStylePicker).props.onTemplate('pulse'));
    await act(async () => rendered.root.findByType(CreativeStylePicker).props.onReaction('thinking'));
    await act(async () => rendered.root.findAllByType(Action).find(button => button.props.title === 'Review poll')!.props.onPress());
    expect(api.create).not.toHaveBeenCalled();
    await act(async () => rendered.root.findAllByType(Action).find(button => button.props.title === 'Publish poll')!.props.onPress());
    expect(api.create).toHaveBeenCalledWith(expect.objectContaining({ question: created.question, options: ['Support', 'Against'], category: 'General', creativeTemplate: 'pulse', reactionId: 'thinking', isAIGenerated: false }));
    expect(JSON.stringify(rendered.toJSON())).toContain('Your poll is live and ready for votes');
  });
  test('my polls shows creator totals and opens performance details', async () => {
    const open = jest.fn();
    const poll = { id: 91, question: 'Should work be flexible?', category: 'General', totalVotes: 12, isActive: true, moderationStatus: 'Published', expiresAt: '2099-01-01', options: [{ id: 1, pollId: 91, text: 'Support', voteCount: 9 }, { id: 2, pollId: 91, text: 'Against', voteCount: 3 }] } as Awaited<ReturnType<typeof api.myPolls>>[number];
    jest.mocked(api.myPolls).mockResolvedValue([poll]);
    const rendered = await mount(React.createElement(MyPollsScreen, { back, openPoll: open }));
    expect(rendered.root.findAllByType(Text).some(node => node.children.join('') === '12 total votes')).toBe(true);
    const card = rendered.root.findAll(node => node.type === Pressable && node.props.accessibilityRole === 'button').find(node => node.findAll(child => child.type === Text && child.children.includes(poll.question)).length > 0)!;
    await act(async () => card.props.onPress());
    expect(open).toHaveBeenCalledWith(poll);
  });
  test('notification read-all waits for the backend before refreshing', async () => {
    jest.mocked(api.notifications).mockResolvedValueOnce([{ id: 7, title: 'Milestone', body: 'Well done', type: 'LevelUp', isRead: false, createdAt: '2026-09-26', pollId: null }] as Awaited<ReturnType<typeof api.notifications>>).mockResolvedValue([]);
    jest.mocked(api.readAll).mockResolvedValue(undefined);
    const rendered = await mount(React.createElement(NotificationsScreen, { back, openPoll }));
    await act(async () => rendered.root.findAllByType(Action).find(button => button.props.title === 'Mark all as read')!.props.onPress());
    expect(api.readAll).toHaveBeenCalledTimes(1);
    expect(api.notifications).toHaveBeenCalledTimes(2);
  });
  test('a failed read action leaves notifications unread and exposes retryable error', async () => {
    jest.mocked(api.notifications).mockResolvedValue([{ id: 7, title: 'Milestone', body: 'Well done', type: 'LevelUp', isRead: false, createdAt: '2026-09-26', pollId: null }] as Awaited<ReturnType<typeof api.notifications>>);
    jest.mocked(api.readAll).mockRejectedValue(new Error('Connection lost. Try again.'));
    const rendered = await mount(React.createElement(NotificationsScreen, { back, openPoll }));
    await act(async () => rendered.root.findAllByType(Action).find(button => button.props.title === 'Mark all as read')!.props.onPress());
    expect(rendered.root.findAllByType(Text).some(node => node.children.includes('Connection lost. Try again.'))).toBe(true);
    expect(api.notifications).toHaveBeenCalledTimes(1);
    expect(rendered.root.findAllByType(Action).find(button => button.props.title === 'Mark all as read')).toBeDefined();
  });
});
