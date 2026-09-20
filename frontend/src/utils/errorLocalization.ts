import type { IntlFormatters } from 'react-intl';

type FormatMessage = IntlFormatters['formatMessage'];

const ERROR_PREFIX = 'errors.';

export function getErrorCode(message: string): string | null {
  if (!message) return null;
  const [rawCode] = message.split('?', 1);
  if (/^vn-\d{4}$/.test(rawCode)) {
    return rawCode;
  }
  return null;
}

export function getErrorArgs(message: string): Record<string, string> {
  const args: Record<string, string> = {};
  const [, query] = message.split('?', 2);
  if (!query) return args;

  const params = new URLSearchParams(query);
  params.forEach((value, key) => {
    args[key] = value;
  });

  return args;
}

export function localizeApiError(
  message: string,
  formatMessage: FormatMessage,
): string {
  const code = getErrorCode(message);
  if (!code) return message;

  return formatMessage(
    { id: `${ERROR_PREFIX}${code}`, defaultMessage: message },
    getErrorArgs(message),
  );
}
