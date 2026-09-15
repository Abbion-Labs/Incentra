import { useCallback, useEffect, useState } from 'react';
import { useLocation, useParams } from 'react-router-dom';
import { api } from '../../api/client';
import type { EvaluatorAnalytics } from '../../api/types';
import { AppLayout } from '../../components/AppLayout';
import { AlertMessages } from '../../components/common/AlertMessages';
import { LoadingEmpty } from '../../components/common/LoadingEmpty';
import { PageBackLink } from '../../components/common/PageBackLink';
import { EvaluatorAnalyticsView } from '../../components/analytics/EvaluatorAnalyticsView';
import { currentYear } from '../../utils/status';
import type { PageBackState } from '../admin/adminNavigation';
import { useIntl } from '../../i18n';

export function ControllerEvaluatorAnalyticsPage() {
  const { formatMessage } = useIntl();
  const { evaluatorEmployeeId } = useParams<{ evaluatorEmployeeId: string }>();
  const location = useLocation();
  const backState = location.state as PageBackState | null;
  const [year, setYear] = useState(currentYear);
  const [analytics, setAnalytics] = useState<EvaluatorAnalytics | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    if (!evaluatorEmployeeId) return;
    setLoading(true);
    setError('');
    try {
      const data = await api.get<EvaluatorAnalytics>(
        `/api/analytics/evaluator?year=${year}&evaluatorEmployeeId=${evaluatorEmployeeId}`,
      );
      setAnalytics(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : formatMessage({ id: 'errors.analyticsLoadFailed' }));
      setAnalytics(null);
    } finally {
      setLoading(false);
    }
  }, [year, evaluatorEmployeeId]);

  useEffect(() => {
    load();
  }, [load]);

  const evaluatorName = analytics?.evaluatorFullName ?? formatMessage({ id: 'admin.evaluators' });

  const backTo = backState?.backTo ?? '/controller/evaluators';
  const backLabel = backState?.backLabelKey
    ? formatMessage({ id: backState.backLabelKey as never })
    : formatMessage({ id: 'controller.backToEvaluators' });

  return (
    <AppLayout title={`${formatMessage({ id: 'admin.analytics' })} — ${evaluatorName}`}>
      <PageBackLink to={backTo} label={backLabel} />
      <AlertMessages error={error} />

      {loading || !analytics ? (
        <LoadingEmpty loading={loading} emptyMessage={formatMessage({ id: 'controller.analyticsUnavailable' })} />
      ) : (
        <EvaluatorAnalyticsView
          analytics={analytics}
          year={year}
          onYearChange={setYear}
          title={`${formatMessage({ id: 'admin.analytics' })} — ${analytics.evaluatorFullName}`}
          hint={formatMessage({ id: 'controller.analyticsHint' })}
        />
      )}
    </AppLayout>
  );
}
