import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../../api/client';
import type { ControllerEvaluatorSummary } from '../../api/types';
import { AlertMessages } from '../../components/common/AlertMessages';
import { EmptyState } from '../../components/common/EmptyState';
import { TableSkeleton } from '../../components/common/LoadingSkeleton';
import { AppLayout } from '../../components/AppLayout';
import { useDebouncedSearch } from '../../hooks/useDebouncedSearch';
import { EmployeeAvatar } from '../../components/employee/EmployeeAvatar';
import { useIntl } from '../../i18n';

export function ControllerEvaluatorsPage() {
  const { formatMessage } = useIntl();
  const navigate = useNavigate();
  const [evaluators, setEvaluators] = useState<ControllerEvaluatorSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const { input: searchInput, debounced: search, setInput: setSearchInput } = useDebouncedSearch();

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const items = await api.get<ControllerEvaluatorSummary[]>('/api/evaluator-settings/my-evaluators');
      setEvaluators(items);
    } catch (e) {
      setError(e instanceof Error ? e.message : formatMessage({ id: 'errors.loadFailed' }));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const filtered = evaluators.filter((ev) => {
    if (!search.trim()) return true;
    const q = search.trim().toLowerCase();
    return (
      ev.employeeFullName.toLowerCase().includes(q)
      || ev.organizationUnitName.toLowerCase().includes(q)
      || ev.jobPositionName.toLowerCase().includes(q)
    );
  });

  return (
    <AppLayout title={formatMessage({ id: 'navigation.controllerEvaluators' })}>
      <div className="card card--filter">
        <div className="form-row filter-bar-search">
          <label htmlFor="evaluator-search">{formatMessage({ id: 'controller.searchEvaluators' })}</label>
          <input
            id="evaluator-search"
            type="search"
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            placeholder={formatMessage({ id: 'controller.evaluatorsSearchPlaceholder' })}
          />
        </div>
      </div>

      <AlertMessages error={error} />

      <div className="card card--flush">
        {loading ? (
          <TableSkeleton rows={4} columns={4} />
        ) : filtered.length === 0 ? (
          <EmptyState
            title={search.trim() ? formatMessage({ id: 'evaluation.noSearchResults' }) : formatMessage({ id: 'controller.noEvaluators' })}
            description={
              search.trim()
                ? formatMessage({ id: 'controller.searchTryDifferentEvaluator' })
                : formatMessage({ id: 'controller.noEvaluatorsAssigned' })
            }
          />
        ) : (
          <div className="table-wrap">
            <table className="table table--hover table--clickable">
              <thead>
                <tr>
                  <th className="col-text">{formatMessage({ id: 'admin.evaluators' })}</th>
                  <th className="col-text">{formatMessage({ id: 'evaluation.orgUnitShort' })}</th>
                  <th className="col-text">{formatMessage({ id: 'evaluation.jobPosition' })}</th>
                  <th className="col-num">{formatMessage({ id: 'controller.subordinates' })}</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map((ev) => (
                  <tr
                    key={ev.employeeId}
                    onClick={() => navigate(`/controller/evaluators/${ev.employeeId}/analytics`)}
                    tabIndex={0}
                  >
                    <td className="cell-primary col-text">
                      <span className="employee-list-name">
                        <EmployeeAvatar
                          employee={{
                            fullName: ev.employeeFullName,
                            firstName: '',
                            lastName: '',
                            avatarUrl: ev.avatarUrl,
                          }}
                          size="sm"
                        />
                        <span>{ev.employeeFullName}</span>
                      </span>
                    </td>
                    <td className="cell-muted col-text">{ev.organizationUnitName || '—'}</td>
                    <td className="cell-muted col-text">{ev.jobPositionName || '—'}</td>
                    <td className="cell-muted col-num">{ev.subordinateCount}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </AppLayout>
  );
}
