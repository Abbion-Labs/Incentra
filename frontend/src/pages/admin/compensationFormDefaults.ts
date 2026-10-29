export const DEFAULT_COMPENSATION_PARAMS = {
  monetaryPool: '500000',

  currency: 'RSD',

  acceptablePerformanceRating: '2.5',

  dependencyWeight: '1',

  exponent: '1.5',

  allowNegativeVariable: false,
} as const;

/** Vrednosti samo za simulaciju na grafikonu — ne čuvaju se u bazi */

export const DEFAULT_PREVIEW_PROFILE = {
  referencePoints: '100',

  referenceSalaryPerPoint: '1000',
} as const;

export const COMPENSATION_FIELD_HINT_KEYS = {
  organizationUnit: 'admin.compensation.hints.organizationUnit',

  year: 'admin.compensation.hints.year',

  monetaryPool: 'admin.compensation.hints.monetaryPool',

  currency: 'admin.compensation.hints.currency',

  acceptablePerformanceRating:
    'admin.compensation.hints.acceptablePerformanceRating',

  exponent: 'admin.compensation.hints.exponent',

  dependencyWeight: 'admin.compensation.hints.dependencyWeight',

  allowNegativeVariable: 'admin.compensation.hints.allowNegativeVariable',

  referencePoints: 'admin.compensation.hints.referencePoints',

  referenceSalaryPerPoint: 'admin.compensation.hints.referenceSalaryPerPoint',
} as const;
