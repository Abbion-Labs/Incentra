import { useIntl } from '../../i18n';

interface LoadingEmptyProps {
  loading: boolean;
  emptyMessage?: string;
}

export function LoadingEmpty({ loading, emptyMessage }: LoadingEmptyProps) {
  const { formatMessage } = useIntl();
  const fallbackEmptyMessage = emptyMessage ?? formatMessage({ id: 'common.notFound' });
  return <div className="empty">{loading ? formatMessage({ id: 'common.loading' }) : fallbackEmptyMessage}</div>;
}
