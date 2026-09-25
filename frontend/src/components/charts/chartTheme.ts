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
