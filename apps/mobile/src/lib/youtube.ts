export function youtubeVideoId(value?: string | null) {
  if (!value) return null;
  try {
    const url = new URL(value);
    const host = url.hostname.toLowerCase().replace(/^www\./, '');
    if (host === 'youtu.be') return clean(url.pathname.split('/').filter(Boolean)[0]);
    if (host === 'youtube.com' || host.endsWith('.youtube.com')) {
      if (url.pathname === '/watch') return clean(url.searchParams.get('v'));
      const [kind, id] = url.pathname.split('/').filter(Boolean);
      if (['shorts', 'embed', 'live'].includes(kind)) return clean(id);
    }
  } catch { return null; }
  return null;
}

function clean(value?: string | null) {
  return value && /^[A-Za-z0-9_-]{6,20}$/.test(value) ? value : null;
}
