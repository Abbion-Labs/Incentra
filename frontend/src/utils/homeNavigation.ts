const HOME_ROUTE_PRIORITY: { role: string; path: string }[] = [
  { role: 'EVALUATOR', path: '/evaluator/workflow' },
  { role: 'CONTROLLER', path: '/controller/workflow' },
  { role: 'EMPLOYEE', path: '/employee' },
  { role: 'PAYROLL', path: '/admin/varijabila/salaries' },
  { role: 'ADMIN', path: '/admin/crud/employees' },
];

export type NavGroup =
  'evaluator' | 'controller' | 'employee' | 'admin' | 'payroll';

export const NAV_GROUP_ORDER: NavGroup[] = [
  'evaluator',
  'controller',
  'employee',
  'admin',
  'payroll',
];

export function homePathForRole(role: string | null): string | null {
  return HOME_ROUTE_PRIORITY.find((entry) => entry.role === role)?.path ?? null;
}

/** Uloge korisnika koje imaju svoje ekrane, redom kojim se nude u prekidaču. */
export function switchableRoles(roles: string[]): string[] {
  return HOME_ROUTE_PRIORITY.filter((entry) => roles.includes(entry.role)).map(
    (entry) => entry.role,
  );
}

/**
 * Uloga u kojoj se stranica otvara: aktivna uloga sesije ako je stranica za
 * nju, inače prva korisnikova uloga kojoj je stranica namenjena. Za tu drugu
 * treba prvo preći u nju, jer token nosi samo ulogu sesije. Stranica bez svoje
 * uloge, kao nalog ili početna, otvara se u ulozi sesije.
 */
export function resolvePageRole(
  userRoles: string[],
  pageRoles: string[] | undefined,
  activeRole: string | null,
): string | null {
  if (!pageRoles) {
    return activeRole;
  }

  if (
    activeRole !== null &&
    userRoles.includes(activeRole) &&
    pageRoles.includes(activeRole)
  ) {
    return activeRole;
  }

  return pageRoles.find((role) => userRoles.includes(role)) ?? null;
}

export function sortNavByGroup<T extends { group: NavGroup }>(items: T[]): T[] {
  return [...items].sort(
    (a, b) =>
      NAV_GROUP_ORDER.indexOf(a.group) - NAV_GROUP_ORDER.indexOf(b.group),
  );
}
