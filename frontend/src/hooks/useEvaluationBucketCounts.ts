import { useCallback, useEffect, useState } from 'react';
import { api } from '../api/client';
import type { EvaluationBucketCounts } from '../api/types';
import { buildEvaluationBucketCountsPath } from '../utils/evaluationApi';

const emptyCounts: EvaluationBucketCounts = {
  planning: 0,
  unrated: 0,
  returned: 0,
  submitted: 0,
  approved: 0,
  pending: 0,
  goalsComplete: 0,
  goalsPending: 0,
};

export function useEvaluationBucketCounts(
  queryKey: string,
  params: {
    year?: number | null;
    quarter?: number | null;
    search?: string;
  },
  enabled = true,
) {
  const [counts, setCounts] = useState<EvaluationBucketCounts>(emptyCounts);
  const [loading, setLoading] = useState(false);

  const load = useCallback(async () => {
    if (!enabled) {
      setCounts(emptyCounts);
      return;
    }

    setLoading(true);
    try {
      const result = await api.get<EvaluationBucketCounts>(
        buildEvaluationBucketCountsPath(params),
      );
      setCounts(result);
    } catch {
      setCounts(emptyCounts);
    } finally {
      setLoading(false);
    }
  }, [enabled, params.quarter, params.search, params.year]);

  useEffect(() => {
    void load();
  }, [load, queryKey]);

  return { counts, loading, reload: load };
}
