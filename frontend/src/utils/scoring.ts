import type {
  EvaluationDetail,
  EvaluationSummary,
  RatingLevel,
} from '../api/types';

export const NOT_RATED_VALUE = 0;

export function isNotRated(level: { value: number; label?: string }): boolean {
  return level.value === NOT_RATED_VALUE || level.label === '/';
}

export function findNotRatedLevelId(levels: RatingLevel[]): number | undefined {
  return levels.find(isNotRated)?.id;
}

export function ratingOptionLabel(level: RatingLevel): string {
  return isNotRated(level) ? '/' : `${level.value} — ${level.label}`;
}

export function ratingDescriptiveLabel(level: RatingLevel | undefined): string {
  if (!level || isNotRated(level)) return '—';
  return level.label;
}

export function findRatingLevel(
  levels: RatingLevel[],
  ratingLevelId: number,
): RatingLevel | undefined {
  return levels.find((level) => level.id === ratingLevelId);
}

export function formatRatingDisplay(value: number, label: string): string {
  return isNotRated({ value, label }) ? '/' : `${value} — ${label}`;
}

export function detailHasIncompleteRatings(detail: EvaluationDetail): boolean {
  if (detail.conditionsFulfilled === false) return false;
  if (detail.hasIncompleteRatings) return true;
  const itemIncomplete = [...detail.goals, ...detail.measures].some(
    (item) =>
      item.ratingLevelValue === NOT_RATED_VALUE ||
      item.ratingLevelLabel === '/',
  );
  if (itemIncomplete) return true;
  return detail.goals.length > 0 && detail.measures.length === 0;
}

export function formatSummaryAverage(
  ev: Pick<
    EvaluationSummary,
    'overallAverage' | 'hasIncompleteRatings' | 'status' | 'goalCount'
  >,
): string {
  if (ev.hasIncompleteRatings) return '/';
  if (
    ev.status === 'Draft' &&
    (ev.goalCount ?? 0) > 0 &&
    ev.overallAverage == null
  )
    return '/';
  if (ev.overallAverage == null) return '—';
  return Number(ev.overallAverage).toFixed(2);
}

export function summaryDescriptiveRatingName(
  ev: Pick<EvaluationSummary, 'descriptiveRatingName' | 'hasIncompleteRatings'>,
): string | null {
  if (ev.hasIncompleteRatings) return null;
  return ev.descriptiveRatingName;
}

export function formatDetailAverage(detail: EvaluationDetail): string {
  if (detailHasIncompleteRatings(detail)) return '/';
  if (
    detail.status === 'Draft' &&
    detail.goals.length > 0 &&
    detail.overallAverage == null
  )
    return '/';
  if (detail.overallAverage == null) return '—';
  return Number(detail.overallAverage).toFixed(2);
}

export function hasLocalIncompleteRatings(
  goals: EvaluationDetail['goals'],
  measures: { ratingLevelId: number }[],
  notRatedLevelId?: number,
): boolean {
  const goalIncomplete = goals.some(
    (g) =>
      g.ratingLevelValue === NOT_RATED_VALUE ||
      g.ratingLevelLabel === '/' ||
      (notRatedLevelId != null && g.ratingLevelId === notRatedLevelId),
  );
  const measureIncomplete =
    notRatedLevelId != null &&
    measures.some((m) => m.ratingLevelId === notRatedLevelId);
  if (goalIncomplete || measureIncomplete) return true;
  return goals.length > 0 && measures.length === 0;
}

export function canSubmitEvaluation(detail: EvaluationDetail): boolean {
  if (detail.conditionsFulfilled === false) {
    return Boolean(detail.conditionsNotMetComment?.trim());
  }
  return !detailHasIncompleteRatings(detail);
}

export function canSubmitEvaluationDraft(
  conditionsFulfilled: boolean,
  conditionsNotMetComment: string,
  incompleteRatings: boolean,
): boolean {
  if (!conditionsFulfilled) {
    return conditionsNotMetComment.trim().length > 0;
  }
  return !incompleteRatings;
}

export function formatAverageDisplay(
  incomplete: boolean,
  overallAverage: number | null,
): string {
  if (incomplete) return '/';
  if (overallAverage == null) return '—';
  return Number(overallAverage).toFixed(2);
}

export function formatComponentAverage(
  incomplete: boolean,
  value: number | null,
  hasItems: boolean,
): string {
  if (incomplete) return '/';
  if (!hasItems) return '—';
  if (value == null) return '—';
  return Number(value).toFixed(2);
}

export function calculateComponentAverage(
  items: { ratingLevelId: number; weight?: number | null }[],
  ratingLevels: RatingLevel[],
): number | null {
  if (items.length === 0 || ratingLevels.length === 0) {
    return null;
  }

  const notRatedIds = new Set(
    ratingLevels.filter(isNotRated).map((level) => level.id),
  );

  if (items.some((item) => notRatedIds.has(item.ratingLevelId))) {
    return null;
  }

  const valueById = new Map(
    ratingLevels.map((level) => [level.id, level.value]),
  );
  const rated = items
    .map((item) => ({
      value: valueById.get(item.ratingLevelId),
      weight: item.weight ?? null,
    }))
    .filter(
      (item): item is { value: number; weight: number | null } =>
        item.value != null,
    );

  if (rated.length === 0) {
    return null;
  }

  const totalWeight = rated
    .filter((item) => item.weight != null && item.weight > 0)
    .reduce((sum, item) => sum + item.weight!, 0);

  if (totalWeight > 0) {
    const weighted = rated
      .filter((item) => item.weight != null && item.weight > 0)
      .reduce((sum, item) => sum + item.value * item.weight!, 0);
    return roundAverage(weighted / totalWeight);
  }

  return roundAverage(
    rated.reduce((sum, item) => sum + item.value, 0) / rated.length,
  );
}

export function calculateOverallAverage(
  goalsAverage: number | null,
  measuresAverage: number | null,
): number | null {
  const parts = [goalsAverage, measuresAverage].filter(
    (value): value is number => value != null,
  );
  if (parts.length === 0) {
    return null;
  }

  return roundAverage(
    parts.reduce((sum, value) => sum + value, 0) / parts.length,
  );
}

const AVERAGE_SCALE = 10_000;

/**
 * Na četiri decimale, polovinu ka parnoj cifri, kao `Math.Round` na serveru. Tako
 * prosek i opisna ocena pre čuvanja odgovaraju onome što server sačuva; prikaz ih
 * ionako zaokružuje na dve.
 */
function roundAverage(value: number): number {
  const scaled = value * AVERAGE_SCALE;
  const floor = Math.floor(scaled);
  const isHalf = Math.abs(scaled - floor - 0.5) < 1e-6;
  const rounded = isHalf
    ? floor % 2 === 0
      ? floor
      : floor + 1
    : Math.round(scaled);
  return rounded / AVERAGE_SCALE;
}
