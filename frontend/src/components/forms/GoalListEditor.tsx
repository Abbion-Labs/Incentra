import { useIntl } from '../../i18n';
import {
  checkGoalWeights,
  hasCustomWeights,
  MIN_GOAL_WEIGHT,
  TOTAL_GOAL_WEIGHT,
  withEvenWeights,
  type GoalDraft,
} from '../../utils/goalsPlanning';
import { TEXT_LIMITS } from '../../utils/textLimits';
import { AutoGrowTextarea } from './AutoGrowTextarea';
import { AddItemButton, RemoveItemButton } from './ListItemButtons';

const maxLength = TEXT_LIMITS.planItem;
// Brojač se pojavljuje tek kad se tekst približi granici.
const showCountFrom = Math.floor(maxLength * 0.8);

interface GoalListEditorProps {
  goals: GoalDraft[];
  setGoals: (goals: GoalDraft[]) => void;
  placeholder: string;
  addLabel: string;
}

/**
 * Ciljevi sa težinom svakog cilja u procentima. Dok ocenjivač ne promeni
 * težine, dodavanje i uklanjanje cilja ih ponovo raspoređuje ravnomerno.
 */
export function GoalListEditor({
  goals,
  setGoals,
  placeholder,
  addLabel,
}: GoalListEditorProps) {
  const { formatMessage } = useIntl();
  const customized = hasCustomWeights(goals);

  const update = (index: number, patch: Partial<GoalDraft>) => {
    const next = [...goals];
    next[index] = { ...goals[index], ...patch };
    setGoals(next);
  };

  const resize = (next: GoalDraft[]) =>
    setGoals(customized ? next : withEvenWeights(next));

  // Uz jedan cilj zbir je isti broj kao u polju, pa se prikazuje tek kad
  // ima više ciljeva ili kad zbir nije ispravan.
  const weights = checkGoalWeights(goals);
  const showTotal =
    goals.some((goal) => goal.description.trim()) &&
    (goals.length > 1 || !weights.valid);

  return (
    <>
      <div className="form-list">
        {goals.map((goal, idx) => (
          <div key={idx} className="form-list__item form-list__item--weighted">
            <span className="form-list__index">{idx + 1}</span>
            <div className="form-list__field">
              <AutoGrowTextarea
                className="form-list__input"
                maxLength={maxLength}
                value={goal.description}
                onChange={(e) => update(idx, { description: e.target.value })}
                placeholder={placeholder}
              />
              {goal.description.length >= showCountFrom && (
                <span className="form-list__count">
                  {goal.description.length} / {maxLength}
                </span>
              )}
            </div>
            <label className="form-list__weight">
              <span className="form-list__weight-caption" aria-hidden="true">
                {formatMessage({ id: 'evaluation.goalWeight' })}
              </span>
              <span className="sr-only">
                {formatMessage(
                  { id: 'evaluation.goalWeightLabel' },
                  { index: idx + 1 },
                )}
              </span>
              <span className="form-list__weight-control">
                <input
                  type="number"
                  inputMode="numeric"
                  min={MIN_GOAL_WEIGHT}
                  max={TOTAL_GOAL_WEIGHT}
                  step={1}
                  value={goal.weight ?? ''}
                  onChange={(e) =>
                    update(idx, {
                      weight:
                        e.target.value === '' ? null : Number(e.target.value),
                    })
                  }
                />
                <span aria-hidden="true">%</span>
              </span>
            </label>
            {goals.length > 1 && (
              <RemoveItemButton
                onClick={() => resize(goals.filter((_, i) => i !== idx))}
              />
            )}
          </div>
        ))}
      </div>
      {/* Zbir stoji ispod kolone sa težinama, kao zbir u tabeli. */}
      <div className="goal-weights__footer">
        <div className="goal-weights__actions">
          <AddItemButton
            label={addLabel}
            onClick={() =>
              resize([
                ...goals,
                { description: '', sortOrder: goals.length, weight: null },
              ])
            }
          />
          {customized && goals.length > 1 && (
            <button
              type="button"
              className="btn btn-secondary btn-sm"
              onClick={() => setGoals(withEvenWeights(goals))}
            >
              {formatMessage({ id: 'evaluation.distributeWeightsEvenly' })}
            </button>
          )}
          {showTotal && (
            <span className="goal-weights__total-label" aria-hidden="true">
              {formatMessage({ id: 'evaluation.planning.weightsTotalLabel' })}
            </span>
          )}
        </div>
        {showTotal && (
          <span
            className={`goal-weights__total ${weights.valid ? 'is-complete' : 'is-invalid'}`}
            role="status"
          >
            <span className="goal-weights__total-value" aria-hidden="true">
              {weights.total}
            </span>
            <span aria-hidden="true">%</span>
            <span className="sr-only">
              {formatMessage(
                { id: 'evaluation.goalWeightsTotal' },
                { total: weights.total, required: TOTAL_GOAL_WEIGHT },
              )}
            </span>
          </span>
        )}
      </div>
    </>
  );
}
