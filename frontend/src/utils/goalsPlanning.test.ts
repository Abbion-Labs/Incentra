import { describe, expect, it } from 'vitest';
import {
  checkGoalWeights,
  evenGoalWeights,
  hasCustomWeights,
  toGoalDrafts,
  type GoalDraft,
} from './goalsPlanning';

const goal = (description: string, weight: number | null): GoalDraft => ({
  description,
  sortOrder: 0,
  weight,
});

describe('goal weights', () => {
  it('splits 100% into whole percents, the remainder going to the first goals', () => {
    expect(evenGoalWeights(3)).toEqual([34, 33, 33]);
    expect(evenGoalWeights(4)).toEqual([25, 25, 25, 25]);
    expect(evenGoalWeights(1)).toEqual([100]);
  });

  it('accepts whole percents from the minimum that make 100', () => {
    expect(checkGoalWeights([goal('A', 60), goal('B', 40)]).valid).toBe(true);
    expect(checkGoalWeights([goal('A', 60), goal('B', 30)])).toMatchObject({
      total: 90,
      valid: false,
    });
    expect(checkGoalWeights([goal('A', 97), goal('B', 3)]).eachValid).toBe(
      false,
    );
    expect(checkGoalWeights([goal('A', 50.5), goal('B', 49.5)]).valid).toBe(
      false,
    );
  });

  it('leaves out goals without a description, as they are not saved', () => {
    expect(checkGoalWeights([goal('A', 100), goal('  ', null)]).valid).toBe(
      true,
    );
  });

  it('keeps saved weights and splits goals saved without them evenly', () => {
    expect(
      toGoalDrafts([
        { description: 'A', sortOrder: 0, weight: 70 },
        { description: 'B', sortOrder: 1, weight: 30 },
      ]).map((g) => g.weight),
    ).toEqual([70, 30]);
    expect(
      toGoalDrafts([
        { description: 'A', sortOrder: 0, weight: null },
        { description: 'B', sortOrder: 1, weight: null },
      ]).map((g) => g.weight),
    ).toEqual([50, 50]);
  });

  it('tells an even split from weights the evaluator changed', () => {
    expect(hasCustomWeights([goal('A', 50), goal('B', 50)])).toBe(false);
    expect(hasCustomWeights([goal('A', 70), goal('B', 30)])).toBe(true);
  });
});
