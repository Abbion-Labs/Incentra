import type { IntlMessage, NestedKeys } from '../i18n/i18n.types';

export type MeasureFormatMessage = (
  descriptor: { id: NestedKeys<IntlMessage> },
) => string;

export const MEASURE_TYPE_DESCRIPTION_KEYS: Record<string, string> = {
  INITIATIVE: 'measures.descriptions.INITIATIVE',
  CREATIVITY: 'measures.descriptions.CREATIVITY',
  INDEPENDENCE: 'measures.descriptions.INDEPENDENCE',
  PRECISION: 'measures.descriptions.PRECISION',
  COLLABORATION: 'measures.descriptions.COLLABORATION',
  ADDITIONAL: 'measures.descriptions.ADDITIONAL',
};

export const MEASURE_TYPE_NAME_KEYS: Record<string, NestedKeys<IntlMessage>> = {
  INITIATIVE: 'measures.names.INITIATIVE',
  CREATIVITY: 'measures.names.CREATIVITY',
  INDEPENDENCE: 'measures.names.INDEPENDENCE',
  PRECISION: 'measures.names.PRECISION',
  COLLABORATION: 'measures.names.COLLABORATION',
  ADDITIONAL: 'measures.names.ADDITIONAL',
};

export function formatMeasureTypeName(
  formatMessage: MeasureFormatMessage,
  options: { code?: string | null; name?: string | null },
): string {
  const code = options.code?.trim();
  if (code && MEASURE_TYPE_NAME_KEYS[code]) {
    return formatMessage({ id: MEASURE_TYPE_NAME_KEYS[code] });
  }
  return options.name?.trim() || '';
}

const MEASURE_RATING_COMMENT_KEYS: Record<string, Record<number, string>> = {
  INDEPENDENCE: {
    1: 'measures.comments.INDEPENDENCE.1',
    2: 'measures.comments.INDEPENDENCE.2',
    3: 'measures.comments.INDEPENDENCE.3',
    4: 'measures.comments.INDEPENDENCE.4',
    5: 'measures.comments.INDEPENDENCE.5',
  },
  CREATIVITY: {
    1: 'measures.comments.CREATIVITY.1',
    2: 'measures.comments.CREATIVITY.2',
    3: 'measures.comments.CREATIVITY.3',
    4: 'measures.comments.CREATIVITY.4',
    5: 'measures.comments.CREATIVITY.5',
  },
  INITIATIVE: {
    1: 'measures.comments.INITIATIVE.1',
    2: 'measures.comments.INITIATIVE.2',
    3: 'measures.comments.INITIATIVE.3',
    4: 'measures.comments.INITIATIVE.4',
    5: 'measures.comments.INITIATIVE.5',
  },
  PRECISION: {
    1: 'measures.comments.PRECISION.1',
    2: 'measures.comments.PRECISION.2',
    3: 'measures.comments.PRECISION.3',
    4: 'measures.comments.PRECISION.4',
    5: 'measures.comments.PRECISION.5',
  },
  COLLABORATION: {
    1: 'measures.comments.COLLABORATION.1',
    2: 'measures.comments.COLLABORATION.2',
    3: 'measures.comments.COLLABORATION.3',
    4: 'measures.comments.COLLABORATION.4',
    5: 'measures.comments.COLLABORATION.5',
  },
  ADDITIONAL: {
    1: 'measures.comments.ADDITIONAL.1',
    2: 'measures.comments.ADDITIONAL.2',
    3: 'measures.comments.ADDITIONAL.3',
    4: 'measures.comments.ADDITIONAL.4',
    5: 'measures.comments.ADDITIONAL.5',
  },
};

export function getMeasureTypeDescription(
  code: string,
  formatMessage: MeasureFormatMessage,
  apiDescription?: string | null,
): string {
  const key = MEASURE_TYPE_DESCRIPTION_KEYS[code];
  if (key) return formatMessage({ id: key as never });
  if (apiDescription?.trim()) return apiDescription.trim();
  return '';
}

export function getMeasureRatingComment(
  measureTypeCode: string,
  ratingValue: number,
  formatMessage: MeasureFormatMessage,
): string {
  if (ratingValue <= 0) return '';
  const key = MEASURE_RATING_COMMENT_KEYS[measureTypeCode]?.[ratingValue];
  if (!key) return '';
  return formatMessage({ id: key as never });
}

export function measureRatingOptionLabel(
  measureTypeCode: string,
  level: { value: number; label: string },
  formatMessage: MeasureFormatMessage,
): string {
  if (level.value <= 0 || level.label === '/') return '/';
  const comment = getMeasureRatingComment(measureTypeCode, level.value, formatMessage);
  if (comment) return `${level.value} — ${comment}`;
  return `${level.value} — ${level.label}`;
}
