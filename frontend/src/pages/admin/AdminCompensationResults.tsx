import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../../api/client';
import { fetchAllPages } from '../../api/paged';
import type { CompensationCalculationStatus, CompensationParameters, CompensationResult, OrganizationUnit } from '../../api/types';
import { InfiniteScrollSentinel } from '../../components/common/InfiniteScrollSentinel';
import { downloadCsv } from '../../utils/downloadCsv';
import { currentYear } from '../../utils/status';
import { usePagedList, useToast } from '../../hooks';
import { useIntl } from '../../i18n';
import { formatDateTime, formatNumber, formatPercent } from '../../utils/formatLocale';

function formatTableAmount(value: number): string {
  return formatNumber(value, 0, 0);
}

/** CSV amounts without decimals and with space thousand separators. */
function formatCsvAmount(value: number): string {
  return Math.round(value)
    .toString()
    .replace(/\B(?=(\d{3})+(?!\d))/g, ' ');
}

export function AdminCompensationResults() {
  const { formatMessage } = useIntl();
  const toast = useToast();
  const [orgUnits, setOrgUnits] = useState<OrganizationUnit[]>([]);
  const [organizationUnitId, setOrganizationUnitId] = useState('');
  const [year, setYear] = useState(String(currentYear));
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [currency, setCurrency] = useState('RSD');
  const [parametersId, setParametersId] = useState<number | null>(null);
  const [calculationStatus, setCalculationStatus] = useState<CompensationCalculationStatus | null>(null);
  const [exporting, setExporting] = useState(false);
  const [finalizing, setFinalizing] = useState(false);
  const yearOptions = useMemo(() => {
    const base = currentYear;
    return [base - 1, base, base + 1];
  }, []);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(search.trim()), 300);
    return () => clearTimeout(timer);
  }, [search]);

  const listQueryKey = `${organizationUnitId}|${year}|${debouncedSearch}|${parametersId ?? 'none'}`;
  const listEnabled = Boolean(organizationUnitId && year);

  const {
    items: results,
    totalCount,
    loading,
    loadingMore,
    hasMore,
    loadMore,
    error: listError,
    setError: setListError,
  } = usePagedList<CompensationResult>({
    queryKey: listQueryKey,
    enabled: listEnabled,
    fetchPage: (page, pageSize) => {
      const params = new URLSearchParams({
        organizationUnitId,
        year,
        page: String(page),
        pageSize: String(pageSize),
      });
      if (debouncedSearch) {
        params.set('search', debouncedSearch);
      }
      if (parametersId) {
        params.set('parametersId', String(parametersId));
      }
      return `/api/compensation-results?${params}`;
    },
  });

  const loadOrgUnits = useCallback(async () => {
    const units = await api.get<OrganizationUnit[]>('/api/organization-units');
    setOrgUnits(units);
    if (units.length > 0) {
      setOrganizationUnitId((current) => current || String(units[0].id));
    }
  }, []);

  const loadParametersMeta = useCallback(async (orgId: string, selectedYear: string) => {
    try {
      const parameters = await api.get<CompensationParameters[]>(
        `/api/compensation-parameters?organizationUnitId=${orgId}&year=${selectedYear}`,
      );
      if (parameters.length > 0) {
        setParametersId(parameters[0].id);
        setCurrency(parameters[0].currency || 'RSD');
        const status = await api.get<CompensationCalculationStatus>(
          `/api/compensation-parameters/${parameters[0].id}/calculation-status`,
        );
        setCalculationStatus(status);
      } else {
        setParametersId(null);
        setCalculationStatus(null);
        setCurrency('RSD');
      }
    } catch {
      setParametersId(null);
      setCalculationStatus(null);
      setCurrency('RSD');
    }
  }, []);

  useEffect(() => {
    loadOrgUnits().catch((e) => {
      toast.error(e instanceof Error ? e.message : formatMessage({ id: 'errors.generic' }));
    });
  }, [loadOrgUnits, formatMessage, toast]);

  useEffect(() => {
    if (organizationUnitId && year) {
      void loadParametersMeta(organizationUnitId, year);
    }
  }, [organizationUnitId, year, loadParametersMeta]);

  useEffect(() => {
    if (listError) toast.error(listError);
  }, [listError, toast]);

  const emptyMessage = debouncedSearch
    ? formatMessage({ id: 'evaluation.noSearchResults' })
    : formatMessage({ id: 'common.filtersNoData' });

  const isFinalized = calculationStatus?.isFinalized ?? false;

  const statusLabel = useMemo(() => {
    if (!calculationStatus || calculationStatus.totalResults === 0) {
      return formatMessage({ id: 'admin.compensationResults.noCalculation' });
    }
    if (calculationStatus.isFinalized) {
      return formatMessage({ id: 'admin.compensationResults.finalized' });
    }
    return formatMessage({ id: 'admin.compensationResults.draftCount' }, { count: calculationStatus.totalResults });
  }, [calculationStatus]);

  async function handleFinalize() {
    if (!parametersId || !calculationStatus || calculationStatus.totalResults === 0) {
      toast.warning(formatMessage({ id: 'errors.noResultsToFinalize' }));
      return;
    }

    if (!window.confirm(formatMessage({ id: 'admin.compensationResults.finalizeConfirm' }))) {
      return;
    }

    setFinalizing(true);
    try {
      const response = await api.post<{ finalizedCount: number }>(
        `/api/compensation-parameters/${parametersId}/finalize`,
      );
      toast.success(formatMessage({ id: 'alerts.finalizedResults' }, { count: response.finalizedCount }));
      await loadParametersMeta(organizationUnitId, year);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : formatMessage({ id: 'errors.finalizeFailed' }));
    } finally {
      setFinalizing(false);
    }
  }

  function exportRows(rows: CompensationResult[]) {
    const orgName = orgUnits.find((unit) => String(unit.id) === organizationUnitId)?.name ?? organizationUnitId;
    downloadCsv(
      `varijabila-${year}-${orgName.replace(/[^\w\s-]/g, '').trim() || 'prikaz'}.csv`,
      [
        formatMessage({ id: 'common.fullName' }),
        formatMessage({ id: 'evaluation.orgUnitShort' }),
        formatMessage({ id: 'admin.compensationResults.fixedSalary' }),
        formatMessage({ id: 'admin.compensationResults.annualCompensation' }),
        formatMessage({ id: 'admin.compensationResults.quarterlyCompensation' }),
        formatMessage({ id: 'admin.compensationResults.monthlyCompensation' }),
        formatMessage({ id: 'admin.compensationResults.payShare' }),
      ],
      rows.map((row) => [
        row.employeeFullName,
        row.organizationUnitName,
        formatCsvAmount(row.fixedSalary),
        formatCsvAmount(row.netCompensation),
        formatCsvAmount(row.quarterlyCompensation),
        formatCsvAmount(row.monthlyCompensation),
        formatPercent(row.compensationPercent, 1),
      ]),
    );
  }

  async function handleExport() {
    if (!organizationUnitId || !year) return;
    setExporting(true);
    setListError('');
    try {
      const hasAllLoaded = totalCount > 0 && results.length >= totalCount;
      const rows = hasAllLoaded
        ? results
        : await fetchAllPages<CompensationResult>((page, pageSize) => {
            const params = new URLSearchParams({
              organizationUnitId,
              year,
              page: String(page),
              pageSize: String(pageSize),
            });
            if (debouncedSearch) {
              params.set('search', debouncedSearch);
            }
            if (parametersId) {
              params.set('parametersId', String(parametersId));
            }
            return `/api/compensation-results?${params}`;
          });
      if (rows.length === 0) {
        toast.warning(formatMessage({ id: 'errors.noDataToExport' }));
        return;
      }
      exportRows(rows);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : formatMessage({ id: 'errors.exportFailed' }));
    } finally {
      setExporting(false);
    }
  }

  return (
    <div className="card card--table-fill">
      <div className="filter-bar compensation-results-toolbar">
        <div className="compensation-results-filters form-grid admin-form__grid">
          <div className="form-row">
            <label htmlFor="results-org">{formatMessage({ id: 'common.organizationUnit' })}</label>
            <select
              id="results-org"
              value={organizationUnitId}
              onChange={(e) => setOrganizationUnitId(e.target.value)}
            >
              {orgUnits.map((unit) => (
                <option key={unit.id} value={unit.id}>{unit.name}</option>
              ))}
            </select>
          </div>
          <div className="form-row">
            <label htmlFor="results-year">{formatMessage({ id: 'common.year' })}</label>
            <select id="results-year" value={year} onChange={(e) => setYear(e.target.value)}>
              {yearOptions.map((y) => (
                <option key={y} value={y}>{y}</option>
              ))}
            </select>
          </div>
          <div className="form-row filter-bar-search">
            <label htmlFor="results-search">{formatMessage({ id: 'admin.employeeSearch' })}</label>
            <input
              id="results-search"
              type="search"
              placeholder={formatMessage({ id: 'admin.searchNamePlaceholder' })}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
        </div>
        {parametersId ? (
          <div className="filter-bar__actions compensation-results-toolbar__actions">
            <span className="table-currency-badge">{currency}</span>
            <button
              type="button"
              className="btn btn-secondary btn-sm"
              onClick={handleExport}
              disabled={exporting || loading || totalCount === 0}
            >
              {exporting
                ? formatMessage({ id: 'admin.compensationResults.exporting' })
                : formatMessage({ id: 'admin.compensationResults.exportCsv' })}
            </button>
          </div>
        ) : null}
      </div>
      {parametersId && calculationStatus && (
        <div className="compensation-params__status">
          <div className="compensation-params__status-main">
            <span
              className={`compensation-params__status-badge ${isFinalized ? 'compensation-params__status-badge--final' : 'compensation-params__status-badge--draft'}`}
            >
              {statusLabel}
            </span>
            {calculationStatus.lastCalculatedAt && (
              <span className="card__hint" style={{ margin: 0 }}>
                {formatMessage({ id: 'admin.compensationResults.lastCalculation' })}{' '}
                {formatDateTime(calculationStatus.lastCalculatedAt)}
              </span>
            )}
          </div>
          {calculationStatus.totalResults > 0 && !isFinalized ? (
            <button
              type="button"
              className="btn btn-primary btn-sm"
              disabled={finalizing}
              onClick={handleFinalize}
            >
              {finalizing
                ? formatMessage({ id: 'admin.compensationResults.finalizing' })
                : formatMessage({ id: 'admin.compensationResults.finalizeResults' })}
            </button>
          ) : null}
        </div>
      )}
      {loading ? (
        <div className="empty">{formatMessage({ id: 'common.loading' })}</div>
      ) : results.length === 0 ? (
        <div className="empty">
          <p>{emptyMessage}</p>
          {!debouncedSearch && (
            <p style={{ marginTop: '0.5rem' }}>
              <Link to="/admin/varijabila/compensation">{formatMessage({ id: 'common.goToParameters' })}</Link>
            </p>
          )}
        </div>
      ) : (
        <div className="compensation-results-table">
          <div className="table-wrap table-wrap--infinite">
            <table className="table table--hover compensation-results-table__grid">
              <thead>
                <tr>
                  <th className="col-text">{formatMessage({ id: 'common.fullName' })}</th>
                  <th className="col-num">{formatMessage({ id: 'admin.compensationResults.fixedSalary' })}</th>
                  <th className="col-num">{formatMessage({ id: 'admin.compensationResults.annualCompensation' })}</th>
                  <th className="col-num">{formatMessage({ id: 'admin.compensationResults.quarterlyCompensation' })}</th>
                  <th className="col-num">{formatMessage({ id: 'admin.compensationResults.monthlyCompensation' })}</th>
                  <th className="col-num">{formatMessage({ id: 'admin.compensationResults.payShare' })}</th>
                </tr>
              </thead>
              <tbody>
                {results.map((row) => (
                  <tr key={row.id}>
                    <td className="cell-primary col-text">{row.employeeFullName}</td>
                    <td className="col-num">{formatTableAmount(row.fixedSalary)}</td>
                    <td className="col-num">{formatTableAmount(row.netCompensation)}</td>
                    <td className="col-num">{formatTableAmount(row.quarterlyCompensation)}</td>
                    <td className="col-num">{formatTableAmount(row.monthlyCompensation)}</td>
                    <td className="col-num">{formatPercent(row.compensationPercent)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            <InfiniteScrollSentinel
              hasMore={hasMore}
              isLoading={loadingMore}
              onLoadMore={loadMore}
            />
          </div>
        </div>
      )}
    </div>
  );
}
