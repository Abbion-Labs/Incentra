import { api } from './client';
import type { PagedResult } from './types';

const MAX_PAGE_SIZE = 100;

export async function fetchAllPages<T>(buildPath: (page: number, pageSize: number) => string): Promise<T[]> {
  const items: T[] = [];
  let page = 1;
  let totalPages = 1;

  while (page <= totalPages) {
    const result = await api.get<PagedResult<T>>(buildPath(page, MAX_PAGE_SIZE));
    const pageItems = result.items ?? [];
    items.push(...pageItems);
    totalPages = result.totalPages || Math.max(1, Math.ceil(result.totalCount / MAX_PAGE_SIZE));
    if (pageItems.length === 0 || (result.totalCount > 0 && items.length >= result.totalCount)) {
      break;
    }
    page += 1;
  }

  return items;
}
