import { Alert, Share } from 'react-native';
import { afterEach, expect, jest, test } from '@jest/globals';
import { pollShareMessage, pollShareUrl, sharePoll, sharePollLink } from './sharePoll';

jest.mock('react-native', () => ({ Alert: { alert: jest.fn() }, Share: { share: jest.fn(async () => ({ action: 'sharedAction' })) } }));

afterEach(() => {
  delete process.env.EXPO_PUBLIC_WEB_URL;
  jest.clearAllMocks();
});

test('created polls include a direct web link when the web origin is configured', async () => {
  process.env.EXPO_PUBLIC_WEB_URL = 'https://pop.example/';
  await sharePollLink({ id: 42, question: 'Should cities add more parks?' });
  expect(Share.share).toHaveBeenCalledWith(expect.objectContaining({
    message: expect.stringContaining('https://pop.example/polls/42'),
  }));
});

test('share presents link and QR alternatives', () => {
  sharePoll({ id: 42, question: 'Should cities add more parks?' });
  const buttons = jest.mocked(Alert.alert).mock.calls[0][2]!;
  expect(buttons.map(button => button.text)).toEqual(['Share link', 'Share QR code', 'Cancel']);
});

test('sharing falls back to the deployed public web origin', () => {
  expect(pollShareMessage({ id: 42, question: 'A question?' })).toContain('A question?');
  expect(pollShareMessage({ id: 42, question: 'A question?' })).not.toContain('undefined');
  expect(pollShareUrl(42)).toBe('https://mvp-react-frontend.azurewebsites.net/polls/42');
});
