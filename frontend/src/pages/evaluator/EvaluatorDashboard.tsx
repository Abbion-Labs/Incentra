import { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import type { EvaluationSummary } from '../../api/types';
import { AlertMessages } from '../../components/common/AlertMessages';
import { EvaluationBucketTabs, evaluationBucketTabs } from '../../components/evaluation/EvaluationBucketTabs';
import { EvaluationSummaryTable, EvaluationSummaryTableCard } from '../../components/evaluation/EvaluationSummaryTable';
import { AppLayout } from '../../components/AppLayout';
import { PeriodFilters, currentYear } from '../../components/PeriodFilters';
import { useDebouncedSearch } from '../../hooks/useDebouncedSearch';
import { useEvaluationBucketCounts } from '../../hooks/useEvaluationBucketCounts';
import { usePagedList } from '../../hooks/usePagedList';
import { useIntl } from '../../i18n';
import { type EvaluationBucket } from '../../utils/evaluationBuckets';
import { buildEvaluationsPagePath, mapEvaluatorBucketCounts } from '../../utils/evaluationApi';

export function EvaluatorDashboard() {
  const { formatMessage } = useIntl();
  const [searchParams] = useSearchParams();
  const tabFromUrl = searchParams.get('tab') as EvaluationBucket | null;
  const initialTab = tabFromUrl && evaluationBucketTabs.includes(tabFromUrl) ? tabFromUrl : 'unrated';
  const [activeTab, setActiveTab] = useState<EvaluationBucket>(initialTab);
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
  const tabCounts = useMemo(() => mapEvaluatorBucketCounts(counts), [counts]);

  useEffect(() => {
    if (tabFromUrl && evaluationBucketTabs.includes(tabFromUrl)) {
      setActiveTab(tabFromUrl);
    }
  }, [tabFromUrl]);

  return (
    <AppLayout title={formatMessage({ id: 'evaluation.ratingTitle' })}>
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
        onTabChange={(tab) => setActiveTab(tab as EvaluationBucket)}
        counts={tabCounts}
      />
      <AlertMessages error={error} />

      <EvaluationSummaryTableCard
        loading={loading}
        empty={!loading && evaluations.length === 0}
        activeTab={activeTab}
      >
        <EvaluationSummaryTable
          evaluations={evaluations}
          activeTab={activeTab}
          detailPath={(id) => `/evaluator/evaluations/${id}`}
          hasMore={hasMore}
          loadingMore={loadingMore}
          onLoadMore={loadMore}
        />
      </EvaluationSummaryTableCard>
    </AppLayout>
  );
}
