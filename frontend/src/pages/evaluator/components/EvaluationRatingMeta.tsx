import type { EvaluationDetail } from '../../../api/types';
import { useIntl } from '../../../i18n';
import {
  evaluationDisplayClass,
  evaluationDisplayLabel,
} from '../../../utils/evaluationBuckets';
import { formatAverageDisplay } from '../../../utils/scoring';
import { AverageDisplay } from '../../../components/evaluation/AverageDisplay';
import { DescriptiveRatingBadge } from '../../../components/evaluation/DescriptiveRatingBadge';
import { EvaluationStatusWithHistory } from '../../../components/evaluation/EvaluationStatusWithHistory';
import { PeriodPill } from '../../../components/common/PageHeader';

export interface EvaluationRatingMetaProps {
  evaluation: EvaluationDetail;
  incompleteRatings: boolean;
  liveOverallAverage?: number | null;
  liveDescriptiveRatingName?: string | null;
  /** Prosek ciljeva i merila dok se ocena unosi; inače se čitaju sa ocene. */
  liveGoalsAverage?: number | null;
  liveMeasuresAverage?: number | null;
}

export function EvaluationStatusSummary({
  evaluation,
  showPeriod = false,
  showStatusHistory = false,
}: Pick<EvaluationRatingMetaProps, 'evaluation'> & {
  showPeriod?: boolean;
  showStatusHistory?: boolean;
}) {
  const { formatMessage } = useIntl();

  const statusBadge = (
    <span
      className={evaluationDisplayClass({
        status: evaluation.status,
        goalCount: evaluation.goals.length,
        controllerComment: evaluation.controllerComment,
      })}
    >
      {evaluationDisplayLabel(
        {
          status: evaluation.status,
          goalCount: evaluation.goals.length,
          controllerComment: evaluation.controllerComment,
        },
        formatMessage,
      )}
    </span>
  );

  return (
    <>
      <div className="goals-employee-card__item">
        <dt>{formatMessage({ id: 'evaluation.statusLabel' })}</dt>
        <dd>
          {showStatusHistory ? (
            <EvaluationStatusWithHistory
              evaluationId={evaluation.id}
              evaluation={evaluation}
            >
              {statusBadge}
            </EvaluationStatusWithHistory>
          ) : (
            statusBadge
          )}
        </dd>
      </div>
      {showPeriod && (
        <div className="goals-employee-card__item">
          <dt>{formatMessage({ id: 'evaluation.period' })}</dt>
          <dd>
            <PeriodPill quarter={evaluation.quarter} year={evaluation.year} />
          </dd>
        </div>
      )}
    </>
  );
}

export function EvaluationScoresSummary({
  evaluation,
  incompleteRatings,
  liveOverallAverage,
  liveDescriptiveRatingName,
  liveGoalsAverage,
  liveMeasuresAverage,
}: EvaluationRatingMetaProps) {
  const { formatMessage } = useIntl();
  const overallAverage = liveOverallAverage ?? evaluation.overallAverage;
  const goalsAverage =
    liveGoalsAverage !== undefined ? liveGoalsAverage : evaluation.goalsAverage;
  const measuresAverage =
    liveMeasuresAverage !== undefined
      ? liveMeasuresAverage
      : evaluation.measuresAverage;
  // Deo koji još nije sasvim ocenjen nema prosek: prikazuje se „/“.
  const componentText = (value: number | null) =>
    value == null ? (incompleteRatings ? '/' : '—') : Number(value).toFixed(2);

  const components = [
    { key: 'goals', labelKey: 'evaluation.summaryGoals', value: goalsAverage },
    {
      key: 'measures',
      labelKey: 'evaluation.summaryMeasures',
      value: measuresAverage,
    },
  ] as const;

  return (
    <div className="evaluation-scores-summary">
      {components.map((component) => (
        <div
          key={component.key}
          className="evaluation-scores-summary__item evaluation-scores-summary__item--part"
        >
          <span className="evaluation-scores-summary__label">
            {formatMessage({ id: component.labelKey })}
          </span>
          <span className="evaluation-scores-summary__part">
            <AverageDisplay value={componentText(component.value)} />
          </span>
        </div>
      ))}
      <div className="evaluation-scores-summary__item evaluation-scores-summary__item--total">
        <span className="evaluation-scores-summary__label">
          {formatMessage({ id: 'evaluation.summaryOverall' })}
        </span>
        <span className="evaluation-scores-summary__value">
          <AverageDisplay
            value={formatAverageDisplay(incompleteRatings, overallAverage)}
          />
        </span>
      </div>
      <div className="evaluation-scores-summary__item">
        <span className="evaluation-scores-summary__label">
          {formatMessage({ id: 'evaluation.descriptiveLabel' })}
        </span>
        {incompleteRatings ? (
          <span className="average-muted">—</span>
        ) : (
          <DescriptiveRatingBadge
            name={liveDescriptiveRatingName ?? evaluation.descriptiveRatingName}
            descriptiveRatingId={
              liveDescriptiveRatingName ? null : evaluation.descriptiveRatingId
            }
          />
        )}
      </div>
    </div>
  );
}
