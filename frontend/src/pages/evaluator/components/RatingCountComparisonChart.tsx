import { useMemo, useState } from 'react';
import type { DescriptiveRatingComparisonItem } from '../../../api/types';
import { useIntl } from '../../../i18n';
import { formatDescriptiveRatingLabel } from '../../../utils/descriptiveRating';

interface RatingCountComparisonChartProps {
  items: DescriptiveRatingComparisonItem[];
  previousYear: number;
  year: number;
}

const SERIES_KEYS = [
  { key: 'previousYearCount' as const, labelKey: 'charts.previousYear', color: '#78909c' },
  { key: 'thisYearCount' as const, labelKey: 'charts.thisYear', color: '#00897b' },
  { key: 'recommendedCount' as const, labelKey: 'charts.recommended', color: '#ef6c00' },
];

interface TooltipState {
  label: string;
  value: number;
  rating: string;
  x: number;
  y: number;
}

export function RatingCountComparisonChart({ items, previousYear, year }: RatingCountComparisonChartProps) {
  const { formatMessage } = useIntl();
  const [tooltip, setTooltip] = useState<TooltipState | null>(null);

  const maxValue = useMemo(() => {
    const peak = items.reduce((max, item) => {
      const localMax = Math.max(item.previousYearCount, item.thisYearCount, item.recommendedCount);
      return Math.max(max, localMax);
    }, 0);
    return Math.max(peak, 1);
  }, [items]);

  if (items.length === 0) {
    return (
      <div className="analytics-chart analytics-chart--empty">
        <p>{formatMessage({ id: 'charts.noRatingComparisonData' })}</p>
      </div>
    );
  }

  const width = 720;
  const height = 300;
  const padding = { top: 24, right: 16, bottom: 56, left: 40 };
  const chartWidth = width - padding.left - padding.right;
  const chartHeight = height - padding.top - padding.bottom;
  const groupWidth = chartWidth / items.length;
  const barWidth = Math.min(16, (groupWidth - 20) / 3);

  function showTooltip(
    event: React.MouseEvent<SVGRectElement>,
    seriesLabel: string,
    value: number,
    rating: string,
  ) {
    const wrap = event.currentTarget.closest('.analytics-chart__canvas-wrap');
    if (!wrap) return;
    const rect = wrap.getBoundingClientRect();
    setTooltip({
      label: seriesLabel,
      value,
      rating,
      x: event.clientX - rect.left,
      y: event.clientY - rect.top,
    });
  }

  return (
    <div className="analytics-chart">
      <div className="analytics-chart__canvas-wrap">
        {tooltip && (
          <div className="analytics-chart__tooltip" style={{ left: tooltip.x, top: tooltip.y }} role="tooltip">
            <span className="analytics-chart__tooltip-period">{tooltip.rating}</span>
            <strong>
              {tooltip.label}: {tooltip.value}
            </strong>
          </div>
        )}

        <svg viewBox={`0 0 ${width} ${height}`} className="analytics-chart__svg" role="img" aria-label={formatMessage({ id: 'analytics.ratingCountComparisonAria' })}>
          {Array.from({ length: maxValue + 1 }, (_, tick) => {
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

          {items.map((item, index) => {
            const ratingLabel = formatDescriptiveRatingLabel(formatMessage, { code: item.code, name: item.name }) ?? item.name;
            const groupX = padding.left + index * groupWidth + groupWidth / 2;
            return (
              <g key={item.descriptiveRatingId}>
                {SERIES_KEYS.map((series, seriesIndex) => {
                  const seriesLabel = formatMessage({ id: series.labelKey as never });
                  const value = item[series.key];
                  const barHeight = (value / maxValue) * chartHeight;
                  const x = groupX - barWidth * 1.5 + seriesIndex * (barWidth + 4);
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
                      opacity={0.9}
                      onMouseEnter={(event) => showTooltip(event, seriesLabel, value, ratingLabel)}
                      onMouseMove={(event) => showTooltip(event, seriesLabel, value, ratingLabel)}
                      onMouseLeave={() => setTooltip(null)}
                    />
                  );
                })}
                <text
                  x={groupX}
                  y={height - padding.bottom + 18}
                  textAnchor="middle"
                  fontSize="10"
                  fill="#475569"
                >
                  {ratingLabel}
                </text>
              </g>
            );
          })}
        </svg>
      </div>

      <div className="analytics-chart__footer">
        <ul className="analytics-chart__legend analytics-chart__legend--inline">
          {SERIES_KEYS.map((series) => (
            <li key={series.key}>
              <span className="analytics-chart__legend-swatch" style={{ background: series.color }} />
              <span>
                {series.key === 'previousYearCount'
                  ? formatMessage({ id: 'charts.selectedYear' }, { year: previousYear })
                  : series.key === 'thisYearCount'
                    ? formatMessage({ id: 'charts.selectedYear' }, { year })
                    : formatMessage({ id: series.labelKey as never })}
              </span>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}
