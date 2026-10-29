import { useMemo, useState } from 'react';
import type { CompensationPreviewPoint } from '../compensationPreview';
import { ChartTooltip } from '../../../components/charts/ChartTooltip';
import { CHART, barPath } from '../../../components/charts/chartTheme';
import { useIntl } from '../../../i18n';
import { formatAmount, formatNumber } from '../../../utils/formatLocale';

interface CompensationRatingPreviewChartProps {
  points: CompensationPreviewPoint[];
  currency: string;
  acceptablePerformanceRating: number;
  /** U panelu pored forme — kompaktniji prikaz koji popunjava visinu */
  variant?: 'default' | 'panel';
  hideFooter?: boolean;
}

/** Tooltip ne sme da izađe iznad okvira grafikona. */
const TOOLTIP_MIN_Y = 84;
const BELOW_THRESHOLD_COLOR = '#cbd5e1';

/** Naziv ide posle vrednosti u tooltipu, pa mu dvotačka ne treba. */
function withoutColon(label: string): string {
  return label.replace(/:\s*$/, '');
}

function formatMoney(value: number, currency: string): string {
  return formatAmount(value, currency, 0);
}

function formatAxisMoney(value: number): string {
  return formatNumber(value, 0);
}

/** Najmanji „lep“ korak koji pokriva opseg, sa blažim zaokruživanjem (niže cifre na osi) */
function stepForAxis(range: number, segments: number): number {
  const minStep = Math.max(range, 1) / segments;
  const exponent = Math.floor(Math.log10(minStep));
  const magnitude = 10 ** exponent;
  const fraction = minStep / magnitude;

  let step: number;
  if (fraction <= 1) step = magnitude;
  else if (fraction <= 2) step = 2 * magnitude;
  else if (fraction <= 2.5) step = 2.5 * magnitude;
  else if (fraction <= 5) step = 5 * magnitude;
  else step = 10 * magnitude;

  if (step * segments < range) {
    if (step === magnitude) step = 2 * magnitude;
    else if (step === 2 * magnitude) step = 2.5 * magnitude;
    else if (step === 2.5 * magnitude) step = 5 * magnitude;
    else step = 10 * magnitude;
  }

  return step;
}

/** Uvek tačno 5 vrednosti na vertikalnoj osi */
function buildNiceAxisScale(
  minValue: number,
  maxValue: number,
): {
  axisMin: number;
  axisMax: number;
  ticks: number[];
} {
  const tickCount = 5;
  const segments = tickCount - 1;
  const safeMax = Math.max(maxValue, 1);
  const axisMin =
    minValue < 0 ? -stepForAxis(Math.abs(minValue), segments) * segments : 0;
  const step = stepForAxis(safeMax - axisMin, segments);
  const axisMax = axisMin + step * segments;

  const ticks = Array.from(
    { length: tickCount },
    (_, index) => axisMin + step * index,
  );

  return { axisMin, axisMax, ticks };
}

