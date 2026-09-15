import { useEffect, useRef } from 'react';
import { useIntl } from '../../i18n';

interface InfiniteScrollSentinelProps {
  hasMore: boolean;
  isLoading: boolean;
  onLoadMore: () => void;
}

export function InfiniteScrollSentinel({ hasMore, isLoading, onLoadMore }: InfiniteScrollSentinelProps) {
  const { formatMessage } = useIntl();
  const sentinelRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const element = sentinelRef.current;
    if (!element || !hasMore) return;

    const observer = new IntersectionObserver(
      (entries) => {
        const entry = entries[0];
        if (entry?.isIntersecting && hasMore && !isLoading) {
          onLoadMore();
        }
      },
      { root: null, rootMargin: '120px', threshold: 0.1 },
    );

    observer.observe(element);
    return () => observer.disconnect();
  }, [hasMore, isLoading, onLoadMore]);

  if (!hasMore) return null;

  return (
    <div ref={sentinelRef} className="infinite-scroll-sentinel" aria-hidden="true">
      {isLoading ? <span className="infinite-scroll-sentinel__label">{formatMessage({ id: 'common.loading' })}</span> : null}
    </div>
  );
}
