import type { DescriptiveRating } from '../api/types';
import type { IntlMessage, NestedKeys } from '../i18n/i18n.types';

export type DescriptiveRatingFormatMessage = (descriptor: {
  id: NestedKeys<IntlMessage>;
}) => string;

export const DESCRIPTIVE_RATING_NAME_KEYS: Record<
  string,
  NestedKeys<IntlMessage>
> = {
  DOES_NOT_MEET: 'descriptiveRatings.names.DOES_NOT_MEET',
  MEETS: 'descriptiveRatings.names.MEETS',
  GOOD: 'descriptiveRatings.names.GOOD',
  EXCEEDS: 'descriptiveRatings.names.EXCEEDS',
  OUTSTANDING: 'descriptiveRatings.names.OUTSTANDING',
};

const KNOWN_NAME_TO_CODE: Record<string, string> = {
  'Ne zadovoljava': 'DOES_NOT_MEET',
  Zadovoljava: 'MEETS',
  Dobar: 'GOOD',
  'Ističe se': 'EXCEEDS',
  'Naročito se ističe': 'OUTSTANDING',
};

const FALLBACK_NAME_BY_CODE: Record<string, string> = {
  DOES_NOT_MEET: 'Ne zadovoljava',
  MEETS: 'Zadovoljava',
  GOOD: 'Dobar',
  EXCEEDS: 'Ističe se',
  OUTSTANDING: 'Naročito se ističe',
};

const FALLBACK_BANDS = [
  { min: 0, max: 1.99, code: 'DOES_NOT_MEET' },
  { min: 2, max: 2.99, code: 'MEETS' },
  { min: 3, max: 3.49, code: 'GOOD' },
  { min: 3.5, max: 4.49, code: 'EXCEEDS' },
  { min: 4.5, max: 5, code: 'OUTSTANDING' },
] as const;

const DESCRIPTIVE_RATING_CLASS_BY_CODE: Record<string, string> = {
  DOES_NOT_MEET: 'badge-descriptive-does-not-meet',
  MEETS: 'badge-descriptive-meets',
  GOOD: 'badge-descriptive-good',
  EXCEEDS: 'badge-descriptive-exceeds',
  OUTSTANDING: 'badge-descriptive-outstanding',
};

// Divergentna skala za grafikone: ispod očekivanja topla, sredina siva, iznad
// očekivanja plava (bezbedno za daltonizam; susedne ocene ΔE ≥ 8).
const DESCRIPTIVE_RATING_CHART_COLOR_BY_CODE: Record<string, string> = {
  DOES_NOT_MEET: '#b2310e',
  MEETS: '#ee8a61',
  GOOD: '#d3d7dd',
  EXCEEDS: '#5a98d8',
  OUTSTANDING: '#1f5aa6',
};

// Boje za tekst opisne ocene: iste nijanse kao tekst bedževa, čitljive na
// beloj podlozi (svetli tonovi sa grafikona nisu za tekst).
const DESCRIPTIVE_RATING_TEXT_COLOR_BY_CODE: Record<string, string> = {
  DOES_NOT_MEET: '#b91c1c',
  MEETS: '#c2410c',
  GOOD: '#a16207',
  EXCEEDS: '#0f766e',
  OUTSTANDING: '#047857',
};

interface RatingBand {
  min: number;
  max: number;
  code: string;
  name?: string;
}

function bandsFromRatings(ratings: DescriptiveRating[]): RatingBand[] {
  return ratings
    .filter((r) => r.isActive && r.minAverage != null)
    .map((r) => ({
      min: r.minAverage!,
      max: r.maxAverage ?? Number.POSITIVE_INFINITY,
      code: r.code,
      name: r.name,
    }));
}

/**
 * Isto pravilo kao na serveru: opseg ide od svog minimuma do minimuma sledećeg,
 * pa svaki prosek upada u tačno jedan. Maksimum važi samo za najviši opseg.
 */
