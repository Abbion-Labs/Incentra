import type { RatingLevel } from '../../api/types';
import { useIntl } from '../../i18n';
import { formatRatingLevelLabel } from '../../utils/ratingLevelLabels';
import { findNotRatedLevelId, isNotRated } from '../../utils/scoring';

interface RatingScaleProps {
  ratingLevels: RatingLevel[];
  value: number;
  /** Bez njega skala samo prikazuje izabranu ocenu. */
  onChange?: (ratingLevelId: number) => void;
  /** Naziv stavke koja se ocenjuje, za čitače ekrana. */
  label: string;
}

/**
 * Ocena jednim klikom: dugmići 1–5 umesto padajuće liste. Ponovni klik na
 * izabranu ocenu je poništava (vraća na „/“).
 */
export function RatingScale({
  ratingLevels,
  value,
  onChange,
  label,
}: RatingScaleProps) {
  const { formatMessage } = useIntl();
  const levels = ratingLevels
    .filter((level) => !isNotRated(level))
    .sort((a, b) => a.value - b.value);
  const notRatedId = findNotRatedLevelId(ratingLevels);
  const readOnly = !onChange;

  return (
    <div
      className={`rating-scale${readOnly ? ' rating-scale--readonly' : ''}`}
      role="radiogroup"
      aria-label={label}
    >
      {levels.map((level) => {
        const selected = level.id === value;
        const title = formatRatingLevelLabel(formatMessage, {
          level,
          value: level.value,
          label: level.label,
        });
        return (
          <button
            key={level.id}
            type="button"
            role="radio"
            aria-checked={selected}
            aria-label={`${level.value} — ${title}`}
            title={title}
            className={`rating-scale__option${selected ? ' is-selected' : ''}`}
            disabled={readOnly}
            onClick={() => {
              if (!onChange) return;
              if (selected && notRatedId != null) onChange(notRatedId);
              else onChange(level.id);
            }}
          >
            {level.value}
          </button>
        );
      })}
    </div>
  );
}
