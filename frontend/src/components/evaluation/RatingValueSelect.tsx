import type { RatingLevel } from '../../api/types';
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
  return (
    <select
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
