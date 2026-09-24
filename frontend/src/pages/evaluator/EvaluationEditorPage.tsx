import { Link, useParams } from 'react-router-dom';
import { useCallback, useEffect, useState } from 'react';
import { api } from '../../api/client';
import type { Employee, EvaluationDetail } from '../../api/types';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { CardSkeleton } from '../../components/common/LoadingSkeleton';
import { PageBackLink } from '../../components/common/PageBackLink';
import { EvaluationPlanningOverview } from '../../components/evaluation/EvaluationPlanningOverview';
import { MeasuresEditorSection } from '../../components/evaluation/MeasuresEditorSection';
import { SubmitEvaluationPanel } from '../../components/evaluation/SubmitEvaluationPanel';
import { TrainingSection } from '../../components/evaluation/TrainingSection';
import { UnsavedChangesIndicator } from '../../components/evaluation/UnsavedChangesIndicator';
import { AppLayout } from '../../components/AppLayout';
import { useAuth } from '../../auth/AuthContext';
import {
  useEvaluation,
  useLookups,
  useToast,
  useUnsavedChangesGuard,
} from '../../hooks';
import {
  EvaluationStatusSummary,
  EvaluationScoresSummary,
} from './components/EvaluationRatingMeta';
import { GoalsPlanningEmployeeCard } from './components/GoalsPlanningEmployeeCard';
import {
  findNotRatedLevelId,
  hasLocalIncompleteRatings,
  calculateComponentAverage,
  calculateOverallAverage,
  canSubmitEvaluationDraft,
} from '../../utils/scoring';
import { descriptiveRatingNameFromAverage } from '../../utils/descriptiveRating';
import { isGoalsPlanningComplete } from '../../utils/goalsPlanning';
import { getMeasureRatingComment } from '../../utils/measureRatingDefaults';
import {
  fetchLatestEvaluation,
  isStaleEvaluationError,
  toRatingGoalSaveItems,
  type RatingGoalSaveItem,
} from '../../utils/evaluationSave';
import type { MeasureDraft } from '../../components/evaluation/MeasuresEditorSection';
import { useIntl } from '../../i18n';

