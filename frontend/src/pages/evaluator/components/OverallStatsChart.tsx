import { useState } from 'react';
import type { OverallStatsComparison } from '../../../api/types';
import { useIntl } from '../../../i18n';

interface OverallStatsChartProps {
  stats: OverallStatsComparison;
  year: number;
}

const METRIC_KEYS = [
  { key: 'average' as const, labelKey: 'charts.average', isVariance: false },
  { key: 'median' as const, labelKey: 'charts.median', isVariance: false },
  { key: 'variance' as const, labelKey: 'charts.variance', isVariance: true },
];

const PERIOD_COLORS = {
  selectedYear: '#1e4d8c',
  allYears: '#64748b',
} as const;

interface TooltipState {
  label: string;
  value: number;
  period: string;
  x: number;
  y: number;
}

function formatValue(isVariance: boolean, value: number | null | undefined): string {
  if (value == null) return '—';
  return isVariance ? value.toFixed(3) : value.toFixed(2);
}

function varianceTicks(maxVariance: number): number[] {
  const max = Math.max(maxVariance, 0.001);
  const step = max <= 0.2 ? 0.05 : max <= 0.5 ? 0.1 : 0.2;
  const ticks: number[] = [];
  for (let value = 0; value <= max + step / 2; value += step) {
    ticks.push(Number(value.toFixed(3)));
  }
  return ticks.length > 0 ? ticks : [0, max];
}

export function OverallStatsChart({ stats, year }: OverallStatsChartProps) {
  const { formatMessage } = useIntl();
  const [tooltip, setTooltip] = useState<TooltipState | null>(null);

  const hasData = METRIC_KEYS.some((metric) =>
    (['selectedYear', 'allYears'] as const).some((period) => stats[period][metric.key] != null),
  );

  if (!hasData) {
    return (
      <div className="analytics-chart analytics-chart--empty">
        <p>{formatMessage({ id: 'charts.noStatsData' })}</p>
      </div>
    );
  }

  const maxRating = Math.max(
    5,
    ...METRIC_KEYS.filter((metric) => !metric.isVariance).flatMap((metric) =>
      (['selectedYear', 'allYears'] as const).map((period) => stats[period][metric.key] ?? 0),
    ),
  );

  const maxVariance = Math.max(
    0.001,
    ...(['selectedYear', 'allYears'] as const).map((period) => stats[period].variance ?? 0),
  ) * 1.1;

  const width = 640;
  const height = 280;
  const padding = { top: 24, right: 48, bottom: 44, left: 40 };
  const chartWidth = width - padding.left - padding.right;
  const chartHeight = height - padding.top - padding.bottom;
  const groupWidth = chartWidth / METRIC_KEYS.length;
  const barWidth = Math.min(22, (groupWidth - 24) / 2);
  const varianceAxisTicks = varianceTicks(maxVariance);

  function periodLabel(periodKey: 'selectedYear' | 'allYears') {
    return periodKey === 'selectedYear'
      ? formatMessage({ id: 'charts.selectedYear' }, { year })
      : formatMessage({ id: 'charts.allYearsPeriod' });
  }

  function barHeight(value: number, metric: (typeof METRIC_KEYS)[number]) {
    if (metric.isVariance) {
      return (value / maxVariance) * chartHeight;
    }
    return (value / maxRating) * chartHeight;
  }

  function showTooltip(
    event: React.MouseEvent<SVGRectElement>,
    label: string,
    value: number,
    period: string,
  ) {
    const wrap = event.currentTarget.closest('.analytics-chart__canvas-wrap');
    if (!wrap) return;
    const rect = wrap.getBoundingClientRect();
    setTooltip({
      label,
      value,
      period,
      x: event.clientX - rect.left,
      y: event.clientY - rect.top,
    });
  }

  return (
    <div className="analytics-chart">
      <div className="analytics-chart__canvas-wrap">
        {tooltip && (
          <div className="analytics-chart__tooltip" style={{ left: tooltip.x, top: tooltip.y }} role="tooltip">
            <span className="analytics-chart__tooltip-period">{tooltip.period}</span>
            <strong>
              {tooltip.label}: {formatValue(tooltip.label === formatMessage({ id: 'charts.variance' }), tooltip.value)}
            </strong>
          </div>
        )}

        <svg
          viewBox={`0 0 ${width} ${height}`}
          className="analytics-chart__svg"
          role="img"
          aria-label={formatMessage({ id: 'charts.statsComparisonAria' }, { year })}
        >
          {[1, 2, 3, 4, 5].map((tick) => {
            const y = padding.top + chartHeight - (tick / maxRating) * chartHeight;
            return (
              <g key={`rating-${tick}`}>
                <line x1={padding.left} y1={y} x2={width - padding.right} y2={y} stroke="#e2e8f0" />
                <text x={padding.left - 8} y={y + 4} textAnchor="end" fontSize="11" fill="#64748b">
                  {tick}
                </text>
              </g>
            );
          })}

          {varianceAxisTicks.map((tick) => {
            const y = padding.top + chartHeight - (tick / maxVariance) * chartHeight;
            return (
              <g key={`variance-${tick}`}>
                <text x={width - padding.right + 8} y={y + 4} textAnchor="start" fontSize="10" fill="#7c3aed">
                  {tick.toFixed(2)}
                </text>
              </g>
            );
          })}

          <line
            x1={padding.left + groupWidth * 2}
            y1={padding.top}
            x2={padding.left + groupWidth * 2}
            y2={padding.top + chartHeight}
            stroke="#e2e8f0"
            strokeDasharray="4 4"
          />

          {METRIC_KEYS.map((metric, index) => {
            const metricLabel = formatMessage({ id: metric.labelKey as never });
            const groupX = padding.left + index * groupWidth + groupWidth / 2;
            return (
              <g key={metric.key}>
                {(['selectedYear', 'allYears'] as const).map((period, periodIndex) => {
                  const value = stats[period][metric.key];
                  if (value == null) return null;
                  const heightPx = barHeight(value, metric);
                  const x = groupX - barWidth - 2 + periodIndex * (barWidth + 4);
                  const y = padding.top + chartHeight - heightPx;
                  return (
                    <rect
                      key={period}
                      x={x}
                      y={y}
                      width={barWidth}
                      height={heightPx}
                      rx={3}
                      fill={PERIOD_COLORS[period]}
                      opacity={period === 'selectedYear' ? 1 : 0.75}
                      onMouseEnter={(event) => showTooltip(event, metricLabel, value, periodLabel(period))}
                      onMouseMove={(event) => showTooltip(event, metricLabel, value, periodLabel(period))}
                      onMouseLeave={() => setTooltip(null)}
                    />
                  );
                })}
                <text x={groupX} y={height - 14} textAnchor="middle" fontSize="11" fill="#475569">
                  {metricLabel}
                </text>
              </g>
            );
          })}
        </svg>
      </div>

      <div className="analytics-chart__footer">
        <ul className="analytics-chart__legend analytics-chart__legend--inline">
          <li>
            <span className="analytics-chart__legend-swatch" style={{ background: '#1e4d8c' }} />
            <span>{formatMessage({ id: 'charts.selectedYear' }, { year })}</span>
          </li>
          <li>
            <span className="analytics-chart__legend-swatch" style={{ background: '#64748b' }} />
            <span>{formatMessage({ id: 'charts.allYearsPeriod' })}</span>
          </li>
          <li>
            <span className="analytics-chart__legend-swatch" style={{ background: '#7c3aed' }} />
            <span>{formatMessage({ id: 'charts.varianceRightAxis' })}</span>
          </li>
        </ul>
      </div>
    </div>
  );
}
