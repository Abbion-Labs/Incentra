import type { IntlFormatters } from 'react-intl';

type FormatMessage = IntlFormatters['formatMessage'];

export const roleLabelKey: Record<string, string> = {
  ADMIN: 'roles.ADMIN',
  EVALUATOR: 'roles.EVALUATOR',
  CONTROLLER: 'roles.CONTROLLER',
  PAYROLL: 'roles.PAYROLL',
  EMPLOYEE: 'roles.EMPLOYEE',
};

export const statusLabelKey: Record<string, string> = {
  Draft: 'status.Draft',
  Submitted: 'status.Submitted',
  UnderReview: 'status.UnderReview',
  Approved: 'status.Approved',
  Rejected: 'status.Rejected',
};

export function roleLabel(role: string, formatMessage: FormatMessage): string {
  const key = roleLabelKey[role];
  return key ? formatMessage({ id: key }) : role;
}

export function statusLabel(
  status: string,
  formatMessage: FormatMessage,
): string {
  const key = statusLabelKey[status];
  return key ? formatMessage({ id: key }) : status;
}

export function statusClass(status: string): string {
  return `badge badge-${status.toLowerCase()}`;
}

export const currentYear = new Date().getFullYear();
export const currentQuarter = Math.ceil((new Date().getMonth() + 1) / 3) as
  1 | 2 | 3 | 4;

export function previousQuarter(
  year: number,
  quarter: number,
): { year: number; quarter: number } {
  if (quarter > 1) {
    return { year, quarter: quarter - 1 };
  }
  return { year: year - 1, quarter: 4 };
}
