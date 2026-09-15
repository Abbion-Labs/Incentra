import { DEFAULT_LOCALE, SUPPORTED_LOCALES, type AppLocale } from './constants';

export function getLocale(locale: string | null | undefined): AppLocale {
  if (!locale) {
    return DEFAULT_LOCALE;
  }

  const language = locale.split('-')[0].toLowerCase();
  return (SUPPORTED_LOCALES as readonly string[]).includes(language)
    ? (language as AppLocale)
    : DEFAULT_LOCALE;
}
