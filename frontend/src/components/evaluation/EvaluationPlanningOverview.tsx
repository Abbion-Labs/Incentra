import type { EvaluationDetail, RatingLevel } from '../../api/types';
import { useIntl } from '../../i18n';
import { FormSection } from '../forms/FormSection';
import {
  ConditionsFulfilledToggle,
  ConditionsNotMetCommentField,
} from './ConditionsFulfilledControls';
import { SectionAverageFooter } from './SectionAverageFooter';
import { RatingValueSelect } from './RatingValueSelect';
import {
  calculateComponentAverage,
  formatComponentAverage,
  ratingValueOptionLabel,
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

function goalsIncomplete(
  goals: EvaluationDetail['goals'],
  ratingLevels: RatingLevel[],
): boolean {
  if (goals.length === 0) return true;
  return goals.some((goal) => {
    const level = findRatingLevel(ratingLevels, goal.ratingLevelId);
    return !level || isNotRated(level);
  });
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

  const goalsAverage = calculateComponentAverage(
    evaluation.goals.map((goal) => ({
      ratingLevelId: goal.ratingLevelId,
      weight: goal.weight,
    })),
    ratingLevels,
  );
  const showWeights = evaluation.goals.some((goal) => goal.weight != null);
  const averageText = formatComponentAverage(
    goalsIncomplete(evaluation.goals, ratingLevels),
    goalsAverage,
    evaluation.goals.length > 0,
  );

  return (
    <>
      <div className="table-wrap">
        <table className="table table--form">
          <thead>
            <tr>
              <th style={{ width: '2rem' }}>#</th>
              <th>{formatMessage({ id: 'evaluation.goalDescription' })}</th>
              {showWeights && (
                <th style={{ width: '5rem' }}>
                  {formatMessage({ id: 'evaluation.goalWeight' })}
                </th>
              )}
              <th style={{ width: '5.5rem' }}>
                {formatMessage({ id: 'evaluation.rating' })}
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
                  <td className="cell-muted">{idx + 1}</td>
                  <td className="cell-primary">{g.description}</td>
                  {showWeights && (
                    <td className="cell-muted cell-nowrap">
                      {formatGoalWeight(g.weight)}
                    </td>
                  )}
                  <td>
                    {editable ? (
                      <RatingValueSelect
                        ratingLevels={ratingLevels}
                        value={g.ratingLevelId}
                        onChange={(ratingLevelId) => {
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
                        }}
                      />
                    ) : selectedLevel ? (
                      ratingValueOptionLabel(selectedLevel)
                    ) : (
                      formatLocalizedRatingDisplay(
                        formatMessage,
                        g.ratingLevelValue,
                        g.ratingLevelLabel,
                      )
                    )}
                  </td>
                  <td className="cell-muted">
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
      <SectionAverageFooter
        label={formatMessage({ id: 'evaluation.goalsAverage' })}
        value={averageText}
      />
    </>
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

  return (
    <>
      <FormSection
        title={formatMessage({ id: 'evaluation.goalsTitle' })}
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
