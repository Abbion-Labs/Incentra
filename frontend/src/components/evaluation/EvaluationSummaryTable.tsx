import { Link, useNavigate } from 'react-router-dom';
import type { KeyboardEvent } from 'react';
import type { EvaluationSummary } from '../../api/types';
import { useIntl } from '../../i18n';
import { EmptyState } from '../common/EmptyState';
import { InfiniteScrollSentinel } from '../common/InfiniteScrollSentinel';
import { TableSkeleton } from '../common/LoadingSkeleton';
import type { EvaluationBucket } from '../../utils/evaluationBuckets';
import { emptyStateByBucketKeys } from '../../utils/evaluationBuckets';
import {
  formatSummaryAverage,
  summaryDescriptiveRatingName,
} from '../../utils/scoring';
import { isNewForController } from '../../utils/controllerBuckets';
import { AverageDisplay } from './AverageDisplay';
import { DescriptiveRatingBadge } from './DescriptiveRatingBadge';

interface EvaluationSummaryTableProps {
  evaluations: EvaluationSummary[];
  activeTab: string;
  detailPath: (id: number) => string;
  showEvaluator?: boolean;
  showNewBadge?: boolean;
  pendingActionLabel?: string;
  defaultActionLabel?: string;
  pendingTabs?: string[];
  hasMore?: boolean;
  loadingMore?: boolean;
  onLoadMore?: () => void;
}

export function EvaluationSummaryTable({
  evaluations,
  activeTab,
  detailPath,
  showEvaluator = false,
  showNewBadge = false,
  pendingActionLabel,
  defaultActionLabel: _defaultActionLabel,
  pendingTabs = ['unrated', 'returned', 'pending'],
  hasMore = false,
  loadingMore = false,
  onLoadMore,
}: EvaluationSummaryTableProps) {
  const { formatMessage } = useIntl();
  const navigate = useNavigate();
  const resolvedPendingActionLabel =
    pendingActionLabel ?? formatMessage({ id: 'evaluation.rateAndSubmit' });
  const isPendingTab = pendingTabs.includes(activeTab);

  function openEvaluation(id: number) {
    navigate(detailPath(id));
  }

  function handleRowKeyDown(
    event: KeyboardEvent<HTMLTableRowElement>,
    id: number,
  ) {
    if (isPendingTab) return;
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      openEvaluation(id);
    }
  }

  return (
    <>
      <div className="table-panel">
        <div className="table-wrap table-wrap--infinite">
          <table
            className={`table table--hover table--stack table--evaluations${isPendingTab ? ' table--evaluations--with-actions' : ' table--clickable'}`}
          >
            <thead>
              <tr>
                <th className="col-text">
                  {formatMessage({ id: 'admin.employees' })}
                </th>
                {showEvaluator && (
                  <th className="col-text">
                    {formatMessage({ id: 'admin.evaluators' })}
                  </th>
                )}
                <th className="col-meta">
                  {formatMessage({ id: 'evaluation.period' })}
                </th>
                <th className="col-num">
                  {formatMessage({ id: 'evaluation.average' })}
                </th>
                <th className="col-meta">
                  {formatMessage({ id: 'evaluation.descriptiveLabel' })}
                </th>
                {isPendingTab && <th className="col-actions"></th>}
              </tr>
            </thead>
            <tbody>
              {evaluations.map((ev) => (
                <tr
                  key={ev.id}
                  onClick={
                    isPendingTab ? undefined : () => openEvaluation(ev.id)
                  }
                  onKeyDown={
                    isPendingTab
                      ? undefined
                      : (event) => handleRowKeyDown(event, ev.id)
                  }
                  tabIndex={isPendingTab ? undefined : 0}
                  role={isPendingTab ? undefined : 'link'}
                >
                  <td className="cell-primary col-text stack-title">
                    {ev.employeeFullName}
                    {showNewBadge && isNewForController(ev) && (
                      <span
                        className="badge badge-new"
                        style={{ marginLeft: '0.5rem' }}
                      >
                        {formatMessage({ id: 'evaluation.new' })}
                      </span>
                    )}
                  </td>
                  {showEvaluator && (
                    <td
                      className="cell-muted col-text"
                      data-label={formatMessage({ id: 'admin.evaluators' })}
                    >
                      {ev.evaluatorFullName}
                    </td>
                  )}
                  <td
                    className="cell-muted col-meta"
                    data-label={formatMessage({ id: 'evaluation.period' })}
                  >
                    Q{ev.quarter}/{ev.year}
                  </td>
                  <td
                    className="col-num"
                    data-label={formatMessage({ id: 'evaluation.average' })}
                  >
                    <AverageDisplay value={formatSummaryAverage(ev)} />
                  </td>
                  <td
                    className="col-meta"
                    data-label={formatMessage({
                      id: 'evaluation.descriptiveLabel',
                    })}
                  >
                    <DescriptiveRatingBadge
                      name={summaryDescriptiveRatingName(ev)}
                    />
                  </td>
                  {isPendingTab && (
                    <td className="col-actions">
                      <Link
                        to={detailPath(ev.id)}
                        className="btn btn-sm btn-primary"
                        onClick={(event) => event.stopPropagation()}
                      >
                        {activeTab === 'returned'
                          ? formatMessage({ id: 'evaluation.revise' })
                          : resolvedPendingActionLabel}
                      </Link>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
          {onLoadMore && (
            <InfiniteScrollSentinel
              hasMore={hasMore}
              isLoading={loadingMore}
              onLoadMore={onLoadMore}
            />
          )}
        </div>
      </div>
    </>
  );
}

export function EvaluationSummaryTableCard({
  loading,
  empty,
  activeTab,
  children,
  emptyStateMap,
  initialLoading = true,
}: {
  loading: boolean;
  empty: boolean;
  activeTab?: string;
  children: React.ReactNode;
  emptyStateMap?: Record<string, { title: string; description: string }>;
  initialLoading?: boolean;
}) {
  const { formatMessage } = useIntl();
  const emptyCopy =
    activeTab && emptyStateMap
      ? emptyStateMap[activeTab]
      : activeTab
        ? emptyStateByBucketKeys[activeTab as EvaluationBucket]
        : null;

  return (
    <div className="card card--flush card--table-fill">
      {loading && initialLoading ? (
        <TableSkeleton rows={4} columns={5} />
      ) : empty ? (
        <EmptyState
          title={
            emptyCopy?.title
              ? formatMessage({ id: emptyCopy.title as never })
              : formatMessage({ id: 'evaluation.noItems' })
          }
          description={
            emptyCopy?.description
              ? formatMessage({ id: emptyCopy.description as never })
              : formatMessage({ id: 'evaluation.noItemsForCategory' })
          }
        />
      ) : (
        children
      )}
    </div>
  );
}

export { bucketLabelKeys as bucketLabels } from '../../utils/evaluationBuckets';
