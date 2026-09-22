import { notifySessionExpired } from '../auth/session';
import type { AuthResponse, UserProfile } from './types';

const LEGACY_TOKEN_KEYS = ['vc_access_token', 'vc_refresh_token'];
const LOCALE_KEY = 'vn-locale';
const REFRESH_LOCK_NAME = 'vc-refresh-session';
const UNEXPECTED_ERROR_CODE = 'vn-0089';
const TRACE_ID_DISPLAY_LENGTH = 8;

// The access token is kept in memory only, and the refresh token is an HttpOnly cookie the browser
// sends on its own. Neither is readable by scripts running on the page.
let accessToken: string | null = null;

// Tokens stored by earlier versions of the app are useless now, and should not be left behind.
// Can be dropped once every active user has loaded the app at least once after this change.
try {
  LEGACY_TOKEN_KEYS.forEach((key) => localStorage.removeItem(key));
} catch {
  // storage unavailable
}

export function getToken(): string | null {
  return accessToken;
}

export function setAccessToken(token: string): void {
  accessToken = token;
}

export function clearToken(): void {
  accessToken = null;
}

export async function restoreSession(): Promise<UserProfile | null> {
  const session = await refreshSession();
  return session?.user ?? null;
}

export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
    public rawMessage: string = message,
    public code?: string,
    public args: Record<string, string> = {},
  ) {
    super(message);
  }
}

export function revokeRefreshToken(): void {
  void fetch('/api/auth/logout', { method: 'POST', keepalive: true }).catch(
    () => undefined,
  );
}

let refreshPromise: Promise<AuthResponse | null> | null = null;

async function requestNewSession(): Promise<AuthResponse | null> {
  try {
    const response = await fetch('/api/auth/refresh', { method: 'POST' });
    if (!response.ok) {
      return null;
    }

    const session = (await response.json()) as AuthResponse;
    setAccessToken(session.accessToken);
    return session;
  } catch {
    return null;
  }
}

// The refresh token rotates on every use, so two tabs must not refresh at the same moment. The lock
// makes them take turns; the second tab then uses the cookie the first one has just replaced.
async function runExclusively<T>(task: () => Promise<T>): Promise<T> {
  if (typeof navigator !== 'undefined' && navigator.locks) {
    return (await navigator.locks.request(REFRESH_LOCK_NAME, task)) as T;
  }

  return task();
}

function refreshSession(): Promise<AuthResponse | null> {
  if (!refreshPromise) {
    refreshPromise = runExclusively(requestNewSession).finally(() => {
      refreshPromise = null;
    });
  }

  return refreshPromise;
}

export async function apiFetch<T>(
  path: string,
  options: RequestInit = {},
  isRetry = false,
): Promise<T> {
  const headers = new Headers(options.headers);
  const isFormData =
    typeof FormData !== 'undefined' && options.body instanceof FormData;
  if (!isFormData && !headers.has('Content-Type') && options.body) {
    headers.set('Content-Type', 'application/json');
  }

  const token = getToken();
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const locale = localStorage.getItem(LOCALE_KEY);
  if (locale) {
    headers.set('Accept-Language', locale);
  }

  const response = await fetch(path, { ...options, headers });

  if (
    response.status === 401 &&
    !isRetry &&
    path !== '/api/auth/login' &&
    path !== '/api/auth/refresh'
  ) {
    const refreshed = await refreshSession();
    if (refreshed) {
      return apiFetch<T>(path, options, true);
    }
    clearToken();
    notifySessionExpired();
  }

  if (!response.ok) {
    let message = response.statusText;
    let rawMessage = response.statusText;
    let code: string | undefined;
    let args: Record<string, string> = {};
    try {
      const body = await response.json();
      rawMessage = body.error ?? body.title ?? message;
      if (rawMessage === UNEXPECTED_ERROR_CODE) {
        const traceId =
          typeof body.traceId === 'string'
            ? body.traceId.slice(0, TRACE_ID_DISPLAY_LENGTH)
            : '-';
        rawMessage = `${rawMessage}?traceId=${encodeURIComponent(traceId)}`;
      }
      const [rawCode, query] = rawMessage.split('?', 2);
      if (/^vn-\d{4}$/.test(rawCode)) {
        code = rawCode;
        const params = new URLSearchParams(query ?? '');
        args = Object.fromEntries(params.entries());
      }
      message = rawMessage;
    } catch {
      // ignore
    }
    throw new ApiError(message, response.status, rawMessage, code, args);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export const api = {
  get: <T>(path: string) => apiFetch<T>(path),
  post: <T>(path: string, body?: unknown) =>
    apiFetch<T>(path, {
      method: 'POST',
      body: body ? JSON.stringify(body) : undefined,
    }),
  postForm: <T>(path: string, formData: FormData) =>
    apiFetch<T>(path, { method: 'POST', body: formData }),
  put: <T>(path: string, body: unknown) =>
    apiFetch<T>(path, { method: 'PUT', body: JSON.stringify(body) }),
  delete: <T>(path: string) => apiFetch<T>(path, { method: 'DELETE' }),
};
