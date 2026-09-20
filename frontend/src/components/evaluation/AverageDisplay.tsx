import { useIntl } from '../../i18n';

interface AverageDisplayProps {
  value: string;
}

export function AverageDisplay({ value }: AverageDisplayProps) {
  const { formatMessage } = useIntl();

  if (value === '/') {
    return (
      <span
        className="average-incomplete"
        title={formatMessage({ id: 'evaluation.incompleteRating' })}
      >
        /
      </span>
    );
  }

  if (value === '—') {
    return <span className="average-muted">—</span>;
  }

  return <span className="average-value">{value}</span>;
}
