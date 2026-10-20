import { API_BASE_URL } from '../config/api';
import { youtubeVideoId } from './youtube';

export function pollCover(poll: { thumbnailUrl?: string | null; mediaPosterUrl?: string | null; mediaType?: string | null; sourceUrl?: string | null }) {
  const youtubeId = youtubeVideoId(poll.sourceUrl);
  const source = poll.mediaType?.toLowerCase() === 'video'
    ? poll.mediaPosterUrl
    : poll.thumbnailUrl || (youtubeId ? `https://i.ytimg.com/vi/${youtubeId}/hqdefault.jpg` : null);
  if (!source) return null;
  return {
    uri: /^https?:\/\//i.test(source) ? source : `${API_BASE_URL}${source.startsWith('/') ? '' : '/'}${source}`,
    isVideo: poll.mediaType?.toLowerCase() === 'video' || !!youtubeId,
  };
}
