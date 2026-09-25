import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../../api/client';
import type { ControllerEvaluatorSummary } from '../../api/types';
import { EmptyState } from '../../components/common/EmptyState';
import { TableSkeleton } from '../../components/common/LoadingSkeleton';
import { AppLayout } from '../../components/AppLayout';
import { useDebouncedSearch, useToast } from '../../hooks';
import { matchesNameSearch, normalizeSearchTerm } from '../../utils/nameSearch';
import { PersonName } from '../../components/employee/PersonName';
import { ToolbarSearch } from '../../components/common/ToolbarSearch';
import { useIntl } from '../../i18n';

export function ControllerEvaluatorsPage() {
  const { formatMessage } = useIntl();
  const toast = useToast();
  const navigate = useNavigate();
  const [evaluators, setEvaluators] = useState<ControllerEvaluatorSummary[]>(
    [],
  );
  const [loading, setLoading] = useState(true);
  const {
    input: searchInput,
    debounced: search,
    setInput: setSearchInput,
  } = useDebouncedSearch();

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const items = await api.get<ControllerEvaluatorSummary[]>(
        '/api/evaluator-settings/my-evaluators',
      );
      setEvaluators(items);
    } catch (e) {
      toast.error(
        e instanceof Error
          ? e.message
          : formatMessage({ id: 'errors.loadFailed' }),
      );
    } finally {
      setLoading(false);
    }
  }, [formatMessage, toast]);

  useEffect(() => {
    load();
  }, [load]);

  const filtered = evaluators.filter((ev) => {
    const q = normalizeSearchTerm(search);
    if (!q) return true;
    return (
      matchesNameSearch(ev.employeeFullName, search) ||
      ev.organizationUnitName.toLowerCase().includes(q) ||
      ev.jobPositionName.toLowerCase().includes(q)
    );
  });

  return (
    <AppLayout title={formatMessage({ id: 'navigation.controllerEvaluators' })}>
      <div className="card card--flush data-panel">
        <div className="data-panel__toolbar">
          <ToolbarSearch
            id="evaluator-search"
            label={formatMessage({ id: 'controller.searchEvaluators' })}
            placeholder={formatMessage({
              id: 'controller.evaluatorsSearchPlaceholder',
            })}
            value={searchInput}
            onChange={setSearchInput}
          />
        </div>
        {loading ? (
          <TableSkeleton rows={4} columns={4} />
        ) : filtered.length === 0 ? (
          <EmptyState
            title={
              search.trim()
                ? formatMessage({ id: 'evaluation.noSearchResults' })
                : formatMessage({ id: 'controller.noEvaluators' })
            }
            description={
              search.trim()
                ? formatMessage({
                    id: 'controller.searchTryDifferentEvaluator',
                  })
                : formatMessage({ id: 'controller.noEvaluatorsAssigned' })
            }
          />
        ) : (
          <div className="table-wrap">
            <table className="table table--hover table--clickable">
              <thead>
                <tr>
                  <th className="col-text">
                    {formatMessage({ id: 'admin.evaluators' })}
                  </th>
                  <th className="col-text">
                    {formatMessage({ id: 'evaluation.orgUnitShort' })}
                  </th>
                  <th className="col-text">
                    {formatMessage({ id: 'evaluation.jobPosition' })}
                  </th>
                  <th className="col-num">
                    {formatMessage({ id: 'controller.subordinates' })}
                  </th>
                </tr>
              </thead>
              <tbody>
                {filtered.map((ev) => (
                  <tr
                    key={ev.employeeId}
                    onClick={() =>
                      navigate(
                        `/controller/evaluators/${ev.employeeId}/analytics`,
                      )
                    }
                    onKeyDown={(event) => {
                      if (event.key === 'Enter' || event.key === ' ') {
                        event.preventDefault();
                        navigate(
                          `/controller/evaluators/${ev.employeeId}/analytics`,
                        );
                      }
                    }}
                    tabIndex={0}
                    role="link"
                  >
                    <td className="cell-primary col-text">
                      <PersonName
                        fullName={ev.employeeFullName}
                        avatarUrl={ev.avatarUrl}
                      />
                    </td>
                    <td className="cell-muted col-text">
                      {ev.organizationUnitName || '—'}
                    </td>
                    <td className="cell-muted col-text">
                      {ev.jobPositionName || '—'}
                    </td>
                    <td className="cell-muted col-num">
                      {ev.subordinateCount}
                    </td>
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
