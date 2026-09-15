import type { EvaluationCondition, EvaluationCriterion, EvaluationGoal } from '../../api/types';
import { useIntl } from '../../i18n';
import { formatLocalizedRatingDisplay } from '../../utils/ratingLevelLabels';

interface EvaluationTextListSectionProps {
  title: string;
  items: EvaluationCondition[] | EvaluationCriterion[];
  emptyMessage?: string;
}

export function EvaluationTextListSection({
  title,
  items,
  emptyMessage,
}: EvaluationTextListSectionProps) {
  const { formatMessage } = useIntl();
  return (
    <div className="card">
      <h2>{title}</h2>
      {items.length === 0 ? (
        <p className="empty">{emptyMessage ?? formatMessage({ id: 'evaluation.noItems' })}</p>
      ) : (
        <ul>
          {items.map((item) => (
            <li key={item.id}>{item.description}</li>
          ))}
        </ul>
      )}
    </div>
  );
}

interface EvaluationGoalsTableProps {
  goals: EvaluationGoal[];
  showComments?: boolean;
}

export function EvaluationGoalsTable({ goals, showComments = true }: EvaluationGoalsTableProps) {
  const { formatMessage } = useIntl();
  return (
    <div className="card">
      <h2>{formatMessage({ id: 'evaluation.goalsTitle' })}</h2>
      {goals.length === 0 ? (
        <p className="empty">{formatMessage({ id: 'evaluation.noGoalsSet' })}</p>
      ) : (
        <table className="table">
          <thead>
            <tr>
              <th className="col-text">{formatMessage({ id: 'common.description' })}</th>
              <th className="col-meta">{formatMessage({ id: 'evaluation.rating' })}</th>
              {showComments && <th className="col-text">{formatMessage({ id: 'common.comment' })}</th>}
            </tr>
          </thead>
          <tbody>
            {goals.map((g) => (
              <tr key={g.id}>
                <td className="col-text">{g.description}</td>
                <td className="col-meta">{formatLocalizedRatingDisplay(formatMessage, g.ratingLevelValue, g.ratingLevelLabel)}</td>
                {showComments && <td className="col-text">{g.comment ?? formatMessage({ id: 'common.emptyValue' })}</td>}
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}

interface EvaluationGoalsListProps {
  goals: EvaluationGoal[];
  showRatings: boolean;
  emptyMessage?: string;
}

export function EvaluationGoalsList({
  goals,
  showRatings,
  emptyMessage,
}: EvaluationGoalsListProps) {
  const { formatMessage } = useIntl();
  return (
    <div className="card">
      <h2>{formatMessage({ id: 'evaluation.goalsTitle' })}</h2>
      <ul>
        {goals.map((g) => (
          <li key={g.id}>
            {g.description}
            {showRatings && g.ratingLevelLabel && (
              <span style={{ color: 'var(--muted)' }}>
                {' '}
                — {formatLocalizedRatingDisplay(formatMessage, g.ratingLevelValue, g.ratingLevelLabel)}
              </span>
            )}
          </li>
        ))}
      </ul>
      {goals.length === 0 && (
        <p className="empty">{emptyMessage ?? formatMessage({ id: 'evaluation.goalsNotDefined' })}</p>
      )}
    </div>
  );
}
