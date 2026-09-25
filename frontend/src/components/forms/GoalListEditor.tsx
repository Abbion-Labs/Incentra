import { FieldHint } from '../common/FieldHint';
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
  const weightHint = formatMessage(
    { id: 'evaluation.goalWeightHint' },
    { min: MIN_GOAL_WEIGHT },
  );

  const update = (index: number, patch: Partial<GoalDraft>) => {
    const next = [...goals];
    next[index] = { ...goals[index], ...patch };
    setGoals(next);
  };

  const resize = (next: GoalDraft[]) =>
    setGoals(customized ? next : withEvenWeights(next));

  return (
    <>
      <div className="form-list">
        {goals.map((goal, idx) => (
          <div key={idx} className="form-list__item form-list__item--weighted">
            <span className="form-list__index">{idx + 1}</span>
            <div className="form-list__field">
              <textarea
                className="form-list__input"
                rows={2}
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
            <div className="form-list__weight">
              <label>
                <span className="sr-only">
                  {formatMessage(
                    { id: 'evaluation.goalWeightLabel' },
                    { index: idx + 1 },
                  )}
                </span>
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
              </label>
              <FieldHint hint={weightHint} />
            </div>
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

/** Zbir pondera uz naslov ciljeva: zelen kad je tačno 100%, crven kad nije. */
export function GoalWeightsTotal({ goals }: { goals: GoalDraft[] }) {
  const { formatMessage } = useIntl();
  const { total, valid } = checkGoalWeights(goals);
  if (!goals.some((goal) => goal.description.trim())) return null;

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
