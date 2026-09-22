import { useMemo } from 'react';
import type { EvaluatorAnalytics } from '../../api/types';
import { useIntl } from '../../i18n';
import { currentYear } from '../../utils/status';
import { DescriptiveRatingPieChart } from '../../pages/evaluator/components/DescriptiveRatingPieChart';
import { OverallStatsChart } from '../../pages/evaluator/components/OverallStatsChart';
import { RatingCountComparisonChart } from '../../pages/evaluator/components/RatingCountComparisonChart';

interface EvaluatorAnalyticsViewProps {
  analytics: EvaluatorAnalytics;
  year: number;
  onYearChange: (year: number) => void;
  title?: string;
  hint?: string;
}

export function EvaluatorAnalyticsView({
  analytics,
  year,
  onYearChange,
  title,
  hint,
}: EvaluatorAnalyticsViewProps) {
  const { formatMessage } = useIntl();
  const yearOptions = useMemo(() => {
    const years = new Set<number>();
    for (let y = currentYear; y >= currentYear - 4; y -= 1) {
      years.add(y);
    }
    years.add(year);
    return [...years].sort((a, b) => b - a);
  }, [year]);

  const displayTitle =
    title ??
    (analytics.evaluatorFullName
      ? formatMessage(
          { id: 'analytics.titleWithName' },
          { name: analytics.evaluatorFullName },
        )
      : formatMessage({ id: 'analytics.title' }));

  return (
    <div className="analytics-page">
      {(title || hint) && (
        <div className="card card--flush analytics-page__intro">
          <div className="card__header">
            <h2 className="card__title">{displayTitle}</h2>
            {hint && <p className="card__hint">{hint}</p>}
          </div>
        </div>
      )}

      <div className="analytics-summary-grid">
        <div className="card analytics-summary-card">
          <span className="analytics-summary-card__label">
            {formatMessage({ id: 'analytics.subordinates' })}
          </span>
          <strong className="analytics-summary-card__value">
            {analytics.subordinateCount}
          </strong>
        </div>
        <div className="card analytics-summary-card">
          <span className="analytics-summary-card__label">
            {formatMessage(
              { id: 'analytics.ratedInYear' },
              { year: analytics.year },
            )}
          </span>
          <strong className="analytics-summary-card__value">
            {analytics.ratedEvaluationsThisYear}
          </strong>
        </div>
        <div className="card analytics-summary-card">
          <span className="analytics-summary-card__label">
            {formatMessage(
              { id: 'analytics.averageInYear' },
              { year: analytics.year },
            )}
          </span>
          <strong className="analytics-summary-card__value">
            {analytics.overallStats.selectedYear.average?.toFixed(2) ?? '—'}
          </strong>
        </div>
        <div className="card analytics-summary-card">
          <span className="analytics-summary-card__label">
            {formatMessage(
              { id: 'analytics.medianInYear' },
              { year: analytics.year },
            )}
          </span>
          <strong className="analytics-summary-card__value">
            {analytics.overallStats.selectedYear.median?.toFixed(2) ?? '—'}
          </strong>
        </div>
        <label className="card analytics-summary-card analytics-summary-card--filter">
          <span className="analytics-summary-card__label">
            {formatMessage({ id: 'common.year' })}
          </span>
          <select
            value={year}
            onChange={(event) => onYearChange(Number(event.target.value))}
          >
            {yearOptions.map((option) => (
              <option key={option} value={option}>
                {option}.
              </option>
            ))}
          </select>
        </label>
      </div>

      <div className="analytics-grid">
        <section className="card analytics-panel">
          <h3>
            {formatMessage({ id: 'analytics.descriptiveDistributionTitle' })}
          </h3>
          <p className="card__hint">
            {formatMessage(
              { id: 'analytics.descriptiveDistributionHint' },
              { year: analytics.year },
            )}
          </p>
          <DescriptiveRatingPieChart
            items={analytics.distributionThisYear}
            year={analytics.year}
          />
        </section>

        <section className="card analytics-panel">
          <h3>{formatMessage({ id: 'analytics.averageAndVarianceTitle' })}</h3>
          <p className="card__hint">
            {formatMessage(
              { id: 'analytics.statsComparisonHint' },
              { year: analytics.year },
            )}
          </p>
          <OverallStatsChart
            stats={analytics.overallStats}
            year={analytics.year}
          />
        </section>

        <section className="card analytics-panel analytics-panel--wide">
          <h3>{formatMessage({ id: 'analytics.ratingComparisonTitle' })}</h3>
          <p className="card__hint">
            {formatMessage(
              { id: 'analytics.ratingComparisonHint' },
              { previousYear: analytics.previousYear, year: analytics.year },
            )}
          </p>
          <RatingCountComparisonChart
            items={analytics.ratingComparison}
            previousYear={analytics.previousYear}
            year={analytics.year}
          />
        </section>
      </div>
    </div>
  );
}
