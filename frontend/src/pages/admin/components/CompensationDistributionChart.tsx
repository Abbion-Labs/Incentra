import { useMemo, useState } from 'react';
import type { CompensationAnalyticsBucket } from '../../../api/types';
import { useIntl } from '../../../i18n';
import { chartColor } from '../../../utils/compensationAnalytics';
import { useAnalyticsChartContainerHeight } from './useAnalyticsChartContainerHeight';

interface CompensationDistributionChartProps {
  buckets: CompensationAnalyticsBucket[];
  ariaLabel: string;
}

interface TooltipState {
  label: string;
  count: number;
  x: number;
  y: number;
}

export function CompensationDistributionChart({ buckets, ariaLabel }: CompensationDistributionChartProps) {
  const { formatMessage } = useIntl();
  const [tooltip, setTooltip] = useState<TooltipState | null>(null);
  const { ref: containerRef, height, width: containerWidth } = useAnalyticsChartContainerHeight(320);

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

  if (visibleBuckets.length === 0 || total === 0) {
    return (
      <div className="analytics-chart analytics-chart--empty">
        <p>{formatMessage({ id: 'charts.noDistributionData' })}</p>
      </div>
    );
  }

  const padding = { top: 24, right: 24, bottom: 108, left: 56 };
  const minContentWidth = visibleBuckets.length * 80 + padding.left + padding.right;
  const width = Math.max(containerWidth || 520, minContentWidth);
  const chartWidth = width - padding.left - padding.right;
  const chartHeight = height - padding.top - padding.bottom;
  const groupWidth = chartWidth / visibleBuckets.length;
  const barWidth = Math.min(56, groupWidth * 0.7);

  function showTooltip(
    event: React.MouseEvent<SVGRectElement>,
    bucket: CompensationAnalyticsBucket,
  ) {
    const wrap = event.currentTarget.closest('.analytics-chart__canvas-wrap');
    if (!wrap) return;
    const rect = wrap.getBoundingClientRect();
    setTooltip({
      label: bucket.label,
      count: bucket.count,
      x: event.clientX - rect.left,
      y: event.clientY - rect.top,
    });
  }

  return (
    <div className="analytics-chart analytics-chart--responsive analytics-chart--fill">
      <div ref={containerRef} className="analytics-chart__canvas-wrap analytics-chart__canvas-wrap--fill">
        {tooltip && (
          <div className="analytics-chart__tooltip" style={{ left: tooltip.x, top: tooltip.y }} role="tooltip">
            <span className="analytics-chart__tooltip-period">{tooltip.label}</span>
            <strong>{formatMessage({ id: 'charts.employeeCount' }, { count: tooltip.count })}</strong>
          </div>
        )}

        <svg
          viewBox={`0 0 ${width} ${height}`}
          className="analytics-chart__svg analytics-chart__svg--fill"
          preserveAspectRatio="xMinYMin meet"
          role="img"
          aria-label={ariaLabel}
          style={containerWidth > 0 && width > containerWidth ? { minWidth: width } : undefined}
        >
          {[0, 0.25, 0.5, 0.75, 1].map((tick) => {
            const value = Math.round(maxValue * tick);
            const y = padding.top + chartHeight - tick * chartHeight;
            return (
              <g key={tick}>
                <line x1={padding.left} y1={y} x2={width - padding.right} y2={y} stroke="#e2e8f0" />
                <text x={padding.left - 8} y={y + 4} textAnchor="end" fontSize="11" fill="#64748b">
                  {value}
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

          {visibleBuckets.map((bucket, index) => {
            const barHeight = (bucket.count / maxValue) * chartHeight;
            const x = padding.left + index * groupWidth + (groupWidth - barWidth) / 2;
            const y = padding.top + chartHeight - barHeight;

            return (
              <g key={bucket.label}>
                <rect
                  x={x}
                  y={y}
                  width={barWidth}
                  height={Math.max(barHeight, bucket.count > 0 ? 4 : 0)}
                  rx={4}
                  fill={chartColor(index)}
                  className="analytics-chart__bar"
                  onMouseEnter={(event) => showTooltip(event, bucket)}
                  onMouseMove={(event) => showTooltip(event, bucket)}
                  onMouseLeave={() => setTooltip(null)}
                />
                <text
                  x={x + barWidth / 2}
                  y={padding.top + chartHeight + 20}
                  textAnchor="end"
                  fontSize="10"
                  fill="#334155"
                  transform={`rotate(-35, ${x + barWidth / 2}, ${padding.top + chartHeight + 20})`}
                >
                  {bucket.label}
                </text>
              </g>
            );
          })}
        </svg>
      </div>
    </div>
  );
}
