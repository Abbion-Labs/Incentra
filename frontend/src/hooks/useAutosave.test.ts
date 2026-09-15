import { describe, expect, it, vi } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useAutosave } from './useAutosave';

describe('useAutosave', () => {
  it('callsOnSaveOnIntervalWhenDirty', async () => {
    vi.useFakeTimers();
    const onSave = vi.fn(async () => true);

    renderHook(() => useAutosave({
      enabled: true,
      isDirty: true,
      onSave,
      intervalMs: 1000,
    }));

    await act(async () => {
      vi.advanceTimersByTime(1000);
    });

    expect(onSave).toHaveBeenCalledTimes(1);
    vi.useRealTimers();
  });

  it('doesNotScheduleWhenNotDirty', async () => {
    vi.useFakeTimers();
    const onSave = vi.fn(async () => true);

    renderHook(() => useAutosave({
      enabled: true,
      isDirty: false,
      onSave,
      intervalMs: 1000,
    }));

    await act(async () => {
      vi.advanceTimersByTime(3000);
    });

    expect(onSave).not.toHaveBeenCalled();
    vi.useRealTimers();
  });
});
