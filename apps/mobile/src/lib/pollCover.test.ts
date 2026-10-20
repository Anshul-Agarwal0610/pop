import { describe, expect, test } from '@jest/globals';
import { pollCover } from './pollCover';

describe('poll cover selection', () => {
  test('photos use the uploaded image', () => {
    expect(pollCover({ thumbnailUrl: 'https://example.com/photo.jpg', mediaType: 'image' }))
      .toEqual({ uri: 'https://example.com/photo.jpg', isVideo: false });
  });
  test('videos use a poster rather than trying to render a video as an image', () => {
    expect(pollCover({ thumbnailUrl: 'https://example.com/video.mp4', mediaPosterUrl: 'https://example.com/poster.jpg', mediaType: 'video' }))
      .toEqual({ uri: 'https://example.com/poster.jpg', isVideo: true });
    expect(pollCover({ thumbnailUrl: 'https://example.com/old-video.mp4', mediaType: 'video' })).toBeNull();
  });
  test('YouTube polls use the source thumbnail and text polls have no cover', () => {
    expect(pollCover({ sourceUrl: 'https://www.youtube.com/watch?v=dQw4w9WgXcQ' }))
      .toEqual({ uri: 'https://i.ytimg.com/vi/dQw4w9WgXcQ/hqdefault.jpg', isVideo: true });
    expect(pollCover({})).toBeNull();
  });
});
