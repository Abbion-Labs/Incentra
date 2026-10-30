import { useIntl } from '../../i18n';

interface ControllerDecisionPanelProps {
  comment: string;
  saving: boolean;
  onCommentChange: (value: string) => void;
  onApprove: () => void;
  onReturnForRevision: () => void;
}

/**
 * Jedan komentar za obe odluke: uz odobrenje je opcion, a pri vraćanju na
 * doradu obavezan, jer ocenjivač iz njega zna šta da ispravi.
 */
export function ControllerDecisionPanel({
  comment,
  saving,
  onCommentChange,
  onApprove,
  onReturnForRevision,
}: ControllerDecisionPanelProps) {
  const { formatMessage } = useIntl();
  return (
    <>
      <div className="form-list controller-decision-fields">
        <div className="form-row">
          <label htmlFor="controller-comment">
            {formatMessage({ id: 'controller.decisionComment' })}
          </label>
          <textarea
            id="controller-comment"
            rows={3}
            value={comment}
            onChange={(e) => onCommentChange(e.target.value)}
            placeholder={formatMessage({
              id: 'controller.decisionCommentPlaceholder',
            })}
            aria-describedby="controller-comment-hint"
          />
          <p id="controller-comment-hint" className="form-hint">
            {formatMessage({ id: 'controller.decisionCommentHint' })}
          </p>
        </div>
      </div>
      <div className="actions">
        <button
          type="button"
          className="btn btn-primary"
          onClick={onApprove}
          disabled={saving}
        >
          {saving
            ? formatMessage({ id: 'buttons.processing' })
            : formatMessage({ id: 'controller.approveEvaluation' })}
        </button>
        <button
          type="button"
          className="btn btn-danger"
          onClick={onReturnForRevision}
          disabled={saving}
        >
          {formatMessage({ id: 'controller.returnForRevision' })}
        </button>
      </div>
    </>
  );
}
