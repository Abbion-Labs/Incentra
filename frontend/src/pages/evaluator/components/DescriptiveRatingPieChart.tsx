import { useEffect, useMemo, useRef, useState } from 'react';
import type { DescriptiveRatingDistributionItem } from '../../../api/types';
import { useIntl } from '../../../i18n';
import {
  descriptiveRatingChartColor,
  formatDescriptiveRatingLabel,
} from '../../../utils/descriptiveRating';

interface DescriptiveRatingPieChartProps {
  items: DescriptiveRatingDistributionItem[];
  year: number;
}

interface Slice {
  key: string;
  label: string;
  count: number;
  percentage: number;
  color: string;
  startAngle: number;
  endAngle: number;
}

function polarToCartesian(
  cx: number,
  cy: number,
  radius: number,
  angle: number,
) {
  const rad = ((angle - 90) * Math.PI) / 180;
  return {
    x: cx + radius * Math.cos(rad),
    y: cy + radius * Math.sin(rad),
  };
}

function describeArc(
  cx: number,
  cy: number,
  radius: number,
  startAngle: number,
  endAngle: number,
) {
  const start = polarToCartesian(cx, cy, radius, endAngle);
  const end = polarToCartesian(cx, cy, radius, startAngle);
  const largeArc = endAngle - startAngle > 180 ? 1 : 0;
  return `M ${cx} ${cy} L ${start.x} ${start.y} A ${radius} ${radius} 0 ${largeArc} 0 ${end.x} ${end.y} Z`;
}

export function DescriptiveRatingPieChart({
  items,
  year,
}: DescriptiveRatingPieChartProps) {
  const { formatMessage } = useIntl();
  const [activeKey, setActiveKey] = useState<string | null>(null);
  const canvasRef = useRef<HTMLDivElement>(null);
  const [pieSize, setPieSize] = useState(280);

  useEffect(() => {
    const element = canvasRef.current;
    if (!element) return;

    const update = () => {
      const width = element.clientWidth;
      if (width > 0) {
        setPieSize(Math.round(width));
      }
    };

    update();
    const observer = new ResizeObserver(update);
    observer.observe(element);
    return () => observer.disconnect();
  }, []);

  const slices = useMemo<Slice[]>(() => {
    const total = items.reduce((sum, item) => sum + item.count, 0);
    if (total === 0) return [];

    let cursor = 0;
    return items
      .filter((item) => item.count > 0)
      .map((item) => {
        const sweep = (item.count / total) * 360;
        const label =
          formatDescriptiveRatingLabel(formatMessage, {
            code: item.code,
            name: item.name,
          }) ?? item.name;
        const slice: Slice = {
          key: item.code,
          label,
          count: item.count,
          percentage: item.percentage,
          color: descriptiveRatingChartColor(item.code),
          startAngle: cursor,
          endAngle: cursor + sweep,
        };
        cursor += sweep;
        return slice;
      });
  }, [items, formatMessage]);

  if (slices.length === 0) {
    return (
      <div className="analytics-chart analytics-chart--empty">
        <p>
          {formatMessage({ id: 'charts.noRatedEmployeesForYear' }, { year })}
        </p>
      </div>
    );
  }

  const size = 280;
  const cx = size / 2;
  const cy = size / 2;
  const radius = 108;

  return (
    <div
      className="analytics-chart analytics-pie-chart"
      style={{ '--pie-chart-size': `${pieSize}px` } as React.CSSProperties}
    >
      <div ref={canvasRef} className="analytics-pie-chart__canvas-wrap">
        <svg
          viewBox={`0 0 ${size} ${size}`}
          className="analytics-chart__svg"
          preserveAspectRatio="xMidYMid meet"
          role="img"
          aria-label={formatMessage(
            { id: 'charts.descriptiveRatingDistributionAria' },
            { year },
          )}
        >
          {slices.map((slice) => (
            <path
              key={slice.key}
              d={describeArc(cx, cy, radius, slice.startAngle, slice.endAngle)}
              fill={slice.color}
              opacity={activeKey && activeKey !== slice.key ? 0.45 : 1}
              onMouseEnter={() => setActiveKey(slice.key)}
              onMouseLeave={() => setActiveKey(null)}
            />
          ))}
          <circle cx={cx} cy={cy} r={52} fill="#fff" />
          <text
            x={cx}
            y={cy - 4}
            textAnchor="middle"
            fontSize="13"
            fill="#64748b"
          >
            {year}.
          </text>
          <text
            x={cx}
            y={cy + 16}
            textAnchor="middle"
            fontSize="18"
            fontWeight="600"
            fill="#0f172a"
          >
            {slices.reduce((sum, slice) => sum + slice.count, 0)}
          </text>
        </svg>
      </div>

      <ul className="analytics-chart__legend">
        {items.map((item) => {
          const label =
            formatDescriptiveRatingLabel(formatMessage, {
              code: item.code,
              name: item.name,
            }) ?? item.name;
          return (
            <li
              key={item.descriptiveRatingId}
              className={activeKey === item.code ? 'is-active' : undefined}
              onMouseEnter={() => setActiveKey(item.code)}
              onMouseLeave={() => setActiveKey(null)}
            >
              <span
                className="analytics-chart__legend-swatch"
                style={{ background: descriptiveRatingChartColor(item.code) }}
              />
              <span className="analytics-chart__legend-body">
                <span className="analytics-chart__legend-label">{label}</span>
                <span className="analytics-chart__legend-value">
                  {item.count} ({item.percentage.toFixed(1)}%)
                </span>
              </span>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
