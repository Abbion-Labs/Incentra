import { useCallback, useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { api } from '../../api/client';
import type { EvaluatorAnalytics } from '../../api/types';
import { AppLayout } from '../../components/AppLayout';
import { LoadingEmpty } from '../../components/common/LoadingEmpty';
import { EvaluatorAnalyticsView } from '../../components/analytics/EvaluatorAnalyticsView';
import { currentYear } from '../../utils/status';
import { useToast } from '../../hooks';
import { useIntl } from '../../i18n';

export function ControllerEvaluatorAnalyticsPage() {
  const { formatMessage } = useIntl();
  const toast = useToast();
  const { evaluatorEmployeeId } = useParams<{ evaluatorEmployeeId: string }>();
  const [year, setYear] = useState(currentYear);
  const [analytics, setAnalytics] = useState<EvaluatorAnalytics | null>(null);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    if (!evaluatorEmployeeId) return;
    setLoading(true);
    try {
      const data = await api.get<EvaluatorAnalytics>(
        `/api/controller/evaluators/${evaluatorEmployeeId}/analytics?year=${year}`,
      );
      setAnalytics(data);
    } catch (e) {
      toast.error(
        e instanceof Error
          ? e.message
          : formatMessage({ id: 'errors.analyticsLoadFailed' }),
      );
      setAnalytics(null);
    } finally {
      setLoading(false);
    }
  }, [year, evaluatorEmployeeId, formatMessage, toast]);

  useEffect(() => {
    load();
  }, [load]);

  const evaluatorName =
    analytics?.evaluatorFullName ?? formatMessage({ id: 'admin.evaluators' });

  return (
    <AppLayout
      title={`${formatMessage({ id: 'admin.analytics' })} — ${evaluatorName}`}
    >
      {loading || !analytics ? (
        <LoadingEmpty
          loading={loading}
          emptyMessage={formatMessage({
            id: 'controller.analyticsUnavailable',
          })}
        />
      ) : (
        <EvaluatorAnalyticsView
          analytics={analytics}
          year={year}
          onYearChange={setYear}
        />
      )}
    </AppLayout>
  );
}
