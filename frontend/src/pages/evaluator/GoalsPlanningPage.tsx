import { useCallback, useEffect, useRef, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { api } from '../../api/client';
import type {
  Employee,
  EvaluationDetail,
  EvaluationSummary,
  PagedResult,
} from '../../api/types';
import { LoadingEmpty } from '../../components/common/LoadingEmpty';
import { FormSection } from '../../components/forms/FormSection';
import { GoalListEditor } from '../../components/forms/GoalListEditor';
import { TextListEditor } from '../../components/forms/TextListEditor';
import { AppLayout } from '../../components/AppLayout';
import { useAuth } from '../../auth/AuthContext';
import { useLookups, useToast, useUnsavedChangesGuard } from '../../hooks';
import {
  checkGoalWeights,
  defaultConversationDatetime,
  hasValidPlanningDraft,
  isGoalsPlanningComplete,
  MIN_GOAL_WEIGHT,
  toGoalDrafts,
  toTextDrafts,
  type GoalDraft,
  type TextItemDraft,
} from '../../utils/goalsPlanning';
import { roleListPath } from '../../utils/evaluationApi';
import { isStaleEvaluationError } from '../../utils/evaluationSave';
import { previousQuarter } from '../../utils/status';
import { GoalsPlanningEmployeeCard } from './components/GoalsPlanningEmployeeCard';
import { GoalsPlanningSidePanel } from './components/GoalsPlanningSidePanel';
import { GoalsReadOnlyOverview } from './components/GoalsReadOnlyOverview';
import {
  CopyPreviousPlanLink,
  PreviousPlanBanner,
  type PreviousPlan,
} from './components/PreviousPlanPrompt';
import { useIntl } from '../../i18n';

interface PreviousPlanResult {
  summary: EvaluationSummary | null;
  detail: EvaluationDetail | null;
  hasPlan: boolean;
}

/** Ocena istog zaposlenog iz datog kvartala i da li u njoj postoji plan. */
async function fetchPreviousPlan(
  employeeId: number,
  year: number,
  quarter: number,
): Promise<PreviousPlanResult> {
  const list = await api.get<PagedResult<EvaluationSummary>>(
    `${roleListPath('evaluations', 'evaluator')}?pageSize=5&year=${year}&quarter=${quarter}&employeeId=${employeeId}`,
  );
  const summary = list.items[0] ?? null;
  if (!summary) return { summary: null, detail: null, hasPlan: false };

  const detail = await api.get<EvaluationDetail>(
    `/api/evaluations/${summary.id}`,
  );
  const hasPlan = [
    ...detail.goals,
    ...detail.conditions,
    ...detail.criteria,
  ].some((item) => item.description.trim());
  return { summary, detail, hasPlan };
}

export function GoalsPlanningPage() {
  const { formatMessage } = useIntl();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const toast = useToast();
  const { loading: lookupsLoading } = useLookups();

  const [evaluation, setEvaluation] = useState<EvaluationDetail | null>(null);
  const [employee, setEmployee] = useState<Employee | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [copying, setCopying] = useState(false);

  const [goals, setGoals] = useState<GoalDraft[]>([]);
  const [conditions, setConditions] = useState<TextItemDraft[]>([]);
  const [criteria, setCriteria] = useState<TextItemDraft[]>([]);
  const [conversationAt, setConversationAt] = useState('');
  const [evaluatorComment, setEvaluatorComment] = useState('');
  const [isDirty, setIsDirty] = useState(false);
  const [previousPlan, setPreviousPlan] = useState<PreviousPlan | null>(null);
  const [previousSummary, setPreviousSummary] =
    useState<EvaluationSummary | null>(null);
  // Plan iz prethodnog kvartala se učitava odmah; preuzimanje čeka isti zahtev.
  const previousPlanRequest = useRef<Promise<PreviousPlanResult> | null>(null);

  const { activeRole } = useAuth();
  // Nacrt menja samo ocenjivač; admin ga samo gleda.
  const editable = evaluation?.status === 'Draft' && activeRole === 'EVALUATOR';
  const goalsLocked = evaluation ? isGoalsPlanningComplete(evaluation) : false;
  const canEditPlanning = editable && !goalsLocked;
  const previous = evaluation
    ? previousQuarter(evaluation.year, evaluation.quarter)
    : null;

  const markDirty = useCallback(() => {
    if (!canEditPlanning) return;
    setIsDirty(true);
  }, [canEditPlanning]);

  const { allowNextNavigation } = useUnsavedChangesGuard(
    canEditPlanning && isDirty,
    formatMessage({ id: 'common.unsavedChangesWarning' }),
  );

  const load = useCallback(async () => {
    if (!id) return;
    setLoading(true);
    try {
      const data = await api.get<EvaluationDetail>(`/api/evaluations/${id}`);
      const employeeData = await api.get<Employee>(
        `/api/employees/${data.employeeId}`,
      );
      setEvaluation(data);
      setEmployee(employeeData);
      setConversationAt(
        data.conversationAt
          ? data.conversationAt.slice(0, 16)
          : defaultConversationDatetime(),
      );
      setEvaluatorComment(data.evaluatorComment ?? '');
      setGoals(toGoalDrafts(data.goals));
      setConditions(
        data.conditions.length > 0
          ? data.conditions.map((c) => ({
              description: c.description,
              sortOrder: c.sortOrder,
            }))
          : [{ description: '', sortOrder: 0 }],
      );
      setCriteria(
        data.criteria.length > 0
          ? data.criteria.map((c) => ({
              description: c.description,
              sortOrder: c.sortOrder,
            }))
          : [{ description: '', sortOrder: 0 }],
      );
      setIsDirty(false);
    } catch (e) {
      toast.error(
        e instanceof Error
          ? e.message
          : formatMessage({ id: 'errors.loadFailed' }),
      );
    } finally {
      setLoading(false);
    }
  }, [id, formatMessage, toast]);

  useEffect(() => {
    load();
  }, [load]);

  const employeeId = evaluation?.employeeId;
  const previousYear = previous?.year;
  const previousQuarterNumber = previous?.quarter;

  useEffect(() => {
    if (
      !canEditPlanning ||
      employeeId == null ||
      previousYear == null ||
      previousQuarterNumber == null
    ) {
      return;
    }

    let active = true;
    const request = fetchPreviousPlan(
      employeeId,
      previousYear,
      previousQuarterNumber,
    );
    previousPlanRequest.current = request;
    setPreviousPlan({ state: 'loading' });
    request.then(
      (result) => {
        if (!active) return;
        setPreviousPlan({
          state: 'ready',
          detail: result.detail,
          hasPlan: result.hasPlan,
        });
        setPreviousSummary(result.summary);
      },
      () => {
        if (active) setPreviousPlan({ state: 'failed' });
      },
    );
    return () => {
      active = false;
    };
  }, [canEditPlanning, employeeId, previousYear, previousQuarterNumber]);

  const handleGoalsChange = useCallback(
    (items: GoalDraft[]) => {
      setGoals(items);
      markDirty();
    },
    [markDirty],
  );

  const handleConditionsChange = useCallback(
    (items: TextItemDraft[]) => {
      setConditions(items);
      markDirty();
    },
    [markDirty],
  );

  const handleCriteriaChange = useCallback(
    (items: TextItemDraft[]) => {
      setCriteria(items);
      markDirty();
    },
    [markDirty],
  );

  const handleConversationAtChange = useCallback(
    (value: string) => {
      setConversationAt(value);
      markDirty();
    },
    [markDirty],
  );

  const handleEvaluatorCommentChange = useCallback(
    (value: string) => {
      setEvaluatorComment(value);
      markDirty();
    },
    [markDirty],
  );

  const persistPlanningDraft = useCallback(async (): Promise<boolean> => {
    if (!evaluation || !canEditPlanning) return false;

    setSaving(true);
    try {
      const validGoals = goals.filter((g) => g.description.trim());
      const validConditions = conditions.filter((c) => c.description.trim());
      const validCriteria = criteria.filter((c) => c.description.trim());

      const current = await api.put<EvaluationDetail>(
        `/api/evaluations/${evaluation.id}/planning-draft`,
        {
          // Verzija sa kojom je stranica učitana: tuđa izmena u međuvremenu
          // se ne pregazi, server javlja konflikt.
          version: evaluation.version,
          conversationAt: new Date(
            conversationAt || defaultConversationDatetime(),
          ).toISOString(),
          evaluatorComment: evaluatorComment || null,
          conditionsNotMetComment: evaluation.conditionsNotMetComment ?? null,
          conditionsFulfilled: evaluation.conditionsFulfilled !== false,
          goals: validGoals.map((g, i) => ({
            description: g.description.trim(),
            ratingLevelId: null,
            comment: null,
            weight: g.weight,
            sortOrder: g.sortOrder ?? i,
          })),
          conditions: validConditions.map((c, i) => ({
            description: c.description.trim(),
            sortOrder: c.sortOrder ?? i,
          })),
          criteria: validCriteria.map((c, i) => ({
            description: c.description.trim(),
            sortOrder: c.sortOrder ?? i,
          })),
        },
      );

      setEvaluation(current);
      setIsDirty(false);
      return true;
    } catch (e) {
      if (isStaleEvaluationError(e)) {
        toast.warning(
          formatMessage({ id: 'errors.evaluationChangedMeanwhile' }),
        );
        await load();
        return false;
      }
      // Izmene ostaju u formi, da ih korisnik ne izgubi zbog greške.
      toast.error(
        e instanceof Error
          ? e.message
          : formatMessage({ id: 'errors.saveFailed' }),
      );
      return false;
    } finally {
      setSaving(false);
    }
  }, [
    canEditPlanning,
    conditions,
    conversationAt,
    criteria,
    evaluation,
    evaluatorComment,
    formatMessage,
    load,
    goals,
    toast,
  ]);

  async function saveAll(): Promise<boolean> {
    if (!evaluation || !canEditPlanning) return false;

    if (!hasValidPlanningDraft(goals, conditions, criteria)) {
      toast.warning(formatMessage({ id: 'errors.goalsPlanningIncomplete' }));
      return false;
    }

    if (!checkGoalWeights(goals).valid) {
      toast.warning(formatMessage({ id: 'errors.goalWeightsInvalid' }));
      return false;
    }

    return persistPlanningDraft();
  }

  async function handleSave() {
    const saved = await saveAll();
    if (saved) {
      allowNextNavigation();
      toast.success(formatMessage({ id: 'alerts.goalsPlanningSaved' }));
      navigate('/evaluator/goals?tab=set');
    }
  }

  async function copyFromPreviousQuarter() {
    if (!evaluation || !previous || !canEditPlanning) return;

    const hasContent = [...goals, ...conditions, ...criteria].some((item) =>
      item.description.trim(),
    );
    if (
      hasContent &&
      !window.confirm(formatMessage({ id: 'evaluation.copyOverwriteConfirm' }))
    ) {
      return;
    }

    const { year, quarter } = previous;
    setCopying(true);
    try {
      // Ako učitavanje nije uspelo, preuzimanje pokušava ponovo.
      const result = await (previousPlan?.state !== 'failed' &&
      previousPlanRequest.current
        ? previousPlanRequest.current
        : fetchPreviousPlan(evaluation.employeeId, year, quarter));
      if (!result.summary) {
        toast.warning(
          formatMessage(
            { id: 'errors.noSavedEvaluationForQuarter' },
            { quarter, year },
          ),
        );
        return;
      }
      if (!result.detail || !result.hasPlan) {
        toast.warning(
          formatMessage(
            { id: 'errors.noPlanningDataForQuarter' },
            { quarter, year },
          ),
        );
        return;
      }

      setGoals(toGoalDrafts(result.detail.goals));
      setConditions(toTextDrafts(result.detail.conditions));
      setCriteria(toTextDrafts(result.detail.criteria));
      markDirty();
      toast.info(
        formatMessage({ id: 'alerts.copiedFromQuarter' }, { quarter, year }),
      );
    } catch (e) {
      toast.error(
        e instanceof Error
          ? e.message
          : formatMessage({ id: 'errors.copyFromPreviousQuarterFailed' }),
      );
    } finally {
      setCopying(false);
    }
  }

  if (loading || !evaluation) {
    return (
      <AppLayout title={formatMessage({ id: 'evaluation.goalsTitle' })}>
        <LoadingEmpty
          loading={loading}
          emptyMessage={formatMessage({ id: 'errors.evaluationNotFound' })}
        />
      </AppLayout>
    );
  }

  if (!editable && !goalsLocked) {
    return (
      <AppLayout title={formatMessage({ id: 'evaluation.goalsTitle' })}>
        <div className="card">
          <p>{formatMessage({ id: 'evaluation.notInGoalsPlanningPhase' })}</p>
          <Link to={`/evaluator/evaluations/${evaluation.id}`}>
            {formatMessage({ id: 'evaluation.review' })}
          </Link>
        </div>
      </AppLayout>
    );
  }

  const title = goalsLocked
    ? `${formatMessage({ id: 'evaluation.goalsTitle' })} — ${evaluation.employeeFullName}`
    : `${formatMessage({ id: 'evaluation.setGoals' })} — ${evaluation.employeeFullName}`;

  if (goalsLocked) {
    return (
      <AppLayout title={title}>
        <GoalsPlanningEmployeeCard
          evaluation={evaluation}
          employee={employee}
          employeeAnalyticsPath={`/evaluator/employees/${evaluation.employeeId}`}
          showStatusHistory
        />
        <div className="alert alert-info">
          {formatMessage({ id: 'evaluation.goalsLockedInfo' })}
        </div>
        <GoalsReadOnlyOverview evaluation={evaluation} />
        {editable && (
          <div className="card card--actions">
            <Link
              to={`/evaluator/evaluations/${evaluation.id}`}
              className="btn btn-primary"
            >
              {formatMessage({ id: 'evaluation.continueToEvaluation' })}
            </Link>
          </div>
        )}
      </AppLayout>
    );
  }

  const previousRating =
    previous && previousSummary?.descriptiveRatingName ? (
      <span className="goals-employee-card__previous">
        {formatMessage(
          { id: 'evaluation.planning.previousRating' },
          { ...previous, rating: previousSummary.descriptiveRatingName },
        )}
      </span>
    ) : undefined;

  // Prazna forma nudi prošli plan na vrhu ciljeva; kad već ima sadržaja,
  // preuzimanje ostaje kao link uz naslov (uz potvrdu pre prepisivanja).
  const formEmpty = ![...goals, ...conditions, ...criteria].some((item) =>
    item.description.trim(),
  );
  const previousPlanReady =
    previousPlan?.state === 'ready' && previousPlan.hasPlan
      ? previousPlan.detail
      : null;
  const noPreviousPlan =
    previousPlan?.state === 'ready' && !previousPlan.hasPlan;
  const copyDisabled = saving || lookupsLoading;

  return (
    <AppLayout title={title}>
      <GoalsPlanningEmployeeCard
        evaluation={evaluation}
        employee={employee}
        employeeAnalyticsPath={`/evaluator/employees/${evaluation.employeeId}`}
        compact
        badges={previousRating}
      />

      <div className="goals-planning">
        <div className="goals-planning__main">
          <FormSection
            title={formatMessage({ id: 'evaluation.goalsTitle' })}
            hint={formatMessage(
              { id: 'evaluation.planning.goalsHint' },
              { min: MIN_GOAL_WEIGHT },
            )}
            className="form-section--actions-top"
            actions={
              previous &&
              previousPlan &&
              !noPreviousPlan &&
              !(formEmpty && previousPlanReady) ? (
                <CopyPreviousPlanLink
                  quarter={previous.quarter}
                  year={previous.year}
                  copying={copying}
                  disabled={copyDisabled}
                  onCopy={copyFromPreviousQuarter}
                />
              ) : undefined
            }
          >
            {previous && formEmpty && previousPlanReady && (
              <PreviousPlanBanner
                quarter={previous.quarter}
                year={previous.year}
                detail={previousPlanReady}
                copying={copying}
                disabled={copyDisabled}
                onCopy={copyFromPreviousQuarter}
              />
            )}
            <GoalListEditor
              goals={goals}
              setGoals={handleGoalsChange}
              placeholder={formatMessage({
                id: 'evaluation.planning.goalPlaceholder',
              })}
              addLabel={formatMessage({ id: 'evaluation.addGoal' })}
            />
          </FormSection>

          <FormSection
            title={formatMessage({ id: 'evaluation.conditionsTitle' })}
            hint={formatMessage({ id: 'evaluation.planning.conditionsHint' })}
          >
            <TextListEditor
              items={conditions}
              setItems={handleConditionsChange}
              placeholder={formatMessage({
                id: 'evaluation.planning.conditionPlaceholder',
              })}
              addLabel={formatMessage({ id: 'evaluation.addCondition' })}
            />
          </FormSection>

          <FormSection
            title={formatMessage({ id: 'evaluation.criteriaTitle' })}
            hint={formatMessage({ id: 'evaluation.planning.criteriaHint' })}
          >
            <TextListEditor
              items={criteria}
              setItems={handleCriteriaChange}
              placeholder={formatMessage({
                id: 'evaluation.planning.criterionPlaceholder',
              })}
              addLabel={formatMessage({ id: 'evaluation.addCriterion' })}
            />
          </FormSection>
        </div>

        <aside className="goals-planning__aside">
          <GoalsPlanningSidePanel
            conversationAt={conversationAt}
            evaluatorComment={evaluatorComment}
            unsaved={canEditPlanning && isDirty}
            saving={saving}
            copying={copying}
            lookupsLoading={lookupsLoading}
            canSubmit={
              hasValidPlanningDraft(goals, conditions, criteria) &&
              checkGoalWeights(goals).valid
            }
            onConversationAtChange={handleConversationAtChange}
            onEvaluatorCommentChange={handleEvaluatorCommentChange}
            onSave={handleSave}
          />
        </aside>
      </div>
    </AppLayout>
  );
}
