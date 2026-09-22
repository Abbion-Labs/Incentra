import { act, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { openSessionChannel } from './sessionChannel';
import type { SessionEvent } from './sessionChannel';

const alice = { id: 1, email: 'alice@local.dev', roles: ['EMPLOYEE'] };
const bob = { id: 2, email: 'bob@local.dev', roles: ['ADMIN'] };

// Delivers each message to every other open channel of the same name, the way tabs of one browser see it.
class FakeBroadcastChannel {
  static open: FakeBroadcastChannel[] = [];

  onmessage: ((event: MessageEvent) => void) | null = null;

  constructor(readonly name: string) {
    FakeBroadcastChannel.open.push(this);
  }

  postMessage(data: unknown) {
    FakeBroadcastChannel.open
      .filter((other) => other !== this && other.name === this.name)
      .forEach((other) =>
        other.onmessage?.(new MessageEvent('message', { data })),
      );
  }

  close() {
    FakeBroadcastChannel.open = FakeBroadcastChannel.open.filter(
      (channel) => channel !== this,
    );
  }
}

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

function stubServer(refreshResponses: Array<() => Response>) {
  const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
    const url = String(input);
    if (url === '/api/auth/refresh') {
      return (
        refreshResponses.shift() ?? (() => new Response(null, { status: 401 }))
      )();
    }
    if (url === '/api/auth/login') {
      return jsonResponse({ accessToken: 'alice-access', user: alice });
    }
    if (url === '/api/auth/logout') {
      return new Response(null, { status: 204 });
    }
    throw new Error(`unexpected request to ${url}`);
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

// The api client keeps the access token in a module variable, so every test starts from fresh modules.
async function renderApp() {
  vi.resetModules();
  const { AuthProvider, useAuth } = await import('./AuthContext');
  const client = await import('../api/client');

  function CurrentUser() {
    const { user, loading, login, logout } = useAuth();
    if (loading) return <p>loading</p>;
    return (
      <div>
        <p>{user ? user.email : 'signed out'}</p>
        <button onClick={() => void login('alice@local.dev', 'secret')}>
          login
        </button>
        <button onClick={logout}>logout</button>
      </div>
    );
  }

  render(
    <AuthProvider>
      <CurrentUser />
    </AuthProvider>,
  );
  return client;
}

// Another tab's AuthProvider opens the same channel.
function openOtherTab() {
  const received: SessionEvent[] = [];
  const channel = openSessionChannel((event) => received.push(event));
  return { channel, received };
}

describe('AuthProvider across tabs', () => {
  beforeEach(() => {
    FakeBroadcastChannel.open = [];
    vi.stubGlobal('BroadcastChannel', FakeBroadcastChannel);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('signs this tab out when another tab signs out', async () => {
    stubServer([
      () => jsonResponse({ accessToken: 'alice-access', user: alice }),
    ]);
    const client = await renderApp();
    await screen.findByText('alice@local.dev');
    const otherTab = openOtherTab();

    act(() => otherTab.channel.announce('signed-out'));

    expect(screen.getByText('signed out')).toBeDefined();
    expect(client.getToken()).toBeNull();
  });

  it('picks up the session another tab has signed in to', async () => {
    stubServer([
      () => new Response(null, { status: 401 }),
      () => jsonResponse({ accessToken: 'bob-access', user: bob }),
    ]);
    const client = await renderApp();
    await screen.findByText('signed out');
    const otherTab = openOtherTab();

    act(() => otherTab.channel.announce('signed-in'));

    expect(await screen.findByText('bob@local.dev')).toBeDefined();
    expect(client.getToken()).toBe('bob-access');
  });

  it('tells the other tabs when this tab signs in and out', async () => {
    stubServer([]);
    await renderApp();
    await screen.findByText('signed out');
    const otherTab = openOtherTab();

    await userEvent.click(screen.getByText('login'));
    await screen.findByText('alice@local.dev');
    await userEvent.click(screen.getByText('logout'));

    expect(otherTab.received).toEqual(['signed-in', 'signed-out']);
  });

  it('does not react to its own announcements', async () => {
    const fetchMock = stubServer([]);
    await renderApp();
    await screen.findByText('signed out');

    await userEvent.click(screen.getByText('login'));
    await screen.findByText('alice@local.dev');

    const refreshes = fetchMock.mock.calls.filter(
      ([url]) => String(url) === '/api/auth/refresh',
    );
    expect(refreshes).toHaveLength(1);
  });
});
