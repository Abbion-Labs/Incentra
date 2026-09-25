import type { EvaluationDetail, RatingLevel } from '../../api/types';
import { useIntl } from '../../i18n';
import { FormSection } from '../forms/FormSection';
import {
  ConditionsFulfilledToggle,
  ConditionsNotMetCommentField,
} from './ConditionsFulfilledControls';
import { RatingScale } from './RatingScale';
import { SectionProgress } from './SectionProgress';
import {
  calculateComponentAverage,
  formatComponentAverage,
  findRatingLevel,
  isNotRated,
} from '../../utils/scoring';
import {
  formatLocalizedRatingDisplay,
  formatRatingLevelLabel,
} from '../../utils/ratingLevelLabels';
import { formatGoalWeight } from '../../utils/goalsPlanning';

interface EvaluationGoalsEditableProps {
  evaluation: EvaluationDetail;
  ratingLevels: RatingLevel[];
  editable: boolean;
  onChange: (evaluation: EvaluationDetail) => void;
}

function isGoalRated(
  goal: EvaluationDetail['goals'][number],
  ratingLevels: RatingLevel[],
): boolean {
  const level = findRatingLevel(ratingLevels, goal.ratingLevelId);
  return Boolean(level && !isNotRated(level));
}

/** Koliko ciljeva je ocenjeno i prosek ciljeva kako se prikazuje. */
function goalsProgress(
  evaluation: EvaluationDetail,
  ratingLevels: RatingLevel[],
) {
  const goals = evaluation.goals;
  const rated = goals.filter((goal) => isGoalRated(goal, ratingLevels)).length;
  const average = calculateComponentAverage(
    goals.map((goal) => ({
      ratingLevelId: goal.ratingLevelId,
      weight: goal.weight,
    })),
    ratingLevels,
  );
  return {
    rated,
    total: goals.length,
    averageText: formatComponentAverage(
      goals.length === 0 || rated < goals.length,
      average,
      goals.length > 0,
    ),
  };
}

