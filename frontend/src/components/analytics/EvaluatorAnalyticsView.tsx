import { useId, useMemo } from 'react';
import type { EvaluatorAnalytics } from '../../api/types';
import { useIntl } from '../../i18n';
import { PageIntro } from '../common/PageIntro';
import { currentYear } from '../../utils/status';
import { DescriptiveRatingPieChart } from '../../pages/evaluator/components/DescriptiveRatingPieChart';
import { OverallStatsChart } from '../../pages/evaluator/components/OverallStatsChart';
import { RatingCountComparisonChart } from '../../pages/evaluator/components/RatingCountComparisonChart';

type KpiIcon = 'people' | 'check' | 'average' | 'median';

const kpiIconPaths: Record<KpiIcon, string[]> = {
  people: [
    'M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2',
    'M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8z',
    'M22 21v-2a4 4 0 0 0-3-3.87',
    'M16 3.13a4 4 0 0 1 0 7.75',
  ],
  check: [
    'M9 11l3 3L22 4',
    'M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11',
  ],
  average: ['M3 3v18h18', 'M7 14l4-4 4 4 5-6'],
  median: ['M3 12h18', 'M7 6v12', 'M17 9v6', 'M12 4v16'],
};

function KpiCard({
  label,
  value,
  icon,
  tone,
}: {
  label: string;
  value: string | number;
  icon: KpiIcon;
  tone: 'blue' | 'teal' | 'violet' | 'amber';
}) {
  return (
    <div
      className={`card analytics-summary-card analytics-summary-card--${tone}`}
    >
      <span className="analytics-summary-card__icon" aria-hidden>
        <svg
          width="18"
          height="18"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.9"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          {kpiIconPaths[icon].map((d) => (
            <path key={d} d={d} />
          ))}
        </svg>
      </span>
      <span className="analytics-summary-card__label">{label}</span>
      <strong className="analytics-summary-card__value">{value}</strong>
    </div>
  );
}

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
  const yearSelectId = useId();
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

  const yearFilter = (
    <div className="analytics-toolbar">
      <label htmlFor={yearSelectId}>
        {formatMessage({ id: 'common.year' })}
      </label>
      <select
        id={yearSelectId}
        value={year}
        onChange={(event) => onYearChange(Number(event.target.value))}
      >
        {yearOptions.map((option) => (
          <option key={option} value={option}>
            {option}.
          </option>
        ))}
      </select>
    </div>
  );

  return (
    <div className="analytics-page">
      {title || hint ? (
        <PageIntro title={displayTitle} subtitle={hint} actions={yearFilter} />
      ) : (
        <div className="analytics-page__toolbar-row">{yearFilter}</div>
      )}

      <div className="analytics-summary-grid">
        <KpiCard
          tone="blue"
          icon="people"
          label={formatMessage({ id: 'analytics.subordinates' })}
          value={analytics.subordinateCount}
        />
        <KpiCard
          tone="teal"
          icon="check"
          label={formatMessage(
            { id: 'analytics.ratedInYear' },
            { year: analytics.year },
          )}
          value={analytics.ratedEvaluationsThisYear}
        />
        <KpiCard
          tone="violet"
          icon="average"
          label={formatMessage(
            { id: 'analytics.averageInYear' },
            { year: analytics.year },
          )}
          value={analytics.overallStats.selectedYear.average?.toFixed(2) ?? '—'}
        />
        <KpiCard
          tone="amber"
          icon="median"
          label={formatMessage(
            { id: 'analytics.medianInYear' },
            { year: analytics.year },
          )}
          value={analytics.overallStats.selectedYear.median?.toFixed(2) ?? '—'}
        />
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
