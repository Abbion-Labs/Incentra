import { useEffect, useState } from 'react';
import { api } from '../api/client';
import type { EvaluationStatusHistoryEntry } from '../api/types';

export function useEvaluationStatusHistory(evaluationId: number | undefined) {
  const [items, setItems] = useState<EvaluationStatusHistoryEntry[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!evaluationId) {
      setItems([]);
      return;
    }

    let cancelled = false;
    setLoading(true);
    setError('');

    api.get<EvaluationStatusHistoryEntry[]>(`/api/evaluations/${evaluationId}/status-history`)
      .then((data) => {
        if (!cancelled) setItems(data);
      })
      .catch((e) => {
        if (!cancelled) {
          setError(e instanceof Error ? e.message : 'Failed to load status history');
          setItems([]);
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [evaluationId]);

  return { items, loading, error };
}
