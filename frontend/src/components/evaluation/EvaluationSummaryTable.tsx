import { useNavigate } from 'react-router-dom';
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
import { PersonName } from '../employee/PersonName';
import { AverageDisplay } from './AverageDisplay';
import { DescriptiveRatingBadge } from './DescriptiveRatingBadge';

interface EvaluationSummaryTableProps {
  evaluations: EvaluationSummary[];
  detailPath: (id: number) => string;
  showEvaluator?: boolean;
  showNewBadge?: boolean;
  hasMore?: boolean;
  loadingMore?: boolean;
  onLoadMore?: () => void;
}

/** Tabela ocena: ceo red otvara ocenu (klik, Enter ili razmak). */
export function EvaluationSummaryTable({
  evaluations,
  detailPath,
  showEvaluator = false,
  showNewBadge = false,
  hasMore = false,
  loadingMore = false,
  onLoadMore,
}: EvaluationSummaryTableProps) {
  const { formatMessage } = useIntl();
  const navigate = useNavigate();

  function openEvaluation(id: number) {
    navigate(detailPath(id));
  }

  function handleRowKeyDown(
    event: KeyboardEvent<HTMLTableRowElement>,
    id: number,
  ) {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      openEvaluation(id);
    }
  }

  return (
    <>
      <div className="table-panel">
        <div className="table-wrap table-wrap--infinite">
          <table className="table table--hover table--stack table--evaluations table--clickable">
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
              </tr>
            </thead>
            <tbody>
              {evaluations.map((ev) => {
                const isNew = showNewBadge && isNewForController(ev);
                return (
                  <tr
                    key={ev.id}
                    className={isNew ? 'table-row--new' : undefined}
                    onClick={() => openEvaluation(ev.id)}
                    onKeyDown={(event) => handleRowKeyDown(event, ev.id)}
                    tabIndex={0}
                    role="link"
                  >
                    <td className="cell-primary col-text stack-title">
                      <PersonName
                        fullName={ev.employeeFullName}
                        highlightLabel={
                          isNew
                            ? formatMessage({ id: 'evaluation.newEvaluation' })
                            : undefined
                        }
                      />
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
                  </tr>
                );
              })}
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
  toolbar,
}: {
  loading: boolean;
  empty: boolean;
  activeTab?: string;
  children: React.ReactNode;
  emptyStateMap?: Record<string, { title: string; description: string }>;
  initialLoading?: boolean;
  /** Traka sa tabovima i filterima na vrhu kartice, iznad tabele. */
  toolbar?: React.ReactNode;
}) {
  const { formatMessage } = useIntl();
  const emptyCopy =
    activeTab && emptyStateMap
      ? emptyStateMap[activeTab]
      : activeTab
        ? emptyStateByBucketKeys[activeTab as EvaluationBucket]
        : null;

  return (
    <div
      className={`card card--flush card--table-fill${toolbar ? ' data-panel' : ''}`}
    >
      {toolbar && <div className="data-panel__toolbar">{toolbar}</div>}
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
