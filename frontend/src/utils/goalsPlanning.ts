import type { EvaluationDetail, EvaluationSummary } from '../api/types';

export interface TextItemDraft {
  description: string;
  sortOrder: number;
}

/** Cilj u planu, sa težinom u celim procentima (udeo u proseku ciljeva). */
export interface GoalDraft extends TextItemDraft {
  weight: number | null;
}

/** Isto pravilo kao na serveru: cilj ispod ovog udela ne pomera prosek. */
export const MIN_GOAL_WEIGHT = 5;
export const TOTAL_GOAL_WEIGHT = 100;

/** 100% podeljeno na `count` celih procenata; ostatak ide prvim ciljevima. */
export function evenGoalWeights(count: number): number[] {
  if (count <= 0) return [];
  const base = Math.floor(TOTAL_GOAL_WEIGHT / count);
  const remainder = TOTAL_GOAL_WEIGHT - base * count;
  return Array.from({ length: count }, (_, i) =>
    i < remainder ? base + 1 : base,
  );
}

export function withEvenWeights(goals: GoalDraft[]): GoalDraft[] {
  const weights = evenGoalWeights(goals.length);
  return goals.map((goal, i) => ({ ...goal, weight: weights[i] }));
}

/** Da li je ocenjivač menjao težine, ili su još ravnomerno raspoređene. */
export function hasCustomWeights(goals: GoalDraft[]): boolean {
  const even = evenGoalWeights(goals.length);
  return goals.some((goal, i) => goal.weight !== even[i]);
}

export interface GoalWeightsCheck {
  total: number;
  /** Svaki cilj ima ceo procenat od minimuma do 100. */
  eachValid: boolean;
  valid: boolean;
}

/** Proverava težine ciljeva koji imaju opis; prazni redovi se ne čuvaju. */
export function checkGoalWeights(goals: GoalDraft[]): GoalWeightsCheck {
  const described = goals.filter((goal) => goal.description.trim());
  const total = described.reduce((sum, goal) => sum + (goal.weight ?? 0), 0);
  const eachValid = described.every(
    (goal) =>
      goal.weight != null &&
      Number.isInteger(goal.weight) &&
      goal.weight >= MIN_GOAL_WEIGHT &&
      goal.weight <= TOTAL_GOAL_WEIGHT,
  );
  return {
    total,
    eachValid,
    valid: described.length > 0 && eachValid && total === TOTAL_GOAL_WEIGHT,
  };
}

/**
 * Ciljevi za formu: sa težinama ako ih svi imaju, a inače ravnomerno
 * raspoređeno, kao što se takvi ciljevi i računaju.
 */
export function toGoalDrafts(
  items: { description: string; sortOrder: number; weight: number | null }[],
): GoalDraft[] {
  const valid = items.filter((item) => item.description.trim());
  if (valid.length === 0) {
    return [{ description: '', sortOrder: 0, weight: TOTAL_GOAL_WEIGHT }];
  }
  const drafts = valid.map((item) => ({
    description: item.description,
    sortOrder: item.sortOrder,
    weight: item.weight,
  }));
  return drafts.every((goal) => goal.weight != null)
    ? drafts
    : withEvenWeights(drafts);
}

export function formatGoalWeight(weight: number | null): string | null {
  return weight == null ? null : `${Number(weight)}%`;
}

export function defaultConversationDatetime(): string {
  const now = new Date();
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}T${pad(now.getHours())}:${pad(now.getMinutes())}`;
}

export function toTextDrafts(
  items: { description: string; sortOrder: number }[],
): TextItemDraft[] {
  const valid = items.filter((item) => item.description.trim());
  if (valid.length === 0) {
    return [{ description: '', sortOrder: 0 }];
  }
  return valid.map((item) => ({
    description: item.description,
    sortOrder: item.sortOrder,
  }));
}

export function isGoalsPlanningComplete(
  evaluation:
    | Pick<EvaluationSummary, 'goalsPlanningComplete' | 'goalCount'>
    | EvaluationDetail,
): boolean {
  if (typeof evaluation.goalsPlanningComplete === 'boolean') {
    return evaluation.goalsPlanningComplete;
  }

  if ('goals' in evaluation) {
    return (
      evaluation.goals.some((goal) => goal.description.trim()) &&
      evaluation.conditions.some((condition) => condition.description.trim()) &&
      evaluation.criteria.some((criterion) => criterion.description.trim())
    );
  }

  return false;
}

export function hasValidPlanningDraft(
  goals: TextItemDraft[],
  conditions: TextItemDraft[],
  criteria: TextItemDraft[],
): boolean {
  return (
    goals.some((goal) => goal.description.trim()) &&
    conditions.some((condition) => condition.description.trim()) &&
    criteria.some((criterion) => criterion.description.trim())
  );
}
