import { describe, expect, it } from 'vitest';
import { readFileSync } from 'node:fs';
import path from 'node:path';
import type { EvaluationDetail, RatingLevel } from '../api/types';
import {
  calculateComponentAverage,
  calculateOverallAverage,
  detailHasIncompleteRatings,
  hasLocalIncompleteRatings,
  isNotRated,
} from './scoring';

const ratingLevels: RatingLevel[] = [
  { id: 1, value: 0, label: '/', description: '' },
  { id: 2, value: 1, label: 'Ne zadovoljava', description: '' },
  { id: 3, value: 2, label: 'Minimalno', description: '' },
  { id: 4, value: 3, label: 'Zadovoljava', description: '' },
  { id: 5, value: 4, label: 'Ističe se', description: '' },
  { id: 6, value: 5, label: 'Naročito', description: '' },
];

describe('scoring', () => {
  it('isNotRated_detectsSlashLabel', () => {
    expect(isNotRated({ value: 0, label: '/' })).toBe(true);
    expect(isNotRated({ value: 3, label: 'Zadovoljava' })).toBe(false);
  });

  it('detailHasIncompleteRatings_whenMeasuresMissing', () => {
    expect(
      detailHasIncompleteRatings({
        hasIncompleteRatings: false,
        goals: [
          {
            ratingLevelValue: 3,
            ratingLevelLabel: 'Zadovoljava',
          } as EvaluationDetail['goals'][number],
        ],
        measures: [],
        status: 'Draft',
        overallAverage: null,
      } as unknown as EvaluationDetail),
    ).toBe(true);
  });

  it('hasLocalIncompleteRatings_withNotRatedLevel', () => {
    expect(
      hasLocalIncompleteRatings(
        [
          {
            ratingLevelId: 1,
            ratingLevelValue: 0,
            ratingLevelLabel: '/',
          } as never,
        ],
        [{ ratingLevelId: 5 }],
        1,
      ),
    ).toBe(true);
  });

  it('goldenScoringCases_matchBackendOutputs', () => {
    const file = path.resolve(
      __dirname,
      '../../tests/golden/scoring-cases.json',
    );
    const cases = JSON.parse(readFileSync(file, 'utf8')) as Array<{
      name: string;
      goals: Array<{ ratingLevelId: number; weight?: number }>;
      measures: Array<{ ratingLevelId: number }>;
      expected: {
        goalsAverage: number | null;
        measuresAverage: number | null;
        overallAverage: number | null;
      };
    }>;

    for (const testCase of cases) {
      const goalsAverage = calculateComponentAverage(
        testCase.goals.map((goal) => ({
          ratingLevelId: goal.ratingLevelId,
          weight: goal.weight ?? null,
        })),
        ratingLevels,
      );
      const measuresAverage = calculateComponentAverage(
        testCase.measures.map((measure) => ({
          ratingLevelId: measure.ratingLevelId,
        })),
        ratingLevels,
      );
      const overallAverage = calculateOverallAverage(
        goalsAverage,
        measuresAverage,
      );

      expect(goalsAverage, testCase.name).toBe(testCase.expected.goalsAverage);
      expect(measuresAverage, testCase.name).toBe(
        testCase.expected.measuresAverage,
      );
      expect(overallAverage, testCase.name).toBe(
        testCase.expected.overallAverage,
      );
    }
  });
});
