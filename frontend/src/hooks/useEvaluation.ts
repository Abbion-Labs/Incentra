import { useCallback, useEffect, useState } from 'react';
import { api } from '../api/client';
import type { EvaluationDetail } from '../api/types';
import { useIntl } from '../i18n';

export function useEvaluation(id: string | undefined) {
  const { formatMessage } = useIntl();
  const [evaluation, setEvaluation] = useState<EvaluationDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const reload = useCallback(async () => {
    if (!id) return;
    setLoading(true);
    setError('');
    try {
      const data = await api.get<EvaluationDetail>(`/api/evaluations/${id}`);
      setEvaluation(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : formatMessage({ id: 'errors.loadFailed' }));
      setEvaluation(null);
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    reload();
  }, [reload]);

  return { evaluation, setEvaluation, loading, error, setError, reload };
}
