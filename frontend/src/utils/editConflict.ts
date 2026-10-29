import { ApiError } from '../api/client';

const CONCURRENCY_CONFLICT = 'vn-0090';
const VERSION_REQUIRED = 'vn-0106';

/**
 * Server odbija izmenu jer je zapis promenjen posle učitavanja forme (ili
 * izmena nije rekla sa koje verzije je napravljena). Forma tada učitava novo
 * stanje umesto da pregazi tuđu izmenu.
 */
export function isEditConflict(error: unknown): boolean {
  return (
    error instanceof ApiError &&
    (error.code === CONCURRENCY_CONFLICT || error.code === VERSION_REQUIRED)
  );
}
