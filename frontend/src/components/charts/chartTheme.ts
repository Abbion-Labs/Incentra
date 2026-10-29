/**
 * Zajedničke boje i mere grafikona. Palete su proverene validatorom za
 * daltonizam (susedne boje ΔE ≥ 8, normalan vid ≥ 15); vrednosti nisu samo
 * boja — legende uvek ispisuju brojeve.
 */
export const CHART = {
  /** Glavna serija (tekuća godina, zaposleni). */
  primary: '#1f5aa6',
  /** Ono sa čim se poredi (prethodni period, sve godine). */
  muted: '#8593a8',
  /** Druga i treća serija kada su sve tri ravnopravne (org. jedinica, radno mesto). */
  secondary: '#0d9488',
  tertiary: '#d97706',
  /** Oznaka cilja/preporuke: tamna linija, ne stubac. */
  marker: '#0f172a',
  grid: '#e8ecf1',
  axisText: '#64748b',
  labelText: '#334155',
  surface: '#ffffff',
  /** Pozadina istaknute grupe (izabrani kvartal). */
  highlight: '#f1f5fb',
} as const;

/** Maksimalna debljina stubca; ostatak mesta u grupi je vazduh. */
export const MAX_BAR_WIDTH = 24;
/** Razmak (u boji podloge) između stubaca iste grupe. */
export const BAR_GAP = 2;
export const BAR_RADIUS = 4;

/**
 * Stubac sa zaobljenim vrhom i ravnim dnom na osnovnoj liniji.
 */
export function barPath(
  x: number,
  y: number,
  width: number,
  height: number,
  radius = BAR_RADIUS,
): string {
  if (height <= 0) return '';
  const r = Math.min(radius, width / 2, height);
  const bottom = y + height;
  return [
    `M${x},${bottom}`,
    `V${y + r}`,
    `Q${x},${y} ${x + r},${y}`,
    `H${x + width - r}`,
    `Q${x + width},${y} ${x + width},${y + r}`,
    `V${bottom}`,
    'Z',
  ].join(' ');
}

/** Plava skala od svetle ka tamnoj, za veličinu (veća vrednost je tamnija). */
export const SEQUENTIAL_BLUES = [
  '#b9d0ee',
  '#8fb3e0',
  '#5f92d2',
  '#3673bd',
  '#1f5aa6',
  '#173f7a',
] as const;

export function sequentialBlue(value: number, max: number): string {
  if (max <= 0) return SEQUENTIAL_BLUES[0];
  const step = Math.floor(
    (Math.max(value, 0) / max) * (SEQUENTIAL_BLUES.length - 1) + 1e-4,
  );
  return SEQUENTIAL_BLUES[Math.min(step, SEQUENTIAL_BLUES.length - 1)];
}

/**
 * Osa sa „lepim“ vrednostima: korak je 1, 2, 2,5 ili 5 × 10ⁿ, a vrh ose je
 * prvi ceo korak iznad najveće vrednosti (npr. 11 → 0, 5, 10, 15).
 */
export function niceAxis(
  maxValue: number,
  { integer = false, targetSteps = 4 } = {},
): { max: number; ticks: number[] } {
  const max = maxValue > 0 ? maxValue : 1;
  const rough = max / targetSteps;
  const magnitude = 10 ** Math.floor(Math.log10(rough));
  const factors =
    integer && magnitude < 10 ? [1, 2, 5, 10] : [1, 2, 2.5, 5, 10];
  let step =
    (factors.find((factor) => factor * magnitude >= rough) ?? 10) * magnitude;
  if (integer) step = Math.max(1, Math.round(step));
  const axisMax = Math.ceil(max / step) * step;
  const count = Math.round(axisMax / step);
  return {
    max: axisMax,
    ticks: Array.from({ length: count + 1 }, (_, index) =>
      Number((index * step).toFixed(6)),
    ),
  };
}
