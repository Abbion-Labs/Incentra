import { useMemo, useState } from 'react';
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

interface Segment {
  key: string;
  label: string;
  count: number;
  percentage: number;
  color: string;
  startAngle: number;
  endAngle: number;
}

const SIZE = 220;
const OUTER_RADIUS = 104;
const INNER_RADIUS = 74;
/** Razmak u boji podloge između delova prstena (u stepenima). */
const GAP_DEGREES = 1.4;
const TOTAL_TEXT_COLOR = '#0f172a';
const CAPTION_TEXT_COLOR = '#64748b';

function polar(radius: number, angle: number) {
  const rad = ((angle - 90) * Math.PI) / 180;
  return {
    x: SIZE / 2 + radius * Math.cos(rad),
    y: SIZE / 2 + radius * Math.sin(rad),
  };
}

function ringPath(startAngle: number, endAngle: number): string {
  // Ceo krug (jedina ocena) ne može jednim lukom: crta se kao dva polukruga.
  if (endAngle - startAngle >= 359.99) {
    return `${ringPath(0, 180)} ${ringPath(180, 360)}`;
  }
  const outerStart = polar(OUTER_RADIUS, startAngle);
  const outerEnd = polar(OUTER_RADIUS, endAngle);
  const innerEnd = polar(INNER_RADIUS, endAngle);
  const innerStart = polar(INNER_RADIUS, startAngle);
  const large = endAngle - startAngle > 180 ? 1 : 0;
  return [
    `M${outerStart.x},${outerStart.y}`,
    `A${OUTER_RADIUS},${OUTER_RADIUS} 0 ${large} 1 ${outerEnd.x},${outerEnd.y}`,
    `L${innerEnd.x},${innerEnd.y}`,
    `A${INNER_RADIUS},${INNER_RADIUS} 0 ${large} 0 ${innerStart.x},${innerStart.y}`,
    'Z',
  ].join(' ');
}

/** Raspodela opisnih ocena: prsten (udeo celine) i legenda sa brojevima. */
export function DescriptiveRatingPieChart({
  items,
  year,
}: DescriptiveRatingPieChartProps) {
  const { formatMessage } = useIntl();
  const [activeKey, setActiveKey] = useState<string | null>(null);

  const labelFor = (item: DescriptiveRatingDistributionItem) =>
    formatDescriptiveRatingLabel(formatMessage, {
      code: item.code,
      name: item.name,
    }) ?? item.name;

  const total = items.reduce((sum, item) => sum + item.count, 0);

  const segments = useMemo<Segment[]>(() => {
    if (total === 0) return [];
    let cursor = 0;
    return items
      .filter((item) => item.count > 0)
      .map((item) => {
        const sweep = (item.count / total) * 360;
        const gap = sweep >= 359.99 ? 0 : GAP_DEGREES / 2;
        const segment: Segment = {
          key: item.code,
          label:
            formatDescriptiveRatingLabel(formatMessage, {
              code: item.code,
              name: item.name,
            }) ?? item.name,
          count: item.count,
          percentage: item.percentage,
          color: descriptiveRatingChartColor(item.code),
          startAngle: cursor + gap,
          endAngle: cursor + sweep - gap,
        };
        cursor += sweep;
        return segment;
      });
  }, [items, total, formatMessage]);

  if (segments.length === 0) {
    return (
      <div className="analytics-chart analytics-chart--empty">
        <p>
          {formatMessage({ id: 'charts.noRatedEmployeesForYear' }, { year })}
        </p>
      </div>
    );
  }

  const active = segments.find((segment) => segment.key === activeKey);

  return (
    <div className="donut-chart">
      <div className="donut-chart__figure">
        <svg
          viewBox={`0 0 ${SIZE} ${SIZE}`}
          className="donut-chart__svg"
          role="img"
          aria-label={formatMessage(
            { id: 'charts.descriptiveRatingDistributionAria' },
            { year },
          )}
        >
          {segments.map((segment) => (
            <path
              key={segment.key}
              d={ringPath(segment.startAngle, segment.endAngle)}
              fill={segment.color}
              className="donut-chart__segment"
              opacity={activeKey && activeKey !== segment.key ? 0.3 : 1}
              onMouseEnter={() => setActiveKey(segment.key)}
              onMouseLeave={() => setActiveKey(null)}
            />
          ))}
          <text
            x={SIZE / 2}
            y={SIZE / 2 - 2}
            textAnchor="middle"
            className="donut-chart__total"
            fill={TOTAL_TEXT_COLOR}
          >
            {active ? active.count : total}
          </text>
          <text
            x={SIZE / 2}
            y={SIZE / 2 + 20}
            textAnchor="middle"
            className="donut-chart__caption"
            fill={CAPTION_TEXT_COLOR}
          >
            {active
              ? `${active.percentage.toFixed(1)}%`
              : formatMessage({ id: 'charts.ratingsTotal' }, { year })}
          </text>
        </svg>
      </div>

      <ul className="donut-chart__legend">
        {items.map((item) => {
          const selected = activeKey === item.code;
          return (
            <li
              key={item.descriptiveRatingId}
              className={`donut-chart__row${selected ? ' is-active' : ''}${activeKey && !selected ? ' is-dimmed' : ''}`}
              onMouseEnter={() => item.count > 0 && setActiveKey(item.code)}
              onMouseLeave={() => setActiveKey(null)}
            >
              <span
                className="donut-chart__swatch"
                style={{ background: descriptiveRatingChartColor(item.code) }}
                aria-hidden
              />
              <span className="donut-chart__label">{labelFor(item)}</span>
              <span className="donut-chart__count">{item.count}</span>
              <span className="donut-chart__percent">
                {item.percentage.toFixed(1)}%
              </span>
              <span className="donut-chart__bar" aria-hidden>
                <span
                  style={{
                    width: `${Math.min(item.percentage, 100)}%`,
                    background: descriptiveRatingChartColor(item.code),
                  }}
                />
              </span>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
