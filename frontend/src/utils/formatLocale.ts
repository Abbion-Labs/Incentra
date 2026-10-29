import { intlLocale } from '../localization';

function resolveIntlLocale(): string {
  try {
    return intlLocale(localStorage.getItem('vn-locale') === 'en' ? 'en' : 'sr');
  } catch {
    return intlLocale('sr');
  }
}

// Napraviti formatter je stotinak puta skuplje od formatiranja jednog broja, a tabele plata i rezultata
// formatiraju stotine iznosa po renderu. Zato se pravi jednom po jeziku i podešavanjima.
const numberFormats = new Map<string, Intl.NumberFormat>();
const dateFormats = new Map<string, Intl.DateTimeFormat>();

function numberFormat(
  key: string,
  options: Intl.NumberFormatOptions,
): Intl.NumberFormat {
  const locale = resolveIntlLocale();
  const cacheKey = `${locale}|${key}`;
  let format = numberFormats.get(cacheKey);
  if (!format) {
    format = new Intl.NumberFormat(locale, options);
    numberFormats.set(cacheKey, format);
  }
  return format;
}

function dateFormat(
  key: string,
  options: Intl.DateTimeFormatOptions,
): Intl.DateTimeFormat {
  const locale = resolveIntlLocale();
  const cacheKey = `${locale}|${key}`;
  let format = dateFormats.get(cacheKey);
  if (!format) {
    format = new Intl.DateTimeFormat(locale, options);
    dateFormats.set(cacheKey, format);
  }
  return format;
}

export function formatAmount(
  value: number,
  currency = 'RSD',
  maximumFractionDigits = 2,
): string {
  return numberFormat(`amount|${currency}|${maximumFractionDigits}`, {
    style: 'currency',
    currency,
    minimumFractionDigits: 0,
    maximumFractionDigits,
  }).format(value);
}

export function formatPercent(
  value: number,
  maximumFractionDigits = 2,
): string {
  return numberFormat(`percent|${maximumFractionDigits}`, {
    style: 'percent',
    maximumFractionDigits,
  }).format(value);
}

export function formatNumber(
  value: number,
  maximumFractionDigits = 0,
  minimumFractionDigits = 0,
): string {
  return numberFormat(
    `number|${maximumFractionDigits}|${minimumFractionDigits}`,
    {
      minimumFractionDigits,
      maximumFractionDigits,
    },
  ).format(value);
}

export function formatDateTime(value: string | Date): string {
  return dateFormat('date-time', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value));
}

export function formatDate(value: string | Date): string {
  return dateFormat('date', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).format(new Date(value));
}
