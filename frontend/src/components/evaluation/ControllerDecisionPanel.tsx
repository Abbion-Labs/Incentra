import { useIntl } from '../../i18n';

interface ControllerDecisionPanelProps {
  controllerComment: string;
  revisionComment: string;
  saving: boolean;
  onControllerCommentChange: (value: string) => void;
  onRevisionCommentChange: (value: string) => void;
  onApprove: () => void;
  onReturnForRevision: () => void;
}

export function ControllerDecisionPanel({
  controllerComment,
  revisionComment,
  saving,
  onControllerCommentChange,
  onRevisionCommentChange,
  onApprove,
  onReturnForRevision,
}: ControllerDecisionPanelProps) {
  const { formatMessage } = useIntl();
  return (
    <>
      <div className="form-list controller-decision-fields">
        <div className="form-row">
          <label htmlFor="controller-comment">
            {formatMessage({ id: 'controller.commentOptional' })}
          </label>
          <textarea
            id="controller-comment"
            rows={3}
            value={controllerComment}
            onChange={(e) => onControllerCommentChange(e.target.value)}
            disabled={saving}
          />
        </div>
        <div className="form-row">
          <label htmlFor="revision-comment">
            {formatMessage({ id: 'controller.revisionCommentRequired' })}
          </label>
          <textarea
            id="revision-comment"
            rows={3}
            value={revisionComment}
            onChange={(e) => onRevisionCommentChange(e.target.value)}
            disabled={saving}
            placeholder={formatMessage({
              id: 'controller.revisionCommentPlaceholder',
            })}
          />
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
