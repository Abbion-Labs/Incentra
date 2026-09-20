import { useIntl } from '../../i18n';

interface UnsavedChangesIndicatorProps {
  visible: boolean;
}

export function UnsavedChangesIndicator({
  visible,
}: UnsavedChangesIndicatorProps) {
  const { formatMessage } = useIntl();

  if (!visible) return null;

  return (
    <p className="evaluation-unsaved-status" role="status">
      {formatMessage({ id: 'evaluation.unsavedChanges' })}
    </p>
  );
}
