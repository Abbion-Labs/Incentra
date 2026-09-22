import { useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { LoadingEmpty } from '../../components/common/LoadingEmpty';
import { PageBackLink } from '../../components/common/PageBackLink';
import {
  EvaluationGoalsList,
  EvaluationTextListSection,
} from '../../components/evaluation/EvaluationGoalsSection';
import { EvaluationHeaderCard } from '../../components/evaluation/EvaluationHeaderCard';
import { EvaluationMeasuresTable } from '../../components/evaluation/EvaluationMeasuresTable';
import { AppLayout } from '../../components/AppLayout';
import { useEvaluation, useToast } from '../../hooks';
import { useIntl } from '../../i18n';

export function EmployeeEvaluationDetailPage() {
  const { formatMessage } = useIntl();
  const toast = useToast();
  const { id } = useParams<{ id: string }>();
  const { evaluation, loading, error } = useEvaluation(id);

  useEffect(() => {
    if (error) toast.error(error);
  }, [error, toast]);

  if (loading || !evaluation) {
    return (
      <AppLayout
        title={formatMessage({ id: 'evaluation.employeeReviewTitle' })}
      >
        <LoadingEmpty
          loading={loading}
          emptyMessage={formatMessage({ id: 'errors.evaluationNotFound' })}
        />
      </AppLayout>
    );
  }

  return (
    <AppLayout
      title={formatMessage(
        { id: 'evaluation.myEvaluationTitle' },
        { quarter: evaluation.quarter, year: evaluation.year },
      )}
    >
      <PageBackLink
        to="/employee"
        label={formatMessage({ id: 'common.back' })}
      />

      <EvaluationHeaderCard
        evaluation={evaluation}
        variant="employee"
        showStatusHistory
      />
      {evaluation.conditionsFulfilled === false &&
        evaluation.conditionsNotMetComment && (
          <div className="alert alert-info">
            <strong>
              {formatMessage({ id: 'evaluation.conditionsNotMetCommentLabel' })}
            </strong>{' '}
            {evaluation.conditionsNotMetComment}
          </div>
        )}

      <EvaluationGoalsList
        goals={evaluation.goals}
        showRatings={
          evaluation.status !== 'Draft' &&
          evaluation.conditionsFulfilled !== false
        }
      />

      <EvaluationTextListSection
        title={formatMessage({ id: 'evaluation.conditionsTitle' })}
        items={evaluation.conditions}
        emptyMessage={formatMessage({ id: 'evaluation.noConditions' })}
      />

      <EvaluationTextListSection
        title={formatMessage({ id: 'evaluation.criteriaTitle' })}
        items={evaluation.criteria}
        emptyMessage={formatMessage({ id: 'evaluation.noCriteria' })}
      />

      {evaluation.conditionsFulfilled !== false && (
        <EvaluationMeasuresTable
          measures={evaluation.measures}
          variant="simple"
        />
      )}
    </AppLayout>
  );
}
