export type SessionEvent = 'signed-in' | 'signed-out';

export interface SessionChannel {
  announce: (event: SessionEvent) => void;
  close: () => void;
}

const CHANNEL_NAME = 'vc-session';

// Tabs of one browser share the refresh cookie, but each keeps its own access token in memory. Signing in or
// out is therefore announced to the other tabs; without it, a tab left open after signing out elsewhere would
// keep showing its data until the user next did something in it.
export function openSessionChannel(
  onEvent: (event: SessionEvent) => void,
): SessionChannel {
  if (typeof BroadcastChannel === 'undefined') {
    return { announce: () => undefined, close: () => undefined };
  }

  const channel = new BroadcastChannel(CHANNEL_NAME);
  channel.onmessage = (message: MessageEvent<unknown>) => {
    if (message.data === 'signed-in' || message.data === 'signed-out') {
      onEvent(message.data);
    }
  };

  return {
    announce: (event) => channel.postMessage(event),
    close: () => channel.close(),
  };
}
