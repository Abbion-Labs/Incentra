import { notifySessionExpired } from '../auth/session';

const TOKEN_KEY = 'vc_access_token';
const REFRESH_TOKEN_KEY = 'vc_refresh_token';
const LOCALE_KEY = 'vn-locale';
const UNEXPECTED_ERROR_CODE = 'vn-0089';
const TRACE_ID_DISPLAY_LENGTH = 8;

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function getRefreshToken(): string | null {
  return localStorage.getItem(REFRESH_TOKEN_KEY);
}

export function setToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token);
}

export function setRefreshToken(token: string): void {
  localStorage.setItem(REFRESH_TOKEN_KEY, token);
}

export function setAuthTokens(accessToken: string, refreshToken: string): void {
  setToken(accessToken);
  setRefreshToken(refreshToken);
}

export function clearToken(): void {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(REFRESH_TOKEN_KEY);
}

export function revokeRefreshToken(): void {
  const refreshToken = getRefreshToken();
  if (!refreshToken) {
    return;
  }

  void fetch('/api/auth/logout', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken }),
    keepalive: true,
  }).catch(() => undefined);
}

export async function ensureValidSession(): Promise<boolean> {
  if (getToken()) {
    return true;
  }

  return tryRefreshToken();
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

interface AuthResponse {
  accessToken: string;
  refreshToken: string;
}

let refreshPromise: Promise<boolean> | null = null;

async function tryRefreshToken(): Promise<boolean> {
  const refreshToken = getRefreshToken();
  if (!refreshToken) {
    return false;
  }

  if (!refreshPromise) {
    refreshPromise = (async () => {
      try {
        const response = await fetch('/api/auth/refresh', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ refreshToken }),
        });

        if (!response.ok) {
          return false;
        }

        const data = (await response.json()) as AuthResponse;
        setAuthTokens(data.accessToken, data.refreshToken);
        return true;
      } catch {
        return false;
      } finally {
        refreshPromise = null;
      }
    })();
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
    const refreshed = await tryRefreshToken();
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
