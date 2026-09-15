import { useIntl } from '../../i18n';
import { localizeApiError } from '../../utils/errorLocalization';

interface AlertMessagesProps {
  error?: string;
  info?: string;
  message?: string;
}

export function AlertMessages({ error, info, message }: AlertMessagesProps) {
  const { formatMessage } = useIntl();
  const localizedError = error ? localizeApiError(error, formatMessage) : '';

  return (
    <>
      {localizedError && <div className="alert alert-error">{localizedError}</div>}
      {info && <div className="alert alert-info">{info}</div>}
      {message && <div className="alert alert-info">{message}</div>}
    </>
  );
}
