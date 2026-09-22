import type { Employee, EvaluationSummary } from '../api/types';
import { isGoalsPlanningComplete } from './goalsPlanning';
import { matchesNameSearch } from './nameSearch';

export type GoalsBucket = 'pending' | 'set';

export const goalsBucketLabelKeys: Record<GoalsBucket, string> = {
  pending: 'evaluation.goalsBucket.pending',
  set: 'evaluation.goalsBucket.set',
};

export const emptyStateByGoalsBucketKeys: Record<
  GoalsBucket,
  { title: string; description: string }
> = {
  pending: {
    title: 'evaluation.goalsBucketEmpty.pendingTitle',
    description: 'evaluation.goalsBucketEmpty.pendingDescription',
  },
  set: {
    title: 'evaluation.goalsBucketEmpty.setTitle',
    description: 'evaluation.goalsBucketEmpty.setDescription',
  },
};

export const goalsBucketLabels = goalsBucketLabelKeys;
export const emptyStateByGoalsBucket = emptyStateByGoalsBucketKeys;

export function isGoalsSet(ev: EvaluationSummary): boolean {
  return isGoalsPlanningComplete(ev);
}

export function filterGoalsBucket(
  evaluations: EvaluationSummary[],
  bucket: GoalsBucket,
  search: string,
): EvaluationSummary[] {
  return evaluations.filter((ev) => {
    const inBucket = bucket === 'set' ? isGoalsSet(ev) : !isGoalsSet(ev);
    if (!inBucket) return false;
    return matchesNameSearch(ev.employeeFullName, search);
  });
}

export function countGoalsBuckets(
  evaluations: EvaluationSummary[],
  employees: Employee[],
  search: string,
): Record<GoalsBucket, number> {
  const matchesSearch = (name: string) => matchesNameSearch(name, search);

  const setCount = evaluations.filter(
    (ev) => isGoalsSet(ev) && matchesSearch(ev.employeeFullName),
  ).length;

  const pendingEmployees = employees.filter((emp) => {
    if (!matchesSearch(emp.fullName)) return false;
    const ev = evaluations.find((e) => e.employeeId === emp.id);
    return !ev || !isGoalsSet(ev);
  });

  return {
    pending: pendingEmployees.length,
    set: setCount,
  };
}
