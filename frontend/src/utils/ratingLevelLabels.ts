import type { RatingLevel } from '../api/types';
import type { IntlMessage, NestedKeys } from '../i18n/i18n.types';
import { NOT_RATED_VALUE, isNotRated } from './scoring';

export type RatingLevelFormatMessage = (descriptor: {
  id: NestedKeys<IntlMessage>;
}) => string;

const RATING_LEVEL_LABEL_KEYS: Record<number, NestedKeys<IntlMessage>> = {
  1: 'ratingLevels.labels.1',
  2: 'ratingLevels.labels.2',
  3: 'ratingLevels.labels.3',
  4: 'ratingLevels.labels.4',
  5: 'ratingLevels.labels.5',
};

const KNOWN_RATING_LABEL_TO_VALUE: Record<string, number> = {
  'Ne zadovoljava': 1,
  'Minimalno zadovoljava': 2,
  Zadovoljava: 3,
  'Ističe se': 4,
  'Naročito se ističe': 5,
};

function resolveRatingLevelValue(
  level?: RatingLevel,
  value?: number,
  label?: string,
): number | null {
  if (level) return level.value;
  if (value != null) return value;
  if (label && label in KNOWN_RATING_LABEL_TO_VALUE)
    return KNOWN_RATING_LABEL_TO_VALUE[label];
  return null;
}

export function formatRatingLevelLabel(
  formatMessage: RatingLevelFormatMessage,
  options: { level?: RatingLevel; value?: number; label?: string },
): string {
  const level = options.level;
  if (level && isNotRated(level)) return '/';
  if (options.label === '/' || options.value === NOT_RATED_VALUE) return '/';

  const value = resolveRatingLevelValue(level, options.value, options.label);
  if (value != null && value > 0 && RATING_LEVEL_LABEL_KEYS[value]) {
    return formatMessage({ id: RATING_LEVEL_LABEL_KEYS[value] });
  }

  return level?.label ?? options.label ?? '—';
}

export function formatLocalizedRatingDisplay(
  formatMessage: RatingLevelFormatMessage,
  value: number,
  label: string,
): string {
  if (value === NOT_RATED_VALUE || label === '/') return '/';
  const localizedLabel = formatRatingLevelLabel(formatMessage, {
    value,
    label,
  });
  return `${value} — ${localizedLabel}`;
}
