import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../../api/client';
import type {
  CompensationAnalytics,
  CompensationAnalyticsChartType,
  OrganizationUnit,
} from '../../api/types';
import { currentYear } from '../../utils/status';
import { COMPENSATION_CHART_OPTIONS } from '../../utils/compensationAnalytics';
import { useToast } from '../../hooks';
import { CompensationDistributionChart } from './components/CompensationDistributionChart';
import { CompensationEmployeeBarChart } from './components/CompensationEmployeeBarChart';
import { useIntl } from '../../i18n';
import { FilterChip } from '../../components/common/ToolbarSearch';

const ALL_ORG_VALUE = 'all';

export function AdminCompensationAnalytics() {
  const { formatMessage } = useIntl();
  const toast = useToast();
  const [orgUnits, setOrgUnits] = useState<OrganizationUnit[]>([]);
  const [organizationUnitId, setOrganizationUnitId] = useState(ALL_ORG_VALUE);
  const [year, setYear] = useState(String(currentYear));
  const [chartType, setChartType] =
    useState<CompensationAnalyticsChartType>('shareDistribution');

  const [analytics, setAnalytics] = useState<CompensationAnalytics | null>(
    null,
  );
  const [loading, setLoading] = useState(true);
  // Brza promena filtera pokreće više zahteva; samo poslednji sme da upiše grafikon.
  const analyticsRequestRef = useRef(0);

  const yearOptions = useMemo(() => {
    const base = currentYear;
    return [base - 1, base, base + 1];
  }, []);

  const selectedChartLabel = useMemo(() => {
    const option = COMPENSATION_CHART_OPTIONS.find(
      (entry) => entry.value === chartType,
    );
    return option ? formatMessage({ id: option.labelKey as never }) : '';
  }, [chartType, formatMessage]);

  const loadOrgUnits = useCallback(async () => {
    const units = await api.get<OrganizationUnit[]>('/api/organization-units');
    setOrgUnits(units);
  }, []);

  const loadAnalytics = useCallback(
    async (
      orgId: string,
      selectedYear: string,
      selectedChart: CompensationAnalyticsChartType,
    ) => {
      if (!selectedYear) return;

      const requestId = ++analyticsRequestRef.current;
      const isLatest = () => requestId === analyticsRequestRef.current;

      setLoading(true);
      try {
        const params = new URLSearchParams({
          year: selectedYear,
          chartType: selectedChart,
        });
        if (orgId !== ALL_ORG_VALUE) {
          params.set('organizationUnitId', orgId);
        }

        const data = await api.get<CompensationAnalytics>(
          `/api/compensation-results/analytics?${params}`,
        );
        if (!isLatest()) return;
        setAnalytics(data);
      } catch (e) {
        if (!isLatest()) return;
        toast.error(
          e instanceof Error
            ? e.message
            : formatMessage({ id: 'admin.analyticsLoadError' }),
        );
        setAnalytics(null);
      } finally {
        if (isLatest()) setLoading(false);
      }
    },
    [formatMessage, toast],
  );

  useEffect(() => {
    loadOrgUnits().catch((e) => {
      toast.error(
        e instanceof Error
          ? e.message
          : formatMessage({ id: 'errors.unknown' }),
      );
      setLoading(false);
    });
  }, [formatMessage, loadOrgUnits, toast]);

  useEffect(() => {
    loadAnalytics(organizationUnitId, year, chartType);
  }, [organizationUnitId, year, chartType, loadAnalytics]);

  const hasData =
    analytics &&
    (analytics.buckets.some((b) => b.count > 0) || analytics.series.length > 0);

  function renderChart() {
    if (!analytics) return null;

    if (analytics.valueFormat === 'count') {
      return (
        <CompensationDistributionChart
          buckets={analytics.buckets}
          ariaLabel={selectedChartLabel}
        />
      );
    }

    return (
      <CompensationEmployeeBarChart
        series={analytics.series}
        valueFormat={analytics.valueFormat}
        currency={analytics.currency}
        ariaLabel={selectedChartLabel}
      />
    );
  }

  return (
    <div className="card card--flush data-panel compensation-analytics">
      <div className="data-panel__toolbar">
        <div className="filter-bar filter-bar--compact">
          <FilterChip
            id="analytics-org"
            label={formatMessage({ id: 'evaluation.orgUnitShort' })}
            value={organizationUnitId}
            onChange={setOrganizationUnitId}
          >
            <option value={ALL_ORG_VALUE}>
              {formatMessage({ id: 'common.allOrganization' })}
            </option>
            {orgUnits.map((unit) => (
              <option key={unit.id} value={unit.id}>
                {unit.name}
              </option>
            ))}
          </FilterChip>
          <FilterChip
            id="analytics-year"
            label={formatMessage({ id: 'common.year' })}
            value={year}
            onChange={setYear}
          >
            {yearOptions.map((y) => (
              <option key={y} value={y}>
                {y}
              </option>
            ))}
          </FilterChip>
          <FilterChip
            id="analytics-chart"
            label={formatMessage({ id: 'common.chartType' })}
            value={chartType}
            onChange={(value) =>
              setChartType(value as CompensationAnalyticsChartType)
            }
          >
            {COMPENSATION_CHART_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {formatMessage({ id: option.labelKey as never })}
              </option>
            ))}
          </FilterChip>
        </div>
      </div>

      <div className="compensation-analytics__body">
        {loading ? (
          <div className="empty">{formatMessage({ id: 'common.loading' })}</div>
        ) : !hasData ? (
          <div className="empty">
            <p>{formatMessage({ id: 'common.filtersNoData' })}</p>
            <p style={{ marginTop: '0.5rem' }}>
              <Link to="/admin/varijabila/compensation">
                {formatMessage({ id: 'common.goToParameters' })}
              </Link>
            </p>
          </div>
        ) : (
          <section className="compensation-analytics__chart">
            {renderChart()}
          </section>
        )}
      </div>
    </div>
  );
}
