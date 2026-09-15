import enErrors from './errors/en.json';
import srErrors from './errors/sr.json';
import enMessages from './messages/en.json';
import srMessages from './messages/sr.json';
import type { IntlMessage } from './i18n.types';
import type { AppLocale } from '../localization';

function concatJsonLocalizations(localizations: IntlMessage[]): IntlMessage {
  return localizations.reduce(
    (result, json) => ({ ...result, ...json }),
    {} as IntlMessage,
  );
}

export function translationsLocale(locale: AppLocale): IntlMessage {
  if (locale === 'en') {
    return concatJsonLocalizations([enMessages, enErrors] as IntlMessage[]);
  }

  return concatJsonLocalizations([srMessages, srErrors] as IntlMessage[]);
}
