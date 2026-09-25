import { useMemo, useState } from 'react';
import type { DescriptiveRatingComparisonItem } from '../../../api/types';
import { ChartTooltip } from '../../../components/charts/ChartTooltip';
import {
  BAR_GAP,
  CHART,
  MAX_BAR_WIDTH,
  barPath,
} from '../../../components/charts/chartTheme';
import { useElementWidth } from '../../../components/charts/useElementWidth';
import { useIntl } from '../../../i18n';
import { formatDescriptiveRatingLabel } from '../../../utils/descriptiveRating';

interface RatingCountComparisonChartProps {
  items: DescriptiveRatingComparisonItem[];
  previousYear: number;
  year: number;
}

// Small counts get a tick for every whole number. Above that the axis steps
// by 5, and moves on to 10, 20, 50, 100… once reaching the peak would take
// more than MAX_Y_INTERVALS steps.
const SMALL_PEAK = 5;
const MIN_STEP_ABOVE_SMALL_PEAK = 5;
const MAX_Y_INTERVALS = 10;

function yAxisStep(peak: number): number {
  if (peak <= SMALL_PEAK) return 1;

  const minStep = Math.max(peak / MAX_Y_INTERVALS, MIN_STEP_ABOVE_SMALL_PEAK);
  const magnitude = 10 ** Math.floor(Math.log10(minStep));
  return (
    [1, 2, 5]
      .map((factor) => factor * magnitude)
      .find((step) => step >= minStep) ?? 10 * magnitude
  );
}

// Obe godine u plavoj (prošla svetlija, ova brend plava kao na grafikonu
// proseka), preporuka narandžasta; provereno za daltonizam.
const PREVIOUS_YEAR_COLOR = '#7ea6d9';
const THIS_YEAR_COLOR = CHART.primary;
const RECOMMENDED_COLOR = CHART.tertiary;

const HEIGHT = 280;
/** Horizontalni prikaz (telefon): visina reda, debljina stubca, margine. */
const ROW_HEIGHT = 58;
const H_BAR = 10;
const H_LEFT = 4;
const H_RIGHT = 36;
const PADDING = { top: 16, right: 12, bottom: 44, left: 36 };

interface HoverState {
  index: number;
  x: number;
  y: number;
}

/**
 * Broj ocena po opisnoj oceni: prošla i ova godina kao stubci (ova godina
 * u brend plavoj, prošla svetlija), preporučena raspodela kao linija-cilj.
 */
