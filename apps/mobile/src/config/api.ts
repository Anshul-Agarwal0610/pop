declare const process: {
  env?: Record<string, string | undefined>;
};

const fallbackApiUrl = 'http://localhost:5177';

function trimTrailingSlash(value: string) {
  return value.replace(/\/+$/, '');
}

export const API_BASE_URL = trimTrailingSlash(
  process.env?.EXPO_PUBLIC_API_URL ?? fallbackApiUrl,
);
