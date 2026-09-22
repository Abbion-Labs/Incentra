import { useCallback, useEffect, useRef, useState } from 'react';
import { api } from '../api/client';
import type { PagedResult } from '../api/types';
import { useIntl } from '../i18n';

const DEFAULT_PAGE_SIZE = 30;

interface UsePagedListOptions {
  queryKey: string;
  pageSize?: number;
  enabled?: boolean;
  fetchPage: (page: number, pageSize: number) => string;
  onError?: (error: Error) => void;
}

export function usePagedList<T>({
  queryKey,
  pageSize = DEFAULT_PAGE_SIZE,
  enabled = true,
  fetchPage,
  onError,
}: UsePagedListOptions) {
  const { formatMessage } = useIntl();
  const [items, setItems] = useState<T[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState('');
  const fetchPageRef = useRef(fetchPage);
  fetchPageRef.current = fetchPage;
  const pageRef = useRef(page);
  pageRef.current = page;
  // Brzo menjanje taba/filtera pokreće više zahteva paralelno; samo poslednji sme da upiše stanje.
  const requestIdRef = useRef(0);

  const hasMore = items.length < totalCount;

  const loadPage = useCallback(
    async (targetPage: number, append: boolean) => {
      if (!enabled) return;

      const requestId = ++requestIdRef.current;
      const isLatest = () => requestId === requestIdRef.current;

      if (append) {
        setLoadingMore(true);
      } else {
        setLoading(true);
      }
      setError('');

      try {
        const result = await api.get<PagedResult<T>>(
          fetchPageRef.current(targetPage, pageSize),
        );
        if (!isLatest()) return;
        const pageItems = result.items ?? [];
        setTotalCount(result.totalCount ?? 0);
        setPage(targetPage);
        setItems((current) =>
          append ? [...current, ...pageItems] : pageItems,
        );
      } catch (e) {
        if (!isLatest()) return;
        const message =
          e instanceof Error
            ? e.message
            : formatMessage({ id: 'errors.loadFailed' });
        setError(message);
        onError?.(e instanceof Error ? e : new Error(message));
        if (!append) {
          setItems([]);
          setTotalCount(0);
        }
      } finally {
        if (isLatest()) {
          setLoading(false);
          setLoadingMore(false);
        }
      }
    },
    [enabled, onError, pageSize],
  );

  const reload = useCallback(() => {
    void loadPage(1, false);
  }, [loadPage]);

  const loadMore = useCallback(() => {
    if (loading || loadingMore || items.length >= totalCount) return;
    void loadPage(pageRef.current + 1, true);
  }, [items.length, loadPage, loading, loadingMore, totalCount]);

  useEffect(() => {
    if (!enabled) {
      requestIdRef.current += 1;
      setLoading(false);
      setLoadingMore(false);
      setItems([]);
      setTotalCount(0);
      setPage(1);
      return;
    }
    reload();
  }, [enabled, queryKey, reload]);

  return {
    items,
    totalCount,
    loading,
    loadingMore,
    error,
    hasMore,
    reload,
    loadMore,
    setError,
  };
}
