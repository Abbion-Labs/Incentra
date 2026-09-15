import { useCallback, useEffect, useState } from 'react';
import { api } from '../../api/client';
import type { EvaluatorAnalytics } from '../../api/types';
import { AppLayout } from '../../components/AppLayout';
import { AlertMessages } from '../../components/common/AlertMessages';
import { LoadingEmpty } from '../../components/common/LoadingEmpty';
import { EvaluatorAnalyticsView } from '../../components/analytics/EvaluatorAnalyticsView';
import { currentYear } from '../../utils/status';
import { useIntl } from '../../i18n';

export function EvaluatorAnalyticsPage() {
  const { formatMessage } = useIntl();
  const [year, setYear] = useState(currentYear);
  const [analytics, setAnalytics] = useState<EvaluatorAnalytics | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const data = await api.get<EvaluatorAnalytics>(`/api/analytics/evaluator?year=${year}`);
      setAnalytics(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : formatMessage({ id: 'errors.analyticsLoadFailed' }));
      setAnalytics(null);
    } finally {
      setLoading(false);
    }
  }, [year]);

  useEffect(() => {
    load();
  }, [load]);

  return (
    <AppLayout title={formatMessage({ id: 'admin.analytics' })}>
      <AlertMessages error={error} />

      {loading || !analytics ? (
        <LoadingEmpty loading={loading} emptyMessage={formatMessage({ id: 'controller.analyticsUnavailable' })} />
      ) : (
        <EvaluatorAnalyticsView analytics={analytics} year={year} onYearChange={setYear} />
      )}
    </AppLayout>
  );
}
