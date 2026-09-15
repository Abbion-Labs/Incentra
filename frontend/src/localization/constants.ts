export const LOCALE_STORAGE_KEY = 'vn-locale';
export const DEFAULT_LOCALE = 'sr';
export const SUPPORTED_LOCALES = ['sr', 'en'] as const;

export type AppLocale = (typeof SUPPORTED_LOCALES)[number];

export const LOCALE_LABELS: Record<AppLocale, string> = {
  sr: 'SR',
  en: 'EN',
};

export function intlLocale(locale: AppLocale): string {
  return locale === 'sr' ? 'sr-RS' : 'en-US';
}
