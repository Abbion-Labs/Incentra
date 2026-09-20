import type { EvaluationDetail, EvaluationSummary } from '../api/types';

export interface TextItemDraft {
  description: string;
  sortOrder: number;
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
