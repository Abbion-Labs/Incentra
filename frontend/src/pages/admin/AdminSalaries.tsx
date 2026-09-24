import {
  Fragment,
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react';

import { api } from '../../api/client';

import { fetchAllPages } from '../../api/paged';

import type { EmployeeSalary, EmployeeSalaryOption } from '../../api/types';

import { InfiniteScrollSentinel } from '../../components/common/InfiniteScrollSentinel';

import { useDebouncedSearch, usePagedList, useToast } from '../../hooks';

import { useIntl } from '../../i18n';
import { isEditConflict } from '../../utils/editConflict';
import { formatNumber } from '../../utils/formatLocale';

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

function formatTableAmount(value: number): string {
  return formatNumber(value, 0, 0);
}

function monthlySalary(points: number, salaryPerPoint: number): number {
  return points * salaryPerPoint;
}

function formatDisplayDate(iso: string): string {
  const [year, month, day] = iso.slice(0, 10).split('-');

  if (!year || !month || !day) return iso;

  return `${day}.${month}.${year}`;
}

function formatPeriod(from: string, to: string | null): string {
  return to
    ? `${formatDisplayDate(from)} – ${formatDisplayDate(to)}`
    : `${formatDisplayDate(from)} – danas`;
}

export function AdminSalaries() {
  const { formatMessage } = useIntl();
  const toast = useToast();

  const {
    input: search,
    debounced: debouncedSearch,
    setInput: setSearch,
  } = useDebouncedSearch();

  const [withoutSalary, setWithoutSalary] = useState<EmployeeSalaryOption[]>(
    [],
  );

  const [loadingWithoutSalary, setLoadingWithoutSalary] = useState(false);

  const [saving, setSaving] = useState(false);

  const [showAdd, setShowAdd] = useState(false);

  const [addEmployeeId, setAddEmployeeId] = useState('');

  const [addPoints, setAddPoints] = useState('');

  const [addSalaryPerPoint, setAddSalaryPerPoint] = useState('');

  const [addEffectiveFrom, setAddEffectiveFrom] = useState(todayIso());

  const [editingId, setEditingId] = useState<number | null>(null);
  // Verzija važeće plate sa kojom je izmena otvorena; šalje se uz izmenu.
  const [editingVersion, setEditingVersion] = useState<number | null>(null);

  const [editPoints, setEditPoints] = useState('');

  const [editSalaryPerPoint, setEditSalaryPerPoint] = useState('');

  const [editEffectiveFrom, setEditEffectiveFrom] = useState('');
  // Datum važeće plate koja se menja: isti datum je ispravka, kasniji nova plata.
  const [editCurrentFrom, setEditCurrentFrom] = useState('');

  const [historyEmployeeId, setHistoryEmployeeId] = useState<number | null>(
    null,
  );

  const [history, setHistory] = useState<EmployeeSalary[]>([]);

  const [historyLoading, setHistoryLoading] = useState(false);

  // Brzi klikovi na „Istorija“ pokreću više zahteva; samo poslednji sme da upiše istoriju.
  const historyRequestRef = useRef(0);

  const listQueryKey = debouncedSearch;

  const {
    items: salaries,

    totalCount,

    loading,

    loadingMore,

    hasMore,

    loadMore,

    reload,

    error: listError,
  } = usePagedList<EmployeeSalary>({
    queryKey: listQueryKey,

    fetchPage: (page, pageSize) => {
      const params = new URLSearchParams({
        page: String(page),

        pageSize: String(pageSize),
      });

      if (debouncedSearch.trim()) {
        params.set('search', debouncedSearch.trim());
      }

      return `/api/employee-salaries?${params}`;
    },
  });

  const displayCurrency = useMemo(
    () => salaries.find((row) => row.currency)?.currency ?? 'RSD',

    [salaries],
  );

  const loadWithoutSalaryOptions = useCallback(async () => {
    setLoadingWithoutSalary(true);

    try {
      const options = await fetchAllPages<EmployeeSalaryOption>(
        (page, pageSize) =>
          `/api/employee-salaries/employees-without-salary?page=${page}&pageSize=${pageSize}`,
      );

      setWithoutSalary(options);
    } catch (e) {
      toast.error(
        e instanceof Error
          ? e.message
          : formatMessage({ id: 'errors.loadEmployeesWithoutSalaryFailed' }),
      );

      setWithoutSalary([]);
    } finally {
      setLoadingWithoutSalary(false);
    }
  }, [formatMessage, toast]);

  useEffect(() => {
    if (showAdd) {
      void loadWithoutSalaryOptions();
    }
  }, [showAdd, loadWithoutSalaryOptions]);

  useEffect(() => {
    if (listError) toast.error(listError);
  }, [listError, toast]);

  function closeHistory() {
    historyRequestRef.current += 1;

    setHistoryEmployeeId(null);

    setHistory([]);

    setHistoryLoading(false);
  }

  async function loadHistory(employeeId: number) {
    if (historyEmployeeId === employeeId) {
      closeHistory();

      return;
    }

    const requestId = ++historyRequestRef.current;

    const isLatest = () => requestId === historyRequestRef.current;

    setHistoryEmployeeId(employeeId);

    setHistoryLoading(true);

    try {
      const rows = await api.get<EmployeeSalary[]>(
        `/api/employee-salaries/${employeeId}/history`,
      );

      if (!isLatest()) return;

      setHistory(rows);
    } catch (e) {
      if (!isLatest()) return;

      toast.error(
        e instanceof Error
          ? e.message
          : formatMessage({ id: 'errors.salaryHistoryLoadFailed' }),
      );

      setHistoryEmployeeId(null);

      setHistory([]);
    } finally {
      if (isLatest()) setHistoryLoading(false);
    }
  }

  async function saveSalary(
    employeeId: number,

    points: number,

    salaryPerPoint: number,

    effectiveFrom: string,

    /** Verzija važeće plate; null kada zaposleni još nema platu. */
    version: number | null,
  ) {
    setSaving(true);

    try {
      const saved = await api.put<EmployeeSalary>(
        `/api/employee-salaries/${employeeId}`,
        {
          points,

          salaryPerPoint,

          effectiveFrom,

          currency: 'RSD',

          version,
        },
      );

      const isCorrection =
        version !== null && effectiveFrom === editCurrentFrom;
      toast.success(
        formatMessage({
          id: isCorrection
            ? 'alerts.salaryCorrected'
            : 'alerts.salarySavedWithHistory',
        }),
      );
      warnAboutCompensation(saved);

      setShowAdd(false);

      setEditingId(null);

      setAddEmployeeId('');

      setAddPoints('');

      setAddSalaryPerPoint('');

      setAddEffectiveFrom(todayIso());

      closeHistory();

      setWithoutSalary([]);

      reload();
    } catch (e) {
      if (isEditConflict(e)) {
        toast.warning(formatMessage({ id: 'errors.recordChangedMeanwhile' }));
        cancelEdit();
        setShowAdd(false);
        closeHistory();
        setWithoutSalary([]);
        reload();
        return;
      }
      toast.error(
        e instanceof Error
          ? e.message
          : formatMessage({ id: 'errors.saveFailed' }),
      );
    } finally {
      setSaving(false);
    }
  }

  /** Ispravka plate koja je već ušla u obračun varijabile traži ponovni obračun. */
  function warnAboutCompensation(saved: EmployeeSalary) {
    const toRecalculate = saved.compensationYearsToRecalculate ?? [];
    const finalized = saved.finalizedCompensationYears ?? [];
    if (toRecalculate.length > 0) {
      toast.warning(
        formatMessage(
          { id: 'alerts.salaryCorrectionRecalculate' },
          { years: toRecalculate.join(', ') },
        ),
      );
    }
    if (finalized.length > 0) {
      toast.warning(
        formatMessage(
          { id: 'alerts.salaryCorrectionFinalized' },
          { years: finalized.join(', ') },
        ),
      );
    }
  }

  function startEdit(row: EmployeeSalary) {
    setEditingId(row.employeeId);

    setEditingVersion(row.version);

    setEditCurrentFrom(row.effectiveFrom);

    setEditPoints(String(row.points));

    setEditSalaryPerPoint(String(row.salaryPerPoint));

    setEditEffectiveFrom(todayIso());

    setShowAdd(false);
  }

  function cancelEdit() {
    setEditingId(null);

    setEditingVersion(null);

    setEditCurrentFrom('');

    setEditPoints('');

    setEditSalaryPerPoint('');

    setEditEffectiveFrom('');
  }

  async function handleAdd(e: React.FormEvent) {
    e.preventDefault();

    const points = Number(addPoints);

    const salaryPerPoint = Number(addSalaryPerPoint);

    if (
      !addEmployeeId ||
      !points ||
      points <= 0 ||
      points > 1000 ||
      !salaryPerPoint ||
      salaryPerPoint <= 0
    ) {
      toast.warning(
        formatMessage({ id: 'errors.invalidSalaryEmployeeAndPoints' }),
      );

      return;
    }

    await saveSalary(
      Number(addEmployeeId),
      points,
      salaryPerPoint,
      addEffectiveFrom,
      // Dodaje se prva plata: nema prethodne verzije.
      null,
    );
  }

  async function handleEditSubmit(e: React.FormEvent, employeeId: number) {
    e.preventDefault();

    const points = Number(editPoints);

    const salaryPerPoint = Number(editSalaryPerPoint);

    if (
      !points ||
      points <= 0 ||
      points > 1000 ||
      !salaryPerPoint ||
      salaryPerPoint <= 0
    ) {
      toast.warning(formatMessage({ id: 'errors.invalidSalaryPoints' }));

      return;
    }

    await saveSalary(
      employeeId,
      points,
      salaryPerPoint,
      editEffectiveFrom,
      editingVersion,
    );
  }

  return (
    <div className="card">
      {showAdd && (
        <form
          className="admin-form card card--nested"
          onSubmit={handleAdd}
          style={{ marginBottom: '1rem' }}
        >
          {loadingWithoutSalary ? (
            <div className="empty">
              {formatMessage({ id: 'admin.salaries.loadingEmployees' })}
            </div>
          ) : withoutSalary.length === 0 ? (
            <div className="empty">
              {formatMessage({ id: 'admin.salaries.allEmployeesHaveSalary' })}
            </div>
          ) : (
            <div className="form-grid">
              <div className="form-row">
                <label htmlFor="salary-employee">
                  {formatMessage({ id: 'admin.employees' })}
                </label>

                <select
                  id="salary-employee"

                  value={addEmployeeId}

                  onChange={(e) => setAddEmployeeId(e.target.value)}

                  required
                >
                  <option value="">
                    {formatMessage({ id: 'common.selectPlaceholder' })}
                  </option>

                  {withoutSalary.map((o) => (
                    <option key={o.employeeId} value={o.employeeId}>
                      {o.fullName} ({o.organizationUnitName})
                    </option>
                  ))}
                </select>
              </div>

              <div className="form-row">
                <label htmlFor="salary-points">
                  {formatMessage({ id: 'admin.salaries.points' })}
                </label>

                <input
                  id="salary-points"

                  type="number"

                  min="1"

                  max="1000"

                  step="1"

                  value={addPoints}

                  onChange={(e) => setAddPoints(e.target.value)}

                  required
                />
              </div>

              <div className="form-row">
                <label htmlFor="salary-per-point">
                  {formatMessage({
                    id: 'admin.salaries.earningsPerPointCurrency',
                  })}
                </label>

                <input
                  id="salary-per-point"

                  type="number"

                  min="1"

                  step="1"

                  value={addSalaryPerPoint}

                  onChange={(e) => setAddSalaryPerPoint(e.target.value)}

                  required
                />
              </div>

              <div className="form-row">
                <label htmlFor="salary-effective">
                  {formatMessage({ id: 'common.validFrom' })}
                </label>

                <input
                  id="salary-effective"

                  type="date"

                  value={addEffectiveFrom}

                  onChange={(e) => setAddEffectiveFrom(e.target.value)}

                  required
                />
              </div>
            </div>
          )}

          <div className="form-actions">
            <button
              type="submit"
              className="btn btn-primary"
              disabled={
                saving || loadingWithoutSalary || withoutSalary.length === 0
              }
            >
              {saving
                ? formatMessage({ id: 'buttons.saving' })
                : formatMessage({ id: 'buttons.save' })}
            </button>

            <button
              type="button"
              className="btn btn-secondary"
              onClick={() => setShowAdd(false)}
              disabled={saving}
            >
              {formatMessage({ id: 'buttons.cancel' })}
            </button>
          </div>
        </form>
      )}

      {!loading && (totalCount > 0 || !showAdd) && (
        <div
          className="filter-bar admin-filters"
          style={{ marginBottom: '1rem' }}
        >
          {totalCount > 0 ? (
            <div className="form-row filter-bar-search">
              <label htmlFor="salary-search">
                {formatMessage({ id: 'common.search' })}
              </label>
              <input
                id="salary-search"
                type="search"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder={formatMessage({
                  id: 'admin.salaries.searchPlaceholder',
                })}
              />
            </div>
          ) : null}
          {!showAdd ? (
            <div className="filter-bar__actions">
              <button
                type="button"
                className="btn btn-primary btn-sm"
                onClick={() => {
                  setShowAdd(true);
                  cancelEdit();
                }}
              >
                {formatMessage({ id: 'admin.salaries.addSalary' })}
              </button>
            </div>
          ) : null}
        </div>
      )}

      {loading ? (
        <div className="empty">{formatMessage({ id: 'common.loading' })}</div>
      ) : totalCount === 0 ? (
        <div className="empty">
          {formatMessage({ id: 'admin.salaries.noSalaries' })}
        </div>
      ) : (
        <>
          <div className="salaries-table">
            <div className="salaries-table__meta">
              <span className="table-currency-badge">{displayCurrency}</span>
            </div>

            <div className="table-wrap">
              <table className="table table--salaries">
                <thead>
                  <tr>
                    <th className="table-col table-col--text">
                      {formatMessage({ id: 'admin.employees' })}
                    </th>

                    <th className="table-col table-col--text">
                      {formatMessage({ id: 'evaluation.orgUnitShort' })}
                    </th>

                    <th className="table-col table-col--num">
                      {formatMessage({ id: 'admin.salaries.points' })}
                    </th>

                    <th className="table-col table-col--amount">
                      {formatMessage({ id: 'admin.salaries.earningsPerPoint' })}
                    </th>

                    <th className="table-col table-col--amount">
                      {formatMessage({ id: 'common.salary' })}
                    </th>

                    <th className="table-col table-col--period">
                      {formatMessage({ id: 'evaluation.period' })}
                    </th>

                    <th className="table-col col-actions">
                      {formatMessage({ id: 'admin.actions' })}
                    </th>
                  </tr>
                </thead>

                <tbody>
                  {salaries.map((row) => (
                    <Fragment key={row.employeeId}>
                      <tr>
                        {editingId === row.employeeId ? (
                          <>
                            <td className="table-col table-col--text">
                              {row.employeeFullName}
                            </td>

                            <td className="table-col table-col--text">
                              {row.organizationUnitName}
                            </td>

                            <td
                              className="table-col table-col--edit"
                              colSpan={4}
                            >
                              <form
                                className="inline-form"
                                onSubmit={(e) =>
                                  handleEditSubmit(e, row.employeeId)
                                }
                              >
                                <input
                                  type="number"

                                  min="1"

                                  max="1000"

                                  step="1"

                                  value={editPoints}

                                  onChange={(e) =>
                                    setEditPoints(e.target.value)
                                  }

                                  required

                                  style={{ maxWidth: '6rem' }}

                                  title={formatMessage({
                                    id: 'admin.salaries.points',
                                  })}
                                />

                                <input
                                  type="number"

                                  min="1"

                                  step="1"

                                  value={editSalaryPerPoint}

                                  onChange={(e) =>
                                    setEditSalaryPerPoint(e.target.value)
                                  }

                                  required

                                  style={{ maxWidth: '8rem' }}

                                  title={formatMessage({
                                    id: 'admin.salaries.earningsPerPoint',
                                  })}
                                />

                                <input
                                  type="date"

                                  value={editEffectiveFrom}

                                  onChange={(e) =>
                                    setEditEffectiveFrom(e.target.value)
                                  }

                                  required

                                  title={formatMessage({
                                    id: 'admin.salaries.validFromAfterCurrentPeriod',
                                  })}
                                />

                                <button
                                  type="submit"
                                  className="btn btn-primary btn-sm"
                                  disabled={saving}
                                >
                                  {formatMessage({ id: 'buttons.save' })}
                                </button>

                                <button
                                  type="button"
                                  className="btn btn-secondary btn-sm"
                                  onClick={cancelEdit}
                                  disabled={saving}
                                >
                                  {formatMessage({ id: 'buttons.cancel' })}
                                </button>
                              </form>
                            </td>

                            <td className="table-col col-actions" />
                          </>
                        ) : (
                          <>
                            <td className="table-col table-col--text">
                              {row.employeeFullName}
                            </td>

                            <td className="table-col table-col--text">
                              {row.organizationUnitName}
                            </td>

                            <td className="table-col table-col--num">
                              {row.points}
                            </td>

                            <td className="table-col table-col--amount">
                              {formatTableAmount(row.salaryPerPoint)}
                            </td>

                            <td className="table-col table-col--amount">
                              {formatTableAmount(
                                monthlySalary(row.points, row.salaryPerPoint),
                              )}
                            </td>

                            <td className="table-col table-col--period">
                              {formatPeriod(row.effectiveFrom, row.effectiveTo)}
                            </td>

                            <td className="table-col col-actions">
                              <div className="col-actions__group">
                                <button
                                  type="button"
                                  className="btn btn-secondary btn-sm"
                                  onClick={() => startEdit(row)}
                                >
                                  Novi unos
                                </button>

                                <button
                                  type="button"

                                  className="btn btn-secondary btn-sm"

                                  onClick={() => loadHistory(row.employeeId)}
                                >
                                  {historyEmployeeId === row.employeeId
                                    ? 'Sakrij'
                                    : 'Istorija'}
                                </button>
                              </div>
                            </td>
                          </>
                        )}
                      </tr>

                      {historyEmployeeId === row.employeeId && (
                        <tr key={`${row.employeeId}-history`}>
                          <td colSpan={7}>
                            {historyLoading ? (
                              <div className="empty">
                                {formatMessage({
                                  id: 'admin.salaries.loadingHistory',
                                })}
                              </div>
                            ) : (
                              <div className="table-wrap">
                                <table className="table table--nested">
                                  <thead>
                                    <tr>
                                      <th className="table-col table-col--num">
                                        {formatMessage({
                                          id: 'admin.salaries.points',
                                        })}
                                      </th>

                                      <th className="table-col table-col--amount">
                                        {formatMessage({
                                          id: 'admin.salaries.earningsPerPoint',
                                        })}
                                      </th>

                                      <th className="table-col table-col--amount">
                                        {formatMessage({ id: 'common.salary' })}
                                      </th>

                                      <th className="table-col table-col--period">
                                        {formatMessage({
                                          id: 'evaluation.period',
                                        })}
                                      </th>

                                      <th className="table-col table-col--status">
                                        {formatMessage({
                                          id: 'evaluation.status',
                                        })}
                                      </th>
                                    </tr>
                                  </thead>

                                  <tbody>
                                    {history.map((h) => (
                                      <tr key={h.id}>
                                        <td className="table-col table-col--num">
                                          {h.points}
                                        </td>

                                        <td className="table-col table-col--amount">
                                          {formatTableAmount(h.salaryPerPoint)}
                                        </td>

                                        <td className="table-col table-col--amount">
                                          {formatTableAmount(
                                            monthlySalary(
                                              h.points,
                                              h.salaryPerPoint,
                                            ),
                                          )}
                                        </td>

                                        <td className="table-col table-col--period">
                                          {formatPeriod(
                                            h.effectiveFrom,
                                            h.effectiveTo,
                                          )}
                                        </td>

                                        <td className="table-col table-col--status">
                                          {h.isCurrent
                                            ? formatMessage({
                                                id: 'admin.salaries.historyCurrent',
                                              })
                                            : formatMessage({
                                                id: 'admin.salaries.historyArchive',
                                              })}
                                        </td>
                                      </tr>
                                    ))}
                                  </tbody>
                                </table>
                              </div>
                            )}
                          </td>
                        </tr>
                      )}
                    </Fragment>
                  ))}
                </tbody>
              </table>
            </div>

            <InfiniteScrollSentinel
              hasMore={hasMore}

              isLoading={loadingMore}

              onLoadMore={loadMore}
            />
          </div>
        </>
      )}
    </div>
  );
}
