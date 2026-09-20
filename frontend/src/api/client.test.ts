import { beforeEach, describe, expect, it, vi } from 'vitest';

const user = { id: 1, email: 'a@b.c', roles: ['ADMIN'] };

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
}

// The module keeps the access token in a module-level variable, so each test needs a fresh import.
async function loadClient() {
  vi.resetModules();
  return import('./client');
}

describe('api client session handling', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.unstubAllGlobals();
  });

  it('drops tokens left in localStorage by earlier versions', async () => {
    localStorage.setItem('vc_access_token', 'old-access');
    localStorage.setItem('vc_refresh_token', 'old-refresh');
    localStorage.setItem('vn-locale', 'en');

    await loadClient();

    expect(localStorage.getItem('vc_access_token')).toBeNull();
    expect(localStorage.getItem('vc_refresh_token')).toBeNull();
    expect(localStorage.getItem('vn-locale')).toBe('en');
  });

  it('keeps the access token out of localStorage', async () => {
    const client = await loadClient();

    client.setAccessToken('secret-access-token');

    expect(client.getToken()).toBe('secret-access-token');
    expect(JSON.stringify({ ...localStorage })).not.toContain('secret-access-token');

    client.clearToken();
    expect(client.getToken()).toBeNull();
  });

  it('restores the session without sending a token, and stores the access token it gets back', async () => {
    const fetchMock = vi.fn(async () => jsonResponse({ accessToken: 'new-access', user }));
    vi.stubGlobal('fetch', fetchMock);
    const client = await loadClient();

    const profile = await client.restoreSession();

    expect(profile).toEqual(user);
    expect(client.getToken()).toBe('new-access');
    const [url, init] = fetchMock.mock.calls[0] as unknown as [string, RequestInit];
    expect(url).toBe('/api/auth/refresh');
    expect(init.method).toBe('POST');
    expect(init.body).toBeUndefined();
  });

  it('returns no profile and no token when the cookie is missing or invalid', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => jsonResponse({ error: 'vn-0080' }, 401)));
    const client = await loadClient();

    await expect(client.restoreSession()).resolves.toBeNull();
    expect(client.getToken()).toBeNull();
  });

  it('returns no profile when the server cannot be reached', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => { throw new Error('offline'); }));
    const client = await loadClient();

    await expect(client.restoreSession()).resolves.toBeNull();
  });

  it('shares a single refresh request between concurrent callers', async () => {
    const fetchMock = vi.fn(async () => jsonResponse({ accessToken: 'new-access', user }));
    vi.stubGlobal('fetch', fetchMock);
    const client = await loadClient();

    await Promise.all([client.restoreSession(), client.restoreSession()]);

    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it('refreshes once and retries the original request after a 401', async () => {
    const calls: Array<{ url: string; auth: string | null }> = [];
    vi.stubGlobal('fetch', vi.fn(async (url: string, init?: RequestInit) => {
      calls.push({ url, auth: new Headers(init?.headers).get('Authorization') });
      if (url === '/api/auth/refresh') {
        return jsonResponse({ accessToken: 'fresh', user });
      }
      return calls.filter((c) => c.url === '/api/data').length === 1
        ? jsonResponse({}, 401)
        : jsonResponse({ ok: true });
    }));
    const client = await loadClient();
    client.setAccessToken('expired');

    const result = await client.apiFetch<{ ok: boolean }>('/api/data');

    expect(result).toEqual({ ok: true });
    expect(calls.map((c) => c.url)).toEqual(['/api/data', '/api/auth/refresh', '/api/data']);
    expect(calls[0].auth).toBe('Bearer expired');
    expect(calls[2].auth).toBe('Bearer fresh');
  });

  // Cross-tab safety: the refresh token rotates, so refreshes must be serialized across tabs.
  // jsdom has no Web Locks, so the lock is stubbed here; the other tests cover the fallback path.
  it('serializes refreshes through a Web Lock when the browser supports it', async () => {
    const request = vi.fn(async (_name: string, task: () => Promise<unknown>) => task());
    Object.defineProperty(navigator, 'locks', { value: { request }, configurable: true, writable: true });
    try {
      vi.stubGlobal('fetch', vi.fn(async () => jsonResponse({ accessToken: 'new-access', user })));
      const client = await loadClient();

      await client.restoreSession();

      expect(request).toHaveBeenCalledTimes(1);
      expect(request.mock.calls[0][0]).toBe('vc-refresh-session');
    } finally {
      Reflect.deleteProperty(navigator, 'locks');
    }
  });

  it('still refreshes when the browser has no Web Locks', async () => {
    expect((navigator as { locks?: unknown }).locks).toBeUndefined();
    vi.stubGlobal('fetch', vi.fn(async () => jsonResponse({ accessToken: 'new-access', user })));
    const client = await loadClient();

    await expect(client.restoreSession()).resolves.toEqual(user);
    expect(client.getToken()).toBe('new-access');
  });

  it('clears the token and reports the session as expired when the refresh fails', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => jsonResponse({ error: 'vn-0080' }, 401)));
    const client = await loadClient();
    const session = await import('../auth/session');
    const onExpired = vi.fn();
    session.setSessionExpiredHandler(onExpired);
    client.setAccessToken('expired');

    await expect(client.apiFetch('/api/data')).rejects.toBeInstanceOf(client.ApiError);

    expect(onExpired).toHaveBeenCalledTimes(1);
    expect(client.getToken()).toBeNull();
    session.setSessionExpiredHandler(null);
  });

  it('logs out without a body and never throws', async () => {
    const fetchMock = vi.fn(async () => { throw new Error('offline'); });
    vi.stubGlobal('fetch', fetchMock);
    const client = await loadClient();

    expect(() => client.revokeRefreshToken()).not.toThrow();

    const [url, init] = fetchMock.mock.calls[0] as unknown as [string, RequestInit];
    expect(url).toBe('/api/auth/logout');
    expect(init.method).toBe('POST');
    expect(init.keepalive).toBe(true);
    expect(init.body).toBeUndefined();
  });
});