function findBand(
  bands: readonly RatingBand[],
  average: number,
): RatingBand | undefined {
  const sorted = [...bands].sort((a, b) => a.min - b.min);
  const reached = sorted.filter((item) => item.min <= average);
  const band = reached[reached.length - 1];
  if (!band) return undefined;
  const isTopBand = band === sorted[sorted.length - 1];
  return isTopBand && average > band.max ? undefined : band;
}

export function descriptiveRatingCodeFromName(
  name: string | null | undefined,
  ratings?: DescriptiveRating[],
): string | null {
  if (!name?.trim()) return null;
  const trimmed = name.trim();
  const fromRatings = ratings?.find((r) => r.name === trimmed)?.code;
  if (fromRatings) return fromRatings;
  return KNOWN_NAME_TO_CODE[trimmed] ?? null;
}

export function resolveDescriptiveRatingCode(
  options: {
    code?: string | null;
    name?: string | null;
    descriptiveRatingId?: number | null;
  },
  ratings?: DescriptiveRating[],
): string | null {
  if (options.code?.trim()) return options.code.trim();
  if (options.descriptiveRatingId != null && ratings?.length) {
    const found = ratings.find((r) => r.id === options.descriptiveRatingId);
    if (found) return found.code;
  }
  return descriptiveRatingCodeFromName(options.name, ratings);
}

export function formatDescriptiveRatingLabel(
  formatMessage: DescriptiveRatingFormatMessage,
  options: {
    code?: string | null;
    name?: string | null;
    descriptiveRatingId?: number | null;
  },
  ratings?: DescriptiveRating[],
): string | null {
  const code = resolveDescriptiveRatingCode(options, ratings);
  if (code && DESCRIPTIVE_RATING_NAME_KEYS[code]) {
    return formatMessage({ id: DESCRIPTIVE_RATING_NAME_KEYS[code] });
  }
  return options.name?.trim() || null;
}

export function descriptiveRatingCodeFromAverage(
  average: number,
  ratings?: DescriptiveRating[],
): string | null {
  const bands = ratings?.length ? bandsFromRatings(ratings) : FALLBACK_BANDS;
  const band = findBand(bands, average);
  return band?.code ?? null;
}

export function descriptiveRatingNameFromAverage(
  average: number,
  ratings?: DescriptiveRating[],
): string | null {
  const bands = ratings?.length ? bandsFromRatings(ratings) : FALLBACK_BANDS;
  const band = findBand(bands, average);
  if (!band) return null;
  if (band.name) return band.name;
  return FALLBACK_NAME_BY_CODE[band.code] ?? null;
}

function resolveCodeForStyling(
  nameOrCode: string | null | undefined,
  ratings?: DescriptiveRating[],
): string | null {
  if (!nameOrCode) return null;
  if (DESCRIPTIVE_RATING_NAME_KEYS[nameOrCode]) return nameOrCode;
  return descriptiveRatingCodeFromName(nameOrCode, ratings);
}

export function descriptiveRatingChartColor(
  nameOrCode: string | null | undefined,
  ratings?: DescriptiveRating[],
): string {
  const code = resolveCodeForStyling(nameOrCode, ratings);
  if (!code) return '#94a3b8';
  return DESCRIPTIVE_RATING_CHART_COLOR_BY_CODE[code] ?? '#64748b';
}

export function descriptiveRatingColor(
  nameOrCode: string | null | undefined,
  ratings?: DescriptiveRating[],
): string {
  const code = resolveCodeForStyling(nameOrCode, ratings);
  if (!code) return '#64748b';
  return DESCRIPTIVE_RATING_TEXT_COLOR_BY_CODE[code] ?? '#475569';
}

export function descriptiveRatingClass(
  nameOrCode: string | null | undefined,
  ratings?: DescriptiveRating[],
): string {
  if (!nameOrCode) return 'badge badge-descriptive-none';
  const code = resolveCodeForStyling(nameOrCode, ratings);
  const variant = code
    ? (DESCRIPTIVE_RATING_CLASS_BY_CODE[code] ?? 'badge-descriptive-default')
    : 'badge-descriptive-default';
  return `badge ${variant}`;
}
