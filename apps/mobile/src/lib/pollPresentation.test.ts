import { describe, expect, test } from '@jest/globals';
import { isPollClosed, nextUnvotedPoll, pollDeadline, resultPercent } from './pollPresentation';
import type { ApiPoll } from '../types/poll';

const now = Date.parse('2026-09-26T12:00:00Z');
const poll = (id: number, overrides: Partial<ApiPoll> = {}): ApiPoll => ({
  id, question: 'A question?', description: '', category: 'Technology', isTrending: true,
  isActive: true, expiresAt: '2099-01-01T00:00:00Z', createdAt: '2026-01-01T00:00:00Z',
  totalVotes: 0, createdByUserId: null, createdByUsername: null, createdByDisplayName: null,
  sourceType: null, sourceUrl: null, thumbnailUrl: null, isAIGenerated: false,
  options: [{ id: 10, pollId: id, text: 'Up', voteCount: 0, votePercentage: 0 }],
  hasVoted: false, userVotedOptionId: null, ...overrides,
});

describe('mobile voting presentation', () => {
  test('expired and inactive polls cannot accept votes', () => {
    expect(isPollClosed(poll(1, { expiresAt: new Date(now).toISOString() }), now)).toBe(true);
    expect(isPollClosed(poll(1, { isActive: false }), now)).toBe(true);
    expect(isPollClosed(poll(1), now)).toBe(false);
  });
  test('deadlines handle the final minute and missing dates', () => {
    expect(pollDeadline(poll(1, { expiresAt: new Date(now + 1000).toISOString() }), now)).toBe('1m left');
    expect(pollDeadline(poll(1, { expiresAt: '' }), now)).toBe('Open');
    expect(pollDeadline(poll(1, { isActive: false }), now)).toBe('Closed');
  });
  test('results use real counts and handle zero votes', () => {
    expect(resultPercent(849, 1248)).toBe(68);
    expect(resultPercent(399, 1248)).toBe(32);
    expect(resultPercent(0, 0)).toBe(0);
    expect(resultPercent(2, 1)).toBe(100);
  });
  test('next poll wraps and skips voted, closed, and unusable polls', () => {
    const polls = [poll(1), poll(2, { hasVoted: true }), poll(3, { isActive: false }), poll(4, { options: [] }), poll(5)];
    expect(nextUnvotedPoll(polls, 1)?.id).toBe(5);
    expect(nextUnvotedPoll(polls, 5)?.id).toBe(1);
    expect(nextUnvotedPoll([poll(1)], 1)).toBeUndefined();
  });
});
