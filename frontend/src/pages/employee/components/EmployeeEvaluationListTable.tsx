import type { KeyboardEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import type { EvaluationSummary } from '../../../api/types';
import { AverageDisplay } from '../../../components/evaluation/AverageDisplay';
import { DescriptiveRatingBadge } from '../../../components/evaluation/DescriptiveRatingBadge';
import { useIntl } from '../../../i18n';
import {
  formatSummaryAverage,
  summaryDescriptiveRatingName,
} from '../../../utils/scoring';

interface EmployeeEvaluationListTableProps {
  evaluations: EvaluationSummary[];
}

export function EmployeeEvaluationListTable({
  evaluations,
}: EmployeeEvaluationListTableProps) {
  const { formatMessage } = useIntl();
  const navigate = useNavigate();

  function openEvaluation(id: number) {
    navigate(`/employee/evaluations/${id}`);
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
    <div className="table-wrap">
      <table className="table table--hover table--clickable">
        <thead>
          <tr>
            <th className="col-meta">
              {formatMessage({ id: 'evaluation.period' })}
            </th>
            <th className="col-text">
              {formatMessage({ id: 'evaluation.evaluator' })}
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
          {evaluations.map((ev) => (
            <tr
              key={ev.id}
              className="table-row--navigable"
              onClick={() => openEvaluation(ev.id)}
              onKeyDown={(event) => handleRowKeyDown(event, ev.id)}
              tabIndex={0}
              role="link"
            >
              <td className="cell-muted col-meta">
                Q{ev.quarter}/{ev.year}
              </td>
              <td className="cell-primary col-text">{ev.evaluatorFullName}</td>
              <td className="col-num">
                <AverageDisplay value={formatSummaryAverage(ev)} />
              </td>
              <td className="col-meta">
                <DescriptiveRatingBadge
                  name={summaryDescriptiveRatingName(ev)}
                />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
