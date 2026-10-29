import { useEffect, useMemo, useState } from 'react';
import type { EmployeeQuarterBenchmark } from '../../../api/types';
import { ChartTooltip } from '../../../components/charts/ChartTooltip';
import {
  BAR_GAP,
  CHART,
  MAX_BAR_WIDTH,
  barPath,
} from '../../../components/charts/chartTheme';
import { useElementWidth } from '../../../components/charts/useElementWidth';
import { FieldHint } from '../../../components/common/FieldHint';
import { useIntl } from '../../../i18n';
import { currentYear } from '../../../utils/status';

interface EvaluationBenchmarkChartProps {
  quarters: EmployeeQuarterBenchmark[];
  selectedYear?: number;
  selectedQuarter?: number;
}

const ALL_YEARS = 'all';

const SERIES = [
  {
    key: 'employeeAverage' as const,
    labelKey: 'charts.benchmarkEmployee',
    color: CHART.primary,
  },
  {
    key: 'organizationUnitAverage' as const,
    labelKey: 'charts.benchmarkOrgUnit',
    color: CHART.secondary,
  },
  {
    key: 'jobPositionAverage' as const,
    labelKey: 'charts.benchmarkJobPosition',
    color: CHART.tertiary,
  },
];

interface ChartPoint {
  key: string;
  label: string;
  employeeAverage: number | null;
  organizationUnitAverage: number | null;
  jobPositionAverage: number | null;
  year?: number;
  quarter?: number;
}

interface HoverState {
  index: number;
  x: number;
  y: number;
}

const HEIGHT = 240;
const PADDING = { top: 16, right: 12, bottom: 32, left: 32 };
const MAX_RATING = 5;

function getDefaultYearFilter(quarters: EmployeeQuarterBenchmark[]): string {
  const years = [...new Set(quarters.map((q) => q.year))];
  if (years.includes(currentYear)) return String(currentYear);
  if (years.length > 0) return String(Math.max(...years));
  return ALL_YEARS;
}

function formatValue(value: number | null | undefined): string {
  return value == null ? '—' : value.toFixed(2);
}

function averageNonNull(values: (number | null | undefined)[]): number | null {
  const valid = values.filter((v): v is number => v != null);
  if (valid.length === 0) return null;
  return valid.reduce((sum, value) => sum + value, 0) / valid.length;
}

/**
 * Prosek zaposlenog po kvartalu u poređenju sa organizacionom jedinicom i
 * radnim mestom. Izabrani kvartal je istaknut, godina se bira iznad grafikona.
 */