function EvaluationGoalsEditable({
  evaluation,
  ratingLevels,
  editable,
  onChange,
}: EvaluationGoalsEditableProps) {
  const { formatMessage } = useIntl();
  if (evaluation.goals.length === 0) {
    return (
      <p className="empty-inline">
        {formatMessage({ id: 'evaluation.noGoalsSet' })}
      </p>
    );
  }

  // Udeo cilja u proseku je ponder iz postavljanja ciljeva; bez pondera se
  // prosek računa kao prosta sredina, pa svaki cilj ima jednak udeo.
  const hasWeights = evaluation.goals.some((goal) => goal.weight != null);
  const equalShare = Math.round(100 / evaluation.goals.length);
  const goalShare = (weight: number | null) =>
    hasWeights ? (formatGoalWeight(weight) ?? '0%') : `${equalShare}%`;
  return (
    <div className="table-wrap">
      <table className="table table--form table--stack">
        <thead>
          <tr>
            <th style={{ width: '2rem' }}>#</th>
            <th>{formatMessage({ id: 'evaluation.goalDescription' })}</th>
            <th style={{ width: '12rem' }}>
              {formatMessage({ id: 'evaluation.rating' })}
            </th>
            <th style={{ width: '6rem' }}>
              {formatMessage({ id: 'evaluation.goalShare' })}
            </th>
            <th style={{ width: '11rem' }}>
              {formatMessage({ id: 'evaluation.descriptiveLabel' })}
            </th>
          </tr>
        </thead>
        <tbody>
          {evaluation.goals.map((g, idx) => {
            const selectedLevel = findRatingLevel(
              ratingLevels,
              g.ratingLevelId,
            );
            return (
              <tr key={g.id}>
                <td className="cell-muted stack-hide">{idx + 1}</td>
                <td className="cell-primary stack-title">
                  <span className="stack-index" aria-hidden>
                    {idx + 1}.
                  </span>
                  {g.description}
                </td>
                <td
                  className="stack-wide"
                  data-label={formatMessage({ id: 'evaluation.rating' })}
                >
                  {selectedLevel ? (
                    <RatingScale
                      ratingLevels={ratingLevels}
                      value={g.ratingLevelId}
                      label={g.description}
                      onChange={
                        editable
                          ? (ratingLevelId) => {
                              const level = findRatingLevel(
                                ratingLevels,
                                ratingLevelId,
                              );
                              const next = evaluation.goals.map((goal) =>
                                goal.id === g.id && level
                                  ? {
                                      ...goal,
                                      ratingLevelId: level.id,
                                      ratingLevelValue: level.value,
                                      ratingLevelLabel: level.label,
                                    }
                                  : goal,
                              );
                              onChange({ ...evaluation, goals: next });
                            }
                          : undefined
                      }
                    />
                  ) : (
                    formatLocalizedRatingDisplay(
                      formatMessage,
                      g.ratingLevelValue,
                      g.ratingLevelLabel,
                    )
                  )}
                </td>
                <td
                  className="cell-nowrap goal-share"
                  data-label={formatMessage({
                    id: 'evaluation.goalShare',
                  })}
                >
                  {goalShare(g.weight)}
                </td>
                <td
                  className="cell-muted"
                  data-label={formatMessage({
                    id: 'evaluation.descriptiveLabel',
                  })}
                >
                  {formatRatingLevelLabel(formatMessage, {
                    level: selectedLevel,
                    value: g.ratingLevelValue,
                    label: g.ratingLevelLabel,
                  })}
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

interface EvaluationPlanningOverviewProps {
  evaluation: EvaluationDetail;
  ratingLevels: RatingLevel[];
  editable: boolean;
  onEvaluationChange: (evaluation: EvaluationDetail) => void;
  showConditionsToggle?: boolean;
  conditionsFulfilled?: boolean;
  conditionsNotMetComment?: string;
  onConditionsFulfilledChange?: (value: boolean) => void;
  onConditionsNotMetCommentChange?: (value: string) => void;
}

export function EvaluationPlanningOverview({
  evaluation,
  ratingLevels,
  editable,
  onEvaluationChange,
  showConditionsToggle = false,
  conditionsFulfilled = true,
  conditionsNotMetComment = '',
  onConditionsFulfilledChange,
  onConditionsNotMetCommentChange,
}: EvaluationPlanningOverviewProps) {
  const { formatMessage } = useIntl();
  const canRateGoals = !showConditionsToggle || conditionsFulfilled;
  const progress = goalsProgress(evaluation, ratingLevels);

  return (
    <>
      <FormSection
        title={formatMessage({ id: 'evaluation.goalsTitle' })}
        meta={
          canRateGoals && progress.total > 0 ? (
            <SectionProgress
              rated={progress.rated}
              total={progress.total}
              average={progress.averageText}
              showCount={editable}
            />
          ) : undefined
        }
        actions={
          showConditionsToggle && onConditionsFulfilledChange ? (
            <ConditionsFulfilledToggle
              checked={conditionsFulfilled}
              editable={editable}
              onChange={onConditionsFulfilledChange}
            />
          ) : undefined
        }
      >
        {!canRateGoals ? (
          <ConditionsNotMetCommentField
            value={conditionsNotMetComment}
            editable={editable && Boolean(onConditionsNotMetCommentChange)}
            onChange={onConditionsNotMetCommentChange ?? (() => {})}
          />
        ) : (
          <EvaluationGoalsEditable
            evaluation={evaluation}
            ratingLevels={ratingLevels}
            editable={editable}
            onChange={onEvaluationChange}
          />
        )}
      </FormSection>
      {canRateGoals &&
        (evaluation.conditions.length > 0 ||
          evaluation.criteria.length > 0) && (
          <div className="form-section-grid">
            {evaluation.conditions.length > 0 && (
              <FormSection
                title={formatMessage({ id: 'evaluation.conditionsTitle' })}
                variant="secondary"
              >
                <ul className="readonly-list">
                  {evaluation.conditions.map((c) => (
                    <li key={c.id}>{c.description}</li>
                  ))}
                </ul>
              </FormSection>
            )}
            {evaluation.criteria.length > 0 && (
              <FormSection
                title={formatMessage({ id: 'evaluation.criteriaTitle' })}
                variant="secondary"
              >
                <ul className="readonly-list">
                  {evaluation.criteria.map((c) => (
                    <li key={c.id}>{c.description}</li>
                  ))}
                </ul>
              </FormSection>
            )}
          </div>
        )}
    </>
  );
}
