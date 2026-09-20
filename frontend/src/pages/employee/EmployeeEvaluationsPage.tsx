import { useEffect, useMemo, useState } from 'react';
import type { EvaluationSummary } from '../../api/types';
import { EmptyState } from '../../components/common/EmptyState';
import { InfiniteScrollSentinel } from '../../components/common/InfiniteScrollSentinel';
import { TableSkeleton } from '../../components/common/LoadingSkeleton';
import {
  EvaluationBucketTabs,
  evaluationBucketTabs,
} from '../../components/evaluation/EvaluationBucketTabs';
import { AppLayout } from '../../components/AppLayout';
import { PeriodFilters, currentYear } from '../../components/PeriodFilters';
import { useIntl } from '../../i18n';
import { useDebouncedSearch, usePagedList, useToast } from '../../hooks';
import { useEvaluationBucketCounts } from '../../hooks/useEvaluationBucketCounts';
import {
  type EvaluationBucket,
  emptyStateByBucketKeys,
} from '../../utils/evaluationBuckets';
import {
  buildEvaluationsPagePath,
  mapEvaluatorBucketCounts,
} from '../../utils/evaluationApi';
import { EmployeeEvaluationListTable } from './components/EmployeeEvaluationListTable';

export function EmployeeEvaluationsPage() {
  const { formatMessage } = useIntl();
  const toast = useToast();
  const [activeTab, setActiveTab] = useState<EvaluationBucket>('unrated');
  const [year, setYear] = useState(currentYear);
  const [quarter, setQuarter] = useState<number | null>(null);
  const {
    input: searchInput,
    debounced: search,
    setInput: setSearchInput,
  } = useDebouncedSearch();

  const listQueryKey = `${year}|${quarter ?? 'all'}|${activeTab}|${search}`;
  const countsQueryKey = `${year}|${quarter ?? 'all'}|${search}`;

  const {
    items: evaluations,
    loading,
    loadingMore,
    error,
    hasMore,
    loadMore,
  } = usePagedList<EvaluationSummary>({
    queryKey: listQueryKey,
    fetchPage: (page, pageSize) =>
      buildEvaluationsPagePath(page, pageSize, {
        year,
        quarter,
        bucket: activeTab,
        search,
      }),
  });

  const { counts } = useEvaluationBucketCounts(countsQueryKey, {
    year,
    quarter,
    search,
  });
  const tabCounts = useMemo(() => mapEvaluatorBucketCounts(counts), [counts]);

  const emptyCopy = emptyStateByBucketKeys[activeTab];

  useEffect(() => {
    if (error) toast.error(error);
  }, [error, toast]);

  return (
    <AppLayout title={formatMessage({ id: 'navigation.employeeEvaluations' })}>
      <div className="card card--filter">
        <PeriodFilters
          year={year}
          quarter={quarter}
          onYearChange={setYear}
          onQuarterChange={setQuarter}
          showAllQuartersOption
          search={searchInput}
          onSearchChange={setSearchInput}
          searchLabel={formatMessage({ id: 'evaluation.search' })}
          searchPlaceholder={formatMessage({
            id: 'evaluation.searchPlaceholder',
          })}
        />
      </div>

      <EvaluationBucketTabs
        activeTab={activeTab}
        onTabChange={(tab) => setActiveTab(tab as EvaluationBucket)}
        tabs={evaluationBucketTabs}
        counts={tabCounts}
      />

      <div className="card card--flush card--table-fill">
        {loading ? (
          <TableSkeleton rows={4} columns={4} />
        ) : evaluations.length === 0 ? (
          <EmptyState
            title={formatMessage({ id: emptyCopy.title as never })}
            description={formatMessage({ id: emptyCopy.description as never })}
          />
        ) : (
          <div className="table-panel">
            <div className="table-wrap table-wrap--infinite">
              <EmployeeEvaluationListTable evaluations={evaluations} />
              <InfiniteScrollSentinel
                hasMore={hasMore}
                isLoading={loadingMore}
                onLoadMore={loadMore}
              />
            </div>
          </div>
        )}
      </div>
    </AppLayout>
  );
}
