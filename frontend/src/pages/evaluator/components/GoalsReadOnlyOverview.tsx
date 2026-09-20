import type { EvaluationDetail } from '../../../api/types';

import {
  EvaluationGoalsList,
  EvaluationTextListSection,
} from '../../../components/evaluation/EvaluationGoalsSection';

import { FormSection } from '../../../components/forms/FormSection';

import { useIntl } from '../../../i18n';

import { formatDateTime } from '../../../utils/formatLocale';

interface GoalsReadOnlyOverviewProps {
  evaluation: EvaluationDetail;
}

export function GoalsReadOnlyOverview({
  evaluation,
}: GoalsReadOnlyOverviewProps) {
  const { formatMessage } = useIntl();

  return (
    <div className="form-page">
      <EvaluationGoalsList goals={evaluation.goals} showRatings={false} />

      <div className="form-section-grid">
        <EvaluationTextListSection
          title={formatMessage({ id: 'evaluation.conditionsTitle' })}

          items={evaluation.conditions}

          emptyMessage={formatMessage({ id: 'evaluation.noConditionsSet' })}
        />

        <EvaluationTextListSection
          title={formatMessage({ id: 'evaluation.criteriaTitle' })}

          items={evaluation.criteria}

          emptyMessage={formatMessage({ id: 'evaluation.noCriteriaSet' })}
        />
      </div>

      {(evaluation.conversationAt || evaluation.evaluatorComment) && (
        <FormSection
          title={formatMessage({ id: 'evaluation.conversation' })}
          variant="secondary"
        >
          <dl className="employee-card__meta">
            {evaluation.conversationAt && (
              <div>
                <dt>{formatMessage({ id: 'evaluation.conversationDate' })}</dt>

                <dd>{formatDateTime(evaluation.conversationAt)}</dd>
              </div>
            )}

            {evaluation.evaluatorComment && (
              <div>
                <dt>{formatMessage({ id: 'evaluation.note' })}</dt>

                <dd>{evaluation.evaluatorComment}</dd>
              </div>
            )}
          </dl>
        </FormSection>
      )}
    </div>
  );
}
