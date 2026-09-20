import { api } from '../api/client';
import type { EvaluationDetail, EvaluationGoal } from '../api/types';

export async function fetchLatestEvaluation(
  id: number,
): Promise<EvaluationDetail> {
  return api.get<EvaluationDetail>(`/api/evaluations/${id}`);
}

export interface RatingGoalSaveItem {
  description: string;
  ratingLevelId: number;
  comment: string | null;
  weight: number | null;
  sortOrder: number;
}

/** Keep goal text from server; apply local ratings from the rating editor. */
export function mergeGoalsForRatingSave(
  serverGoals: EvaluationGoal[],
  localGoals: EvaluationGoal[],
): RatingGoalSaveItem[] {
  const localById = new Map(localGoals.map((goal) => [goal.id, goal]));

  return serverGoals.map((serverGoal) => {
    const local =
      localById.get(serverGoal.id) ??
      localGoals.find((goal) => goal.sortOrder === serverGoal.sortOrder);

    return {
      description: serverGoal.description,
      ratingLevelId: local?.ratingLevelId ?? serverGoal.ratingLevelId,
      comment: local?.comment ?? serverGoal.comment,
      weight: local?.weight ?? serverGoal.weight,
      sortOrder: serverGoal.sortOrder,
    };
  });
}
