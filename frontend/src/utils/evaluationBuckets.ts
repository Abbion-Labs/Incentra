import type { EvaluationSummary } from '../api/types';
import type { IntlFormatters } from 'react-intl';
import { statusLabel } from './status';

export type EvaluationBucket = 'planning' | 'unrated' | 'returned' | 'submitted' | 'approved';

type FormatMessage = IntlFormatters['formatMessage'];

export const bucketLabelKeys: Record<EvaluationBucket, string> = {
  planning: 'evaluation.bucket.planning',
  unrated: 'evaluation.bucket.unrated',
  returned: 'evaluation.bucket.returned',
  submitted: 'evaluation.bucket.submitted',
  approved: 'evaluation.bucket.approved',
};

export const bucketTabLabelKeys: Record<EvaluationBucket, string> = {
  planning: 'evaluation.bucketTab.planning',
  unrated: 'evaluation.bucketTab.unrated',
  returned: 'evaluation.bucketTab.returned',
  submitted: 'evaluation.bucketTab.submitted',
  approved: 'evaluation.bucketTab.approved',
};

export function effectiveGoalCount(ev: EvaluationSummary): number {
  return ev.goalCount ?? 0;
}

export function classifyEvaluation(ev: EvaluationSummary): EvaluationBucket | null {
  if (ev.status === 'Approved') return 'approved';
  if (ev.status === 'Submitted' || ev.status === 'UnderReview') return 'submitted';
  if (ev.status === 'Draft') {
    if (ev.controllerComment) return 'returned';
    if (ev.goalsPlanningComplete ?? effectiveGoalCount(ev) > 0) return 'unrated';
    return 'planning';
  }
  return null;
}

export function isReturnedEvaluation(
  ev: Pick<EvaluationSummary, 'status' | 'goalCount' | 'controllerComment'>,
): boolean {
  return classifyEvaluation(ev as EvaluationSummary) === 'returned';
}

/** Prikaz statusa u UI — nema posebnog „nacrta“, neocenjene ocene čekaju slanje kontroloru. */
export function evaluationDisplayLabel(
  ev: Pick<EvaluationSummary, 'status' | 'goalCount' | 'controllerComment'>,
  formatMessage: FormatMessage,
): string {
  const bucket = classifyEvaluation(ev as EvaluationSummary);
  if (bucket === 'planning') return formatMessage({ id: 'evaluation.bucket.planning' });
  if (bucket === 'unrated') return formatMessage({ id: 'status.Draft' });
  if (bucket === 'returned') return formatMessage({ id: 'evaluation.bucket.returned' });
  return statusLabel(ev.status, formatMessage);
}

export function evaluationDisplayClass(
  ev: Pick<EvaluationSummary, 'status' | 'goalCount' | 'controllerComment'>,
): string {
  const bucket = classifyEvaluation(ev as EvaluationSummary);
  if (bucket === 'unrated') return 'badge badge-unrated';
  if (bucket === 'returned') return 'badge badge-returned';
  if (bucket === 'planning') return 'badge badge-planning';
  return `badge badge-${ev.status.toLowerCase()}`;
}

export function matchesEmployeeSearch(ev: EvaluationSummary, search: string): boolean {
  const term = search.trim().toLowerCase();
  if (!term) return true;
  return ev.employeeFullName.toLowerCase().includes(term);
}

export function filterByBucket(
  evaluations: EvaluationSummary[],
  bucket: EvaluationBucket,
  search: string,
): EvaluationSummary[] {
  return evaluations.filter((ev) => classifyEvaluation(ev) === bucket && matchesEmployeeSearch(ev, search));
}

export function countEvaluationsByBucket(
  evaluations: EvaluationSummary[],
  search = '',
): Record<EvaluationBucket, number> {
  const counts: Record<EvaluationBucket, number> = {
    planning: 0,
    unrated: 0,
    returned: 0,
    submitted: 0,
    approved: 0,
  };
  for (const ev of evaluations) {
    const bucket = classifyEvaluation(ev);
    if (bucket && matchesEmployeeSearch(ev, search)) {
      counts[bucket] += 1;
    }
  }
  return counts;
}

export const emptyStateByBucketKeys: Record<EvaluationBucket, { title: string; description: string }> = {
  planning: {
    title: 'evaluation.bucketEmpty.planningTitle',
    description: 'evaluation.bucketEmpty.planningDescription',
  },
  unrated: {
    title: 'evaluation.bucketEmpty.unratedTitle',
    description: 'evaluation.bucketEmpty.unratedDescription',
  },
  returned: {
    title: 'evaluation.bucketEmpty.returnedTitle',
    description: 'evaluation.bucketEmpty.returnedDescription',
  },
  submitted: {
    title: 'evaluation.bucketEmpty.submittedTitle',
    description: 'evaluation.bucketEmpty.submittedDescription',
  },
  approved: {
    title: 'evaluation.bucketEmpty.approvedTitle',
    description: 'evaluation.bucketEmpty.approvedDescription',
  },
};

export const bucketLabels = bucketLabelKeys;
export const bucketTabLabels = bucketTabLabelKeys;
export const emptyStateByBucket = emptyStateByBucketKeys;
