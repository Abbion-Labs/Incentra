import type { ReactNode } from 'react';
import { act, renderHook, waitFor } from '@testing-library/react';
import { IntlProvider } from 'react-intl';
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { PagedResult } from '../api/types';
import { mockApiGetDeferred } from '../test/deferredApi';
import { usePagedList } from './usePagedList';

function page(items: string[]): PagedResult<string> {
  return {
    items,
    totalCount: items.length,
    page: 1,
    pageSize: 30,
  } as PagedResult<string>;
}

const wrapper = ({ children }: { children: ReactNode }) => (
  <IntlProvider locale="sr" messages={{}} onError={() => undefined}>
    {children}
  </IntlProvider>
);

function renderTabList(initialTab: string) {
  return renderHook(
    ({ tab }: { tab: string }) =>
      usePagedList<string>({
        queryKey: tab,
        fetchPage: (p, size) =>
          `/api/evaluations?bucket=${tab}&page=${p}&pageSize=${size}`,
      }),
    { wrapper, initialProps: { tab: initialTab } },
  );
}

describe('usePagedList', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('ignores a stale response that arrives after the latest one', async () => {
    const pending = mockApiGetDeferred();
    const { result, rerender } = renderTabList('approved');
    const approvedPath = '/api/evaluations?bucket=approved&page=1&pageSize=30';
    const unratedPath = '/api/evaluations?bucket=unrated&page=1&pageSize=30';

    await waitFor(() => expect(pending.has(approvedPath)).toBe(true));
    rerender({ tab: 'unrated' });
    await waitFor(() => expect(pending.has(unratedPath)).toBe(true));

    await act(async () => {
      pending.get(unratedPath)!.resolve(page(['unrated-1']));
    });
    expect(result.current.items).toEqual(['unrated-1']);
    expect(result.current.loading).toBe(false);

    await act(async () => {
      pending.get(approvedPath)!.resolve(page(['approved-1', 'approved-2']));
    });
    expect(result.current.items).toEqual(['unrated-1']);
    expect(result.current.totalCount).toBe(1);
  });

  it('stays loading until the latest request settles', async () => {
    const pending = mockApiGetDeferred();
    const { result, rerender } = renderTabList('approved');
    const approvedPath = '/api/evaluations?bucket=approved&page=1&pageSize=30';
    const unratedPath = '/api/evaluations?bucket=unrated&page=1&pageSize=30';

    await waitFor(() => expect(pending.has(approvedPath)).toBe(true));
    rerender({ tab: 'unrated' });
    await waitFor(() => expect(pending.has(unratedPath)).toBe(true));

    await act(async () => {
      pending.get(approvedPath)!.reject(new Error('boom'));
    });
    expect(result.current.loading).toBe(true);
    expect(result.current.error).toBe('');

    await act(async () => {
      pending.get(unratedPath)!.resolve(page(['unrated-1']));
    });
    expect(result.current.loading).toBe(false);
    expect(result.current.items).toEqual(['unrated-1']);
  });
});
