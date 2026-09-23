import { useCallback, useEffect, useRef, useState } from 'react';
import { api } from '../api/client';
import type { EvaluationBucketCounts } from '../api/types';
import {
  buildEvaluationBucketCountsPath,
  type RoleListScope,
} from '../utils/evaluationApi';

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
    scope?: RoleListScope;
    year?: number | null;
    quarter?: number | null;
    search?: string;
  },
  enabled = true,
) {
  const [counts, setCounts] = useState<EvaluationBucketCounts>(emptyCounts);
  const [loading, setLoading] = useState(false);
  // Samo odgovor poslednjeg zahteva sme da upiše brojače (brza promena filtera).
  const requestIdRef = useRef(0);

  const load = useCallback(async () => {
    const requestId = ++requestIdRef.current;
    const isLatest = () => requestId === requestIdRef.current;

    if (!enabled) {
      setCounts(emptyCounts);
      setLoading(false);
      return;
    }

    setLoading(true);
    try {
      const result = await api.get<EvaluationBucketCounts>(
        buildEvaluationBucketCountsPath(params),
      );
      if (isLatest()) setCounts(result);
    } catch {
      if (isLatest()) setCounts(emptyCounts);
    } finally {
      if (isLatest()) setLoading(false);
    }
  }, [enabled, params.quarter, params.scope, params.search, params.year]);

  useEffect(() => {
    void load();
  }, [load, queryKey]);

  return { counts, loading, reload: load };
}
