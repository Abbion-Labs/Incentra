import {
  EvaluationScoresSummary,
  type EvaluationRatingMetaProps,
} from '../../pages/evaluator/components/EvaluationRatingMeta';
import { useIntl } from '../../i18n';

interface SubmitEvaluationPanelProps extends EvaluationRatingMetaProps {
  submitAllowed: boolean;
  submitRequirementsKey?:
    | 'evaluation.submitRequirements'
    | 'evaluation.submitRequirementsConditionsNotMet';
  /** Broj ciljeva i merila koji još nisu ocenjeni. */
  remainingCount?: number;
  saving: boolean;
  onSave: () => void;
  onSubmit: () => void;
}

export function SubmitEvaluationPanel({
  evaluation,
  incompleteRatings,
  liveOverallAverage,
  liveDescriptiveRatingName,
  liveGoalsAverage,
  liveMeasuresAverage,
  remainingCount = 0,
  submitAllowed,
  submitRequirementsKey = 'evaluation.submitRequirements',
  saving,
  onSave,
  onSubmit,
}: SubmitEvaluationPanelProps) {
  const { formatMessage } = useIntl();
  return (
    <section className="evaluation-submit-footer">
      {evaluation.conditionsFulfilled !== false && (
        <EvaluationScoresSummary
          evaluation={evaluation}
          incompleteRatings={incompleteRatings}
          liveOverallAverage={liveOverallAverage}
          liveDescriptiveRatingName={liveDescriptiveRatingName}
          liveGoalsAverage={liveGoalsAverage}
          liveMeasuresAverage={liveMeasuresAverage}
        />
      )}
      <div className="actions">
        {/* Šta još treba pre slanja: napomena, ne greška. */}
        {!submitAllowed && (
          <p className="evaluation-submit-footer__hint">
            {evaluation.conditionsFulfilled !== false && remainingCount > 0
              ? formatMessage(
                  { id: 'evaluation.remainingToRate' },
                  { count: remainingCount },
                )
              : formatMessage({ id: submitRequirementsKey })}
          </p>
        )}
        <button
          type="button"
          className="btn btn-secondary"
          onClick={onSave}
          disabled={saving}
        >
          {saving
            ? formatMessage({ id: 'buttons.saving' })
            : formatMessage({ id: 'buttons.save' })}
        </button>
        <button
          type="button"
          className="btn btn-primary"
          onClick={onSubmit}
          disabled={saving || !submitAllowed}
        >
          {saving
            ? formatMessage({ id: 'buttons.submitting' })
            : formatMessage({
                id:
                  evaluation.controllerEmployeeId == null
                    ? 'buttons.submitFinal'
                    : 'buttons.submitToController',
              })}
        </button>
      </div>
    </section>
  );
}
