import { useMemo, useState } from 'react';
import type { CompensationAnalyticsBucket } from '../../../api/types';
import { ChartTooltip } from '../../../components/charts/ChartTooltip';
import {
  CHART,
  barPath,
  niceAxis,
  sequentialBlue,
} from '../../../components/charts/chartTheme';
import { useIntl } from '../../../i18n';
import { useAnalyticsChartContainerHeight } from './useAnalyticsChartContainerHeight';

interface CompensationDistributionChartProps {
  buckets: CompensationAnalyticsBucket[];
  ariaLabel: string;
}

interface HoverState {
  index: number;
  x: number;
  y: number;
}

/** Tooltip ne sme da izađe iznad okvira grafikona (okvir ima skrol). */
const TOOLTIP_MIN_Y = 84;

/**
 * Broj zaposlenih po rasponu: nijansa plave prati broj (brojniji raspon je
 * tamniji), broj iznad svakog stupca.
 */
export function CompensationDistributionChart({
  buckets,
  ariaLabel,
}: CompensationDistributionChartProps) {
  const { formatMessage } = useIntl();
  const [hover, setHover] = useState<HoverState | null>(null);
  const {
    ref: containerRef,
    height,
    width: containerWidth,
  } = useAnalyticsChartContainerHeight(320);

  const visibleBuckets = useMemo(() => {
    const first = buckets.findIndex((bucket) => bucket.count > 0);
    if (first < 0) return buckets;

    let last = first;
    for (let index = buckets.length - 1; index >= first; index -= 1) {
      if (buckets[index].count > 0) {
        last = index;
        break;
      }
    }

    return buckets.slice(first, last + 1);
  }, [buckets]);

  const maxValue = useMemo(
    () => Math.max(...visibleBuckets.map((bucket) => bucket.count), 1),
    [visibleBuckets],
  );

  const total = useMemo(
    () => buckets.reduce((sum, bucket) => sum + bucket.count, 0),
    [buckets],
  );

  // Osa sa lepim vrednostima; nijansa stubca i dalje prati najveću vrednost.
  const axis = niceAxis(maxValue, { integer: true });

  if (visibleBuckets.length === 0 || total === 0) {
    return (
      <div className="analytics-chart analytics-chart--empty">
        <p>{formatMessage({ id: 'charts.noDistributionData' })}</p>
      </div>
    );
  }

  const padding = { top: 28, right: 24, bottom: 108, left: 56 };
  const minContentWidth =
    visibleBuckets.length * 56 + padding.left + padding.right;
  const width = Math.max(containerWidth || 520, minContentWidth);
  const chartWidth = width - padding.left - padding.right;
  const chartHeight = height - padding.top - padding.bottom;
  const baseline = padding.top + chartHeight;
  const groupWidth = chartWidth / visibleBuckets.length;
  const barWidth = Math.min(48, groupWidth * 0.6);

  const hovered = hover ? visibleBuckets[hover.index] : null;

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
                label: formatMessage(
                  { id: 'charts.employeeCount' },
                  { count: hovered.count },
                ),
                value: `${Math.round((hovered.count / total) * 100)}%`,
                color: sequentialBlue(hovered.count, maxValue),
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
                  {value}
                </text>
              </g>
            );
          })}

          {visibleBuckets.map((bucket, index) => {
            const groupLeft = padding.left + index * groupWidth;
            const barHeight = (bucket.count / axis.max) * chartHeight;
            const x = groupLeft + (groupWidth - barWidth) / 2;
            const y = baseline - barHeight;
            const labelY = baseline + 20;

            return (
              <g key={bucket.label}>
                {hover?.index === index && (
                  <rect
                    x={groupLeft + 4}
                    y={padding.top}
                    width={groupWidth - 8}
                    height={chartHeight}
                    rx={6}
                    fill={CHART.highlight}
                  />
                )}
                {bucket.count > 0 && (
                  <>
                    <path
                      d={barPath(x, y, barWidth, Math.max(barHeight, 4))}
                      fill={sequentialBlue(bucket.count, maxValue)}
                    />
                    <text
                      x={x + barWidth / 2}
                      y={y - 6}
                      textAnchor="middle"
                      className="chart-value-text"
                    >
                      {bucket.count}
                    </text>
                  </>
                )}
                <text
                  x={x + barWidth / 2}
                  y={labelY}
                  textAnchor="end"
                  className="chart-category-text"
                  transform={`rotate(-35, ${x + barWidth / 2}, ${labelY})`}
                >
                  {bucket.label}
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
