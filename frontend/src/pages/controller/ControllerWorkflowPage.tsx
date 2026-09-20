import { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import type { EvaluationSummary } from '../../api/types';
import { EvaluationBucketTabs } from '../../components/evaluation/EvaluationBucketTabs';
import { EvaluationSummaryTable, EvaluationSummaryTableCard } from '../../components/evaluation/EvaluationSummaryTable';
import { AppLayout } from '../../components/AppLayout';
import { PeriodFilters, currentYear } from '../../components/PeriodFilters';
import { useDebouncedSearch, usePagedList, useToast } from '../../hooks';
import { useEvaluationBucketCounts } from '../../hooks/useEvaluationBucketCounts';
import { useIntl } from '../../i18n';
import {
  type ControllerBucket,
  controllerBucketTabLabels,
  controllerBucketTabs,
  emptyStateByControllerBucket,
} from '../../utils/controllerBuckets';
import { buildEvaluationsPagePath, mapControllerBucketCounts } from '../../utils/evaluationApi';

export function ControllerWorkflowPage() {
  const { formatMessage } = useIntl();
  const toast = useToast();
  const [searchParams] = useSearchParams();
  const tabFromUrl = searchParams.get('tab') as ControllerBucket | null;
  const initialTab = tabFromUrl && controllerBucketTabs.includes(tabFromUrl) ? tabFromUrl : 'pending';
  const [activeTab, setActiveTab] = useState<ControllerBucket>(initialTab);
  const [year, setYear] = useState(currentYear);
  const [quarter, setQuarter] = useState<number | null>(null);
  const { input: searchInput, debounced: search, setInput: setSearchInput } = useDebouncedSearch();

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

  const { counts } = useEvaluationBucketCounts(countsQueryKey, { year, quarter, search });
  const tabCounts = useMemo(() => mapControllerBucketCounts(counts), [counts]);

  useEffect(() => {
    if (error) toast.error(error);
  }, [error, toast]);

  useEffect(() => {
    if (tabFromUrl && controllerBucketTabs.includes(tabFromUrl)) {
      setActiveTab(tabFromUrl);
    }
  }, [tabFromUrl]);

  return (
    <AppLayout title={formatMessage({ id: 'controller.evaluationsTitle' })}>
      <div className="card card--filter">
        <PeriodFilters
          year={year}
          quarter={quarter}
          onYearChange={setYear}
          onQuarterChange={setQuarter}
          showAllQuartersOption
          search={searchInput}
          onSearchChange={setSearchInput}
        />
      </div>

      <EvaluationBucketTabs
        activeTab={activeTab}
        onTabChange={(tab) => setActiveTab(tab as ControllerBucket)}
        tabs={controllerBucketTabs}
        counts={tabCounts}
        tabLabels={controllerBucketTabLabels}
      />

      <EvaluationSummaryTableCard
        loading={loading}
        empty={!loading && evaluations.length === 0}
        activeTab={activeTab}
        emptyStateMap={emptyStateByControllerBucket}
      >
        <EvaluationSummaryTable
          evaluations={evaluations}
          activeTab={activeTab}
          detailPath={(id) => `/controller/evaluations/${id}`}
          showEvaluator
          showNewBadge
          pendingActionLabel={formatMessage({ id: 'controller.reviewAndDecision' })}
          pendingTabs={['pending']}
          hasMore={hasMore}
          loadingMore={loadingMore}
          onLoadMore={loadMore}
        />
      </EvaluationSummaryTableCard>
    </AppLayout>
  );
}
