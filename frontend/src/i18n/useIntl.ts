import { useCallback } from 'react';
import type { IntlFormatters, PrimitiveType } from 'react-intl';
import { useIntl as useReactIntl } from 'react-intl';
import type { IntlMessage, NestedKeys } from './i18n.types';

type FormatMessageArgs = Parameters<IntlFormatters['formatMessage']>;
type MessageValues = Record<string, PrimitiveType>;

export function useIntl() {
  const { formatMessage, ...rest } = useReactIntl();

  const typedFormatMessage = useCallback(
    (
      descriptor: FormatMessageArgs[0] & {
        id?: NestedKeys<IntlMessage>;
      },
      values?: MessageValues,
      options?: FormatMessageArgs[2],
    ) => formatMessage(descriptor, values, options) as string,
    [formatMessage],
  );

  return {
    ...rest,
    formatMessage: typedFormatMessage,
  };
}
