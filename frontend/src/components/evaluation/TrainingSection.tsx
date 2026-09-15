import { FormSection } from '../forms/FormSection';
import { useIntl } from '../../i18n';

interface TrainingSectionProps {
  trainingsAttended: string;
  missingKnowledgeSkills: string;
  selfDevelopmentSuggestions: string;
  evaluatorComment: string;
  editable: boolean;
  onTrainingsAttendedChange: (value: string) => void;
  onMissingKnowledgeSkillsChange: (value: string) => void;
  onSelfDevelopmentSuggestionsChange: (value: string) => void;
  onEvaluatorCommentChange: (value: string) => void;
}

const TRAINING_FIELDS = [
  {
    id: 'trainings-attended',
    labelKey: 'evaluation.trainingFields.attended',
    valueKey: 'trainingsAttended' as const,
    onChangeKey: 'onTrainingsAttendedChange' as const,
  },
  {
    id: 'missing-knowledge',
    labelKey: 'evaluation.trainingFields.missingKnowledge',
    valueKey: 'missingKnowledgeSkills' as const,
    onChangeKey: 'onMissingKnowledgeSkillsChange' as const,
  },
  {
    id: 'self-development',
    labelKey: 'evaluation.trainingFields.selfDevelopment',
    valueKey: 'selfDevelopmentSuggestions' as const,
    onChangeKey: 'onSelfDevelopmentSuggestionsChange' as const,
  },
  {
    id: 'evaluator-comment',
    labelKey: 'evaluation.trainingFields.evaluatorComment',
    valueKey: 'evaluatorComment' as const,
    onChangeKey: 'onEvaluatorCommentChange' as const,
  },
] as const;

export function TrainingSection({
  trainingsAttended,
  missingKnowledgeSkills,
  selfDevelopmentSuggestions,
  evaluatorComment,
  editable,
  onTrainingsAttendedChange,
  onMissingKnowledgeSkillsChange,
  onSelfDevelopmentSuggestionsChange,
  onEvaluatorCommentChange,
}: TrainingSectionProps) {
  const { formatMessage } = useIntl();
  const values = {
    trainingsAttended,
    missingKnowledgeSkills,
    selfDevelopmentSuggestions,
    evaluatorComment,
  };
  const handlers = {
    onTrainingsAttendedChange,
    onMissingKnowledgeSkillsChange,
    onSelfDevelopmentSuggestionsChange,
    onEvaluatorCommentChange,
  };

  return (
    <FormSection title={formatMessage({ id: 'evaluation.trainingAndDevelopment' })}>
      <div className="form-list">
        {TRAINING_FIELDS.map((field) => (
          <div key={field.id} className="form-row">
            <label htmlFor={field.id}>{formatMessage({ id: field.labelKey as never })}</label>
            <textarea
              id={field.id}
              rows={3}
              value={values[field.valueKey]}
              onChange={(e) => handlers[field.onChangeKey](e.target.value)}
              disabled={!editable}
            />
          </div>
        ))}
      </div>
    </FormSection>
  );
}
