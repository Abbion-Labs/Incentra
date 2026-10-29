import { describe, expect, it } from 'vitest';
import {
  homePathForRole,
  resolvePageRole,
  sortNavByGroup,
  switchableRoles,
} from '../utils/homeNavigation';

describe('homeNavigation', () => {
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

  it('offers the roles that have screens, in home priority', () => {
    expect(switchableRoles(['ADMIN', 'EMPLOYEE', 'EVALUATOR', 'X'])).toEqual([
      'EVALUATOR',
      'EMPLOYEE',
      'ADMIN',
    ]);
  });

  it('opens the home page of the given role', () => {
    expect(homePathForRole('CONTROLLER')).toBe('/controller/workflow');
    expect(homePathForRole(null)).toBeNull();
  });

  describe('resolvePageRole', () => {
    const roles = ['EVALUATOR', 'CONTROLLER', 'ADMIN'];

    it('keeps the active role when the page is for it', () => {
      expect(resolvePageRole(roles, ['EVALUATOR', 'ADMIN'], 'ADMIN')).toBe(
        'ADMIN',
      );
    });

    it('names the role the session has to move into', () => {
      expect(resolvePageRole(roles, ['CONTROLLER', 'ADMIN'], 'EVALUATOR')).toBe(
        'CONTROLLER',
      );
    });

    it('keeps the role of the session on a page without one of its own', () => {
      expect(resolvePageRole(roles, undefined, 'CONTROLLER')).toBe('CONTROLLER');
    });

    it('finds no role for a page of roles the user does not hold', () => {
      expect(resolvePageRole(['EVALUATOR'], ['PAYROLL'], 'EVALUATOR')).toBe(
        null,
      );
    });
  });
});
