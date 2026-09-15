export interface CompensationPreviewParams {
  acceptablePerformanceRating: number;
  dependencyWeight: number;
  exponent: number;
  allowNegativeVariable: boolean;
  referencePoints: number;
  referenceSalaryPerPoint: number;
}

export interface CompensationPreviewPoint {
  rating: number;
  annualVariable: number;
  monthlyVariable: number;
}

function calculateVariance(values: number[], mean: number): number {
  if (values.length === 0) return 0;
  return values.reduce((sum, value) => sum + (value - mean) ** 2, 0) / values.length;
}

function calculatePonder(
  overallAverage: number,
  acceptablePerformanceRating: number,
  exponent: number,
  allowNegativeVariable: boolean,
): number {
  const difference = overallAverage - acceptablePerformanceRating;

  if (overallAverage > acceptablePerformanceRating) {
    return difference ** exponent;
  }

  if (!allowNegativeVariable) {
    return 0;
  }

  if (difference < 0) {
    return -(Math.abs(difference) ** exponent);
  }

  return difference === 0 ? 0 : difference ** exponent;
}

function distributePool(
  inputs: Array<{ rating: number; points: number; ponder: number; salaryPerPoint: number }>,
  monetaryPool: number,
  dependencyWeight: number,
): Array<{ rating: number; annualVariable: number; monthlyVariable: number }> {
  if (inputs.length === 0) return [];

  const pointsList = inputs.map((input) => input.points);
  const meanPoints = pointsList.reduce((sum, value) => sum + value, 0) / pointsList.length;
  const variance = calculateVariance(pointsList, meanPoints);
  const maxPoints = Math.max(...pointsList, 1);

  const ponderSum = inputs.reduce((sum, input) => sum + input.ponder, 0);

  const intermediate = inputs.map((input) => {
    const zScore = variance > 0 ? (input.points - meanPoints) / variance : 0;
    return {
      input,
      zScore,
      normalizedPoints: input.points / maxPoints,
      compensationWithoutSalary: ponderSum !== 0 ? (monetaryPool * input.ponder) / ponderSum : 0,
    };
  });

  const maxZScore = Math.max(...intermediate.map((item) => item.zScore), 0);
  const maxZScoreSafe = maxZScore !== 0 ? maxZScore : 1;

  const withAq = intermediate.map((item) => {
    const normalizedZ = item.zScore / maxZScoreSafe;
    const aq = item.input.ponder * (1 + normalizedZ * dependencyWeight);
    return { ...item, normalizedZ, aq };
  });

  const aqSum = withAq.reduce((sum, item) => sum + item.aq, 0);

  return withAq.map((item) => {
    const annualVariable = aqSum !== 0 ? (item.aq / aqSum) * monetaryPool : 0;
    return {
      rating: item.input.rating,
      annualVariable,
      monthlyVariable: annualVariable / 12,
    };
  });
}

export function previewCompensationByRating(params: CompensationPreviewParams): CompensationPreviewPoint[] {
  const ratings = [1, 2, 3, 4, 5] as const;
  /** Skala grafikona: godišnja referentna zarada (bodovi × zarada po bodu × 12) */
  const previewPool = params.referencePoints * params.referenceSalaryPerPoint * 12;

  const peerInputs = ratings
    .map((rating) => ({
      rating,
      points: params.referencePoints,
      salaryPerPoint: params.referenceSalaryPerPoint,
      ponder: calculatePonder(
        rating,
        params.acceptablePerformanceRating,
        params.exponent,
        params.allowNegativeVariable,
      ),
    }))
    .filter((input) => input.ponder !== 0 || input.rating >= params.acceptablePerformanceRating);

  const distributed = distributePool(peerInputs, previewPool, params.dependencyWeight);

  return ratings.map((targetRating) => {
    if (!params.allowNegativeVariable && targetRating < params.acceptablePerformanceRating) {
      return { rating: targetRating, annualVariable: 0, monthlyVariable: 0 };
    }

    const ponder = calculatePonder(
      targetRating,
      params.acceptablePerformanceRating,
      params.exponent,
      params.allowNegativeVariable,
    );

    if (ponder === 0 && targetRating < params.acceptablePerformanceRating) {
      return { rating: targetRating, annualVariable: 0, monthlyVariable: 0 };
    }

    const match = distributed.find((item) => item.rating === targetRating);
    return match ?? { rating: targetRating, annualVariable: 0, monthlyVariable: 0 };
  });
}
