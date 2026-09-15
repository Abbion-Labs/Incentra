import { intlLocale } from '../localization';

function resolveIntlLocale(): string {
  try {
    return intlLocale(localStorage.getItem('vn-locale') === 'en' ? 'en' : 'sr');
  } catch {
    return intlLocale('sr');
  }
}

export function formatAmount(value: number, currency = 'RSD', maximumFractionDigits = 2): string {
  return new Intl.NumberFormat(resolveIntlLocale(), {
    style: 'currency',
    currency,
    minimumFractionDigits: 0,
    maximumFractionDigits,
  }).format(value);
}

export function formatPercent(value: number, maximumFractionDigits = 2): string {
  return new Intl.NumberFormat(resolveIntlLocale(), {
    style: 'percent',
    maximumFractionDigits,
  }).format(value);
}

export function formatNumber(value: number, maximumFractionDigits = 0, minimumFractionDigits = 0): string {
  return new Intl.NumberFormat(resolveIntlLocale(), {
    minimumFractionDigits,
    maximumFractionDigits,
  }).format(value);
}

export function formatDateTime(value: string | Date): string {
  return new Intl.DateTimeFormat(resolveIntlLocale(), {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value));
}

export function formatDate(value: string | Date): string {
  return new Intl.DateTimeFormat(resolveIntlLocale(), {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).format(new Date(value));
}
