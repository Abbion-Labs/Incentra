import { describe, expect, it } from 'vitest';
import { resolveHomePath, sortNavByGroup } from '../utils/homeNavigation';

describe('homeNavigation', () => {
  it('resolveHomePath_prioritizesEvaluator', () => {
    expect(resolveHomePath(['ADMIN', 'EVALUATOR'])).toBe('/evaluator/workflow');
  });

  it('resolveHomePath_returnsNullForEmptyRoles', () => {
    expect(resolveHomePath([])).toBeNull();
  });

  it('sortNavByGroup_ordersByWorkflowPriority', () => {
    const sorted = sortNavByGroup([
      { group: 'admin' as const, label: 'admin' },
      { group: 'evaluator' as const, label: 'evaluator' },
      { group: 'employee' as const, label: 'employee' },
    ]);
    expect(sorted.map((item) => item.group)).toEqual([
      'evaluator',
      'employee',
      'admin',
    ]);
  });
});
