import { useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { LoadingEmpty } from '../../components/common/LoadingEmpty';
import { PageBackLink } from '../../components/common/PageBackLink';
import { EvaluationPlanningOverview } from '../../components/evaluation/EvaluationPlanningOverview';
import { MeasuresEditorSection } from '../../components/evaluation/MeasuresEditorSection';
import { TrainingSection } from '../../components/evaluation/TrainingSection';
import { AppLayout } from '../../components/AppLayout';
import { useEmployee, useEvaluation, useLookups, useToast } from '../../hooks';
import { useIntl } from '../../i18n';
import {
  EvaluationScoresSummary,
  EvaluationStatusSummary,
} from '../evaluator/components/EvaluationRatingMeta';
import { GoalsPlanningEmployeeCard } from '../evaluator/components/GoalsPlanningEmployeeCard';
import { GoalsReadOnlyOverview } from '../evaluator/components/GoalsReadOnlyOverview';
import { measuresWithRatingComments } from '../../utils/measureRatingDefaults';
import { detailHasIncompleteRatings } from '../../utils/scoring';

const noop = () => {};

export function EmployeeEvaluationDetailPage() {
  const { formatMessage } = useIntl();
  const toast = useToast();
  const { id } = useParams<{ id: string }>();
  const { ratingLevels, measureTypes } = useLookups();
  const { evaluation, loading, error } = useEvaluation(id);
  const employee = useEmployee(evaluation?.employeeId);

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

  // Dok je ocena nacrt, zaposleni vidi samo plan; ocene vidi tek kad je poslata.
  const ratingsVisible = evaluation.status !== 'Draft';
  const conditionsFulfilled = evaluation.conditionsFulfilled !== false;

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

      <div className="form-page">
        <GoalsPlanningEmployeeCard
          evaluation={evaluation}
          employee={employee}
          showEvaluator
          showStatusHistory
          ratingAside={
            <EvaluationStatusSummary
              evaluation={evaluation}
              showStatusHistory
            />
          }
          footer={
            evaluation.controllerComment ||
            (ratingsVisible && evaluation.evaluatorComment) ? (
              <>
                {evaluation.controllerComment && (
                  <div className="alert alert-info goals-employee-card__controller-comment">
                    <strong>
                      {formatMessage({
                        id: 'evaluation.controllerCommentLabel',
                      })}
                    </strong>{' '}
                    {evaluation.controllerComment}
                  </div>
                )}
                {/* U nacrtu je napomena ocenjivača u delu o razgovoru. */}
                {ratingsVisible && evaluation.evaluatorComment && (
                  <p className="page-header__note">
                    <strong>
                      {formatMessage({ id: 'evaluation.evaluatorNoteLabel' })}
                    </strong>{' '}
                    {evaluation.evaluatorComment}
                  </p>
                )}
              </>
            ) : undefined
          }
        />

        {ratingsVisible ? (
          <>
            <EvaluationPlanningOverview
              evaluation={evaluation}
              ratingLevels={ratingLevels}
              editable={false}
              onEvaluationChange={noop}
              showConditionsToggle
              conditionsFulfilled={conditionsFulfilled}
              conditionsNotMetComment={evaluation.conditionsNotMetComment ?? ''}
            />

            {conditionsFulfilled && (
              <>
                <MeasuresEditorSection
                  measures={measuresWithRatingComments(
                    evaluation.measures,
                    measureTypes,
                    ratingLevels,
                    formatMessage,
                  )}
                  measureTypes={measureTypes}
                  ratingLevels={ratingLevels}
                  editable={false}
                  onMeasuresChange={noop}
                />

                <TrainingSection
                  trainingsAttended={
                    evaluation.training?.trainingDescription ?? ''
                  }
                  missingKnowledgeSkills={
                    evaluation.training?.knowledgeDescription ?? ''
                  }
                  selfDevelopmentSuggestions={
                    evaluation.training?.developmentDescription ?? ''
                  }
                  evaluatorComment={evaluation.training?.evaluatorComment ?? ''}
                  editable={false}
                  onTrainingsAttendedChange={noop}
                  onMissingKnowledgeSkillsChange={noop}
                  onSelfDevelopmentSuggestionsChange={noop}
                  onEvaluatorCommentChange={noop}
                />
              </>
            )}

            <section className="evaluation-submit-footer">
              {conditionsFulfilled && (
                <EvaluationScoresSummary
                  evaluation={evaluation}
                  incompleteRatings={detailHasIncompleteRatings(evaluation)}
                />
              )}
            </section>
          </>
        ) : (
          <GoalsReadOnlyOverview evaluation={evaluation} />
        )}
      </div>
    </AppLayout>
  );
}
