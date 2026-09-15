import { useIntl } from '../i18n';
import { LOCALE_LABELS, SUPPORTED_LOCALES, useLocale } from '../localization';

export function LocaleSwitcher() {
  const { locale, setLocale } = useLocale();
  const { formatMessage } = useIntl();

  return (
    <div className="locale-switch" role="group" aria-label={formatMessage({ id: 'common.language' })}>
      {SUPPORTED_LOCALES.map((value) => (
        <button
          key={value}
          type="button"
          className={`locale-switch__btn${locale === value ? ' is-active' : ''}`}
          onClick={() => setLocale(value)}
          aria-pressed={locale === value}
        >
          {LOCALE_LABELS[value]}
        </button>
      ))}
    </div>
  );
}
