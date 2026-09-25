import { useIntl } from '../../i18n';

interface SectionProgressProps {
  rated: number;
  total: number;
  /** Prosek sekcije kako se prikazuje („/“ dok nije sve ocenjeno). */
  average: string;
  /** Brojač se prikazuje samo dok se ocena unosi. */
  showCount?: boolean;
}

/** Uz naslov sekcije: koliko je stavki ocenjeno i prosek sekcije. */
export function SectionProgress({
  rated,
  total,
  average,
  showCount = true,
}: SectionProgressProps) {
  const { formatMessage } = useIntl();
  const complete = total > 0 && rated === total;

  return (
    <div className="section-progress">
      {showCount && total > 0 && (
        <span
          className={`section-progress__count${complete ? ' is-complete' : ''}`}
        >
          {formatMessage({ id: 'evaluation.ratedProgress' }, { rated, total })}
        </span>
      )}
      <span className="section-progress__average">
        <span className="section-progress__label">
          {formatMessage({ id: 'evaluation.sectionAverage' })}
        </span>
        <strong>{average}</strong>
      </span>
    </div>
  );
}
