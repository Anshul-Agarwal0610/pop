import { expect, test } from '@jest/globals';
import { youtubeVideoId } from './youtube';

test.each([
  ['https://www.youtube.com/watch?v=dQw4w9WgXcQ', 'dQw4w9WgXcQ'],
  ['https://youtu.be/dQw4w9WgXcQ?t=4', 'dQw4w9WgXcQ'],
  ['https://youtube.com/shorts/dQw4w9WgXcQ', 'dQw4w9WgXcQ'],
  ['https://youtube.com/live/dQw4w9WgXcQ', 'dQw4w9WgXcQ'],
])('extracts a YouTube id from %s', (url, id) => expect(youtubeVideoId(url)).toBe(id));

test('rejects unrelated and malformed URLs', () => {
  expect(youtubeVideoId('https://example.com/watch?v=dQw4w9WgXcQ')).toBeNull();
  expect(youtubeVideoId('not a url')).toBeNull();
});
