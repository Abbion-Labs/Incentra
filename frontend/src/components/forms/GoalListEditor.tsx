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

  return (
    <>
      {goals.some((goal) => goal.description.trim()) && (
        <div className="goal-weights-summary">
          <GoalWeightsBar goals={goals} />
          <GoalWeightsTotal goals={goals} />
        </div>
      )}
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
      <div className="goal-weights__footer">
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
      </div>
    </>
  );
}

/**
 * Raspodela težina na jednoj traci, segment po cilju sa opisom.
 * Neraspoređeni deo ostaje prazan, a preko 100% traka postaje crvena.
 */
function GoalWeightsBar({ goals }: { goals: GoalDraft[] }) {
  const { total } = checkGoalWeights(goals);
  const scale = Math.max(total, TOTAL_GOAL_WEIGHT);
  const over = total > TOTAL_GOAL_WEIGHT;

  return (
    <div
      className={`goal-weights-bar${over ? ' is-over' : ''}`}
      aria-hidden="true"
    >
      {goals.map((goal, idx) =>
        goal.description.trim() && goal.weight ? (
          <span
            key={idx}
            className="goal-weights-bar__segment"
            style={{ width: `${(goal.weight / scale) * 100}%` }}
            title={`${idx + 1}: ${goal.weight}%`}
          />
        ) : null,
      )}
    </div>
  );
}

/** Zbir pondera uz traku: zelen kad je tačno 100%, crven kad nije. */
function GoalWeightsTotal({ goals }: { goals: GoalDraft[] }) {
  const { formatMessage } = useIntl();
  const { total, valid } = checkGoalWeights(goals);

  return (
    <span
      className={`section-progress__count ${valid ? 'is-complete' : 'is-invalid'}`}
      role="status"
    >
      {formatMessage(
        { id: 'evaluation.goalWeightsTotal' },
        { total, required: TOTAL_GOAL_WEIGHT },
      )}
    </span>
  );
}
