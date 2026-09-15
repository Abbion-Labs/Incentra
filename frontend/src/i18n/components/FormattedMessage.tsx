import type { ReactNode } from 'react';
import { FormattedMessage as ReactIntlFormattedMessage } from 'react-intl';
import type { Props as ReactIntlFormattedMessageProps } from 'react-intl/src/components/message';
import type { IntlMessage, NestedKeys } from '../i18n.types';

type FormattedMessageProps = ReactIntlFormattedMessageProps<Record<string, ReactNode>> & {
  id?: NestedKeys<IntlMessage>;
};

export function FormattedMessage({ id, ...rest }: FormattedMessageProps) {
  return <ReactIntlFormattedMessage id={id} {...rest} />;
}
