import { useId } from 'react';
import { UnsavedChangesIndicator } from '../../../components/evaluation/UnsavedChangesIndicator';
import { useIntl } from '../../../i18n';

export interface PlanningCheck {
  id: string;
  label: string;
  done: boolean;
}

interface GoalsPlanningSidePanelProps {
  checks: PlanningCheck[];
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
 * Bočni panel plana: šta još nedostaje, podaci o razgovoru i postavljanje
 * ciljeva, uz napomenu da se plan posle toga zaključava.
 */
export function GoalsPlanningSidePanel({
  checks,
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
  const lockNoteId = useId();

  return (
    <section className="card goals-planning__panel">
      <div className="goals-planning__panel-header">
        <h2 className="goals-planning__panel-title">
          {formatMessage({ id: 'evaluation.planning.checklistTitle' })}
        </h2>
        <UnsavedChangesIndicator visible={unsaved} />
      </div>

      <ul className="goals-checklist">
        {checks.map((check) => (
          <li
            key={check.id}
            className={`goals-checklist__item ${check.done ? 'is-done' : 'is-missing'}`}
          >
            {check.done ? <CheckIcon /> : <AlertIcon />}
            <span>{check.label}</span>
            <span className="sr-only">
              {formatMessage({
                id: check.done
                  ? 'evaluation.planning.checkDone'
                  : 'evaluation.planning.checkMissing',
              })}
            </span>
          </li>
        ))}
      </ul>

      <div className="goals-planning__conversation">
        <h3 className="goals-planning__subtitle">
          {formatMessage({ id: 'evaluation.conversation' })}
        </h3>
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
      </div>

      <button
        type="button"
        className="btn btn-primary goals-planning__submit"
        onClick={onSave}
        disabled={saving || copying || lookupsLoading || !canSubmit}
        aria-describedby={lockNoteId}
      >
        {saving
          ? formatMessage({ id: 'evaluation.settingGoals' })
          : formatMessage({ id: 'evaluation.setGoals' })}
      </button>
      <p id={lockNoteId} className="goals-planning__lock-note">
        <LockIcon />
        {formatMessage({ id: 'evaluation.planning.lockNote' })}
      </p>
    </section>
  );
}

function CheckIcon() {
  return (
    <svg
      viewBox="0 0 24 24"
      width="16"
      height="16"
      fill="none"
      stroke="currentColor"
      strokeWidth="2.25"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden
    >
      <path d="M20 6 9 17l-5-5" />
    </svg>
  );
}

function AlertIcon() {
  return (
    <svg
      viewBox="0 0 24 24"
      width="16"
      height="16"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden
    >
      <circle cx="12" cy="12" r="9" />
      <path d="M12 8v4M12 16h.01" />
    </svg>
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
