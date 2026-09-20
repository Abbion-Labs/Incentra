import { useCallback, useEffect, useState } from "react";
import { api } from "../../api/client";
import type { EvaluatorAnalytics } from "../../api/types";
import { AppLayout } from "../../components/AppLayout";
import { LoadingEmpty } from "../../components/common/LoadingEmpty";
import { EvaluatorAnalyticsView } from "../../components/analytics/EvaluatorAnalyticsView";
import { useToast } from "../../hooks";
import { currentYear } from "../../utils/status";
import { useIntl } from "../../i18n";

export function EvaluatorAnalyticsPage() {
  const { formatMessage } = useIntl();
  const toast = useToast();
  const [year, setYear] = useState(currentYear);
  const [analytics, setAnalytics] = useState<EvaluatorAnalytics | null>(null);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const data = await api.get<EvaluatorAnalytics>(
        `/api/analytics/evaluator?year=${year}`,
      );
      setAnalytics(data);
    } catch (e) {
      toast.error(
        e instanceof Error
          ? e.message
          : formatMessage({ id: "errors.analyticsLoadFailed" }),
      );
      setAnalytics(null);
    } finally {
      setLoading(false);
    }
  }, [year, formatMessage, toast]);

  useEffect(() => {
    load();
  }, [load]);

  return (
    <AppLayout title={formatMessage({ id: "admin.analytics" })}>
      {loading || !analytics ? (
        <LoadingEmpty
          loading={loading}
          emptyMessage={formatMessage({
            id: "controller.analyticsUnavailable",
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
