import type { DescriptiveRating } from '../api/types';
import type { IntlMessage, NestedKeys } from '../i18n/i18n.types';

export type DescriptiveRatingFormatMessage = (
  descriptor: { id: NestedKeys<IntlMessage> },
) => string;

export const DESCRIPTIVE_RATING_NAME_KEYS: Record<string, NestedKeys<IntlMessage>> = {
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

const DESCRIPTIVE_RATING_CHART_COLOR_BY_CODE: Record<string, string> = {
  DOES_NOT_MEET: '#dc2626',
  MEETS: '#f97316',
  GOOD: '#2563eb',
  EXCEEDS: '#7c3aed',
  OUTSTANDING: '#059669',
};

function bandsFromRatings(ratings: DescriptiveRating[]) {
  return ratings
    .filter((r) => r.isActive && r.minAverage != null && r.maxAverage != null)
    .sort((a, b) => a.sortOrder - b.sortOrder)
    .map((r) => ({
      min: r.minAverage!,
      max: r.maxAverage!,
      code: r.code,
      name: r.name,
    }));
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
  options: { code?: string | null; name?: string | null; descriptiveRatingId?: number | null },
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
  options: { code?: string | null; name?: string | null; descriptiveRatingId?: number | null },
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
  const band = bands.find((item) => average >= item.min && average <= item.max);
  return band?.code ?? null;
}

export function descriptiveRatingNameFromAverage(
  average: number,
  ratings?: DescriptiveRating[],
): string | null {
  const bands = ratings?.length ? bandsFromRatings(ratings) : FALLBACK_BANDS;
  const band = bands.find((item) => average >= item.min && average <= item.max);
  if (!band) return null;
  if ('name' in band && band.name) return band.name;
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
  return descriptiveRatingChartColor(nameOrCode, ratings);
}

export function descriptiveRatingClass(
  nameOrCode: string | null | undefined,
  ratings?: DescriptiveRating[],
): string {
  if (!nameOrCode) return 'badge badge-descriptive-none';
  const code = resolveCodeForStyling(nameOrCode, ratings);
  const variant = code
    ? DESCRIPTIVE_RATING_CLASS_BY_CODE[code] ?? 'badge-descriptive-default'
    : 'badge-descriptive-default';
  return `badge ${variant}`;
}
