import { useIntl } from '../../i18n';

export type AutosaveStatus = 'idle' | 'dirty' | 'saving' | 'saved';

interface EvaluationAutosaveIndicatorProps {
  status: AutosaveStatus;
}

export function EvaluationAutosaveIndicator({ status }: EvaluationAutosaveIndicatorProps) {
  const { formatMessage } = useIntl();

  if (status === 'idle') return null;

  const messageId =
    status === 'dirty'
      ? 'evaluation.unsavedChanges'
      : status === 'saving'
        ? 'evaluation.autosaving'
        : 'evaluation.autosaved';

  return (
    <p className={`evaluation-autosave-status evaluation-autosave-status--${status}`} role="status">
      {formatMessage({ id: messageId })}
    </p>
  );
}
