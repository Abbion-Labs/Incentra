import { useCallback, useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { api } from "../../api/client";
import type {
  Employee,
  EvaluationDetail,
  EvaluationSummary,
  PagedResult,
} from "../../api/types";
import { LoadingEmpty } from "../../components/common/LoadingEmpty";
import { FormSection } from "../../components/forms/FormSection";
import { TextListEditor } from "../../components/forms/TextListEditor";
import { AppLayout } from "../../components/AppLayout";
import { useLookups, useToast, useUnsavedChangesGuard } from "../../hooks";
import { UnsavedChangesIndicator } from "../../components/evaluation/UnsavedChangesIndicator";
import {
  defaultConversationDatetime,
  hasValidPlanningDraft,
  isGoalsPlanningComplete,
  toTextDrafts,
  type TextItemDraft,
} from "../../utils/goalsPlanning";
import { fetchLatestEvaluation } from "../../utils/evaluationSave";
import { previousQuarter } from "../../utils/status";
import { CopyFromPreviousQuarterButton } from "./components/CopyFromPreviousQuarterButton";
import { GoalsConversationForm } from "./components/GoalsConversationForm";
import { GoalsPlanningEmployeeCard } from "./components/GoalsPlanningEmployeeCard";
import { GoalsReadOnlyOverview } from "./components/GoalsReadOnlyOverview";
import { useIntl } from "../../i18n";

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

  const [goals, setGoals] = useState<TextItemDraft[]>([]);
  const [conditions, setConditions] = useState<TextItemDraft[]>([]);
  const [criteria, setCriteria] = useState<TextItemDraft[]>([]);
  const [conversationAt, setConversationAt] = useState("");
  const [evaluatorComment, setEvaluatorComment] = useState("");
  const [isDirty, setIsDirty] = useState(false);

  const editable = evaluation?.status === "Draft";
  const goalsLocked = evaluation ? isGoalsPlanningComplete(evaluation) : false;
  const canEditPlanning = editable && !goalsLocked;

  const markDirty = useCallback(() => {
    if (!canEditPlanning) return;
    setIsDirty(true);
  }, [canEditPlanning]);

  const { allowNextNavigation } = useUnsavedChangesGuard(
    canEditPlanning && isDirty,
    formatMessage({ id: "common.unsavedChangesWarning" }),
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
      setEvaluatorComment(data.evaluatorComment ?? "");
      setGoals(
        data.goals.length > 0
          ? data.goals.map((g) => ({
              description: g.description,
              sortOrder: g.sortOrder,
            }))
          : [{ description: "", sortOrder: 0 }],
      );
      setConditions(
        data.conditions.length > 0
          ? data.conditions.map((c) => ({
              description: c.description,
              sortOrder: c.sortOrder,
            }))
          : [{ description: "", sortOrder: 0 }],
      );
      setCriteria(
        data.criteria.length > 0
          ? data.criteria.map((c) => ({
              description: c.description,
              sortOrder: c.sortOrder,
            }))
          : [{ description: "", sortOrder: 0 }],
      );
      setIsDirty(false);
    } catch (e) {
      toast.error(
        e instanceof Error
          ? e.message
          : formatMessage({ id: "errors.loadFailed" }),
      );
    } finally {
      setLoading(false);
    }
  }, [id, formatMessage, toast]);

  useEffect(() => {
    load();
  }, [load]);

  const handleGoalsChange = useCallback(
    (items: TextItemDraft[]) => {
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
      const server = await fetchLatestEvaluation(evaluation.id);

      const validGoals = goals.filter((g) => g.description.trim());
      const validConditions = conditions.filter((c) => c.description.trim());
      const validCriteria = criteria.filter((c) => c.description.trim());

      const current = await api.put<EvaluationDetail>(
        `/api/evaluations/${evaluation.id}/planning-draft`,
        {
          version: server.version,
          conversationAt: new Date(
            conversationAt || defaultConversationDatetime(),
          ).toISOString(),
          evaluatorComment: evaluatorComment || null,
          conditionsNotMetComment: server.conditionsNotMetComment ?? null,
          conditionsFulfilled: server.conditionsFulfilled !== false,
          goals: validGoals.map((g, i) => ({
            description: g.description.trim(),
            ratingLevelId: null,
            comment: null,
            weight: null,
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
      try {
        const latest = await fetchLatestEvaluation(evaluation.id);
        setEvaluation(latest);
      } catch {
        // ignore sync failure
      }
      toast.error(
        e instanceof Error
          ? e.message
          : formatMessage({ id: "errors.saveFailed" }),
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
    goals,
    toast,
  ]);

  async function saveAll(): Promise<boolean> {
    if (!evaluation || !canEditPlanning) return false;

    if (!hasValidPlanningDraft(goals, conditions, criteria)) {
      toast.warning(formatMessage({ id: "errors.goalsPlanningIncomplete" }));
      return false;
    }

    return persistPlanningDraft();
  }

  async function handleSave() {
    const saved = await saveAll();
    if (saved) {
      allowNextNavigation();
      toast.success(formatMessage({ id: "alerts.goalsPlanningSaved" }));
      navigate("/evaluator/goals?tab=set");
    }
  }

  async function copyFromPreviousQuarter() {
    if (!evaluation || !canEditPlanning) return;

    const hasContent = [...goals, ...conditions, ...criteria].some((item) =>
      item.description.trim(),
    );
    if (
      hasContent &&
      !window.confirm(formatMessage({ id: "evaluation.copyOverwriteConfirm" }))
    ) {
      return;
    }

    const { year, quarter } = previousQuarter(
      evaluation.year,
      evaluation.quarter,
    );
    setCopying(true);
    try {
      const list = await api.get<PagedResult<EvaluationSummary>>(
        `/api/evaluations?pageSize=5&year=${year}&quarter=${quarter}&employeeId=${evaluation.employeeId}`,
      );
      const previous = list.items[0];
      if (!previous) {
        toast.warning(
          formatMessage(
            { id: "errors.noSavedEvaluationForQuarter" },
            { quarter, year },
          ),
        );
        return;
      }

      const detail = await api.get<EvaluationDetail>(
        `/api/evaluations/${previous.id}`,
      );
      const hasGoals = detail.goals.some((g) => g.description.trim());
      const hasConditions = detail.conditions.some((c) => c.description.trim());
      const hasCriteria = detail.criteria.some((c) => c.description.trim());
      if (!hasGoals && !hasConditions && !hasCriteria) {
        toast.warning(
          formatMessage(
            { id: "errors.noPlanningDataForQuarter" },
            { quarter, year },
          ),
        );
        return;
      }

      setGoals(toTextDrafts(detail.goals));
      setConditions(toTextDrafts(detail.conditions));
      setCriteria(toTextDrafts(detail.criteria));
      markDirty();
      toast.info(
        formatMessage({ id: "alerts.copiedFromQuarter" }, { quarter, year }),
      );
    } catch (e) {
      toast.error(
        e instanceof Error
          ? e.message
          : formatMessage({ id: "errors.copyFromPreviousQuarterFailed" }),
      );
    } finally {
      setCopying(false);
    }
  }

  if (loading || !evaluation) {
    return (
      <AppLayout title={formatMessage({ id: "evaluation.goalsTitle" })}>
        <LoadingEmpty
          loading={loading}
          emptyMessage={formatMessage({ id: "errors.evaluationNotFound" })}
        />
      </AppLayout>
    );
  }

  if (!editable && !goalsLocked) {
    return (
      <AppLayout title={formatMessage({ id: "evaluation.goalsTitle" })}>
        <div className="card">
          <p>{formatMessage({ id: "evaluation.notInGoalsPlanningPhase" })}</p>
          <Link to={`/evaluator/evaluations/${evaluation.id}`}>
            {formatMessage({ id: "evaluation.review" })}
          </Link>
        </div>
      </AppLayout>
    );
  }

  const copyButton = canEditPlanning ? (
    <CopyFromPreviousQuarterButton
      copying={copying}
      saving={saving}
      lookupsLoading={lookupsLoading}
      onClick={copyFromPreviousQuarter}
    />
  ) : undefined;

  return (
    <AppLayout
      title={
        goalsLocked
          ? `${formatMessage({ id: "evaluation.goalsTitle" })} — ${evaluation.employeeFullName}`
          : `${formatMessage({ id: "evaluation.setGoals" })} — ${evaluation.employeeFullName}`
      }
    >
      <GoalsPlanningEmployeeCard
        evaluation={evaluation}
        employee={employee}
        employeeAnalyticsPath={`/evaluator/employees/${evaluation.employeeId}`}
        showStatusHistory
      />
      <UnsavedChangesIndicator visible={canEditPlanning && isDirty} />

      {goalsLocked ? (
        <>
          <div className="alert alert-info">
            {formatMessage({ id: "evaluation.goalsLockedInfo" })}
          </div>
          <GoalsReadOnlyOverview evaluation={evaluation} />
          {editable && (
            <div className="card">
              <Link
                to={`/evaluator/evaluations/${evaluation.id}`}
                className="btn btn-primary"
              >
                {formatMessage({ id: "evaluation.continueToEvaluation" })}
              </Link>
            </div>
          )}
        </>
      ) : (
        <div className="form-page">
          <FormSection
            title={formatMessage({ id: "evaluation.goalsTitle" })}
            hint={formatMessage({ id: "evaluation.goalsPlanningHint" })}
            actions={copyButton}
          >
            <TextListEditor
              items={goals}
              setItems={handleGoalsChange}
              placeholder={formatMessage({
                id: "evaluation.goalDescriptionPlaceholder",
              })}
              addLabel={formatMessage({ id: "evaluation.addGoal" })}
            />
          </FormSection>

          <div className="form-section-grid">
            <FormSection
              title={formatMessage({ id: "evaluation.conditionsTitle" })}
              hint={formatMessage({ id: "evaluation.conditionsHint" })}
              variant="secondary"
            >
              <TextListEditor
                items={conditions}
                setItems={handleConditionsChange}
                placeholder={formatMessage({
                  id: "evaluation.conditionPlaceholder",
                })}
                addLabel={formatMessage({ id: "evaluation.addCondition" })}
              />
            </FormSection>

            <FormSection
              title={formatMessage({ id: "evaluation.criteriaTitle" })}
              hint={formatMessage({ id: "evaluation.criteriaHint" })}
              variant="secondary"
            >
              <TextListEditor
                items={criteria}
                setItems={handleCriteriaChange}
                placeholder={formatMessage({
                  id: "evaluation.criterionPlaceholder",
                })}
                addLabel={formatMessage({ id: "evaluation.addCriterion" })}
              />
            </FormSection>
          </div>

          <GoalsConversationForm
            conversationAt={conversationAt}
            evaluatorComment={evaluatorComment}
            saving={saving}
            lookupsLoading={lookupsLoading}
            canSubmit={hasValidPlanningDraft(goals, conditions, criteria)}
            onConversationAtChange={handleConversationAtChange}
            onEvaluatorCommentChange={handleEvaluatorCommentChange}
            onSave={handleSave}
          />
        </div>
      )}
    </AppLayout>
  );
}
