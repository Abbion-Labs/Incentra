import type { Employee } from '../../../api/types';
import { EmptyState } from '../../../components/common/EmptyState';
import { useIntl } from '../../../i18n';

interface PlanningEmployeesTableProps {
  employees: Employee[];
  creatingFor: number | null;
  search: string;
  /** Bez njega (admin samo gleda) dugme za postavljanje ciljeva se ne prikazuje. */
  onStartPlanning?: (employeeId: number) => void;
  evaluationLabel: (employeeId: number) => string;
}

export function PlanningEmployeesTable({
  employees,
  creatingFor,
  search,
  onStartPlanning,
  evaluationLabel,
}: PlanningEmployeesTableProps) {
  const { formatMessage } = useIntl();
  if (employees.length === 0) {
    return (
      <EmptyState
        title={
          search.trim()
            ? formatMessage({ id: 'evaluation.noSearchResults' })
            : formatMessage({ id: 'evaluation.bucketEmpty.planningTitle' })
        }
        description={
          search.trim()
            ? formatMessage({ id: 'evaluation.tryDifferentSearch' })
            : formatMessage({
                id: 'evaluation.bucketEmpty.planningDescription',
              })
        }
      />
    );
  }

  return (
    <div className="table-wrap">
      <table className="table table--hover table--stack">
        <thead>
          <tr>
            <th className="col-text">
              {formatMessage({ id: 'admin.employees' })}
            </th>
            <th className="col-text">
              {formatMessage({ id: 'evaluation.orgUnitShort' })}
            </th>
            <th className="col-text">
              {formatMessage({ id: 'evaluation.jobPosition' })}
            </th>
            <th className="col-actions"></th>
          </tr>
        </thead>
        <tbody>
          {employees.map((emp) => (
            <tr key={emp.id}>
              <td className="cell-primary col-text stack-title">
                {emp.fullName}
              </td>
              <td
                className="cell-muted col-text"
                data-label={formatMessage({ id: 'evaluation.orgUnitShort' })}
              >
                {emp.organizationUnitName}
              </td>
              <td
                className="cell-muted col-text"
                data-label={formatMessage({ id: 'evaluation.jobPosition' })}
              >
                {emp.jobPositionName}
              </td>
              <td className="col-actions">
                {onStartPlanning && (
                  <button
                    type="button"
                    className="btn btn-primary btn-sm"
                    disabled={creatingFor === emp.id}
                    onClick={() => onStartPlanning(emp.id)}
                  >
                    {creatingFor === emp.id
                      ? formatMessage({ id: 'common.loading' })
                      : evaluationLabel(emp.id)}
                  </button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
