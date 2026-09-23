import { ApiError, api } from '../api/client';
import type { EvaluationDetail, EvaluationGoal } from '../api/types';

const EVALUATION_VERSION_CONFLICT = 'vn-0024';
const CONCURRENCY_CONFLICT = 'vn-0090';

export async function fetchLatestEvaluation(
  id: number,
): Promise<EvaluationDetail> {
  return api.get<EvaluationDetail>(`/api/evaluations/${id}`);
}

/**
 * Server odbija čuvanje jer je ocena izmenjena posle učitavanja stranice.
 * Stranica tada učitava novo stanje umesto da pregazi tuđu izmenu.
 */
export function isStaleEvaluationError(error: unknown): boolean {
  return (
    error instanceof ApiError &&
    (error.code === EVALUATION_VERSION_CONFLICT ||
      error.code === CONCURRENCY_CONFLICT)
  );
}

export interface RatingGoalSaveItem {
  description: string;
  ratingLevelId: number;
  comment: string | null;
  weight: number | null;
  sortOrder: number;
}

/** Ciljevi onakvi kakve je stranica učitala, sa ocenama iz editora. */
export function toRatingGoalSaveItems(
  goals: EvaluationGoal[],
): RatingGoalSaveItem[] {
  return goals.map((goal) => ({
    description: goal.description,
    ratingLevelId: goal.ratingLevelId,
    comment: goal.comment,
    weight: goal.weight,
    sortOrder: goal.sortOrder,
  }));
}
