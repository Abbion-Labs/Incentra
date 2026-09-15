import type { EmployeeQuarterBenchmark } from '../../../api/types';
import { AverageDisplay } from '../../../components/evaluation/AverageDisplay';
import { useIntl } from '../../../i18n';
import { statusClass, statusLabel } from '../../../utils/status';

interface EmployeeQuarterListProps {
  quarters: EmployeeQuarterBenchmark[];
  selectedEvaluationId: number | null;
  onSelect: (evaluationId: number) => void;
}

function QuarterStatusBadge({ quarter }: { quarter: EmployeeQuarterBenchmark }) {
  const { formatMessage } = useIntl();
  if (quarter.status === 'Draft') {
    if (quarter.hasIncompleteRatings) {
      return <span className="badge badge-unrated">{formatMessage({ id: 'status.Draft' })}</span>;
    }

    return <span className="badge badge-planning">{formatMessage({ id: 'evaluation.bucket.planning' })}</span>;
  }

  return <span className={statusClass(quarter.status)}>{statusLabel(quarter.status, formatMessage)}</span>;
}

export function EmployeeQuarterList({ quarters, selectedEvaluationId, onSelect }: EmployeeQuarterListProps) {
  const { formatMessage } = useIntl();
  if (quarters.length === 0) {
    return <p className="empty-inline">{formatMessage({ id: 'evaluation.employeeNoEvaluations' })}</p>;
  }

  return (
    <ul className="quarter-list">
      {quarters.map((q) => {
        const active = q.evaluationId === selectedEvaluationId;
        return (
          <li key={q.evaluationId}>
            <button
              type="button"
              className={`quarter-list__item ${active ? 'quarter-list__item--active' : ''}`}
              onClick={() => onSelect(q.evaluationId)}
            >
              <div className="quarter-list__period">
                <strong>Q{q.quarter}/{q.year}</strong>
                <QuarterStatusBadge quarter={q} />
              </div>
              <div className="quarter-list__meta">
                {formatMessage({ id: 'evaluation.average' })}:{' '}
                <AverageDisplay
                  value={q.hasIncompleteRatings || q.employeeAverage == null ? '/' : q.employeeAverage.toFixed(2)}
                />
              </div>
            </button>
          </li>
        );
      })}
    </ul>
  );
}
