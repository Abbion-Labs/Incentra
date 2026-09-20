export interface UserProfile {
  id: number;
  email: string;
  roles: string[];
  employeeId: number | null;
  employeeFullName: string | null;
  employeeFirstName: string | null;
  employeeLastName: string | null;
  employeeAvatarUrl: string | null;
  emailNotificationsEnabled: boolean;
}

export interface AuthResponse {
  accessToken: string;
  user: UserProfile;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface EvaluationBucketCounts {
  planning: number;
  unrated: number;
  returned: number;
  submitted: number;
  approved: number;
  pending: number;
  goalsComplete: number;
  goalsPending: number;
}

export interface Employee {
  id: number;
  firstName: string;
  lastName: string;
  fullName: string;
  organizationUnitId: number;
  organizationUnitName: string;
  jobPositionId: number;
  jobPositionName: string;
  educationLevelId: number | null;
  educationLevelName: string | null;
  evaluatorEmployeeId: number | null;
  evaluatorFullName: string | null;
  userId: number | null;
  isActive: boolean;
  hiredAt: string | null;
  avatarUrl: string | null;
}

export interface EvaluationSummary {
  id: number;
  employeeId: number;
  employeeFullName: string;
  organizationUnitName: string;
  evaluatorEmployeeId: number;
  evaluatorFullName: string;
  controllerEmployeeId: number | null;
  controllerFullName: string | null;
  year: number;
  quarter: number;
  status: string;
  overallAverage: number | null;
  descriptiveRatingName: string | null;
  submittedAt: string | null;
  approvedAt: string | null;
  version: number;
  goalCount?: number;
  goalsPlanningComplete?: boolean;
  hasIncompleteRatings?: boolean;
  controllerComment?: string | null;
  controllerViewedAt?: string | null;
  conditionsFulfilled?: boolean;
  excludedFromCompensation?: boolean;
}

export interface EvaluationDetail extends EvaluationSummary {
  goalsAverage: number | null;
  measuresAverage: number | null;
  descriptiveRatingId: number | null;
  conversationAt: string | null;
  reviewedAt: string | null;
  evaluatorComment: string | null;
  conditionsNotMetComment?: string | null;
  controllerComment: string | null;
  rejectionReason: string | null;
  goals: EvaluationGoal[];
  measures: EvaluationMeasure[];
  criteria: EvaluationCriterion[];
  conditions: EvaluationCondition[];
  training: EvaluationTraining | null;
}

export interface EvaluationStatusHistoryEntry {
  id: number;
  fromStatus: string | null;
  toStatus: string;
  changedByUserId: number;
  comment: string | null;
  changedAt: string;
}

export interface EvaluationGoal {
  id: number;
  description: string;
  ratingLevelId: number;
  ratingLevelValue: number;
  ratingLevelLabel: string;
  comment: string | null;
  weight: number | null;
  sortOrder: number;
}

export interface EvaluationMeasure {
  id: number;
  measureTypeId: number;
  measureTypeName: string;
  measureDescriptionId: number | null;
  measureDescription: string | null;
  customDescription: string | null;
  ratingComment: string | null;
  ratingLevelId: number;
  ratingLevelValue: number;
  ratingLevelLabel: string;
  sortOrder: number;
}

export interface EvaluationCriterion {
  id: number;
  description: string;
  sortOrder: number;
}

export interface EvaluationCondition {
  id: number;
  description: string;
  sortOrder: number;
}

export interface EvaluationTraining {
  id: number;
  trainingDescription: string | null;
  knowledgeDescription: string | null;
  developmentDescription: string | null;
  evaluatorComment: string | null;
}

export interface RatingLevel {
  id: number;
  value: number;
  label: string;
  description: string;
}

export interface MeasureType {
  id: number;
  code: string;
  name: string;
  description?: string | null;
  sortOrder: number;
  descriptions: { id: number; description: string }[];
}

export interface OrganizationUnit {
  id: number;
  name: string;
  code: string | null;
  isActive: boolean;
}

export interface CompensationParameters {
  id: number;
  organizationUnitId: number;
  organizationUnitName: string;
  year: number;
  monetaryPool: number;
  currency: string;
  acceptablePerformanceRating: number;
  upperLimitCoefficient: number;
  dependencyWeight: number;
  exponent: number;
  allowNegativeVariable: boolean;
  isActive: boolean;
}

export interface CompensationCalculationStatus {
  parametersId: number;
  totalResults: number;
  finalizedResults: number;
  isFinalized: boolean;
  lastCalculatedAt: string | null;
}

export interface CompensationResult {
  id: number;
  employeeId: number;
  employeeFullName: string;
  organizationUnitName: string;
  parametersId: number;
  year: number;
  overallAverage: number;
  points: number;
  salaryPointsValue: number;
  fixedSalary: number;
  compensationPercent: number;
  netCompensation: number;
  quarterlyCompensation: number;
  monthlyCompensation: number;
  isFinal: boolean;
  calculatedAt: string;
}

export type CompensationAnalyticsChartType =
  | 'shareDistribution'
  | 'shareByEmployee'
  | 'netDistribution'
  | 'netByEmployee'
  | 'monthlyByEmployee';

export interface CompensationAnalyticsBucket {
  label: string;
  count: number;
}

export interface CompensationAnalyticsSeriesPoint {
  label: string;
  value: number;
}

export interface CompensationAnalytics {
  chartType: CompensationAnalyticsChartType;
  year: number;
  currency: string;
  organizationUnitName: string | null;
  organizationUnitKey?: string | null;
  valueFormat: 'count' | 'percent' | 'currency';
  buckets: CompensationAnalyticsBucket[];
  series: CompensationAnalyticsSeriesPoint[];
}

export interface JobPosition {
  id: number;
  name: string;
  sortOrder: number;
  isActive: boolean;
}

export interface EducationLevel {
  id: number;
  name: string;
  sortOrder: number;
  isActive: boolean;
}

export interface DescriptiveRating {
  id: number;
  code: string;
  name: string;
  minAverage: number | null;
  maxAverage: number | null;
  sortOrder: number;
  isActive: boolean;
  recommendedShare: number;
}

export interface EvaluatorSettings {
  employeeId: number;
  employeeFullName: string;
  controllerEmployeeId: number;
  controllerFullName: string;
  thresholdDoesNotMeet: number;
  thresholdMeets: number;
  thresholdGood: number;
  thresholdExceeds: number;
  percentDoesNotMeet: number;
  percentMeets: number;
  percentGood: number;
  percentExceeds: number;
}

export interface EmployeeQuarterBenchmark {
  evaluationId: number;
  year: number;
  quarter: number;
  status: string;
  employeeAverage: number | null;
  organizationUnitAverage: number | null;
  jobPositionAverage: number | null;
  hasIncompleteRatings: boolean;
  descriptiveRatingName: string | null;
}

export interface EmployeeEvaluationBenchmarks {
  employee: Employee;
  quarters: EmployeeQuarterBenchmark[];
}

export interface DescriptiveRatingDistributionItem {
  descriptiveRatingId: number;
  code: string;
  name: string;
  count: number;
  percentage: number;
  sortOrder: number;
}

export interface DescriptiveRatingComparisonItem {
  descriptiveRatingId: number;
  code: string;
  name: string;
  previousYearCount: number;
  thisYearCount: number;
  recommendedCount: number;
  sortOrder: number;
}

export interface EvaluatorPeriodStats {
  average: number | null;
  median: number | null;
  variance: number | null;
}

export interface OverallStatsComparison {
  selectedYear: EvaluatorPeriodStats;
  allYears: EvaluatorPeriodStats;
}

export interface ControllerEvaluatorSummary {
  employeeId: number;
  employeeFullName: string;
  organizationUnitName: string;
  jobPositionName: string;
  avatarUrl: string | null;
  subordinateCount: number;
}

export interface AdminUser {
  id: number;
  email: string;
  roles: string[];
  isActive: boolean;
  employeeId: number | null;
  employeeFullName: string | null;
}

export interface EmployeeSalary {
  id: number;
  employeeId: number;
  employeeFullName: string;
  organizationUnitName: string;
  points: number;
  salaryPerPoint: number;
  currency: string;
  effectiveFrom: string;
  effectiveTo: string | null;
  isCurrent: boolean;
  updatedAt: string;
}

export interface EmployeeSalaryOption {
  employeeId: number;
  fullName: string;
  organizationUnitName: string;
}

export interface EvaluatorAnalytics {
  evaluatorEmployeeId?: number;
  evaluatorFullName?: string;
  year: number;
  previousYear: number;
  subordinateCount: number;
  ratedEvaluationsThisYear: number;
  distributionThisYear: DescriptiveRatingDistributionItem[];
  ratingComparison: DescriptiveRatingComparisonItem[];
  overallStats: OverallStatsComparison;
}
