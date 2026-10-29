import { useLayoutEffect, useRef, useState } from 'react';

export interface ChartTooltipRow {
  label: string;
  value: string;
  color: string;
  /** Oznaka serije: kratka linija (podrazumevano) ili tanka crta za marker. */
  kind?: 'line' | 'marker';
}

interface ChartTooltipProps {
  title: string;
  rows: ChartTooltipRow[];
  x: number;
  y: number;
  /** Širina okvira grafikona, da tooltip ne izađe van njega. */
  containerWidth: number;
}

/** Procena širine pre prvog merenja. */
const ESTIMATED_WIDTH = 180;
const EDGE_GAP = 4;

/**
 * Tooltip grafikona: bela kartica, vrednost napred i istaknuta, naziv serije
 * sekundaran, serija označena kratkom linijom svoje boje.
 */
export function ChartTooltip({
  title,
  rows,
  x,
  y,
  containerWidth,
}: ChartTooltipProps) {
  const ref = useRef<HTMLDivElement>(null);
  const [tooltipWidth, setTooltipWidth] = useState(ESTIMATED_WIDTH);

  // Stvarna širina (zavisi od naziva serija), da tooltip uz ivicu ne izađe
  // van grafikona niti se suzi i skrati tekst.
  useLayoutEffect(() => {
    const measured = ref.current?.offsetWidth;
    if (measured && measured !== tooltipWidth) setTooltipWidth(measured);
  }, [title, tooltipWidth]);

  const half = tooltipWidth / 2 + EDGE_GAP;
  const left = Math.max(half, Math.min(x, containerWidth - half));
  return (
    <div
      ref={ref}
      className="chart-tooltip"
      style={{ left, top: y }}
      role="tooltip"
    >
      <div className="chart-tooltip__title">{title}</div>
      {rows.map((row) => (
        <div key={row.label} className="chart-tooltip__row">
          <span
            className={`chart-tooltip__key chart-tooltip__key--${row.kind ?? 'line'}`}
            style={{ background: row.color }}
            aria-hidden
          />
          <strong className="chart-tooltip__value">{row.value}</strong>
          <span className="chart-tooltip__label">{row.label}</span>
        </div>
      ))}
    </div>
  );
}
