import { Link, useNavigate } from 'react-router-dom';
import type { KeyboardEvent } from 'react';
import type { EvaluationSummary } from '../../../api/types';
import { useIntl } from '../../../i18n';
import { evaluationDisplayClass, evaluationDisplayLabel } from '../../../utils/evaluationBuckets';
import { formatSummaryAverage } from '../../../utils/scoring';
import { AverageDisplay } from '../../../components/evaluation/AverageDisplay';
import { formatDate } from '../../../utils/formatLocale';

interface ControllerEvaluationTableProps {
  evaluations: EvaluationSummary[];
}

function needsControllerAction(ev: EvaluationSummary): boolean {
  return ev.status === 'Submitted' || ev.status === 'UnderReview';
}

export function ControllerEvaluationTable({ evaluations }: ControllerEvaluationTableProps) {
  const { formatMessage } = useIntl();
  const navigate = useNavigate();
  const showActions = evaluations.some(needsControllerAction);

  function openEvaluation(id: number) {
    navigate(`/controller/evaluations/${id}`);
  }

  function handleRowKeyDown(event: KeyboardEvent<HTMLTableRowElement>, ev: EvaluationSummary) {
    if (needsControllerAction(ev)) return;
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      openEvaluation(ev.id);
    }
  }

  return (
    <div className="table-wrap">
      <table className={`table table--hover table--evaluations table--clickable${showActions ? ' table--evaluations--with-actions' : ''}`}>
        <thead>
          <tr>
            <th className="col-text">{formatMessage({ id: 'admin.employees' })}</th>
            <th className="col-text">{formatMessage({ id: 'admin.evaluators' })}</th>
            <th className="col-meta">{formatMessage({ id: 'evaluation.period' })}</th>
            <th className="col-num">{formatMessage({ id: 'evaluation.average' })}</th>
            <th className="col-meta">{formatMessage({ id: 'evaluation.status' })}</th>
            <th className="col-meta">{formatMessage({ id: 'evaluation.submittedAt' })}</th>
            {showActions && <th className="col-actions"></th>}
          </tr>
        </thead>
        <tbody>
          {evaluations.map((ev) => {
            const actionable = needsControllerAction(ev);
            return (
              <tr
                key={ev.id}
                className={actionable ? undefined : 'table-row--navigable'}
                onClick={actionable ? undefined : () => openEvaluation(ev.id)}
                onKeyDown={actionable ? undefined : (event) => handleRowKeyDown(event, ev)}
                tabIndex={actionable ? undefined : 0}
                role={actionable ? undefined : 'link'}
              >
                <td className="cell-primary col-text">{ev.employeeFullName}</td>
                <td className="cell-muted col-text">{ev.evaluatorFullName}</td>
                <td className="cell-muted col-meta">
                  Q{ev.quarter}/{ev.year}
                </td>
                <td className="col-num">
                  <AverageDisplay value={formatSummaryAverage(ev)} />
                </td>
                <td className="col-meta">
                  <span className={evaluationDisplayClass(ev)}>{evaluationDisplayLabel(ev, formatMessage)}</span>
                </td>
                <td className="cell-muted col-meta">
                  {ev.submittedAt ? formatDate(ev.submittedAt) : '—'}
                </td>
                {showActions && (
                  <td className="col-actions">
                    {actionable ? (
                      <Link
                        to={`/controller/evaluations/${ev.id}`}
                        className="btn btn-primary btn-sm"
                        onClick={(event) => event.stopPropagation()}
                      >
                        {formatMessage({ id: 'controller.reviewAndDecision' })}
                      </Link>
                    ) : null}
                  </td>
                )}
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

interface ControllerFiltersProps {
  year: number;
  quarter: number;
  statusFilter: string;
  onYearChange: (year: number) => void;
  onQuarterChange: (quarter: 1 | 2 | 3 | 4) => void;
  onStatusFilterChange: (value: string) => void;
}

export function ControllerFilters({
  year,
  quarter,
  statusFilter,
  onYearChange,
  onQuarterChange,
  onStatusFilterChange,
}: ControllerFiltersProps) {
  const { formatMessage } = useIntl();
  return (
    <div className="card card--filter">
      <div className="form-grid form-grid--filters">
        <div className="form-row">
          <label>{formatMessage({ id: 'common.year' })}</label>
          <input type="number" value={year} onChange={(e) => onYearChange(Number(e.target.value))} />
        </div>
        <div className="form-row">
          <label>{formatMessage({ id: 'evaluation.quarter' })}</label>
          <select value={quarter} onChange={(e) => onQuarterChange(Number(e.target.value) as 1 | 2 | 3 | 4)}>
            {[1, 2, 3, 4].map((q) => (
              <option key={q} value={q}>
                Q{q}
              </option>
            ))}
          </select>
        </div>
        <div className="form-row">
          <label>{formatMessage({ id: 'evaluation.status' })}</label>
          <select value={statusFilter} onChange={(e) => onStatusFilterChange(e.target.value)}>
            <option value="pending">{formatMessage({ id: 'status.UnderReview' })}</option>
            <option value="approved">{formatMessage({ id: 'status.Approved' })}</option>
            <option value="returned">{formatMessage({ id: 'evaluation.bucket.returned' })}</option>
            <option value="all">{formatMessage({ id: 'evaluation.all' })}</option>
          </select>
        </div>
      </div>
    </div>
  );
}
