import { useIntl } from '../../i18n';

interface StickyEditorActionsProps {
  submitAllowed: boolean;
  saving: boolean;
  onSubmit: () => void;
  onCancel: () => void;
}

export function StickyEditorActions({
  submitAllowed,
  saving,
  onSubmit,
  onCancel,
}: StickyEditorActionsProps) {
  const { formatMessage } = useIntl();
  return (
    <div className="sticky-action-bar">
      <div className="sticky-action-bar__inner">
        <div className="sticky-action-bar__hint">
          {!submitAllowed ? (
            <span className="sticky-action-bar__warning">
              {formatMessage({ id: 'evaluation.submitRequirements' })}
            </span>
          ) : (
            <span className="sticky-action-bar__ok">{formatMessage({ id: 'evaluation.readyToSubmit' })}</span>
          )}
        </div>
        <div className="sticky-action-bar__actions">
          <button type="button" className="btn btn-secondary" onClick={onCancel} disabled={saving}>
            {formatMessage({ id: 'buttons.back' })}
          </button>
          <button
            type="button"
            className="btn btn-primary"
            onClick={onSubmit}
            disabled={saving || !submitAllowed}
            title={!submitAllowed ? formatMessage({ id: 'evaluation.completeRatingsRequired' }) : undefined}
          >
            {saving ? formatMessage({ id: 'buttons.submitting' }) : formatMessage({ id: 'buttons.submitToController' })}
          </button>
        </div>
      </div>
    </div>
  );
}
