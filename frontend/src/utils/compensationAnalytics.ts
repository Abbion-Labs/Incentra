import type { CompensationAnalyticsChartType } from '../api/types';

export const COMPENSATION_CHART_OPTIONS: Array<{
  value: CompensationAnalyticsChartType;
  labelKey: string;
}> = [
  {
    value: 'shareDistribution',
    labelKey: 'charts.compensation.shareDistribution',
  },
  { value: 'shareByEmployee', labelKey: 'charts.compensation.shareByEmployee' },
  { value: 'netDistribution', labelKey: 'charts.compensation.netDistribution' },
  { value: 'netByEmployee', labelKey: 'charts.compensation.netByEmployee' },
  {
    value: 'monthlyByEmployee',
    labelKey: 'charts.compensation.monthlyByEmployee',
  },
];

export const CHART_BAR_COLORS = [
  '#1E2761',
  '#7A2048',
  '#408EC6',
  '#2A9D8F',
  '#E76F51',
];

export function chartColor(index: number): string {
  return CHART_BAR_COLORS[index % CHART_BAR_COLORS.length];
}

export function shortEmployeeLabel(name: string, maxLength = 14): string {
  if (name.length <= maxLength) return name;
  const parts = name.split(' ');
  if (parts.length >= 2) {
    return `${parts[0].charAt(0)}. ${parts[parts.length - 1]}`;
  }
  return `${name.slice(0, maxLength - 1)}…`;
}
