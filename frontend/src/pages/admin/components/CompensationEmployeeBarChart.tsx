import { useMemo, useState } from 'react';
import type { CompensationAnalyticsSeriesPoint } from '../../../api/types';
import { useIntl } from '../../../i18n';
import {
  chartColor,
  shortEmployeeLabel,
} from '../../../utils/compensationAnalytics';
import { formatAmount, formatPercent } from '../../../utils/formatLocale';
import { useAnalyticsChartContainerHeight } from './useAnalyticsChartContainerHeight';

interface CompensationEmployeeBarChartProps {
  series: CompensationAnalyticsSeriesPoint[];
  valueFormat: 'percent' | 'currency';
  currency: string;
  ariaLabel: string;
}

interface TooltipState {
  label: string;
  value: number;
  x: number;
  y: number;
}

function formatValue(
  value: number,
  valueFormat: 'percent' | 'currency',
  currency: string,
): string {
  if (valueFormat === 'percent') {
    return formatPercent(value / 100, 1);
  }

  return formatAmount(value, currency, 0);
}

export function CompensationEmployeeBarChart({
  series,
  valueFormat,
  currency,
  ariaLabel,
}: CompensationEmployeeBarChartProps) {
  const { formatMessage } = useIntl();
  const [tooltip, setTooltip] = useState<TooltipState | null>(null);
  const {
    ref: containerRef,
    height,
    width: containerWidth,
  } = useAnalyticsChartContainerHeight(320);

  const maxValue = useMemo(
    () =>
      Math.max(
        ...series.map((point) => point.value),
        valueFormat === 'percent' ? 1 : 1,
      ),
    [series, valueFormat],
  );

  if (series.length === 0) {
    return (
      <div className="analytics-chart analytics-chart--empty">
        <p>{formatMessage({ id: 'charts.noChartData' })}</p>
      </div>
    );
  }

  const barSlotWidth = 48;
  const padding = {
    top: 24,
    right: 24,
    bottom: 108,
    left: valueFormat === 'currency' ? 96 : 64,
  };
  const minContentWidth =
    series.length * barSlotWidth + padding.left + padding.right;
  const width = Math.max(containerWidth || 520, minContentWidth);
  const chartWidth = width - padding.left - padding.right;
  const chartHeight = height - padding.top - padding.bottom;
  const groupWidth = chartWidth / series.length;
  const barWidth = Math.min(40, groupWidth * 0.65);

  function showTooltip(
    event: React.MouseEvent<SVGRectElement>,
    point: CompensationAnalyticsSeriesPoint,
  ) {
    const wrap = event.currentTarget.closest('.analytics-chart__canvas-wrap');
    if (!wrap) return;
    const rect = wrap.getBoundingClientRect();
    setTooltip({
      label: point.label,
      value: point.value,
      x: event.clientX - rect.left,
      y: event.clientY - rect.top,
    });
  }

  return (
    <div className="analytics-chart analytics-chart--responsive analytics-chart--fill">
      <div
        ref={containerRef}
        className="analytics-chart__canvas-wrap analytics-chart__canvas-wrap--fill"
      >
        {tooltip && (
          <div
            className="analytics-chart__tooltip"
            style={{ left: tooltip.x, top: tooltip.y }}
            role="tooltip"
          >
            <span className="analytics-chart__tooltip-period">
              {tooltip.label}
            </span>
            <strong>{formatValue(tooltip.value, valueFormat, currency)}</strong>
          </div>
        )}

        <svg
          viewBox={`0 0 ${width} ${height}`}
          className="analytics-chart__svg analytics-chart__svg--fill"
          preserveAspectRatio="xMinYMin meet"
          role="img"
          aria-label={ariaLabel}
          style={
            containerWidth > 0 && width > containerWidth
              ? { minWidth: width }
              : undefined
          }
        >
          {[0, 0.25, 0.5, 0.75, 1].map((tick) => {
            const value = maxValue * tick;
            const y = padding.top + chartHeight - tick * chartHeight;
            return (
              <g key={tick}>
                <line
                  x1={padding.left}
                  y1={y}
                  x2={width - padding.right}
                  y2={y}
                  stroke="#e2e8f0"
                />
                <text
                  x={padding.left - 8}
                  y={y + 4}
                  textAnchor="end"
                  fontSize="11"
                  fill="#64748b"
                >
                  {formatValue(value, valueFormat, currency)}
                </text>
              </g>
            );
          })}

          <line
            x1={padding.left}
            y1={padding.top + chartHeight}
            x2={width - padding.right}
            y2={padding.top + chartHeight}
            stroke="#94a3b8"
          />

          {series.map((point, index) => {
            const barHeight = (point.value / maxValue) * chartHeight;
            const x =
              padding.left + index * groupWidth + (groupWidth - barWidth) / 2;
            const y = padding.top + chartHeight - barHeight;

            return (
              <g key={`${point.label}-${index}`}>
                <rect
                  x={x}
                  y={y}
                  width={barWidth}
                  height={Math.max(barHeight, 0)}
                  rx={4}
                  fill={chartColor(index)}
                  className="analytics-chart__bar"
                  onMouseEnter={(event) => showTooltip(event, point)}
                  onMouseMove={(event) => showTooltip(event, point)}
                  onMouseLeave={() => setTooltip(null)}
                />
                <text
                  x={x + barWidth / 2}
                  y={padding.top + chartHeight + 20}
                  textAnchor="end"
                  fontSize="11"
                  fill="#334155"
                  transform={`rotate(-35, ${x + barWidth / 2}, ${padding.top + chartHeight + 20})`}
                >
                  {shortEmployeeLabel(point.label)}
                </text>
              </g>
            );
          })}
        </svg>
      </div>
    </div>
  );
}
