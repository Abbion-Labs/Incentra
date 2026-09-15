import { useEffect, useMemo, useState } from 'react';
import type { EmployeeQuarterBenchmark } from '../../../api/types';
import { useIntl } from '../../../i18n';
import { currentYear } from '../../../utils/status';

interface EvaluationBenchmarkChartProps {
  quarters: EmployeeQuarterBenchmark[];
  selectedYear?: number;
  selectedQuarter?: number;
}

const ALL_YEARS = 'all';

const SERIES_KEYS = [
  { key: 'employeeAverage' as const, labelKey: 'charts.benchmarkEmployee', color: '#1e4d8c' },
  { key: 'organizationUnitAverage' as const, labelKey: 'charts.benchmarkOrgUnit', color: '#0d9488' },
  { key: 'jobPositionAverage' as const, labelKey: 'charts.benchmarkJobPosition', color: '#d97706' },
];

interface BarTooltip {
  label: string;
  value: number;
  period: string;
  x: number;
  y: number;
}

interface ChartPoint {
  key: string;
  label: string;
  employeeAverage: number | null;
  organizationUnitAverage: number | null;
  jobPositionAverage: number | null;
  year?: number;
  quarter?: number;
}

function getDefaultYearFilter(quarters: EmployeeQuarterBenchmark[]): string {
  const years = [...new Set(quarters.map((q) => q.year))];
  if (years.includes(currentYear)) return String(currentYear);
  if (years.length > 0) return String(Math.max(...years));
  return ALL_YEARS;
}

function formatValue(value: number | null | undefined): string {
  if (value == null) return '—';
  return value.toFixed(2);
}

function averageNonNull(values: (number | null | undefined)[]): number | null {
  const valid = values.filter((v): v is number => v != null);
  if (valid.length === 0) return null;
  return valid.reduce((sum, value) => sum + value, 0) / valid.length;
}

