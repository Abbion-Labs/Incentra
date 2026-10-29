import type { EvaluationDetail } from '../../../api/types';
import { useIntl } from '../../../i18n';
import { formatGoalWeight } from '../../../utils/goalsPlanning';

export type PreviousPlan =
  | { state: 'loading' }
  | { state: 'failed' }
  | { state: 'ready'; detail: EvaluationDetail | null; hasPlan: boolean };

interface PreviousPlanCardProps {
  quarter: number;
  year: number;
  plan: PreviousPlan;
  copying: boolean;
  disabled: boolean;
  onCopy: () => void;
}

/** Plan iz prethodnog kvartala kao polazna tačka, sa preuzimanjem u formu. */
export function PreviousPlanCard({
  quarter,
  year,
  plan,
  copying,
  disabled,
  onCopy,
}: PreviousPlanCardProps) {
  const { formatMessage } = useIntl();
  const ready = plan.state === 'ready';
  const detail = ready ? plan.detail : null;

  if (ready && !plan.hasPlan) {
    return (
      <section className="card goals-previous-plan">
        <h2 className="goals-planning__panel-title">
          {formatMessage(
            { id: 'evaluation.planning.previousPlanTitle' },
            { quarter, year },
          )}
        </h2>
        <p className="goals-previous-plan__empty">
          {formatMessage({ id: 'evaluation.planning.previousPlanNone' })}
        </p>
      </section>
    );
  }

  const goals = detail?.goals.filter((goal) => goal.description.trim()) ?? [];
  const count = (items: { description: string }[] = []) =>
    items.filter((item) => item.description.trim()).length;

  return (
    <section className="card goals-previous-plan">
      <h2 className="goals-planning__panel-title">
        {formatMessage(
          { id: 'evaluation.planning.previousPlanTitle' },
          { quarter, year },
        )}
      </h2>
      {plan.state === 'loading' && (
        <p className="goals-previous-plan__empty">
          {formatMessage({ id: 'common.loading' })}
        </p>
      )}
      {detail && (
        <>
          <ol className="goals-previous-plan__goals">
            {goals.map((goal) => (
              <li key={goal.id}>
                <span className="goals-previous-plan__goal">
                  {goal.description}
                </span>
                {goal.weight != null && (
                  <span className="goals-previous-plan__weight">
                    {formatGoalWeight(goal.weight)}
                  </span>
                )}
              </li>
            ))}
          </ol>
          <p className="goals-previous-plan__meta">
            {formatMessage(
              { id: 'evaluation.planning.previousPlanMeta' },
              {
                conditions: count(detail.conditions),
                criteria: count(detail.criteria),
              },
            )}
          </p>
        </>
      )}
      <button
        type="button"
        className="btn btn-secondary btn-sm goals-previous-plan__copy"
        onClick={onCopy}
        disabled={copying || disabled}
      >
        {copying
          ? formatMessage({ id: 'common.loading' })
          : formatMessage({ id: 'evaluation.planning.copyPreviousPlan' })}
      </button>
    </section>
  );
}
