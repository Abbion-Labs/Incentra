import { vi } from 'vitest';

/** jsdom nema `window.matchMedia`; komponente koje ga koriste dobijaju „ne poklapa se“. */
export function stubMatchMedia() {
  vi.stubGlobal('matchMedia', (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addEventListener: () => undefined,
    removeEventListener: () => undefined,
    addListener: () => undefined,
    removeListener: () => undefined,
    dispatchEvent: () => false,
  }));
}
