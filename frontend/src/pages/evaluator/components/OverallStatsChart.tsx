import { useState } from 'react';
import type { OverallStatsComparison } from '../../../api/types';
import { ChartTooltip } from '../../../components/charts/ChartTooltip';
import {
  BAR_GAP,
  CHART,
  MAX_BAR_WIDTH,
  barPath,
} from '../../../components/charts/chartTheme';
import { useElementWidth } from '../../../components/charts/useElementWidth';
import { useIntl } from '../../../i18n';

interface OverallStatsChartProps {
  stats: OverallStatsComparison;
  year: number;
}

type Period = 'selectedYear' | 'allYears';

const PERIODS: Period[] = ['selectedYear', 'allYears'];
const PERIOD_COLOR: Record<Period, string> = {
  selectedYear: CHART.primary,
  allYears: CHART.muted,
};
// Prosek i medijana su na skali ocena (1–5); varijansa ima drugu skalu, pa se
// ne crta na istoj osi nego se prikazuje kao broj ispod grafikona.
const METRICS = [
  { key: 'average' as const, labelKey: 'charts.average' },
  { key: 'median' as const, labelKey: 'charts.median' },
];

const HEIGHT = 240;
const PADDING = { top: 28, right: 12, bottom: 36, left: 32 };
const MAX_RATING = 5;

interface HoverState {
  metricIndex: number;
  x: number;
  y: number;
}

function formatValue(value: number | null | undefined, digits = 2): string {
  return value == null ? '—' : value.toFixed(digits);
}

export function OverallStatsChart({ stats, year }: OverallStatsChartProps) {
  const { formatMessage } = useIntl();
  const { ref, width, nodeRef } = useElementWidth<HTMLDivElement>();
  const [hover, setHover] = useState<HoverState | null>(null);

  const periodLabel = (period: Period) =>
    period === 'selectedYear'
      ? formatMessage({ id: 'charts.selectedYear' }, { year })
      : formatMessage({ id: 'charts.allYearsPeriod' });

  const hasData = PERIODS.some((period) =>
    [...METRICS.map((m) => m.key), 'variance' as const].some(
      (key) => stats[period][key] != null,
    ),
  );

  if (!hasData) {
    return (
      <div className="analytics-chart analytics-chart--empty">
        <p>{formatMessage({ id: 'charts.noStatsData' })}</p>
      </div>
    );
  }

  const plotWidth = Math.max(width - PADDING.left - PADDING.right, 120);
  const plotHeight = HEIGHT - PADDING.top - PADDING.bottom;
  const groupWidth = plotWidth / METRICS.length;
  const barWidth = Math.min(MAX_BAR_WIDTH, (groupWidth - 32) / 2);
  const yFor = (value: number) =>
    PADDING.top + plotHeight - (value / MAX_RATING) * plotHeight;

  return (
    <div className="analytics-chart chart-block">
      {/* Legenda iznad grafikona: prvo se vidi šta boje znače. */}
      <ul className="chart-legend chart-legend--top">
        {PERIODS.map((period) => (
          <li key={period}>
            <span
              className="chart-legend__swatch"
              style={{ background: PERIOD_COLOR[period] }}
              aria-hidden
            />
            {periodLabel(period)}
          </li>
        ))}
      </ul>

      <div className="chart-canvas" ref={ref}>
        {hover && (
          <ChartTooltip
            title={formatMessage({
              id: METRICS[hover.metricIndex].labelKey as never,
            })}
            rows={PERIODS.map((period) => ({
              label: periodLabel(period),
              value: formatValue(stats[period][METRICS[hover.metricIndex].key]),
              color: PERIOD_COLOR[period],
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
          aria-label={formatMessage(
            { id: 'charts.statsComparisonAria' },
            { year },
          )}
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

          {METRICS.map((metric, metricIndex) => {
            const groupLeft = PADDING.left + metricIndex * groupWidth;
            const groupCenter = groupLeft + groupWidth / 2;
            const isHovered = hover?.metricIndex === metricIndex;
            return (
              <g key={metric.key}>
                {isHovered && (
                  <rect
                    x={groupLeft + 8}
                    y={PADDING.top}
                    width={groupWidth - 16}
                    height={plotHeight}
                    rx={6}
                    fill={CHART.highlight}
                  />
                )}
                {PERIODS.map((period, periodIndex) => {
                  const value = stats[period][metric.key];
                  if (value == null) return null;
                  const x =
                    groupCenter -
                    barWidth -
                    BAR_GAP / 2 +
                    periodIndex * (barWidth + BAR_GAP);
                  const y = yFor(value);
                  return (
                    <g key={period}>
                      <path
                        d={barPath(
                          x,
                          y,
                          barWidth,
                          PADDING.top + plotHeight - y,
                        )}
                        fill={PERIOD_COLOR[period]}
                      />
                      {period === 'selectedYear' && (
                        <text
                          x={x + barWidth / 2}
                          y={y - 6}
                          textAnchor="middle"
                          className="chart-value-text"
                        >
                          {formatValue(value)}
                        </text>
                      )}
                    </g>
                  );
                })}
                <text
                  x={groupCenter}
                  y={HEIGHT - 12}
                  textAnchor="middle"
                  className="chart-category-text"
                >
                  {formatMessage({ id: metric.labelKey as never })}
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
                      metricIndex,
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

      <dl className="chart-stats">
        <dt>{formatMessage({ id: 'charts.variance' })}</dt>
        {PERIODS.map((period) => (
          <dd key={period}>
            <span
              className="chart-legend__swatch"
              style={{ background: PERIOD_COLOR[period] }}
              aria-hidden
            />
            <strong>{formatValue(stats[period].variance, 3)}</strong>
            <span>{periodLabel(period)}</span>
          </dd>
        ))}
      </dl>
    </div>
  );
}
