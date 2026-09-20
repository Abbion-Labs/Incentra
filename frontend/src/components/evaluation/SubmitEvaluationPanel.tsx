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
  saving: boolean;
  onSave: () => void;
  onSubmit: () => void;
}

export function SubmitEvaluationPanel({
  evaluation,
  incompleteRatings,
  liveOverallAverage,
  liveDescriptiveRatingName,
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
        />
      )}
      {!submitAllowed && (
        <p className="form-hint form-hint--error">
          {formatMessage({ id: submitRequirementsKey })}
        </p>
      )}
      <div className="actions">
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
            : formatMessage({ id: 'buttons.submitToController' })}
        </button>
      </div>
    </section>
  );
}
