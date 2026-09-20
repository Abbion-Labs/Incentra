import type { EvaluationSummary } from '../api/types';
import { classifyEvaluation, matchesEmployeeSearch } from './evaluationBuckets';

export type ControllerBucket = 'pending' | 'returned' | 'approved';

export const controllerBucketTabs: ControllerBucket[] = [
  'pending',
  'returned',
  'approved',
];

export const controllerBucketLabelKeys: Record<ControllerBucket, string> = {
  pending: 'controller.bucket.pending',
  returned: 'controller.bucket.returned',
  approved: 'controller.bucket.approved',
};

export const controllerBucketTabLabelKeys: Record<ControllerBucket, string> = {
  pending: 'controller.bucketTab.pending',
  returned: 'controller.bucketTab.returned',
  approved: 'controller.bucketTab.approved',
};

export const emptyStateByControllerBucketKeys: Record<
  ControllerBucket,
  { title: string; description: string }
> = {
  pending: {
    title: 'controller.bucketEmpty.pendingTitle',
    description: 'controller.bucketEmpty.pendingDescription',
  },
  returned: {
    title: 'controller.bucketEmpty.returnedTitle',
    description: 'controller.bucketEmpty.returnedDescription',
  },
  approved: {
    title: 'controller.bucketEmpty.approvedTitle',
    description: 'controller.bucketEmpty.approvedDescription',
  },
};

export const controllerBucketLabels = controllerBucketLabelKeys;
export const controllerBucketTabLabels = controllerBucketTabLabelKeys;
export const emptyStateByControllerBucket = emptyStateByControllerBucketKeys;

export function isNewForController(ev: EvaluationSummary): boolean {
  return (
    (ev.status === 'Submitted' || ev.status === 'UnderReview') &&
    !ev.controllerViewedAt
  );
}

export function classifyForController(
  ev: EvaluationSummary,
): ControllerBucket | null {
  if (ev.status === 'Approved') return 'approved';
  if (classifyEvaluation(ev) === 'returned') return 'returned';
  if (ev.status === 'Submitted' || ev.status === 'UnderReview')
    return 'pending';
  return null;
}

export function filterByControllerBucket(
  evaluations: EvaluationSummary[],
  bucket: ControllerBucket,
  search: string,
): EvaluationSummary[] {
  return evaluations.filter((ev) => {
    if (classifyForController(ev) !== bucket) return false;
    return matchesEmployeeSearch(ev, search);
  });
}

export function countEvaluationsByControllerBucket(
  evaluations: EvaluationSummary[],
  search = '',
): Record<ControllerBucket, number> {
  const counts: Record<ControllerBucket, number> = {
    pending: 0,
    returned: 0,
    approved: 0,
  };

  for (const ev of evaluations) {
    const bucket = classifyForController(ev);
    if (bucket && matchesEmployeeSearch(ev, search)) {
      counts[bucket] += 1;
    }
  }

  return counts;
}