export function CompensationRatingPreviewChart({
  points,
  currency,
  acceptablePerformanceRating,
  variant = 'default',
  hideFooter = false,
}: CompensationRatingPreviewChartProps) {
  const { formatMessage } = useIntl();
  const [hover, setHover] = useState<{
    index: number;
    x: number;
    y: number;
    wrapWidth: number;
  } | null>(null);

  const isPanel = variant === 'panel';

  // Ispod praga prihvatljivog učinka nema varijabile: sivo; iznad: brend plava.
  const barColor = (rating: number) =>
    rating < acceptablePerformanceRating
      ? BELOW_THRESHOLD_COLOR
      : CHART.primary;

  const dataMax = useMemo(
    () => Math.max(...points.map((point) => point.monthlyVariable), 0),
    [points],
  );

  const dataMin = useMemo(
    () => Math.min(...points.map((point) => point.monthlyVariable), 0),
    [points],
  );

  const {
    axisMin,
    axisMax,
    ticks: axisTicks,
  } = useMemo(
    () => buildNiceAxisScale(dataMin, Math.max(dataMax, 1)),
    [dataMin, dataMax],
  );

  const axisRange = Math.max(axisMax - axisMin, 1);
  const width = isPanel ? 520 : 720;
  const height = isPanel ? 300 : 320;
  const padding = isPanel
    ? { top: 20, right: 16, bottom: 48, left: 88 }
    : { top: 24, right: 16, bottom: 56, left: 96 };
  const chartWidth = width - padding.left - padding.right;
  const chartHeight = height - padding.top - padding.bottom;
  const groupWidth = chartWidth / points.length;
  const barWidth = Math.min(isPanel ? 40 : 48, groupWidth * 0.55);

  function showTooltip(event: React.MouseEvent<SVGRectElement>, index: number) {
    const wrap = event.currentTarget.closest('.analytics-chart__canvas-wrap');
    if (!wrap) return;
    const rect = wrap.getBoundingClientRect();
    setHover({
      index,
      x: event.clientX - rect.left,
      y: Math.max(event.clientY - rect.top, TOOLTIP_MIN_Y),
      wrapWidth: rect.width,
    });
  }

  const hovered = hover ? points[hover.index] : null;

  return (
    <div
      className={`analytics-chart${isPanel ? ' analytics-chart--panel' : ''}`}
    >
      <div className="analytics-chart__canvas-wrap">
        {hover && hovered && (
          <ChartTooltip
            title={formatMessage(
              { id: 'charts.ratingTooltip' },
              { rating: hovered.rating },
            )}
            rows={[
              {
                label: withoutColon(formatMessage({ id: 'charts.monthly' })),
                value: formatMoney(hovered.monthlyVariable, currency),
                color: barColor(hovered.rating),
              },
              {
                label: withoutColon(formatMessage({ id: 'charts.annual' })),
                value: formatMoney(hovered.annualVariable, currency),
                color: barColor(hovered.rating),
              },
            ]}
            x={hover.x}
            y={hover.y}
            containerWidth={hover.wrapWidth}
          />
        )}

        <svg
          viewBox={`0 0 ${width} ${height}`}
          className="analytics-chart__svg"
          preserveAspectRatio="xMinYMid meet"
          role="img"
          aria-label={formatMessage({ id: 'charts.compensationPreviewAria' })}
        >
          <text
            x={12}
            y={padding.top + chartHeight / 2}
            textAnchor="middle"
            className="chart-axis-text"
            transform={`rotate(-90, 12, ${padding.top + chartHeight / 2})`}
          >
            {currency}
          </text>

          {axisTicks.map((value) => {
            const y =
              padding.top +
              chartHeight -
              ((value - axisMin) / axisRange) * chartHeight;
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
                  x={padding.left - 10}
                  y={y + 4}
                  textAnchor="end"
                  className="chart-axis-text"
                >
                  {formatAxisMoney(value)}
                </text>
              </g>
            );
          })}

          <line
            x1={padding.left}
            y1={padding.top + chartHeight}
            x2={width - padding.right}
            y2={padding.top + chartHeight}
            stroke={CHART.axisText}
            strokeOpacity={0.4}
          />

          {points.map((point, index) => {
            const normalized = (point.monthlyVariable - axisMin) / axisRange;
            const barHeight = Math.max(normalized * chartHeight, 0);
            const x =
              padding.left + index * groupWidth + (groupWidth - barWidth) / 2;
            const belowThreshold = point.rating < acceptablePerformanceRating;
            const visibleHeight = Math.max(
              barHeight,
              belowThreshold && point.monthlyVariable === 0 ? 2 : 0,
            );

            return (
              <g key={point.rating}>
                {hover?.index === index && (
                  <rect
                    x={padding.left + index * groupWidth + 4}
                    y={padding.top}
                    width={groupWidth - 8}
                    height={chartHeight}
                    rx={6}
                    fill={CHART.highlight}
                  />
                )}
                {visibleHeight > 0 && (
                  <path
                    d={barPath(
                      x,
                      padding.top + chartHeight - visibleHeight,
                      barWidth,
                      visibleHeight,
                    )}
                    fill={barColor(point.rating)}
                  />
                )}
                <rect
                  x={padding.left + index * groupWidth}
                  y={padding.top}
                  width={groupWidth}
                  height={chartHeight}
                  fill="transparent"
                  onMouseMove={(event) => showTooltip(event, index)}
                  onMouseLeave={() => setHover(null)}
                />
                <text
                  x={x + barWidth / 2}
                  y={padding.top + chartHeight + 22}
                  textAnchor="middle"
                  className="chart-category-text"
                >
                  {point.rating}
                </text>
              </g>
            );
          })}
        </svg>
      </div>

      {!hideFooter && (
        <div className="analytics-chart__footer">
          <p className="card__hint" style={{ margin: 0 }}>
            {formatMessage(
              { id: 'charts.compensationPreviewHint' },
              { threshold: acceptablePerformanceRating },
            )}
          </p>
        </div>
      )}
    </div>
  );
}
