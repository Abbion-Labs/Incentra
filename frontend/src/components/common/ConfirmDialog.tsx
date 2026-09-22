import { useIntl } from '../../i18n';

interface ConfirmDialogProps {
  open: boolean;
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  /** Dok akcija traje, dugmad su blokirana da bi se sprečio dupli klik. */
  busy?: boolean;
  busyLabel?: string;
  onConfirm: () => void;
  onCancel: () => void;
}

export function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel,
  cancelLabel,
  busy = false,
  busyLabel,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  const { formatMessage } = useIntl();
  if (!open) return null;

  return (
    <div
      className="dialog-backdrop"
      role="presentation"
      onClick={busy ? undefined : onCancel}
    >
      <div
        className="dialog"
        role="dialog"
        aria-modal="true"
        onClick={(e) => e.stopPropagation()}
      >
        <h3 className="dialog__title">{title}</h3>
        <p className="dialog__message">{message}</p>
        <div className="dialog__actions">
          <button
            type="button"
            className="btn btn-secondary"
            onClick={onCancel}
            disabled={busy}
          >
            {cancelLabel ?? formatMessage({ id: 'buttons.cancel' })}
          </button>
          <button
            type="button"
            className="btn btn-primary"
            onClick={onConfirm}
            disabled={busy}
          >
            {busy && busyLabel
              ? busyLabel
              : (confirmLabel ?? formatMessage({ id: 'buttons.confirm' }))}
          </button>
        </div>
      </div>
    </div>
  );
}
