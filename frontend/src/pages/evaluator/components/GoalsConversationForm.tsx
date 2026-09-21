import { useIntl } from '../../../i18n';

interface GoalsConversationFormProps {
  conversationAt: string;
  evaluatorComment: string;
  saving: boolean;
  /** Kopiranje iz prethodnog kvartala još menja ciljeve, pa se čuvanje čeka. */
  copying: boolean;
  lookupsLoading: boolean;
  canSubmit: boolean;
  onConversationAtChange: (value: string) => void;
  onEvaluatorCommentChange: (value: string) => void;
  onSave: () => void;
}

export function GoalsConversationForm({
  conversationAt,
  evaluatorComment,
  saving,
  copying,
  lookupsLoading,
  canSubmit,
  onConversationAtChange,
  onEvaluatorCommentChange,
  onSave,
}: GoalsConversationFormProps) {
  const { formatMessage } = useIntl();

  return (
    <div className="card goals-conversation-form">
      <div className="form-grid form-grid--2">
        <div className="form-row">
          <label>{formatMessage({ id: 'evaluation.conversationNote' })}</label>
          <textarea
            rows={3}
            value={evaluatorComment}
            onChange={(e) => onEvaluatorCommentChange(e.target.value)}
          />
        </div>
        <div className="form-row">
          <label>{formatMessage({ id: 'evaluation.conversationDate' })}</label>
          <input
            type="datetime-local"
            value={conversationAt}
            onChange={(e) => onConversationAtChange(e.target.value)}
          />
        </div>
      </div>
      <div className="form-section__footer">
        <button
          type="button"
          className="btn btn-primary"
          onClick={onSave}
          disabled={saving || copying || lookupsLoading || !canSubmit}
        >
          {saving
            ? formatMessage({ id: 'evaluation.settingGoals' })
            : formatMessage({ id: 'evaluation.setGoals' })}
        </button>
      </div>
    </div>
  );
}
