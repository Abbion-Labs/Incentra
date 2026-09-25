import type { KeyboardEvent } from 'react';
import type { Employee } from '../../../api/types';
import { EmptyState } from '../../../components/common/EmptyState';
import { PersonName } from '../../../components/employee/PersonName';
import { useIntl } from '../../../i18n';

interface PlanningEmployeesTableProps {
  employees: Employee[];
  creatingFor: number | null;
  search: string;
  /** Bez njega (admin samo gleda) redovi se ne mogu otvoriti za postavljanje ciljeva. */
  onStartPlanning?: (employeeId: number) => void;
}

export function PlanningEmployeesTable({
  employees,
  creatingFor,
  search,
  onStartPlanning,
}: PlanningEmployeesTableProps) {
  const { formatMessage } = useIntl();
  const busy = creatingFor !== null;

  // Klik na red pokreće postavljanje ciljeva; dok se jedna ocena kreira,
  // ostali redovi čekaju da se ne bi napravile dve odjednom.
  function startPlanning(employeeId: number) {
    if (!onStartPlanning || busy) return;
    onStartPlanning(employeeId);
  }

  function handleRowKeyDown(
    event: KeyboardEvent<HTMLTableRowElement>,
    employeeId: number,
  ) {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      startPlanning(employeeId);
    }
  }
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
      <table
        className={`table table--hover table--stack${onStartPlanning ? ' table--clickable' : ''}`}
      >
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
          </tr>
        </thead>
        <tbody>
          {employees.map((emp) => (
            <tr
              key={emp.id}
              className={creatingFor === emp.id ? 'table-row--busy' : undefined}
              aria-busy={creatingFor === emp.id || undefined}
              onClick={
                onStartPlanning ? () => startPlanning(emp.id) : undefined
              }
              onKeyDown={
                onStartPlanning
                  ? (event) => handleRowKeyDown(event, emp.id)
                  : undefined
              }
              tabIndex={onStartPlanning ? 0 : undefined}
              role={onStartPlanning ? 'button' : undefined}
              aria-label={
                onStartPlanning
                  ? `${formatMessage({ id: 'evaluation.setGoals' })}: ${emp.fullName}`
                  : undefined
              }
            >
              <td className="cell-primary col-text stack-title">
                <PersonName fullName={emp.fullName} avatarUrl={emp.avatarUrl} />
                {creatingFor === emp.id && (
                  <span className="table-row__busy-label">
                    {formatMessage({ id: 'common.loading' })}
                  </span>
                )}
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
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
