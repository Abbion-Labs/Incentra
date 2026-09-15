import { useMemo } from 'react';
import { useIntl } from '../i18n';
import {
  bucketLabelKeys,
  bucketTabLabelKeys,
  type EvaluationBucket,
} from '../utils/evaluationBuckets';
import { roleLabel, statusLabel } from '../utils/status';

export function useRoleLabel() {
  const { formatMessage } = useIntl();
  return useMemo(() => (role: string) => roleLabel(role, formatMessage), [formatMessage]);
}

export function useStatusLabel() {
  const { formatMessage } = useIntl();
  return useMemo(() => (status: string) => statusLabel(status, formatMessage), [formatMessage]);
}

export function useBucketLabels() {
  const { formatMessage } = useIntl();

  return useMemo(
    () => ({
      label: (bucket: EvaluationBucket) => formatMessage({ id: bucketLabelKeys[bucket] as never }),
      tabLabel: (bucket: EvaluationBucket) => formatMessage({ id: bucketTabLabelKeys[bucket] as never }),
    }),
    [formatMessage],
  );
}
