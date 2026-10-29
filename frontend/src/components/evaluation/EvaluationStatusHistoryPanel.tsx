import type { EvaluationStatusHistoryEntry } from '../../api/types';
import { useIntl } from '../../i18n';
import { roleLabel, statusLabel } from '../../utils/status';
import { formatDateTime } from '../../utils/formatLocale';

interface EvaluationStatusHistoryPanelProps {
  items: EvaluationStatusHistoryEntry[];
  loading: boolean;
  error: string;
}

export function EvaluationStatusHistoryPanel({
  items,
  loading,
  error,
}: EvaluationStatusHistoryPanelProps) {
  const { formatMessage } = useIntl();

  if (loading) {
    return (
      <p className="empty-inline">{formatMessage({ id: 'common.loading' })}</p>
    );
  }

  if (error) {
    return <p className="empty-inline">{error}</p>;
  }

  if (items.length === 0) {
    return (
      <p className="empty-inline">
        {formatMessage({ id: 'evaluation.statusHistoryEmpty' })}
      </p>
    );
  }

  return (
    <ol className="evaluation-status-history__list">
      {items.map((entry) => (
        <li key={entry.id} className="evaluation-status-history__item">
          <div className="evaluation-status-history__meta">
            <time dateTime={entry.changedAt}>
              {formatDateTime(entry.changedAt)}
            </time>
            {entry.changedByRoleCode && (
              <span className="evaluation-status-history__role">
                {roleLabel(entry.changedByRoleCode, formatMessage)}
              </span>
            )}
          </div>
          <p className="evaluation-status-history__transition">
            {entry.fromStatus
              ? formatMessage(
                  { id: 'evaluation.statusHistoryTransition' },
                  {
                    from: statusLabel(entry.fromStatus, formatMessage),
                    to: statusLabel(entry.toStatus, formatMessage),
                  },
                )
              : formatMessage(
                  { id: 'evaluation.statusHistoryInitial' },
                  { status: statusLabel(entry.toStatus, formatMessage) },
                )}
          </p>
          {entry.comment?.trim() && (
            <p className="evaluation-status-history__comment">
              {entry.comment}
            </p>
          )}
        </li>
      ))}
    </ol>
  );
}
