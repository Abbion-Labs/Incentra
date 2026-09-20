import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { api } from '../../api/client';
import type { Employee, EvaluationDetail } from '../../api/types';
import { CardSkeleton } from '../../components/common/LoadingSkeleton';
import { ControllerDecisionPanel } from '../../components/evaluation/ControllerDecisionPanel';
import { EvaluationPlanningOverview } from '../../components/evaluation/EvaluationPlanningOverview';
import { MeasuresEditorSection, type MeasureDraft } from '../../components/evaluation/MeasuresEditorSection';
import { TrainingSection } from '../../components/evaluation/TrainingSection';
import { AppLayout } from '../../components/AppLayout';
import { useEvaluation, useLookups, useToast } from '../../hooks';
import { useIntl } from '../../i18n';
import { GoalsPlanningEmployeeCard } from '../evaluator/components/GoalsPlanningEmployeeCard';
import { EvaluationScoresSummary, EvaluationStatusSummary } from '../evaluator/components/EvaluationRatingMeta';
import { detailHasIncompleteRatings } from '../../utils/scoring';
import { getMeasureRatingComment } from '../../utils/measureRatingDefaults';

export function ControllerReviewPage() {
  const { formatMessage } = useIntl();
  const { id } = useParams<{ id: string }>();
  const toast = useToast();
  const { ratingLevels, measureTypes } = useLookups();
  const { evaluation, setEvaluation, loading, error } = useEvaluation(id);
  const [employee, setEmployee] = useState<Employee | null>(null);
  const [saving, setSaving] = useState(false);
  const [controllerComment, setControllerComment] = useState('');
  const [revisionComment, setRevisionComment] = useState('');
  const [measures, setMeasures] = useState<MeasureDraft[]>([]);
  const [trainingsAttended, setTrainingsAttended] = useState('');
  const [missingKnowledgeSkills, setMissingKnowledgeSkills] = useState('');
  const [selfDevelopmentSuggestions, setSelfDevelopmentSuggestions] = useState('');
  const [trainingEvaluatorComment, setTrainingEvaluatorComment] = useState('');

  const canReview = evaluation?.status === 'Submitted' || evaluation?.status === 'UnderReview';
  const incompleteRatings = evaluation ? detailHasIncompleteRatings(evaluation) : false;

  useEffect(() => {
    if (error) toast.error(error);
  }, [error, toast]);

  useEffect(() => {
    if (!evaluation?.employeeId) {
      setEmployee(null);
      return;
    }

    let cancelled = false;
    api.get<Employee>(`/api/employees/${evaluation.employeeId}`)
      .then((data) => {
        if (!cancelled) setEmployee(data);
      })
      .catch(() => {
        if (!cancelled) setEmployee(null);
      });

    return () => {
      cancelled = true;
    };
  }, [evaluation?.employeeId]);

  useEffect(() => {
    if (!evaluation) return;
    setMeasures(
      evaluation.measures.map((m) => {
        const mt = measureTypes.find((t) => t.id === m.measureTypeId);
        const level = ratingLevels.find((rl) => rl.id === m.ratingLevelId);
        const autoComment =
          mt && level && level.value > 0
            ? getMeasureRatingComment(mt.code, level.value, formatMessage)
            : '';

        return {
          measureTypeId: m.measureTypeId,
          ratingComment: m.ratingComment?.trim() ? m.ratingComment : autoComment,
          ratingLevelId: m.ratingLevelId,
          sortOrder: m.sortOrder,
        };
      }),
    );
    setTrainingsAttended(evaluation.training?.trainingDescription ?? '');
    setMissingKnowledgeSkills(evaluation.training?.knowledgeDescription ?? '');
    setSelfDevelopmentSuggestions(evaluation.training?.developmentDescription ?? '');
    setTrainingEvaluatorComment(evaluation.training?.evaluatorComment ?? '');
  }, [evaluation?.id, evaluation?.version, measureTypes, ratingLevels, formatMessage]);

  useEffect(() => {
    if (evaluation?.controllerComment) {
      setControllerComment(evaluation.controllerComment);
    }
  }, [evaluation?.id, evaluation?.controllerComment]);

  async function startReviewIfNeeded() {
    if (!evaluation || evaluation.status !== 'Submitted') return evaluation;
    return api.post<EvaluationDetail>(`/api/evaluations/${evaluation.id}/start-review`, {
      version: evaluation.version,
    });
  }

  async function approve() {
    if (!evaluation) return;
    setSaving(true);
    try {
      let current = await startReviewIfNeeded();
      if (current) setEvaluation(current);
      const updated = await api.post<EvaluationDetail>(`/api/evaluations/${evaluation.id}/approve`, {
        version: current?.version ?? evaluation.version,
        controllerComment: controllerComment || null,
      });
      setEvaluation(updated);
      toast.success(formatMessage({ id: 'alerts.evaluationApproved' }));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : formatMessage({ id: 'errors.approveFailed' }));
    } finally {
      setSaving(false);
    }
  }

  async function returnForRevision() {
    if (!evaluation) return;
    if (!revisionComment.trim()) {
      toast.warning(formatMessage({ id: 'errors.revisionCommentRequired' }));
      return;
    }
    setSaving(true);
    try {
      let current = await startReviewIfNeeded();
      if (current) setEvaluation(current);
      const updated = await api.post<EvaluationDetail>(
        `/api/evaluations/${evaluation.id}/return-for-revision`,
        {
          version: current?.version ?? evaluation.version,
          revisionComment: revisionComment.trim(),
        },
      );
      setEvaluation(updated);
      toast.success(formatMessage({ id: 'alerts.evaluationReturned' }));
      setRevisionComment('');
    } catch (e) {
      toast.error(e instanceof Error ? e.message : formatMessage({ id: 'errors.returnFailed' }));
    } finally {
      setSaving(false);
    }
  }

  if (loading || !evaluation) {
    return (
      <AppLayout title={formatMessage({ id: 'controller.reviewTitle' })}>
        {loading ? (
          <>
            <CardSkeleton lines={2} />
            <CardSkeleton lines={5} />
          </>
        ) : (
          <div className="empty-state">
            <p className="empty-state__title">{formatMessage({ id: 'errors.evaluationNotFound' })}</p>
          </div>
        )}
      </AppLayout>
    );
  }

  return (
    <AppLayout title={formatMessage({ id: 'controller.reviewTitleWithName' }, { name: evaluation.employeeFullName })}>
      <div className="form-page">
        <GoalsPlanningEmployeeCard
          evaluation={evaluation}
          employee={employee}
          showEvaluator
          hidePeriod
          showStatusHistory
          employeeAnalyticsPath={`/controller/employees/${evaluation.employeeId}`}
          ratingAside={<EvaluationStatusSummary evaluation={evaluation} showPeriod showStatusHistory />}
          footer={evaluation.controllerComment && !canReview ? (
            <div className="alert alert-info goals-employee-card__controller-comment">
              <strong>{formatMessage({ id: 'evaluation.controllerCommentLabel' })}</strong> {evaluation.controllerComment}
            </div>
          ) : undefined}
        />

        <EvaluationPlanningOverview
          evaluation={evaluation}
          ratingLevels={ratingLevels}
          editable={false}
          onEvaluationChange={setEvaluation}
          showConditionsToggle
          conditionsFulfilled={evaluation.conditionsFulfilled !== false}
          conditionsNotMetComment={evaluation.conditionsNotMetComment ?? ''}
        />

        {evaluation.conditionsFulfilled !== false && (
          <>
            <MeasuresEditorSection
              measures={measures}
              measureTypes={measureTypes}
              ratingLevels={ratingLevels}
              editable={false}
              onMeasuresChange={setMeasures}
            />

            <TrainingSection
              trainingsAttended={trainingsAttended}
              missingKnowledgeSkills={missingKnowledgeSkills}
              selfDevelopmentSuggestions={selfDevelopmentSuggestions}
              evaluatorComment={trainingEvaluatorComment}
              editable={false}
              onTrainingsAttendedChange={setTrainingsAttended}
              onMissingKnowledgeSkillsChange={setMissingKnowledgeSkills}
              onSelfDevelopmentSuggestionsChange={setSelfDevelopmentSuggestions}
              onEvaluatorCommentChange={setTrainingEvaluatorComment}
            />
          </>
        )}

        <section className="evaluation-submit-footer">
          {evaluation.conditionsFulfilled !== false && (
            <EvaluationScoresSummary
              evaluation={evaluation}
              incompleteRatings={incompleteRatings}
            />
          )}
          {canReview && (
            <ControllerDecisionPanel
              controllerComment={controllerComment}
              revisionComment={revisionComment}
              saving={saving}
              onControllerCommentChange={setControllerComment}
              onRevisionCommentChange={setRevisionComment}
              onApprove={approve}
              onReturnForRevision={returnForRevision}
            />
          )}
        </section>

      </div>
    </AppLayout>
  );
}
