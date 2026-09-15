import type { ReactNode } from 'react';
import { IntlProvider } from 'react-intl';
import { flattenMessages, translationsLocale } from '../i18n';
import { intlLocale, useLocale } from '../localization';

export function AppIntlProvider({ children }: { children: ReactNode }) {
  const { locale } = useLocale();

  return (
    <IntlProvider
      locale={intlLocale(locale)}
      messages={flattenMessages(translationsLocale(locale))}
      defaultLocale="sr-RS"
    >
      {children}
    </IntlProvider>
  );
}
