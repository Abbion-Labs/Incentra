import { useIntl } from '../../i18n';

interface ReadOnlyTextProps {
  id: string;
  label: string;
  value: string;
}

/**
 * Tekst koji se ne menja prikazuje se kao tekst, ne kao onemogućeno polje:
 * prazan sivi okvir izgleda kao da nešto nedostaje ili se ne učitava.
 */
export function ReadOnlyText({ id, label, value }: ReadOnlyTextProps) {
  const { formatMessage } = useIntl();
  const text = value.trim();

  return (
    <div className="form-row">
      <span id={`${id}-label`} className="form-row__label">
        {label}
      </span>
      <p
        id={id}
        className={`readonly-text${text ? '' : ' readonly-text--empty'}`}
        aria-labelledby={`${id}-label`}
      >
        {text || formatMessage({ id: 'common.emptyValue' })}
      </p>
    </div>
  );
}
