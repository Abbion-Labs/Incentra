import type { EvaluationDetail } from '../../../api/types';
import { useIntl } from '../../../i18n';
import { evaluationDisplayClass, evaluationDisplayLabel } from '../../../utils/evaluationBuckets';
import { formatAverageDisplay } from '../../../utils/scoring';
import { AverageDisplay } from '../../../components/evaluation/AverageDisplay';
import { EvaluationStatusWithHistory } from '../../../components/evaluation/EvaluationStatusWithHistory';
import { PeriodPill } from '../../../components/common/PageHeader';
import { descriptiveRatingColor, formatDescriptiveRatingLabel } from '../../../utils/descriptiveRating';

export interface EvaluationRatingMetaProps {
  evaluation: EvaluationDetail;
  incompleteRatings: boolean;
  liveOverallAverage?: number | null;
  liveDescriptiveRatingName?: string | null;
}

export function EvaluationStatusSummary({
  evaluation,
  showPeriod = false,
  showStatusHistory = false,
}: Pick<EvaluationRatingMetaProps, 'evaluation'> & { showPeriod?: boolean; showStatusHistory?: boolean }) {
  const { formatMessage } = useIntl();

  const statusBadge = (
    <span
      className={evaluationDisplayClass({
        status: evaluation.status,
        goalCount: evaluation.goals.length,
        controllerComment: evaluation.controllerComment,
      })}
    >
      {evaluationDisplayLabel({
        status: evaluation.status,
        goalCount: evaluation.goals.length,
        controllerComment: evaluation.controllerComment,
      }, formatMessage)}
    </span>
  );

  return (
    <>
      <div className="goals-employee-card__item">
        <dt>{formatMessage({ id: 'evaluation.statusLabel' })}</dt>
        <dd>
          {showStatusHistory ? (
            <EvaluationStatusWithHistory evaluationId={evaluation.id} evaluation={evaluation}>
              {statusBadge}
            </EvaluationStatusWithHistory>
          ) : statusBadge}
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
}: EvaluationRatingMetaProps) {
  const { formatMessage } = useIntl();
  const overallAverage = liveOverallAverage ?? evaluation.overallAverage;
  const descriptiveRatingLabel = formatDescriptiveRatingLabel(formatMessage, {
    name: liveDescriptiveRatingName ?? evaluation.descriptiveRatingName,
    descriptiveRatingId: evaluation.descriptiveRatingId,
  });
  const descriptiveRatingStyleKey =
    liveDescriptiveRatingName ?? evaluation.descriptiveRatingName ?? null;
  const averageText = formatAverageDisplay(incompleteRatings, overallAverage);

  return (
    <div className="evaluation-scores-summary">
      <div className="evaluation-scores-summary__item">
        <span className="evaluation-scores-summary__label">{formatMessage({ id: 'evaluation.averageLabel' })}</span>
        <span className="evaluation-scores-summary__value">
          <AverageDisplay value={averageText} />
        </span>
      </div>
      <div className="evaluation-scores-summary__item">
        <span className="evaluation-scores-summary__label">{formatMessage({ id: 'evaluation.descriptiveLabel' })}</span>
        {!incompleteRatings && descriptiveRatingLabel ? (
          <span
            className="evaluation-scores-summary__descriptive"
            style={{ color: descriptiveRatingColor(descriptiveRatingStyleKey) }}
          >
            {descriptiveRatingLabel}
          </span>
        ) : (
          <span className="average-muted">—</span>
        )}
      </div>
    </div>
  );
}
