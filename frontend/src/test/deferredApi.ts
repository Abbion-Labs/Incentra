import { vi } from 'vitest';
import { api } from '../api/client';

export interface DeferredRequest {
  resolve: (value: unknown) => void;
  reject: (error: Error) => void;
}

/**
 * Zamenjuje `api.get` obećanjima koja test ručno razrešava, po putanji zahteva,
 * da bi se odgovori mogli pustiti obrnutim redosledom. Putanje iz `immediate`
 * odmah vraćaju zadatu vrednost.
 */
export function mockApiGetDeferred(immediate: Record<string, unknown> = {}) {
  const pending = new Map<string, DeferredRequest>();
  vi.spyOn(api, 'get').mockImplementation((path: string) => {
    if (path in immediate) return Promise.resolve(immediate[path] as never);
    return new Promise<never>((resolve, reject) => {
      pending.set(path, {
        resolve: resolve as DeferredRequest['resolve'],
        reject,
      });
    });
  });
  return pending;
}
