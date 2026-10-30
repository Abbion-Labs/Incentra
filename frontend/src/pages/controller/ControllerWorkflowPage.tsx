import { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { api } from '../../api/client';
import type {
  ControllerEvaluatorSummary,
  EvaluationSummary,
} from '../../api/types';
import { FilterChip } from '../../components/common/ToolbarSearch';
import { EvaluationBucketTabs } from '../../components/evaluation/EvaluationBucketTabs';
import {
  EvaluationSummaryTable,
  EvaluationSummaryTableCard,
} from '../../components/evaluation/EvaluationSummaryTable';
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
import {
  buildEvaluationsPagePath,
  mapControllerBucketCounts,
} from '../../utils/evaluationApi';

export function ControllerWorkflowPage() {
  const { formatMessage } = useIntl();
  const toast = useToast();
  const [searchParams] = useSearchParams();
  const tabFromUrl = searchParams.get('tab') as ControllerBucket | null;
  const initialTab =
    tabFromUrl && controllerBucketTabs.includes(tabFromUrl)
      ? tabFromUrl
      : 'pending';
  const [activeTab, setActiveTab] = useState<ControllerBucket>(initialTab);
  const [year, setYear] = useState(currentYear);
  const [quarter, setQuarter] = useState<number | null>(null);
  const [evaluatorId, setEvaluatorId] = useState<number | null>(null);
  const [evaluators, setEvaluators] = useState<ControllerEvaluatorSummary[]>(
    [],
  );
  const {
    input: searchInput,
    debounced: search,
    setInput: setSearchInput,
  } = useDebouncedSearch();

  const listQueryKey = `${year}|${quarter ?? 'all'}|${activeTab}|${search}|${evaluatorId ?? 'all'}`;
  const countsQueryKey = `${year}|${quarter ?? 'all'}|${search}|${evaluatorId ?? 'all'}`;

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
        scope: 'controller',
        year,
        quarter,
        bucket: activeTab,
        search,
        evaluatorEmployeeId: evaluatorId,
      }),
  });

  const { counts } = useEvaluationBucketCounts(countsQueryKey, {
    scope: 'controller',
    year,
    quarter,
    search,
    evaluatorEmployeeId: evaluatorId,
  });
  const tabCounts = useMemo(() => mapControllerBucketCounts(counts), [counts]);

  useEffect(() => {
    if (error) toast.error(error);
  }, [error, toast]);

  // Filter po ocenjivaču ima smisla tek kad kontrolor ima više ocenjivača.
  useEffect(() => {
    api
      .get<ControllerEvaluatorSummary[]>(
        '/api/evaluator-settings/my-evaluators',
      )
      .then(setEvaluators)
      .catch(() => setEvaluators([]));
  }, []);

  useEffect(() => {
    if (tabFromUrl && controllerBucketTabs.includes(tabFromUrl)) {
      setActiveTab(tabFromUrl);
    }
  }, [tabFromUrl]);

  return (
    <AppLayout title={formatMessage({ id: 'controller.evaluationsTitle' })}>
      <EvaluationSummaryTableCard
        loading={loading}
        empty={!loading && evaluations.length === 0}
        activeTab={activeTab}
        emptyStateMap={emptyStateByControllerBucket}
        toolbar={
          <>
            <EvaluationBucketTabs
              activeTab={activeTab}
              onTabChange={(tab) => setActiveTab(tab as ControllerBucket)}
              tabs={controllerBucketTabs}
              counts={tabCounts}
              tabLabels={controllerBucketTabLabels}
            />
            <PeriodFilters
              compact
              year={year}
              quarter={quarter}
              onYearChange={setYear}
              onQuarterChange={setQuarter}
              showAllQuartersOption
              search={searchInput}
              onSearchChange={setSearchInput}
              canReset={
                searchInput !== '' ||
                year !== currentYear ||
                quarter !== null ||
                evaluatorId !== null
              }
              onReset={() => {
                setSearchInput('');
                setYear(currentYear);
                setQuarter(null);
                setEvaluatorId(null);
              }}
            >
              {evaluators.length > 1 && (
                <FilterChip
                  label={formatMessage({ id: 'evaluation.evaluator' })}
                  value={evaluatorId == null ? '' : String(evaluatorId)}
                  onChange={(value) =>
                    setEvaluatorId(value === '' ? null : Number(value))
                  }
                >
                  <option value="">
                    {formatMessage({ id: 'common.allShort' })}
                  </option>
                  {evaluators.map((evaluator) => (
                    <option
                      key={evaluator.employeeId}
                      value={evaluator.employeeId}
                    >
                      {evaluator.employeeFullName}
                    </option>
                  ))}
                </FilterChip>
              )}
            </PeriodFilters>
          </>
        }
      >
        <EvaluationSummaryTable
          evaluations={evaluations}
          detailPath={(id) => `/controller/evaluations/${id}`}
          showEvaluator
          showNewBadge
          hasMore={hasMore}
          loadingMore={loadingMore}
          onLoadMore={loadMore}
        />
      </EvaluationSummaryTableCard>
    </AppLayout>
  );
}
