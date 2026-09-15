import type { EvaluationBucketCounts } from '../api/types';

export function buildEvaluationsPagePath(
  page: number,
  pageSize: number,
  params: {
    year?: number | null;
    quarter?: number | null;
    bucket?: string;
    search?: string;
    status?: string;
    employeeId?: number;
  },
): string {
  const query = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
  });

  if (params.year != null) query.set('year', String(params.year));
  if (params.quarter != null) query.set('quarter', String(params.quarter));
  if (params.bucket) query.set('bucket', params.bucket);
  if (params.search?.trim()) query.set('search', params.search.trim());
  if (params.status) query.set('status', params.status);
  if (params.employeeId != null) query.set('employeeId', String(params.employeeId));

  return `/api/evaluations?${query}`;
}

export function buildEvaluationBucketCountsPath(params: {
  year?: number | null;
  quarter?: number | null;
  search?: string;
}): string {
  const query = new URLSearchParams();
  if (params.year != null) query.set('year', String(params.year));
  if (params.quarter != null) query.set('quarter', String(params.quarter));
  if (params.search?.trim()) query.set('search', params.search.trim());
  const suffix = query.toString();
  return suffix ? `/api/evaluations/bucket-counts?${suffix}` : '/api/evaluations/bucket-counts';
}

export function mapEvaluatorBucketCounts(counts: EvaluationBucketCounts) {
  return {
    planning: counts.planning,
    unrated: counts.unrated,
    returned: counts.returned,
    submitted: counts.submitted,
    approved: counts.approved,
  };
}

export function mapControllerBucketCounts(counts: EvaluationBucketCounts) {
  return {
    pending: counts.pending,
    returned: counts.returned,
    approved: counts.approved,
  };
}

export function mapGoalsBucketCounts(counts: EvaluationBucketCounts, employeesPending: number) {
  return {
    pending: employeesPending,
    set: counts.goalsComplete,
  };
}