export function RatingCountComparisonChart({
  items,
  previousYear,
  year,
}: RatingCountComparisonChartProps) {
  const { formatMessage } = useIntl();
  const { ref, width, nodeRef } = useElementWidth<HTMLDivElement>();
  const [hover, setHover] = useState<HoverState | null>(null);

  const { maxValue, yTicks } = useMemo(() => {
    const peak = items.reduce(
      (max, item) =>
        Math.max(
          max,
          item.previousYearCount,
          item.thisYearCount,
          item.recommendedCount,
        ),
      0,
    );
    const step = yAxisStep(peak);
    const intervals = Math.max(Math.ceil(peak / step), 1);
    return {
      maxValue: intervals * step,
      yTicks: Array.from({ length: intervals + 1 }, (_, index) => index * step),
    };
  }, [items]);

  if (items.length === 0) {
    return (
      <div className="analytics-chart analytics-chart--empty">
        <p>{formatMessage({ id: 'charts.noRatingComparisonData' })}</p>
      </div>
    );
  }

  const previousLabel = formatMessage(
    { id: 'charts.selectedYear' },
    { year: previousYear },
  );
  const currentLabel = formatMessage({ id: 'charts.selectedYear' }, { year });
  const recommendedLabel = formatMessage({ id: 'charts.recommended' });
  const ratingLabel = (item: DescriptiveRatingComparisonItem) =>
    formatDescriptiveRatingLabel(formatMessage, {
      code: item.code,
      name: item.name,
    }) ?? item.name;

  // Na uskom ekranu (telefon) ocene su redovi sa horizontalnim stupcima, pa se
  // nazivi ocena ne guraju ispod ose.
  const horizontal = width < 480;
  const rowsHeight = items.length * ROW_HEIGHT;
  const xFor = (value: number) =>
    H_LEFT + (value / maxValue) * (width - H_LEFT - H_RIGHT);

  const plotWidth = Math.max(width - PADDING.left - PADDING.right, 160);
  const plotHeight = HEIGHT - PADDING.top - PADDING.bottom;
  const groupWidth = plotWidth / items.length;
  const barWidth = Math.min(MAX_BAR_WIDTH, (groupWidth - 20) / 2);
  const baseline = PADDING.top + plotHeight;
  const yFor = (value: number) => baseline - (value / maxValue) * plotHeight;
  // Na uskom ekranu nazivi ocena idu u dva reda.
  const compact = groupWidth < 96;

  return (
    <div className="analytics-chart chart-block">
      <div className="chart-canvas" ref={ref}>
        {hover && (
          <ChartTooltip
            title={ratingLabel(items[hover.index])}
            rows={[
              {
                label: currentLabel,
                value: String(items[hover.index].thisYearCount),
                color: THIS_YEAR_COLOR,
              },
              {
                label: previousLabel,
                value: String(items[hover.index].previousYearCount),
                color: PREVIOUS_YEAR_COLOR,
              },
              {
                label: recommendedLabel,
                value: String(items[hover.index].recommendedCount),
                color: RECOMMENDED_COLOR,
                kind: 'marker',
              },
            ]}
            x={hover.x}
            y={hover.y}
            containerWidth={width}
          />
        )}
        {horizontal ? (
          <svg
            width={width}
            height={rowsHeight}
            className="chart-svg"
            role="img"
            aria-label={formatMessage({
              id: 'analytics.ratingCountComparisonAria',
            })}
          >
            {items.map((item, index) => {
              const top = index * ROW_HEIGHT;
              const barsTop = top + 22;
              const markerX = xFor(item.recommendedCount);
              return (
                <g key={item.descriptiveRatingId}>
                  {hover?.index === index && (
                    <rect
                      x={0}
                      y={top + 2}
                      width={width}
                      height={ROW_HEIGHT - 4}
                      rx={6}
                      fill={CHART.highlight}
                    />
                  )}
                  <text x={H_LEFT} y={top + 15} className="chart-category-text">
                    {ratingLabel(item)}
                  </text>
                  {[
                    {
                      value: item.previousYearCount,
                      color: PREVIOUS_YEAR_COLOR,
                    },
                    { value: item.thisYearCount, color: THIS_YEAR_COLOR },
                  ].map((bar, barIndex) => {
                    const y = barsTop + barIndex * (H_BAR + BAR_GAP);
                    const length = xFor(bar.value) - H_LEFT;
                    return (
                      <g key={barIndex}>
                        {length > 0 && (
                          <rect
                            x={H_LEFT}
                            y={y}
                            width={length}
                            height={H_BAR}
                            rx={3}
                            fill={bar.color}
                          />
                        )}
                        <text
                          x={
                            Math.max(
                              H_LEFT + Math.max(length, 0),
                              item.recommendedCount > 0 ? markerX + 2 : 0,
                            ) + 6
                          }
                          y={y + H_BAR - 1}
                          className={
                            barIndex === 1
                              ? 'chart-value-text'
                              : 'chart-axis-text'
                          }
                        >
                          {bar.value}
                        </text>
                      </g>
                    );
                  })}
                  {item.recommendedCount > 0 && (
                    <>
                      <line
                        x1={markerX}
                        x2={markerX}
                        y1={barsTop - 4}
                        y2={barsTop + H_BAR * 2 + BAR_GAP + 4}
                        stroke={CHART.surface}
                        strokeWidth={5}
                        strokeLinecap="round"
                      />
                      <line
                        x1={markerX}
                        x2={markerX}
                        y1={barsTop - 4}
                        y2={barsTop + H_BAR * 2 + BAR_GAP + 4}
                        stroke={RECOMMENDED_COLOR}
                        strokeWidth={2}
                        strokeLinecap="round"
                      />
                    </>
                  )}
                  <rect
                    x={0}
                    y={top}
                    width={width}
                    height={ROW_HEIGHT}
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
        ) : (
          <svg
            width={width}
            height={HEIGHT}
            className="chart-svg"
            role="img"
            aria-label={formatMessage({
              id: 'analytics.ratingCountComparisonAria',
            })}
          >
            {yTicks.map((tick) => (
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

            {items.map((item, index) => {
              const groupLeft = PADDING.left + index * groupWidth;
              const center = groupLeft + groupWidth / 2;
              const previousX = center - barWidth - BAR_GAP / 2;
              const currentX = center + BAR_GAP / 2;
              const markerY = yFor(item.recommendedCount);
              const label = ratingLabel(item);
              const words = label.split(' ');
              return (
                <g key={item.descriptiveRatingId}>
                  {hover?.index === index && (
                    <rect
                      x={groupLeft + 4}
                      y={PADDING.top}
                      width={groupWidth - 8}
                      height={plotHeight}
                      rx={6}
                      fill={CHART.highlight}
                    />
                  )}
                  <path
                    d={barPath(
                      previousX,
                      yFor(item.previousYearCount),
                      barWidth,
                      baseline - yFor(item.previousYearCount),
                    )}
                    fill={PREVIOUS_YEAR_COLOR}
                  />
                  <path
                    d={barPath(
                      currentX,
                      yFor(item.thisYearCount),
                      barWidth,
                      baseline - yFor(item.thisYearCount),
                    )}
                    fill={THIS_YEAR_COLOR}
                  />
                  {item.recommendedCount > 0 && (
                    <line
                      x1={previousX - 5}
                      x2={currentX + barWidth + 5}
                      y1={markerY}
                      y2={markerY}
                      stroke={CHART.surface}
                      strokeWidth={5}
                      strokeLinecap="round"
                    />
                  )}
                  {item.recommendedCount > 0 && (
                    <line
                      x1={previousX - 5}
                      x2={currentX + barWidth + 5}
                      y1={markerY}
                      y2={markerY}
                      stroke={RECOMMENDED_COLOR}
                      strokeWidth={2}
                      strokeLinecap="round"
                    />
                  )}
                  <text
                    x={center}
                    y={baseline + 18}
                    textAnchor="middle"
                    className="chart-category-text"
                  >
                    {compact && words.length > 1 ? (
                      <>
                        <tspan x={center}>{words.slice(0, -1).join(' ')}</tspan>
                        <tspan x={center} dy={14}>
                          {words[words.length - 1]}
                        </tspan>
                      </>
                    ) : (
                      label
                    )}
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
        )}
      </div>

      <ul className="chart-legend">
        <li>
          <span
            className="chart-legend__swatch"
            style={{ background: PREVIOUS_YEAR_COLOR }}
            aria-hidden
          />
          {previousLabel}
        </li>
        <li>
          <span
            className="chart-legend__swatch"
            style={{ background: THIS_YEAR_COLOR }}
            aria-hidden
          />
          {currentLabel}
        </li>
        <li>
          <span
            className="chart-legend__line"
            style={{ background: RECOMMENDED_COLOR }}
            aria-hidden
          />
          {recommendedLabel}
        </li>
      </ul>
    </div>
  );
}
