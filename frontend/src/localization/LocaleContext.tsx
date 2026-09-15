import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { DEFAULT_LOCALE, LOCALE_STORAGE_KEY, type AppLocale } from './constants';
import { getLocale } from './getLocale';

interface LocaleContextValue {
  locale: AppLocale;
  setLocale: (locale: AppLocale) => void;
}

const LocaleContext = createContext<LocaleContextValue | null>(null);

function readStoredLocale(): AppLocale {
  try {
    return getLocale(localStorage.getItem(LOCALE_STORAGE_KEY));
  } catch {
    return DEFAULT_LOCALE;
  }
}

export function LocaleProvider({ children }: { children: ReactNode }) {
  const [locale, setLocaleState] = useState<AppLocale>(readStoredLocale);

  const setLocale = useCallback((next: AppLocale) => {
    setLocaleState(getLocale(next));
    try {
      localStorage.setItem(LOCALE_STORAGE_KEY, getLocale(next));
    } catch {
      // ignore storage errors
    }
  }, []);

  useEffect(() => {
    document.documentElement.lang = locale === 'sr' ? 'sr' : 'en';
  }, [locale]);

  const value = useMemo(() => ({ locale, setLocale }), [locale, setLocale]);

  return <LocaleContext.Provider value={value}>{children}</LocaleContext.Provider>;
}

export function useLocale(): LocaleContextValue {
  const ctx = useContext(LocaleContext);
  if (!ctx) {
    throw new Error('useLocale must be used within LocaleProvider');
  }
  return ctx;
}
