import { apiRequest } from './api';
import type { ApiPoll } from '../types/poll';
import type * as T from '../types/features';

const send = (method: string, body?: unknown): RequestInit => ({ method, ...(body === undefined ? {} : { body: JSON.stringify(body) }) });
export const categories = ['General', 'Technology', 'Society', 'Work', 'Environment', 'Culture', 'Sports', 'Health', 'Politics'];
export const featureApi = {
  browse: (query = '', category = '') => apiRequest<ApiPoll[]>(query.trim() ? `/api/polls/search?q=${encodeURIComponent(query.trim())}&category=${encodeURIComponent(category)}` : `/api/polls?category=${encodeURIComponent(category)}`),
  personalized: () => apiRequest<ApiPoll[]>('/api/polls/personalized?count=50'),
  poll: (id: number) => apiRequest<ApiPoll>(`/api/polls/${id}`),
  myPolls: (count = 100) => apiRequest<ApiPoll[]>(`/api/polls/mine?count=${count}`),
  create: (payload: T.CreatePollPayload) => apiRequest<ApiPoll>('/api/polls', send('POST', payload)),
  uploadPollMedia: (file: { uri: string; name: string; type: string }) => {
    const body = new FormData();
    body.append('file', file as unknown as Blob);
    return apiRequest<{ url: string; mediaType: 'image' | 'video' }>('/api/poll-media', { method: 'POST', body });
  },
  report: (id: number, reason: string) => apiRequest<void>(`/api/polls/${id}/report`, send('POST', { reason })),
  notifications: () => apiRequest<T.ApiNotification[]>('/api/notifications'),
  read: (id: number) => apiRequest<void>(`/api/notifications/${id}/read`, send('PATCH')),
  readAll: () => apiRequest<void>('/api/notifications/read-all', send('POST')),
  notificationPreferences: () => apiRequest<T.ApiNotificationPreference[]>('/api/notifications/preferences'),
  saveNotificationPreferences: (disabledTypes: string[]) => apiRequest<T.ApiNotificationPreference[]>('/api/notifications/preferences', send('PUT', { disabledTypes })),
  challenges: (state: string) => apiRequest<T.ApiChallenge[]>(`/api/challenges?state=${state}`),
  achievements: () => apiRequest<T.ApiAchievementCollection>('/api/achievements/me'),
  title: (badgeId: number | null) => apiRequest<void>('/api/achievements/me/title', send(badgeId === null ? 'DELETE' : 'PUT', badgeId === null ? undefined : { badgeId })),
  history: (count = 30) => apiRequest<T.ApiVoteHistoryItem[]>(`/api/users/me/votes?count=${count}`),
  streak: () => apiRequest<T.ApiStreakStatus>('/api/users/me/streak'),
  resetCategories: () => apiRequest<void>('/api/users/me/preferences/categories', send('DELETE')),
  rankings: (period: 'weekly' | 'allTime', offset = 0) => apiRequest<T.ApiLeaderboardResponse>(`/api/users/leaderboard/rankings?period=${period}&limit=20&offset=${offset}`),
  wellness: () => apiRequest<T.ApiWellnessOverview>('/api/wellness/overview'),
  checkIn: (pollId: number, optionId: number, note: string) => apiRequest<T.ApiWellnessResponse>('/api/wellness/responses', send('POST', { pollId, optionId, note })),
  deleteWellness: () => apiRequest<void>('/api/wellness/responses', send('DELETE')),
  modes: () => apiRequest<T.ApiGameMode[]>('/api/game-modes'),
  activeGame: async () => (await apiRequest<T.ApiGameSession | undefined>('/api/game-sessions/active')) ?? null,
  startGame: (category: string, timed: boolean) => apiRequest<T.ApiGameSession>('/api/game-sessions', send('POST', { mode: 'OpinionSprint', category, timed })),
  game: (id: number) => apiRequest<T.ApiGameSession>(`/api/game-sessions/${id}`),
  gameVote: (id: number, position: number, pollId: number, optionId: number) => apiRequest<T.ApiGameVoteResult>(`/api/game-sessions/${id}/votes`, send('POST', { position, pollId, optionId })),
  completeGame: (id: number) => apiRequest<T.ApiGameSession>(`/api/game-sessions/${id}/complete`, send('POST')),
  predictionOpportunities: (type: 'Winner' | 'Split' = 'Winner') => apiRequest<T.ApiPredictionOpportunity[]>(`/api/predictions/available?type=${type}&count=12`),
  predictions: () => apiRequest<T.ApiPredictionEntry[]>('/api/predictions?count=50'),
  predict: (pollId: number, predictedOptionId: number, predictionType: 'Winner' | 'Split' = 'Winner', predictedPercentage?: number) => apiRequest<T.ApiPredictionEntry>('/api/predictions', send('POST', { pollId, predictedOptionId, predictionType, predictedPercentage })),
  castVote: (pollId: number, optionId: number) => apiRequest<unknown>('/api/votes', send('POST', { pollId, optionId })),
  friends: () => apiRequest<T.Paged<T.FriendConnection>>('/api/social/friends'),
  searchUsers: (query: string, cursor = '') => apiRequest<T.Paged<T.SocialUser>>(`/api/social/users?query=${encodeURIComponent(query)}&cursor=${encodeURIComponent(cursor)}`),
  friendRequest: (targetUserId: number) => apiRequest<void>('/api/social/friends/requests', send('POST', { targetUserId })),
  acceptFriend: (id: number) => apiRequest<void>(`/api/social/friends/requests/${id}/accept`, send('POST')),
  declineFriend: (id: number) => apiRequest<void>(`/api/social/friends/requests/${id}/decline`, send('POST')),
  removeFriend: (id: number) => apiRequest<void>(`/api/social/friends/${id}`, send('DELETE')),
  block: (targetUserId: number) => apiRequest<void>('/api/social/blocks', send('POST', { targetUserId })),
  friendsBoard: () => apiRequest<T.WeeklyLeaderboard>('/api/social/leaderboards/friends'),
  groups: () => apiRequest<T.Paged<T.SocialGroup>>('/api/social/groups'),
  createGroup: (name: string) => apiRequest<T.SocialGroup>('/api/social/groups', send('POST', { name })),
  invite: (id: number, targetUserId: number) => apiRequest<{ token: string }>(`/api/social/groups/${id}/invites`, send('POST', { targetUserId })),
  acceptInvite: (token: string) => apiRequest<void>(`/api/social/group-invites/${encodeURIComponent(token)}/accept`, send('POST')),
  leaveGroup: (id: number) => apiRequest<void>(`/api/social/groups/${id}/membership`, send('DELETE')),
  groupBoard: (id: number) => apiRequest<T.WeeklyLeaderboard>(`/api/social/groups/${id}/leaderboard`),
};
