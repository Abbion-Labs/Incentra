import type { RatingLevel } from '../../api/types';
import { useIntl } from '../../i18n';
import { ratingValueOptionLabel } from '../../utils/scoring';

interface RatingValueSelectProps {
  ratingLevels: RatingLevel[];
  value: number;
  onChange: (ratingLevelId: number) => void;
}

export function RatingValueSelect({
  ratingLevels,
  value,
  onChange,
}: RatingValueSelectProps) {
  const { formatMessage } = useIntl();
  return (
    <select
      aria-label={formatMessage({ id: 'evaluation.rating' })}
      className="select-inline select-inline--rating-value"
      value={value}
      onChange={(e) => onChange(Number(e.target.value))}
    >
      {ratingLevels.map((level) => (
        <option key={level.id} value={level.id}>
          {ratingValueOptionLabel(level)}
        </option>
      ))}
    </select>
  );
}
