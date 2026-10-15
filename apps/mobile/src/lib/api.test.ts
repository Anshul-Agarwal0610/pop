import { afterEach, beforeEach, expect, jest, test } from '@jest/globals';
import { apiRequest, authApi, usersApi } from './api';
import { featureApi } from './featuresApi';

jest.mock('./session', () => ({ getToken: async () => 'test-session' }));
jest.mock('../config/api', () => ({ API_BASE_URL: 'https://example.invalid' }));
const mockFetch = jest.fn<typeof fetch>();
beforeEach(() => { jest.spyOn(globalThis, 'fetch').mockImplementation(mockFetch); mockFetch.mockReset(); });
afterEach(() => { jest.restoreAllMocks(); });

test('marking notifications read accepts 204 without parsing JSON', async () => {
  mockFetch.mockResolvedValue(new Response(null, { status: 204 }));
  await expect(featureApi.read(7)).resolves.toBeUndefined();
  expect(mockFetch).toHaveBeenCalledWith('https://example.invalid/api/notifications/7/read', expect.objectContaining({ method: 'PATCH', headers: expect.objectContaining({ Authorization: 'Bearer test-session' }) }));
});
test('active game maps no-content to no active session', async () => {
  mockFetch.mockResolvedValue(new Response(null, { status: 204 }));
  await expect(featureApi.activeGame()).resolves.toBeNull();
});
test('empty successful bodies work for delete and update actions', async () => {
  mockFetch.mockResolvedValue(new Response('', { status: 200 }));
  await expect(featureApi.title(null)).resolves.toBeUndefined();
});
test('failed writes surface validation messages without claiming success', async () => {
  mockFetch.mockResolvedValue(new Response(JSON.stringify({ errors: { Question: ['Question is required.'] } }), { status: 400 }));
  await expect(apiRequest('/api/polls', { method: 'POST' })).rejects.toThrow('Question is required.');
});
test('failed requests retain the HTTP status for recovery flows', async () => {
  mockFetch.mockResolvedValue(new Response(JSON.stringify({ message: 'You have already voted on this poll.' }), { status: 409 }));
  await expect(apiRequest('/api/votes')).rejects.toEqual(expect.objectContaining({ status: 409 }));
});
test('search encodes query and category separately', async () => {
  mockFetch.mockResolvedValue(new Response('[]'));
  await featureApi.browse('AI & work?', 'Technology');
  expect(mockFetch.mock.calls[0][0]).toBe('https://example.invalid/api/polls/search?q=AI%20%26%20work%3F&category=Technology');
});
test('multipart uploads preserve the runtime boundary header', async () => {
  mockFetch.mockResolvedValue(new Response(JSON.stringify({ url: '/api/poll-media/photo.jpg', mediaType: 'image' })));
  const body = new FormData();
  body.append('file', new Blob(['photo'], { type: 'image/jpeg' }), 'photo.jpg');
  await apiRequest('/api/poll-media', { method: 'POST', body });
  const request = mockFetch.mock.calls[0][1]!;
  expect(request.headers).toEqual(expect.objectContaining({ Authorization: 'Bearer test-session' }));
  expect(request.headers).not.toHaveProperty('Content-Type');
});
test('Google tokens use the backend session exchange', async () => {
  mockFetch.mockResolvedValue(new Response(JSON.stringify({ token: 'jwt', user: {} })));
  await authApi.google('google-id-token', true);
  expect(mockFetch).toHaveBeenCalledWith('https://example.invalid/api/auth/google', expect.objectContaining({ method: 'POST', body: JSON.stringify({ idToken: 'google-id-token', acceptTerms: true }) }));
});
test('profile edits send display name and avatar together', async () => {
  mockFetch.mockResolvedValue(new Response(JSON.stringify({ id: 1, displayName: 'New name' })));
  await usersApi.updateProfile('New name', 'https://example.com/avatar.jpg');
  expect(mockFetch).toHaveBeenCalledWith('https://example.invalid/api/users/me/profile', expect.objectContaining({ method: 'PUT', body: JSON.stringify({ displayName: 'New name', avatarUrl: 'https://example.com/avatar.jpg' }) }));
});
test('account deletion calls the authenticated self-service endpoint', async () => {
  mockFetch.mockResolvedValue(new Response(null, { status: 204 }));
  await expect(usersApi.deleteAccount()).resolves.toBeUndefined();
  expect(mockFetch).toHaveBeenCalledWith('https://example.invalid/api/users/me', expect.objectContaining({
    method: 'DELETE',
    headers: expect.objectContaining({ Authorization: 'Bearer test-session' }),
  }));
});
