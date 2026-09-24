import { useNavigate } from 'react-router-dom';

import type { Employee } from '../../../api/types';

import { EmployeeAvatar } from '../../../components/employee/EmployeeAvatar';

import { EmptyState } from '../../../components/common/EmptyState';

import { TableSkeleton } from '../../../components/common/LoadingSkeleton';

import { useIntl } from '../../../i18n';

interface EmployeeListTableProps {
  employees: Employee[];

  loading: boolean;

  search: string;

  profilePath?: (employeeId: number) => string;

  emptyDescriptionKey?: string;
}

export function EmployeeListTable({
  employees,

  loading,

  search,

  profilePath = (id) => `/evaluator/employees/${id}`,

  emptyDescriptionKey = 'evaluation.noEmployeesEvaluator',
}: EmployeeListTableProps) {
  const navigate = useNavigate();

  const { formatMessage } = useIntl();

  if (loading) {
    return <TableSkeleton rows={6} columns={5} />;
  }

  if (employees.length === 0) {
    return (
      <EmptyState
        title={
          search.trim()
            ? formatMessage({ id: 'evaluation.noSearchResults' })
            : formatMessage({ id: 'evaluation.noEmployees' })
        }

        description={
          search.trim()
            ? formatMessage({ id: 'evaluation.tryDifferentEmployeeSearch' })
            : formatMessage({ id: emptyDescriptionKey as never })
        }
      />
    );
  }

  return (
    <div className="table-wrap">
      <table className="table table--hover table--clickable table--stack table--employees">
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

            <th className="col-text">
              {formatMessage({ id: 'common.education' })}
            </th>

            <th className="col-meta">
              {formatMessage({ id: 'evaluation.status' })}
            </th>
          </tr>
        </thead>

        <tbody>
          {employees.map((emp) => (
            <tr
              key={emp.id}
              onClick={() => navigate(profilePath(emp.id))}
              onKeyDown={(event) => {
                if (event.key === 'Enter' || event.key === ' ') {
                  event.preventDefault();
                  navigate(profilePath(emp.id));
                }
              }}
              tabIndex={0}
              role="link"
            >
              <td className="cell-primary col-text stack-title">
                <span className="employee-list-name">
                  <EmployeeAvatar employee={emp} size="sm" />

                  <span>{emp.fullName}</span>
                </span>
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

              <td
                className="cell-muted col-text"
                data-label={formatMessage({ id: 'common.education' })}
              >
                {emp.educationLevelName ??
                  formatMessage({ id: 'common.emptyValue' })}
              </td>

              <td
                className="col-meta"
                data-label={formatMessage({ id: 'evaluation.status' })}
              >
                <span
                  className={`badge ${emp.isActive ? 'badge-approved' : 'badge-draft'}`}
                >
                  {emp.isActive
                    ? formatMessage({ id: 'common.active' })
                    : formatMessage({ id: 'common.inactive' })}
                </span>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
