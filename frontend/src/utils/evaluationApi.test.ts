import { describe, expect, it } from 'vitest';
import {
  buildEvaluationBucketCountsPath,
  buildEvaluationsPagePath,
} from './evaluationApi';

describe('evaluationApi', () => {
  it('asks the list of the role the screen is for', () => {
    expect(
      buildEvaluationsPagePath(1, 30, {
        scope: 'evaluator',
        bucket: 'unrated',
      }),
    ).toBe('/api/evaluator/evaluations?page=1&pageSize=30&bucket=unrated');
    expect(
      buildEvaluationsPagePath(2, 30, { scope: 'controller', year: 2026 }),
    ).toBe('/api/controller/evaluations?page=2&pageSize=30&year=2026');
  });

  it('asks the bucket counts of the role the screen is for', () => {
    expect(buildEvaluationBucketCountsPath({ scope: 'controller' })).toBe(
      '/api/controller/evaluations/bucket-counts',
    );
    expect(
      buildEvaluationBucketCountsPath({ scope: 'evaluator', quarter: 3 }),
    ).toBe('/api/evaluator/evaluations/bucket-counts?quarter=3');
  });

  it('keeps the shared list for screens without a role of their own', () => {
    expect(buildEvaluationsPagePath(1, 30, { employeeId: 7 })).toBe(
      '/api/evaluations?page=1&pageSize=30&employeeId=7',
    );
    expect(buildEvaluationBucketCountsPath({})).toBe(
      '/api/evaluations/bucket-counts',
    );
  });
});