export function EvaluationEditorPage() {
  const { formatMessage } = useIntl();
  const { id } = useParams<{ id: string }>();
  const toast = useToast();
  const { ratingLevels, measureTypes, descriptiveRatings } = useLookups();
  const { evaluation, setEvaluation, loading, error } = useEvaluation(id);

  const [employee, setEmployee] = useState<Employee | null>(null);
  const [saving, setSaving] = useState(false);
  const [confirmSubmit, setConfirmSubmit] = useState(false);
  const [measures, setMeasures] = useState<MeasureDraft[]>([]);
  const [trainingsAttended, setTrainingsAttended] = useState('');
  const [missingKnowledgeSkills, setMissingKnowledgeSkills] = useState('');
  const [selfDevelopmentSuggestions, setSelfDevelopmentSuggestions] =
    useState('');
  const [trainingEvaluatorComment, setTrainingEvaluatorComment] = useState('');
  const [conditionsFulfilled, setConditionsFulfilled] = useState(true);
  const [conditionsNotMetComment, setConditionsNotMetComment] = useState('');
  const [isDirty, setIsDirty] = useState(false);

  const { activeRole } = useAuth();
  // Nacrt menja samo ocenjivač; admin ga samo gleda.
  const editable = evaluation?.status === 'Draft' && activeRole === 'EVALUATOR';
  // Ocenjivač bez kontrolora: poslata ocena je odmah odobrena.
  const approvesOnSubmit = evaluation?.controllerEmployeeId == null;
  const notRatedLevelId = findNotRatedLevelId(ratingLevels);

  const markDirty = useCallback(() => {
    if (!editable) return;
    setIsDirty(true);
  }, [editable]);

  useUnsavedChangesGuard(
    editable && isDirty,
    formatMessage({ id: 'common.unsavedChangesWarning' }),
  );

  useEffect(() => {
    if (error) toast.error(error);
  }, [error, toast]);

  useEffect(() => {
    if (!evaluation?.employeeId) {
      setEmployee(null);
      return;
    }

    let cancelled = false;
    api
      .get<Employee>(`/api/employees/${evaluation.employeeId}`)
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
    setIsDirty(false);

    const hydratedMeasures: MeasureDraft[] =
      evaluation.measures.length > 0
        ? evaluation.measures.map((m) => {
            const mt = measureTypes.find((t) => t.id === m.measureTypeId);
            const level = ratingLevels.find((rl) => rl.id === m.ratingLevelId);
            const autoComment =
              mt && level && level.value > 0
                ? getMeasureRatingComment(mt.code, level.value, formatMessage)
                : '';

            return {
              measureTypeId: m.measureTypeId,
              ratingComment: m.ratingComment?.trim()
                ? m.ratingComment
                : autoComment,
              ratingLevelId: m.ratingLevelId,
              sortOrder: m.sortOrder,
            };
          })
        : measureTypes.map((mt, i) => ({
            measureTypeId: mt.id,
            ratingComment: '',
            ratingLevelId: notRatedLevelId ?? ratingLevels[0]?.id ?? 1,
            sortOrder: mt.sortOrder ?? i,
          }));

    setMeasures(hydratedMeasures);
    setTrainingsAttended(evaluation.training?.trainingDescription ?? '');
    setMissingKnowledgeSkills(evaluation.training?.knowledgeDescription ?? '');
    setSelfDevelopmentSuggestions(
      evaluation.training?.developmentDescription ?? '',
    );
    setTrainingEvaluatorComment(evaluation.training?.evaluatorComment ?? '');
    setConditionsFulfilled(evaluation.conditionsFulfilled !== false);
    setConditionsNotMetComment(evaluation.conditionsNotMetComment ?? '');
  }, [
    evaluation?.id,
    evaluation?.version,
    measureTypes,
    ratingLevels,
    formatMessage,
    notRatedLevelId,
  ]);

  function handleEvaluationChange(next: EvaluationDetail) {
    setEvaluation(next);
    markDirty();
  }

  const handleMeasuresChange = useCallback(
    (next: MeasureDraft[]) => {
      setMeasures(next);
      markDirty();
    },
    [markDirty],
  );

  const handleTrainingsAttendedChange = useCallback(
    (value: string) => {
      setTrainingsAttended(value);
      markDirty();
    },
    [markDirty],
  );

  const handleMissingKnowledgeSkillsChange = useCallback(
    (value: string) => {
      setMissingKnowledgeSkills(value);
      markDirty();
    },
    [markDirty],
  );

  const handleSelfDevelopmentSuggestionsChange = useCallback(
    (value: string) => {
      setSelfDevelopmentSuggestions(value);
      markDirty();
    },
    [markDirty],
  );

  const handleTrainingEvaluatorCommentChange = useCallback(
    (value: string) => {
      setTrainingEvaluatorComment(value);
      markDirty();
    },
    [markDirty],
  );

  const handleConditionsFulfilledChange = useCallback(
    (value: boolean) => {
      setConditionsFulfilled(value);
      markDirty();
    },
    [markDirty],
  );

  const handleConditionsNotMetCommentChange = useCallback(
    (value: string) => {
      setConditionsNotMetComment(value);
      markDirty();
    },
    [markDirty],
  );

  /** Ocena je izmenjena posle učitavanja: prikazuje se njeno novo stanje. */
  const reloadAfterConflict = useCallback(
    async (id: number) => {
      toast.warning(formatMessage({ id: 'errors.evaluationChangedMeanwhile' }));
      try {
        setEvaluation(await fetchLatestEvaluation(id));
      } catch {
        // Stranica ostaje na starom stanju; sledeće čuvanje opet javlja konflikt.
      }
    },
    [formatMessage, setEvaluation, toast],
  );

  const persistEvaluation = useCallback(
    async (options?: {
      silent?: boolean;
    }): Promise<EvaluationDetail | null> => {
      if (!evaluation) return null;
      if (!options?.silent) {
        setSaving(true);
      }
      try {
        // Verzija sa kojom je stranica učitana: ako je ocenu u međuvremenu
        // sačuvao neko drugi, server odbija čuvanje umesto da je pregazi.
        const payload: {
          version: number;
          conversationAt: string | null;
          evaluatorComment: string | null;
          conditionsNotMetComment: string | null;
          conditionsFulfilled: boolean;
          goals?: RatingGoalSaveItem[];
          measures?: Array<{
            measureTypeId: number;
            ratingComment: string | null;
            ratingLevelId: number;
            sortOrder: number;
          }>;
          training?: {
            trainingDescription: string | null;
            knowledgeDescription: string | null;
            developmentDescription: string | null;
            evaluatorComment: string | null;
          };
        } = {
          version: evaluation.version,
          conversationAt: evaluation.conversationAt,
          evaluatorComment: evaluation.evaluatorComment,
          conditionsNotMetComment: conditionsNotMetComment || null,
          conditionsFulfilled,
        };

        if (conditionsFulfilled) {
          payload.goals = toRatingGoalSaveItems(evaluation.goals);
          payload.measures = measures.map((m, i) => ({
            measureTypeId: m.measureTypeId,
            ratingComment: m.ratingComment || null,
            ratingLevelId: m.ratingLevelId,
            sortOrder: m.sortOrder ?? i,
          }));
          payload.training = {
            trainingDescription: trainingsAttended || null,
            knowledgeDescription: missingKnowledgeSkills || null,
            developmentDescription: selfDevelopmentSuggestions || null,
            evaluatorComment: trainingEvaluatorComment || null,
          };
        }

        const current = await api.put<EvaluationDetail>(
          `/api/evaluations/${evaluation.id}/rating-draft`,
          payload,
        );

        setEvaluation(current);
        setIsDirty(false);
        if (!options?.silent) {
          toast.success(formatMessage({ id: 'alerts.evaluationSaved' }));
        }
        return current;
      } catch (e) {
        if (isStaleEvaluationError(e)) {
          await reloadAfterConflict(evaluation.id);
          return null;
        }
        // Izmene ostaju u formi, da ih korisnik ne izgubi zbog greške.
        toast.error(
          e instanceof Error
            ? e.message
            : formatMessage({ id: 'errors.saveFailed' }),
        );
        return null;
      } finally {
        if (!options?.silent) {
          setSaving(false);
        }
      }
    },
    [
      evaluation,
      conditionsNotMetComment,
      conditionsFulfilled,
      formatMessage,
      measures,
      missingKnowledgeSkills,
      reloadAfterConflict,
      selfDevelopmentSuggestions,
      setEvaluation,
      toast,
      trainingEvaluatorComment,
      trainingsAttended,
    ],
  );

  async function saveAll() {
    await persistEvaluation();
  }

  async function submitEvaluation() {
    if (!evaluation) return;
    setSaving(true);
    try {
      const saved = await persistEvaluation({ silent: true });
      if (!saved) {
        return;
      }

      const updated = await api.post<EvaluationDetail>(
        `/api/evaluations/${saved.id}/submit`,
        {
          version: saved.version,
        },
      );
      setEvaluation(updated);
      setIsDirty(false);
      toast.success(
        formatMessage({
          id:
            updated.status === 'Approved'
              ? 'alerts.evaluationApprovedOnSubmit'
              : 'alerts.evaluationSubmitted',
        }),
      );
      setConfirmSubmit(false);
    } catch (e) {
      if (isStaleEvaluationError(e)) {
        setConfirmSubmit(false);
        await reloadAfterConflict(evaluation.id);
        return;
      }
      toast.error(
        e instanceof Error
          ? e.message
          : formatMessage({ id: 'errors.submitFailed' }),
      );
    } finally {
      setSaving(false);
    }
  }

  const incompleteRatings =
    evaluation && conditionsFulfilled
      ? hasLocalIncompleteRatings(evaluation.goals, measures, notRatedLevelId)
      : false;

  const submitAllowed = evaluation
    ? canSubmitEvaluationDraft(
        conditionsFulfilled,
        conditionsNotMetComment,
        incompleteRatings,
      )
    : false;

  const liveGoalsAverage = evaluation
    ? calculateComponentAverage(
        evaluation.goals.map((goal) => ({
          ratingLevelId: goal.ratingLevelId,
          weight: goal.weight,
        })),
        ratingLevels,
      )
    : null;
  const liveMeasuresAverage = calculateComponentAverage(
    measures.map((measure) => ({ ratingLevelId: measure.ratingLevelId })),
    ratingLevels,
  );
  const liveOverallAverage = calculateOverallAverage(
    liveGoalsAverage,
    liveMeasuresAverage,
  );
  const liveDescriptiveRatingName =
    !incompleteRatings && liveOverallAverage != null
      ? descriptiveRatingNameFromAverage(liveOverallAverage, descriptiveRatings)
      : null;

  if (loading || !evaluation) {
    return (
      <AppLayout title={formatMessage({ id: 'evaluation.title' })}>
        {loading ? (
          <>
            <CardSkeleton lines={2} />
            <CardSkeleton lines={5} />
          </>
        ) : (
          <div className="empty-state">
            <p className="empty-state__title">
              {formatMessage({ id: 'errors.evaluationNotFound' })}
            </p>
          </div>
        )}
      </AppLayout>
    );
  }

  if (editable && !isGoalsPlanningComplete(evaluation)) {
    return (
      <AppLayout title={formatMessage({ id: 'evaluation.title' })}>
        <div className="card">
          <p>{formatMessage({ id: 'evaluation.setPlanningBeforeRating' })}</p>
          <Link
            to={`/evaluator/goals/evaluations/${evaluation.id}`}
            className="btn btn-primary"
          >
            {formatMessage({ id: 'evaluation.setGoals' })}
          </Link>
        </div>
      </AppLayout>
    );
  }

  return (
    <AppLayout
      title={`${formatMessage({ id: 'evaluation.ratingTitle' })} — ${evaluation.employeeFullName}`}
    >
      <PageBackLink
        to="/evaluator/workflow"
        label={formatMessage({ id: 'buttons.backToList' })}
      />
      <div className="form-page">
        <GoalsPlanningEmployeeCard
          evaluation={evaluation}
          employee={employee}
          employeeAnalyticsPath={`/evaluator/employees/${evaluation.employeeId}`}
          showStatusHistory
          ratingAside={
            <EvaluationStatusSummary
              evaluation={evaluation}
              showStatusHistory
            />
          }
          footer={
            evaluation.controllerComment ? (
              <div className="alert alert-info goals-employee-card__controller-comment">
                <strong>
                  {formatMessage({ id: 'evaluation.controllerCommentLabel' })}
                </strong>{' '}
                {evaluation.controllerComment}
              </div>
            ) : undefined
          }
        />
        <UnsavedChangesIndicator visible={editable && isDirty} />

        {/* Dok traje snimanje/slanje polja su zaključana: odgovor servera ponovo puni
            formu, pa bi se izmene unete u međuvremenu tiho izgubile. */}
        <fieldset className="form-page__fieldset" disabled={saving}>
          <EvaluationPlanningOverview
            evaluation={evaluation}
            ratingLevels={ratingLevels}
            editable={editable}
            onEvaluationChange={handleEvaluationChange}
            showConditionsToggle={editable}
            conditionsFulfilled={conditionsFulfilled}
            conditionsNotMetComment={conditionsNotMetComment}
            onConditionsFulfilledChange={handleConditionsFulfilledChange}
            onConditionsNotMetCommentChange={
              handleConditionsNotMetCommentChange
            }
          />

          {conditionsFulfilled && (
            <>
              <MeasuresEditorSection
                measures={measures}
                measureTypes={measureTypes}
                ratingLevels={ratingLevels}
                editable={editable}
                onMeasuresChange={handleMeasuresChange}
              />

              <TrainingSection
                trainingsAttended={trainingsAttended}
                missingKnowledgeSkills={missingKnowledgeSkills}
                selfDevelopmentSuggestions={selfDevelopmentSuggestions}
                evaluatorComment={trainingEvaluatorComment}
                editable={editable}
                onTrainingsAttendedChange={handleTrainingsAttendedChange}
                onMissingKnowledgeSkillsChange={
                  handleMissingKnowledgeSkillsChange
                }
                onSelfDevelopmentSuggestionsChange={
                  handleSelfDevelopmentSuggestionsChange
                }
                onEvaluatorCommentChange={handleTrainingEvaluatorCommentChange}
              />
            </>
          )}
        </fieldset>

        {editable ? (
          <SubmitEvaluationPanel
            evaluation={{ ...evaluation, conditionsFulfilled }}
            incompleteRatings={incompleteRatings}
            liveOverallAverage={liveOverallAverage}
            liveDescriptiveRatingName={liveDescriptiveRatingName}
            submitAllowed={submitAllowed}
            submitRequirementsKey={
              conditionsFulfilled
                ? 'evaluation.submitRequirements'
                : 'evaluation.submitRequirementsConditionsNotMet'
            }
            saving={saving}
            onSave={saveAll}
            onSubmit={() => setConfirmSubmit(true)}
          />
        ) : (
          <section className="evaluation-submit-footer">
            {conditionsFulfilled && (
              <EvaluationScoresSummary
                evaluation={evaluation}
                incompleteRatings={incompleteRatings}
              />
            )}
          </section>
        )}
      </div>

      {editable && (
        <>
          <ConfirmDialog
            open={confirmSubmit}
            title={formatMessage({
              id: approvesOnSubmit
                ? 'evaluation.submitFinalTitle'
                : 'evaluation.submitToControllerTitle',
            })}
            message={formatMessage({
              id: approvesOnSubmit
                ? 'evaluation.submitFinalConfirm'
                : 'evaluation.submitToControllerConfirm',
            })}
            confirmLabel={formatMessage({ id: 'buttons.submit' })}
            busy={saving}
            busyLabel={formatMessage({ id: 'buttons.submitting' })}
            onConfirm={submitEvaluation}
            onCancel={() => setConfirmSubmit(false)}
          />
        </>
      )}
    </AppLayout>
  );
}
