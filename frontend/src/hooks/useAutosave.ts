import { useEffect, useRef } from 'react';

interface UseAutosaveOptions {
  enabled: boolean;
  isDirty: boolean;
  onSave: () => Promise<boolean>;
  intervalMs?: number;
}

export function useAutosave({
  enabled,
  isDirty,
  onSave,
  intervalMs = 30_000,
}: UseAutosaveOptions) {
  const savingRef = useRef(false);
  const onSaveRef = useRef(onSave);

  useEffect(() => {
    onSaveRef.current = onSave;
  }, [onSave]);

  useEffect(() => {
    if (!enabled || !isDirty) return;

    const timer = window.setInterval(async () => {
      if (savingRef.current) return;
      savingRef.current = true;
      try {
        await onSaveRef.current();
      } finally {
        savingRef.current = false;
      }
    }, intervalMs);

    return () => window.clearInterval(timer);
  }, [enabled, isDirty, intervalMs]);
}
