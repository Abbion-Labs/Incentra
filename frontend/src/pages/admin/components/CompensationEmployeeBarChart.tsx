import { useMemo, useState } from 'react';
import type { CompensationAnalyticsSeriesPoint } from '../../../api/types';
import { ChartTooltip } from '../../../components/charts/ChartTooltip';
import {
  CHART,
  barPath,
  niceAxis,
  sequentialBlue,
} from '../../../components/charts/chartTheme';
import { useIntl } from '../../../i18n';
import { shortEmployeeLabel } from '../../../utils/compensationAnalytics';
import { formatAmount, formatPercent } from '../../../utils/formatLocale';
import { useAnalyticsChartContainerHeight } from './useAnalyticsChartContainerHeight';

interface CompensationEmployeeBarChartProps {
  series: CompensationAnalyticsSeriesPoint[];
  valueFormat: 'percent' | 'currency';
  currency: string;
  ariaLabel: string;
}

interface HoverState {
  index: number;
  x: number;
  y: number;
}

/** Tooltip ne sme da izađe iznad okvira grafikona (okvir ima skrol). */
const TOOLTIP_MIN_Y = 84;

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

/** Vrednost po zaposlenom: nijansa plave prati iznos (veći iznos je tamniji). */
export function CompensationEmployeeBarChart({
  series,
  valueFormat,
  currency,
  ariaLabel,
}: CompensationEmployeeBarChartProps) {
  const { formatMessage } = useIntl();
  const [hover, setHover] = useState<HoverState | null>(null);
  const {
    ref: containerRef,
    height,
    width: containerWidth,
  } = useAnalyticsChartContainerHeight(320);

  const maxValue = useMemo(
    () => Math.max(...series.map((point) => point.value), 1),
    [series],
  );

  // Osa sa lepim vrednostima; nijansa stubca i dalje prati najveću vrednost.
  const axis = niceAxis(maxValue);

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
  const baseline = padding.top + chartHeight;
  const groupWidth = chartWidth / series.length;
  const barWidth = Math.min(32, groupWidth * 0.6);

  const hovered = hover ? series[hover.index] : null;

  return (
    <div className="analytics-chart analytics-chart--responsive analytics-chart--fill">
      <div
        ref={containerRef}
        className="analytics-chart__canvas-wrap analytics-chart__canvas-wrap--fill"
      >
        {hover && hovered && (
          <ChartTooltip
            title={hovered.label}
            rows={[
              {
                // Naziv grafikona je već iznad; u tooltipu je dovoljna vrednost.
                label: '',
                value: formatValue(hovered.value, valueFormat, currency),
                color: sequentialBlue(hovered.value, maxValue),
              },
            ]}
            x={hover.x}
            y={Math.max(hover.y, TOOLTIP_MIN_Y)}
            containerWidth={width}
          />
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
          {axis.ticks.map((value) => {
            const y = baseline - (value / axis.max) * chartHeight;
            return (
              <g key={value}>
                <line
                  x1={padding.left}
                  y1={y}
                  x2={width - padding.right}
                  y2={y}
                  stroke={CHART.grid}
                />
                <text
                  x={padding.left - 8}
                  y={y + 4}
                  textAnchor="end"
                  className="chart-axis-text"
                >
                  {formatValue(value, valueFormat, currency)}
                </text>
              </g>
            );
          })}

          {series.map((point, index) => {
            const groupLeft = padding.left + index * groupWidth;
            const barHeight = (point.value / axis.max) * chartHeight;
            const x = groupLeft + (groupWidth - barWidth) / 2;
            const labelY = baseline + 20;
            const isHovered = hover?.index === index;

            return (
              <g key={`${point.label}-${index}`}>
                {isHovered && (
                  <rect
                    x={groupLeft + 2}
                    y={padding.top}
                    width={groupWidth - 4}
                    height={chartHeight}
                    rx={6}
                    fill={CHART.highlight}
                  />
                )}
                {barHeight > 0 && (
                  <path
                    d={barPath(x, baseline - barHeight, barWidth, barHeight)}
                    fill={sequentialBlue(point.value, maxValue)}
                  />
                )}
                <text
                  x={x + barWidth / 2}
                  y={labelY}
                  textAnchor="end"
                  className={`chart-category-text${isHovered ? ' is-selected' : ''}`}
                  transform={`rotate(-35, ${x + barWidth / 2}, ${labelY})`}
                >
                  {shortEmployeeLabel(point.label)}
                </text>
                <rect
                  x={groupLeft}
                  y={padding.top}
                  width={groupWidth}
                  height={chartHeight}
                  fill="transparent"
                  onMouseMove={(event) => {
                    const wrap = containerRef.current;
                    if (!wrap) return;
                    const box = wrap.getBoundingClientRect();
                    setHover({
                      index,
                      x: event.clientX - box.left + wrap.scrollLeft,
                      y: event.clientY - box.top,
                    });
                  }}
                  onMouseLeave={() => setHover(null)}
                />
              </g>
            );
          })}

          <line
            x1={padding.left}
            y1={baseline}
            x2={width - padding.right}
            y2={baseline}
            stroke={CHART.axisText}
            strokeOpacity={0.4}
          />
        </svg>
      </div>
    </div>
  );
}
