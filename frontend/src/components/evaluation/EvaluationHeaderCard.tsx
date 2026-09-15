import type { EvaluationDetail } from '../../api/types';
import { useIntl } from '../../i18n';
import { evaluationDisplayClass, evaluationDisplayLabel } from '../../utils/evaluationBuckets';
import { detailHasIncompleteRatings, formatAverageDisplay, formatDetailAverage } from '../../utils/scoring';
import { statusClass, statusLabel } from '../../utils/status';
import { AverageDisplay } from './AverageDisplay';
import { DescriptiveRatingBadge } from './DescriptiveRatingBadge';
import { EvaluationStatusWithHistory } from './EvaluationStatusWithHistory';
import { PeriodPill } from '../common/PageHeader';

type HeaderVariant = 'evaluator' | 'controller' | 'employee';

interface EvaluationHeaderCardProps {
  evaluation: EvaluationDetail;
  variant: HeaderVariant;
  incompleteRatings?: boolean;
  showStatusHistory?: boolean;
}

export function EvaluationHeaderCard({
  evaluation,
  variant,
  incompleteRatings,
  showStatusHistory = false,
}: EvaluationHeaderCardProps) {
  const { formatMessage } = useIntl();
  const showIncomplete = incompleteRatings ?? detailHasIncompleteRatings(evaluation);
  const averageText = incompleteRatings !== undefined
    ? formatAverageDisplay(incompleteRatings, evaluation.overallAverage)
    : formatDetailAverage(evaluation);
  const averageLabel = variant === 'controller' ? formatMessage({ id: 'evaluation.totalAverage' }) : formatMessage({ id: 'evaluation.average' });

  const statusBadge = variant === 'controller' ? (
    <span className={statusClass(evaluation.status)}>{statusLabel(evaluation.status, formatMessage)}</span>
  ) : (
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

  const statusDisplay = showStatusHistory ? (
    <EvaluationStatusWithHistory evaluationId={evaluation.id} evaluation={evaluation}>
      {statusBadge}
    </EvaluationStatusWithHistory>
  ) : statusBadge;

  return (
    <header className="page-header page-header--evaluation">
      <div className="page-header__layout">
        <div className="page-header__main">
          <div className="page-header__title-row">
            <h2 className="page-header__title">
              {variant === 'employee' ? evaluation.evaluatorFullName : evaluation.employeeFullName}
            </h2>
            {statusDisplay}
          </div>
          <div className="page-header__meta">
            {variant === 'employee' ? (
              <PeriodPill quarter={evaluation.quarter} year={evaluation.year} />
            ) : variant === 'controller' ? (
              <>
                <span>{evaluation.organizationUnitName}</span>
                <span className="meta-sep">·</span>
                <span>{formatMessage({ id: 'evaluation.evaluatorPrefix' }, { name: evaluation.evaluatorFullName })}</span>
                <span className="meta-sep">·</span>
                <PeriodPill quarter={evaluation.quarter} year={evaluation.year} />
              </>
            ) : (
              <PeriodPill quarter={evaluation.quarter} year={evaluation.year} />
            )}
          </div>
          <div className="page-header__stats">
            <span className="stat-pill">
              {averageLabel}: <AverageDisplay value={averageText} />
              {!showIncomplete && evaluation.descriptiveRatingName && (
                <>
                  <span className="stat-pill__suffix"> · </span>
                  <DescriptiveRatingBadge
                    name={evaluation.descriptiveRatingName}
                    descriptiveRatingId={evaluation.descriptiveRatingId}
                  />
                </>
              )}
            </span>
          </div>
        </div>
      </div>

      {(evaluation.controllerComment && variant !== 'employee') && (
        <div className="alert alert-info page-header__alert">
          <strong>{formatMessage({ id: 'evaluation.controllerCommentLabel' })}</strong> {evaluation.controllerComment}
        </div>
      )}

      {evaluation.evaluatorComment && variant !== 'evaluator' && (
        <p className="page-header__note">
          <strong>{formatMessage({ id: 'evaluation.evaluatorNoteLabel' })}</strong> {evaluation.evaluatorComment}
        </p>
      )}

      {evaluation.controllerComment && variant === 'employee' && (
        <div className="alert alert-info page-header__alert">
          <strong>{formatMessage({ id: 'evaluation.controllerCommentLabel' })}</strong> {evaluation.controllerComment}
        </div>
      )}

      {evaluation.evaluatorComment && variant === 'employee' && (
        <p className="page-header__note">
          <strong>{formatMessage({ id: 'evaluation.evaluatorNoteLabel' })}</strong> {evaluation.evaluatorComment}
        </p>
      )}
    </header>
  );
}
