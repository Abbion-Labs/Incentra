import { useId } from 'react';
import { UnsavedChangesIndicator } from '../../../components/evaluation/UnsavedChangesIndicator';
import { useIntl } from '../../../i18n';

interface GoalsPlanningSidePanelProps {
  conversationAt: string;
  evaluatorComment: string;
  unsaved: boolean;
  saving: boolean;
  /** Kopiranje iz prethodnog kvartala još menja ciljeve, pa se čuvanje čeka. */
  copying: boolean;
  lookupsLoading: boolean;
  canSubmit: boolean;
  onConversationAtChange: (value: string) => void;
  onEvaluatorCommentChange: (value: string) => void;
  onSave: () => void;
}

/**
 * Bočni panel plana: podaci o razgovoru i postavljanje ciljeva, uz napomenu
 * da se plan posle toga zaključava.
 */
export function GoalsPlanningSidePanel({
  conversationAt,
  evaluatorComment,
  unsaved,
  saving,
  copying,
  lookupsLoading,
  canSubmit,
  onConversationAtChange,
  onEvaluatorCommentChange,
  onSave,
}: GoalsPlanningSidePanelProps) {
  const { formatMessage } = useIntl();
  const dateId = useId();
  const noteId = useId();
  const hintId = useId();
  const lockNoteId = useId();

  return (
    <section className="card goals-planning__panel">
      <div className="goals-planning__panel-header">
        <h2 className="goals-planning__panel-title">
          {formatMessage({ id: 'evaluation.conversation' })}
        </h2>
        <UnsavedChangesIndicator visible={unsaved} />
      </div>

      <div className="form-row">
        <label htmlFor={dateId}>
          {formatMessage({ id: 'evaluation.conversationDate' })}
        </label>
        <input
          id={dateId}
          type="datetime-local"
          value={conversationAt}
          onChange={(e) => onConversationAtChange(e.target.value)}
        />
      </div>
      <div className="form-row">
        <label htmlFor={noteId}>
          {formatMessage({ id: 'evaluation.conversationNote' })}
        </label>
        <textarea
          id={noteId}
          rows={3}
          value={evaluatorComment}
          onChange={(e) => onEvaluatorCommentChange(e.target.value)}
        />
      </div>

      <div className="goals-planning__actions">
        {/* Dok plan nije kompletan, dugme je onemogućeno i kaže se zašto. */}
        {!canSubmit && (
          <p id={hintId} className="goals-planning__hint">
            {formatMessage({ id: 'evaluation.planning.incompleteHint' })}
          </p>
        )}
        <button
          type="button"
          className="btn btn-primary goals-planning__submit"
          onClick={onSave}
          disabled={saving || copying || lookupsLoading || !canSubmit}
          aria-describedby={canSubmit ? lockNoteId : `${hintId} ${lockNoteId}`}
        >
          {saving
            ? formatMessage({ id: 'evaluation.settingGoals' })
            : formatMessage({ id: 'evaluation.setGoals' })}
        </button>
        <p id={lockNoteId} className="goals-planning__lock-note">
          <LockIcon />
          {formatMessage({ id: 'evaluation.planning.lockNote' })}
        </p>
      </div>
    </section>
  );
}

function LockIcon() {
  return (
    <svg
      viewBox="0 0 24 24"
      width="14"
      height="14"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden
    >
      <rect x="5" y="11" width="14" height="10" rx="2" />
      <path d="M8 11V7a4 4 0 0 1 8 0v4" />
    </svg>
  );
}
