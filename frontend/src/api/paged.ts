import { api } from './client';
import type { PagedResult } from './types';

const MAX_PAGE_SIZE = 100;

export async function fetchAllPages<T>(
  buildPath: (page: number, pageSize: number) => string,
): Promise<T[]> {
  const first = await api.get<PagedResult<T>>(buildPath(1, MAX_PAGE_SIZE));
  const items: T[] = [...(first.items ?? [])];
  const totalPages =
    first.totalPages ||
    Math.max(1, Math.ceil(first.totalCount / MAX_PAGE_SIZE));

  if (
    totalPages <= 1 ||
    items.length === 0 ||
    (first.totalCount > 0 && items.length >= first.totalCount)
  ) {
    return items;
  }

  // Posle prve strane zna se koliko ih ima, pa se ostale učitavaju istovremeno.
  const rest = await Promise.all(
    Array.from({ length: totalPages - 1 }, (_, index) =>
      api.get<PagedResult<T>>(buildPath(index + 2, MAX_PAGE_SIZE)),
    ),
  );
  for (const page of rest) {
    items.push(...(page.items ?? []));
  }

  return items;
}
