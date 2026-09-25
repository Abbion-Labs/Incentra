import type { KeyboardEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import type { EvaluationSummary } from '../../../api/types';
import { EmptyState } from '../../../components/common/EmptyState';
import { PersonName } from '../../../components/employee/PersonName';
import { useIntl } from '../../../i18n';
import {
  evaluationDisplayClass,
  evaluationDisplayLabel,
} from '../../../utils/evaluationBuckets';

interface SetGoalsTableProps {
  evaluations: EvaluationSummary[];
  search: string;
}

export function SetGoalsTable({ evaluations, search }: SetGoalsTableProps) {
  const { formatMessage } = useIntl();
  const navigate = useNavigate();

  function openGoals(id: number) {
    navigate(`/evaluator/goals/evaluations/${id}`);
  }

  function handleRowKeyDown(
    event: KeyboardEvent<HTMLTableRowElement>,
    id: number,
  ) {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      openGoals(id);
    }
  }

  if (evaluations.length === 0) {
    return (
      <EmptyState
        title={
          search.trim()
            ? formatMessage({ id: 'evaluation.noSearchResults' })
            : formatMessage({ id: 'evaluation.noGoalsSet' })
        }
        description={
          search.trim()
            ? formatMessage({ id: 'evaluation.tryDifferentSearch' })
            : formatMessage({ id: 'evaluation.goalsWillAppear' })
        }
      />
    );
  }

  return (
    <div className="table-wrap">
      <table className="table table--hover table--clickable table--stack">
        <thead>
          <tr>
            <th className="col-text">
              {formatMessage({ id: 'admin.employees' })}
            </th>
            <th className="col-meta">
              {formatMessage({ id: 'evaluation.period' })}
            </th>
            <th className="col-num">
              {formatMessage({ id: 'evaluation.goalsCount' })}
            </th>
            <th className="col-meta">
              {formatMessage({ id: 'evaluation.statusLabel' })}
            </th>
          </tr>
        </thead>
        <tbody>
          {evaluations.map((ev) => (
            <tr
              key={ev.id}
              className="table-row--navigable"
              onClick={() => openGoals(ev.id)}
              onKeyDown={(event) => handleRowKeyDown(event, ev.id)}
              tabIndex={0}
              role="link"
            >
              <td className="cell-primary col-text stack-title">
                <PersonName fullName={ev.employeeFullName} />
              </td>
              <td
                className="cell-muted col-meta"
                data-label={formatMessage({ id: 'evaluation.period' })}
              >
                Q{ev.quarter}/{ev.year}
              </td>
              <td
                className="cell-muted col-num"
                data-label={formatMessage({ id: 'evaluation.goalsCount' })}
              >
                {ev.goalCount ?? 0}
              </td>
              <td
                className="col-meta"
                data-label={formatMessage({ id: 'evaluation.statusLabel' })}
              >
                <span className={evaluationDisplayClass(ev)}>
                  {evaluationDisplayLabel(ev, formatMessage)}
                </span>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