export function EvaluationBenchmarkChart({
  quarters,
  selectedYear,
  selectedQuarter,
}: EvaluationBenchmarkChartProps) {
  const { formatMessage } = useIntl();
  const [yearFilter, setYearFilter] = useState<string>(() => getDefaultYearFilter(quarters));
  const [tooltip, setTooltip] = useState<BarTooltip | null>(null);

  useEffect(() => {
    setYearFilter((current) => {
      if (current !== ALL_YEARS && quarters.some((q) => String(q.year) === current)) {
        return current;
      }
      return getDefaultYearFilter(quarters);
    });
  }, [quarters]);

  const availableYears = useMemo(
    () => [...new Set(quarters.map((q) => q.year))].sort((a, b) => b - a),
    [quarters],
  );

  const filteredQuarters = useMemo(() => {
    if (yearFilter === ALL_YEARS) return quarters;
    const year = Number(yearFilter);
    return quarters.filter((q) => q.year === year);
  }, [quarters, yearFilter]);

  const isAggregateView = yearFilter === ALL_YEARS;

  const chartData = useMemo<ChartPoint[]>(() => {
    if (isAggregateView) {
      return [
        {
          key: 'aggregate',
          label: formatMessage({ id: 'charts.benchmarkTotal' }),
          employeeAverage: averageNonNull(filteredQuarters.map((q) => q.employeeAverage)),
          organizationUnitAverage: averageNonNull(filteredQuarters.map((q) => q.organizationUnitAverage)),
          jobPositionAverage: averageNonNull(filteredQuarters.map((q) => q.jobPositionAverage)),
        },
      ];
    }

    return [...filteredQuarters]
      .reverse()
      .map((point) => ({
        key: `${point.year}-${point.quarter}`,
        label: `Q${point.quarter}/${point.year}`,
        employeeAverage: point.employeeAverage,
        organizationUnitAverage: point.organizationUnitAverage,
        jobPositionAverage: point.jobPositionAverage,
        year: point.year,
        quarter: point.quarter,
      }));
  }, [filteredQuarters, isAggregateView]);

  if (quarters.length === 0) {
    return (
      <div className="benchmark-chart benchmark-chart--empty">
        <p>{formatMessage({ id: 'charts.noChartData' })}</p>
      </div>
    );
  }

  const width = 640;
  const height = 280;
  const padding = { top: 24, right: 16, bottom: 44, left: 40 };
  const chartWidth = width - padding.left - padding.right;
  const chartHeight = height - padding.top - padding.bottom;
  const maxValue = 5;
  const groupWidth = chartData.length > 0 ? chartWidth / chartData.length : chartWidth;
  const barWidth = isAggregateView
    ? Math.min(72, (chartWidth - 80) / 3)
    : Math.min(18, (groupWidth - 16) / 3);
  const aggregatePeriodLabel = formatMessage({ id: 'charts.benchmarkAllYears' });

  function showTooltip(
    event: React.MouseEvent<SVGRectElement>,
    seriesLabel: string,
    value: number,
    period: string,
  ) {
    const wrap = event.currentTarget.closest('.benchmark-chart__canvas-wrap');
    if (!wrap) return;
    const rect = wrap.getBoundingClientRect();
    setTooltip({
      label: seriesLabel,
      value,
      period,
      x: event.clientX - rect.left,
      y: event.clientY - rect.top,
    });
  }

  return (
    <div className="benchmark-chart">
      {chartData.length === 0 ? (
        <div className="benchmark-chart benchmark-chart--empty">
          <p>{formatMessage({ id: 'charts.noEvaluationsForPeriod' })}</p>
        </div>
      ) : (
        <div className="benchmark-chart__canvas-wrap">
            {tooltip && (
              <div
                className="benchmark-chart__tooltip"
                style={{ left: tooltip.x, top: tooltip.y }}
                role="tooltip"
              >
                <span className="benchmark-chart__tooltip-period">{tooltip.period}</span>
                <strong>
                  {tooltip.label}: {formatValue(tooltip.value)}
                </strong>
              </div>
            )}

            <svg
              viewBox={`0 0 ${width} ${height}`}
              className="benchmark-chart__svg"
              role="img"
              aria-label={
                isAggregateView
                  ? formatMessage({ id: 'analytics.benchmarkAggregateAria' })
                  : formatMessage({ id: 'analytics.benchmarkQuarterlyAria' })
              }
            >
              {[1, 2, 3, 4, 5].map((tick) => {
                const y = padding.top + chartHeight - (tick / maxValue) * chartHeight;
                return (
                  <g key={tick}>
                    <line x1={padding.left} y1={y} x2={width - padding.right} y2={y} stroke="#e2e8f0" />
                    <text x={padding.left - 8} y={y + 4} textAnchor="end" fontSize="11" fill="#64748b">
                      {tick}
                    </text>
                  </g>
                );
              })}

              {chartData.map((point, index) => {
                const groupX = padding.left + index * groupWidth + groupWidth / 2;
                const isSelected =
                  !isAggregateView &&
                  point.year === selectedYear &&
                  point.quarter === selectedQuarter;
                const period = isAggregateView ? aggregatePeriodLabel : point.label;

                return (
                  <g key={point.key}>
                    {SERIES_KEYS.map((series, seriesIndex) => {
                      const seriesLabel = formatMessage({ id: series.labelKey as never });
                      const value = point[series.key];
                      if (value == null) return null;
                      const barHeight = (value / maxValue) * chartHeight;
                      const x = isAggregateView
                        ? groupX - barWidth * 1.5 + seriesIndex * (barWidth + 12)
                        : groupX - barWidth * 1.5 + seriesIndex * (barWidth + 2);
                      const y = padding.top + chartHeight - barHeight;
                      return (
                        <rect
                          key={series.key}
                          x={x}
                          y={y}
                          width={barWidth}
                          height={barHeight}
                          rx={3}
                          fill={series.color}
                          opacity={isSelected ? 1 : 0.85}
                          className="benchmark-chart__bar"
                          onMouseEnter={(e) => showTooltip(e, seriesLabel, value, period)}
                          onMouseMove={(e) => showTooltip(e, seriesLabel, value, period)}
                          onMouseLeave={() => setTooltip(null)}
                        >
                          <title>{`${seriesLabel} (${period}): ${formatValue(value)}`}</title>
                        </rect>
                      );
                    })}
                    <text
                      x={groupX}
                      y={height - padding.bottom + 20}
                      textAnchor="middle"
                      fontSize="11"
                      fill={isSelected ? '#1e4d8c' : '#64748b'}
                      fontWeight={isSelected ? 600 : 400}
                    >
                      {point.label}
                    </text>
                  </g>
                );
              })}
            </svg>
        </div>
      )}

      <div className="benchmark-chart__footer">
        <div className="benchmark-chart__footer-inner">
          <div className="benchmark-chart__legend" aria-label={formatMessage({ id: 'analytics.chartLegendAria' })}>
            {SERIES_KEYS.map((series) => (
              <span key={series.key} className="benchmark-chart__legend-item">
                <span className="benchmark-chart__swatch" style={{ background: series.color }} aria-hidden />
                {formatMessage({ id: series.labelKey as never })}
              </span>
            ))}
          </div>

          <label className="benchmark-chart__filter" htmlFor="chart-year-filter">
            {formatMessage({ id: 'common.year' })}
            <select
              id="chart-year-filter"
              value={yearFilter}
              onChange={(e) => setYearFilter(e.target.value)}
            >
              <option value={ALL_YEARS}>{formatMessage({ id: 'charts.benchmarkAllYears' })}</option>
              {availableYears.map((year) => (
                <option key={year} value={year}>
                  {year}
                </option>
              ))}
            </select>
          </label>
        </div>
      </div>
    </div>
  );
}
