import { useEffect, useMemo, useState } from 'react';
import type { EvaluationSummary } from '../../api/types';
import { EmptyState } from '../../components/common/EmptyState';
import { InfiniteScrollSentinel } from '../../components/common/InfiniteScrollSentinel';
import { TableSkeleton } from '../../components/common/LoadingSkeleton';
import { AppLayout } from '../../components/AppLayout';
import { useIntl } from '../../i18n';
import { classifyEvaluation } from '../../utils/evaluationBuckets';
import { currentQuarter, currentYear } from '../../utils/status';
import { buildEvaluationsPagePath } from '../../utils/evaluationApi';
import { usePagedList, useToast } from '../../hooks';
import {
  ControllerEvaluationTable,
  ControllerFilters,
} from './components/ControllerEvaluationTable';

const statusBucketMap: Record<string, string> = {
  pending: 'pending',
  approved: 'approved',
  returned: 'returned',
};

export function ControllerDashboard() {
  const { formatMessage } = useIntl();
  const toast = useToast();
  const [year, setYear] = useState(currentYear);
  const [quarter, setQuarter] = useState(currentQuarter);
  const [statusFilter, setStatusFilter] = useState<string>('pending');

  const bucket = statusBucketMap[statusFilter] ?? 'pending';
  const listQueryKey = `${year}|${quarter}|${bucket}`;

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
        bucket,
      }),
  });

  const filtered = useMemo(
    () =>
      evaluations.filter((evaluation) => {
        if (statusFilter === 'pending') {
          return (
            evaluation.status === 'Submitted' ||
            evaluation.status === 'UnderReview'
          );
        }
        if (statusFilter === 'approved')
          return evaluation.status === 'Approved';
        if (statusFilter === 'returned')
          return classifyEvaluation(evaluation) === 'returned';
        return true;
      }),
    [evaluations, statusFilter],
  );

  useEffect(() => {
    if (error) toast.error(error);
  }, [error, toast]);

  return (
    <AppLayout title={formatMessage({ id: 'controller.dashboardTitle' })}>
      <ControllerFilters
        year={year}
        quarter={quarter}
        statusFilter={statusFilter}
        onYearChange={setYear}
        onQuarterChange={setQuarter}
        onStatusFilterChange={setStatusFilter}
      />

      <div className="card card--flush card--table-fill">
        {loading ? (
          <TableSkeleton rows={5} columns={7} />
        ) : filtered.length === 0 ? (
          <EmptyState
            title={formatMessage({ id: 'controller.dashboardEmptyTitle' })}
            description={formatMessage({
              id: 'controller.dashboardEmptyDescription',
            })}
          />
        ) : (
          <div className="table-panel">
            <div className="table-wrap table-wrap--infinite">
              <ControllerEvaluationTable evaluations={filtered} />
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