export function EvaluationBenchmarkChart({
  quarters,
  selectedYear,
  selectedQuarter,
}: EvaluationBenchmarkChartProps) {
  const { formatMessage } = useIntl();
  const { ref, width, nodeRef } = useElementWidth<HTMLDivElement>();
  const [yearFilter, setYearFilter] = useState<string>(() =>
    getDefaultYearFilter(quarters),
  );
  const [hover, setHover] = useState<HoverState | null>(null);

  useEffect(() => {
    setYearFilter((current) => {
      if (
        current !== ALL_YEARS &&
        quarters.some((q) => String(q.year) === current)
      ) {
        return current;
      }
      return getDefaultYearFilter(quarters);
    });
  }, [quarters]);

  const availableYears = useMemo(
    () => [...new Set(quarters.map((q) => q.year))].sort((a, b) => b - a),
    [quarters],
  );

  const isAggregateView = yearFilter === ALL_YEARS;

  const chartData = useMemo<ChartPoint[]>(() => {
    const filtered = isAggregateView
      ? quarters
      : quarters.filter((q) => q.year === Number(yearFilter));
    if (isAggregateView) {
      return [
        {
          key: 'aggregate',
          label: formatMessage({ id: 'charts.benchmarkTotal' }),
          employeeAverage: averageNonNull(
            filtered.map((q) => q.employeeAverage),
          ),
          organizationUnitAverage: averageNonNull(
            filtered.map((q) => q.organizationUnitAverage),
          ),
          jobPositionAverage: averageNonNull(
            filtered.map((q) => q.jobPositionAverage),
          ),
        },
      ];
    }
    return [...filtered].reverse().map((point) => ({
      key: `${point.year}-${point.quarter}`,
      label: `Q${point.quarter}/${point.year}`,
      employeeAverage: point.employeeAverage,
      organizationUnitAverage: point.organizationUnitAverage,
      jobPositionAverage: point.jobPositionAverage,
      year: point.year,
      quarter: point.quarter,
    }));
  }, [quarters, yearFilter, isAggregateView, formatMessage]);

  if (quarters.length === 0) {
    return (
      <div className="benchmark-chart benchmark-chart--empty">
        <p>{formatMessage({ id: 'charts.noChartData' })}</p>
      </div>
    );
  }

  const plotWidth = Math.max(width - PADDING.left - PADDING.right, 160);
  const plotHeight = HEIGHT - PADDING.top - PADDING.bottom;
  const baseline = PADDING.top + plotHeight;
  const groupWidth = plotWidth / Math.max(chartData.length, 1);
  const barWidth = Math.min(MAX_BAR_WIDTH, (groupWidth - 20) / 3);
  const yFor = (value: number) => baseline - (value / MAX_RATING) * plotHeight;
  const periodLabel = (point: ChartPoint) =>
    isAggregateView
      ? formatMessage({ id: 'charts.benchmarkAllYears' })
      : point.label;

  return (
    <div className="benchmark-chart chart-block">
      <div className="chart-toolbar">
        <div className="chart-title-row">
          <h3 className="chart-title">
            {formatMessage({ id: 'charts.benchmarkTitle' })}
          </h3>
          <FieldHint hint={formatMessage({ id: 'charts.benchmarkHint' })} />
        </div>
        <label className="filter-chip" htmlFor="chart-year-filter">
          <span className="filter-chip__label">
            {formatMessage({ id: 'common.year' })}
          </span>
          <select
            id="chart-year-filter"
            value={yearFilter}
            onChange={(e) => setYearFilter(e.target.value)}
          >
            <option value={ALL_YEARS}>
              {formatMessage({ id: 'charts.benchmarkAllYears' })}
            </option>
            {availableYears.map((year) => (
              <option key={year} value={year}>
                {year}
              </option>
            ))}
          </select>
        </label>
      </div>

      <ul
        className="chart-legend chart-legend--top"
        aria-label={formatMessage({ id: 'analytics.chartLegendAria' })}
      >
        {SERIES.map((series) => (
          <li key={series.key}>
            <span
              className="chart-legend__swatch"
              style={{ background: series.color }}
              aria-hidden
            />
            {formatMessage({ id: series.labelKey as never })}
          </li>
        ))}
      </ul>

      {chartData.length === 0 ? (
        <div className="benchmark-chart--empty">
          <p>{formatMessage({ id: 'charts.noEvaluationsForPeriod' })}</p>
        </div>
      ) : (
        <div className="chart-canvas" ref={ref}>
          {hover && (
            <ChartTooltip
              title={periodLabel(chartData[hover.index])}
              rows={SERIES.map((series) => ({
                label: formatMessage({ id: series.labelKey as never }),
                value: formatValue(chartData[hover.index][series.key]),
                color: series.color,
              }))}
              x={hover.x}
              y={hover.y}
              containerWidth={width}
            />
          )}
          <svg
            width={width}
            height={HEIGHT}
            className="chart-svg"
            role="img"
            aria-label={
              isAggregateView
                ? formatMessage({ id: 'analytics.benchmarkAggregateAria' })
                : formatMessage({ id: 'analytics.benchmarkQuarterlyAria' })
            }
          >
            {[0, 1, 2, 3, 4, 5].map((tick) => (
              <g key={tick}>
                <line
                  x1={PADDING.left}
                  x2={width - PADDING.right}
                  y1={yFor(tick)}
                  y2={yFor(tick)}
                  stroke={CHART.grid}
                />
                <text
                  x={PADDING.left - 8}
                  y={yFor(tick) + 4}
                  textAnchor="end"
                  className="chart-axis-text"
                >
                  {tick}
                </text>
              </g>
            ))}

            {chartData.map((point, index) => {
              const groupLeft = PADDING.left + index * groupWidth;
              const center = groupLeft + groupWidth / 2;
              const isSelected =
                !isAggregateView &&
                point.year === selectedYear &&
                point.quarter === selectedQuarter;
              // Pozadina samo pod mišem; izabrani kvartal nosi plavu oznaku ispod ose.
              const highlighted = hover?.index === index;
              return (
                <g key={point.key}>
                  {highlighted && (
                    <rect
                      x={groupLeft + 4}
                      y={PADDING.top}
                      width={groupWidth - 8}
                      height={plotHeight}
                      rx={6}
                      fill={CHART.highlight}
                    />
                  )}
                  {SERIES.map((series, seriesIndex) => {
                    const value = point[series.key];
                    if (value == null) return null;
                    const x =
                      center -
                      (barWidth * 3 + BAR_GAP * 2) / 2 +
                      seriesIndex * (barWidth + BAR_GAP);
                    return (
                      <path
                        key={series.key}
                        d={barPath(
                          x,
                          yFor(value),
                          barWidth,
                          baseline - yFor(value),
                        )}
                        fill={series.color}
                      />
                    );
                  })}
                  <text
                    x={center}
                    y={HEIGHT - 10}
                    textAnchor="middle"
                    className={`chart-category-text${isSelected ? ' is-selected' : ''}`}
                  >
                    {/* Uzak ekran: samo kvartal, godina je izabrana iznad. */}
                    {groupWidth < 72 && point.quarter
                      ? `Q${point.quarter}`
                      : point.label}
                  </text>
                  <rect
                    x={groupLeft}
                    y={PADDING.top}
                    width={groupWidth}
                    height={plotHeight}
                    fill="transparent"
                    onMouseMove={(event) => {
                      const box = nodeRef.current?.getBoundingClientRect();
                      if (!box) return;
                      setHover({
                        index,
                        x: event.clientX - box.left,
                        y: event.clientY - box.top,
                      });
                    }}
                    onMouseLeave={() => setHover(null)}
                  />
                </g>
              );
            })}
          </svg>
        </div>
      )}
    </div>
  );
}
