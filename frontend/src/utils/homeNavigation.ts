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

export function resolveHomePath(roles: string[]): string | null {
  for (const entry of HOME_ROUTE_PRIORITY) {
    if (roles.includes(entry.role)) {
      return entry.path;
    }
  }
  return null;
}

export function sortNavByGroup<T extends { group: NavGroup }>(items: T[]): T[] {
  return [...items].sort(
    (a, b) =>
      NAV_GROUP_ORDER.indexOf(a.group) - NAV_GROUP_ORDER.indexOf(b.group),
  );
}
