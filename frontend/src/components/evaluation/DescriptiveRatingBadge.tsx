import { useIntl } from '../../i18n';
import {
  descriptiveRatingClass,
  formatDescriptiveRatingLabel,
  resolveDescriptiveRatingCode,
} from '../../utils/descriptiveRating';

interface DescriptiveRatingBadgeProps {
  name: string | null | undefined;
  code?: string | null;
  descriptiveRatingId?: number | null;
  className?: string;
  emptyLabel?: string;
}

export function DescriptiveRatingBadge({
  name,
  code,
  descriptiveRatingId,
  className,
  emptyLabel = '—',
}: DescriptiveRatingBadgeProps) {
  const { formatMessage } = useIntl();
  const label = formatDescriptiveRatingLabel(formatMessage, {
    code,
    name,
    descriptiveRatingId,
  });
  const styleCode = resolveDescriptiveRatingCode({
    code,
    name,
    descriptiveRatingId,
  });

  if (!label) {
    return (
      <span className={`average-muted ${className ?? ''}`.trim()}>
        {emptyLabel}
      </span>
    );
  }

  return (
    <span
      className={`${descriptiveRatingClass(styleCode ?? name)} ${className ?? ''}`.trim()}
    >
      {label}
    </span>
  );
}
