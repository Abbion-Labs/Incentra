import { vi } from 'vitest';
import { api } from '../api/client';

export interface DeferredRequest {
  resolve: (value: unknown) => void;
  reject: (error: Error) => void;
}

type ImmediateResponses =
  Record<string, unknown> | ((path: string) => unknown | undefined);

/**
 * Zamenjuje `api.get` obećanjima koja test ručno razrešava, po putanji zahteva,
 * da bi se odgovori mogli pustiti obrnutim redosledom. Putanje iz `immediate`
 * (mapa ili funkcija koja vraća `undefined` za ostale) odmah vraćaju vrednost.
 */
export function mockApiGetDeferred(immediate: ImmediateResponses = {}) {
  const lookup =
    typeof immediate === 'function'
      ? immediate
      : (path: string) => immediate[path];
  const pending = new Map<string, DeferredRequest>();
  vi.spyOn(api, 'get').mockImplementation((path: string) => {
    const value = lookup(path);
    if (value !== undefined) return Promise.resolve(value as never);
    return new Promise<never>((resolve, reject) => {
      pending.set(path, {
        resolve: resolve as DeferredRequest['resolve'],
        reject,
      });
    });
  });
  return pending;
}
