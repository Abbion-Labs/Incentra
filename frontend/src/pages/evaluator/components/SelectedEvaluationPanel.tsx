import { Link } from 'react-router-dom';
import type { EvaluationDetail } from '../../../api/types';
import { DescriptiveRatingBadge } from '../../../components/evaluation/DescriptiveRatingBadge';
import { AverageDisplay } from '../../../components/evaluation/AverageDisplay';
import { PeriodPill } from '../../../components/common/PageHeader';
import { CardSkeleton } from '../../../components/common/LoadingSkeleton';
import { useIntl } from '../../../i18n';
import { classifyEvaluation, evaluationDisplayClass, evaluationDisplayLabel } from '../../../utils/evaluationBuckets';
import {
  detailHasIncompleteRatings,
  formatComponentAverage,
  formatDetailAverage,
} from '../../../utils/scoring';

interface SelectedEvaluationPanelProps {
  evaluation: EvaluationDetail | null;
  loading: boolean;
  getDetailPath?: (evaluation: EvaluationDetail) => string;
  actionLabel?: string;
}

export function SelectedEvaluationPanel({
  evaluation,
  loading,
  getDetailPath,
  actionLabel,
}: SelectedEvaluationPanelProps) {
  const { formatMessage } = useIntl();
  if (loading) {
    return <CardSkeleton lines={6} />;
  }

  if (!evaluation) {
    return (
      <div className="card card--muted">
        <p className="empty-inline">{formatMessage({ id: 'evaluation.selectFromQuarterList' })}</p>
      </div>
    );
  }

  const bucket = classifyEvaluation(evaluation);
  const detailPath = getDetailPath
    ? getDetailPath(evaluation)
    : bucket === 'planning'
      ? `/evaluator/goals/evaluations/${evaluation.id}`
      : `/evaluator/evaluations/${evaluation.id}`;
  const buttonLabel = actionLabel ?? (bucket === 'planning'
    ? formatMessage({ id: 'evaluation.setGoals' })
    : formatMessage({ id: 'evaluation.viewFull' }));
  const incomplete = detailHasIncompleteRatings(evaluation);

  return (
    <div className="selected-evaluation card">
      <div className="selected-evaluation__header">
        <div className="selected-evaluation__title-row">
          <h3 className="selected-evaluation__title">
            <PeriodPill quarter={evaluation.quarter} year={evaluation.year} />
          </h3>
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
        </div>
      </div>

      <dl className="selected-evaluation__stats">
        <div>
          <dt>{formatMessage({ id: 'evaluation.goalsAverage' })}</dt>
          <dd>
            <AverageDisplay
              value={formatComponentAverage(incomplete, evaluation.goalsAverage, evaluation.goals.length > 0)}
            />
          </dd>
        </div>
        <div>
          <dt>{formatMessage({ id: 'evaluation.measuresAverage' })}</dt>
          <dd>
            <AverageDisplay
              value={formatComponentAverage(incomplete, evaluation.measuresAverage, evaluation.measures.length > 0)}
            />
          </dd>
        </div>
        <div>
          <dt>{formatMessage({ id: 'evaluation.totalAverage' })}</dt>
          <dd>
            <AverageDisplay value={formatDetailAverage(evaluation)} />
          </dd>
        </div>
        <div>
          <dt>{formatMessage({ id: 'evaluation.descriptiveLabel' })}</dt>
          <dd>
            <DescriptiveRatingBadge
              name={incomplete ? null : evaluation.descriptiveRatingName}
              descriptiveRatingId={incomplete ? undefined : evaluation.descriptiveRatingId}
            />
          </dd>
        </div>
      </dl>

      <div className="selected-evaluation__actions">
        <Link to={detailPath} className="btn btn-primary btn-sm">
          {buttonLabel}
        </Link>
      </div>
    </div>
  );
}
