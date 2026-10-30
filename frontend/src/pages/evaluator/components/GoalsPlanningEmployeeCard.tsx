import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import type { Employee, EvaluationDetail } from '../../../api/types';
import { EmployeeAvatar } from '../../../components/employee/EmployeeAvatar';
import { PeriodPill } from '../../../components/common/PageHeader';
import { EvaluationStatusSummary } from './EvaluationRatingMeta';
import { useIntl } from '../../../i18n';

interface GoalsPlanningEmployeeCardProps {
  evaluation: EvaluationDetail;
  employee?: Employee | null;
  ratingAside?: ReactNode;
  footer?: ReactNode;
  showEvaluator?: boolean;
  employeeAnalyticsPath?: string;
  showStatusHistory?: boolean;
  /** Jedan red: ime, radno mesto i jedinica, period i dodatne oznake. */
  compact?: boolean;
  badges?: ReactNode;
}

export function GoalsPlanningEmployeeCard({
  evaluation,
  employee,
  ratingAside,
  footer,
  showEvaluator = false,
  employeeAnalyticsPath,
  showStatusHistory = false,
  compact = false,
  badges,
}: GoalsPlanningEmployeeCardProps) {
  const { formatMessage } = useIntl();
  const analyticsHint = formatMessage({
    id: 'evaluation.viewEmployeeAnalytics',
  });
  const avatarEmployee: Pick<
    Employee,
    'fullName' | 'firstName' | 'lastName' | 'avatarUrl'
  > = employee ?? {
    fullName: evaluation.employeeFullName,
    firstName: evaluation.employeeFullName.split(' ')[0] ?? '',
    lastName: evaluation.employeeFullName.split(' ').slice(1).join(' '),
    avatarUrl: null,
  };

  const identity = (
    <>
      <EmployeeAvatar employee={avatarEmployee} size="lg" />
      <span className="goals-employee-card__name">
        {evaluation.employeeFullName}
      </span>
    </>
  );

  if (compact) {
    const summary = (
      <>
        <EmployeeAvatar employee={avatarEmployee} size="md" />
        <span className="goals-employee-card__summary">
          <span className="goals-employee-card__name">
            {evaluation.employeeFullName}
          </span>
          <span className="goals-employee-card__meta">
            {[employee?.jobPositionName, evaluation.organizationUnitName]
              .filter(Boolean)
              .join(' · ')}
          </span>
        </span>
      </>
    );

    return (
      <div className="card goals-employee-card goals-employee-card--compact">
        {employeeAnalyticsPath ? (
          <Link
            to={employeeAnalyticsPath}
            className="goals-employee-card__identity goals-employee-card__identity--link"
            title={analyticsHint}
          >
            {summary}
          </Link>
        ) : (
          <div className="goals-employee-card__identity">{summary}</div>
        )}
        <div className="goals-employee-card__badges">
          <PeriodPill quarter={evaluation.quarter} year={evaluation.year} />
          {badges}
        </div>
      </div>
    );
  }

  const asideContent =
    ratingAside ??
    (showStatusHistory ? (
      <EvaluationStatusSummary evaluation={evaluation} showStatusHistory />
    ) : null);

  return (
    <div className="card goals-employee-card">
      <div className="goals-employee-card__body">
        <div className="goals-employee-card__main">
          {employeeAnalyticsPath ? (
            <Link
              to={employeeAnalyticsPath}
              className="goals-employee-card__identity goals-employee-card__identity--link"
              title={analyticsHint}
              aria-label={analyticsHint}
            >
              {identity}
            </Link>
          ) : (
            <div className="goals-employee-card__identity">{identity}</div>
          )}
          <dl className="goals-employee-card__fields">
            <div className="goals-employee-card__item">
              <dt>{formatMessage({ id: 'evaluation.orgUnitShort' })}</dt>
              <dd>{evaluation.organizationUnitName}</dd>
            </div>
            {employee?.jobPositionName && (
              <div className="goals-employee-card__item">
                <dt>{formatMessage({ id: 'evaluation.jobPosition' })}</dt>
                <dd>{employee.jobPositionName}</dd>
              </div>
            )}
            {showEvaluator && evaluation.evaluatorFullName && (
              <div className="goals-employee-card__item">
                <dt>{formatMessage({ id: 'evaluation.evaluator' })}</dt>
                <dd>{evaluation.evaluatorFullName}</dd>
              </div>
            )}
            <div className="goals-employee-card__item">
              <dt>{formatMessage({ id: 'evaluation.period' })}</dt>
              <dd>
                <PeriodPill
                  quarter={evaluation.quarter}
                  year={evaluation.year}
                />
              </dd>
            </div>
            {asideContent}
          </dl>
        </div>
      </div>
      {footer}
    </div>
  );
}
