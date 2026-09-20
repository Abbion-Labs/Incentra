import { Link, useLocation } from 'react-router-dom';
import { useEffect, useState } from 'react';
import type { CSSProperties } from 'react';
import { useAuth } from '../auth/AuthContext';
import { useIntl } from '../i18n';
import { BrandMark } from './BrandMark';
import { LocaleSwitcher } from './LocaleSwitcher';
import { roleLabel } from '../utils/status';
import { sortNavByGroup, type NavGroup } from '../utils/homeNavigation';
import { SidebarNavIcon, type SidebarNavIconName } from './SidebarNavIcon';
import { TopbarUserAvatar } from './common/TopbarUserAvatar';

const SIDEBAR_STORAGE_KEY = 'vn-sidebar-collapsed';
const SIDEBAR_WIDTH = '220px';

function readSidebarCollapsed(): boolean {
  try {
    return localStorage.getItem(SIDEBAR_STORAGE_KEY) === '1';
  } catch {
    return false;
  }
}

interface NavItem {
  to: string;
  labelKey: string;
  icon: SidebarNavIconName;
  roles: string[];
  group: NavGroup;
  match?: (pathname: string) => boolean;
}

const navItems: NavItem[] = [
  {
    to: '/evaluator',
    labelKey: 'navigation.evaluatorEmployees',
    icon: 'employees',
    roles: ['EVALUATOR', 'ADMIN'],
    group: 'evaluator',
    match: (p) => p === '/evaluator' || p.startsWith('/evaluator/employees'),
  },
  {
    to: '/evaluator/goals',
    labelKey: 'navigation.evaluatorGoals',
    icon: 'goals',
    roles: ['EVALUATOR', 'ADMIN'],
    group: 'evaluator',
    match: (p) => p.startsWith('/evaluator/goals'),
  },
  {
    to: '/evaluator/workflow',
    labelKey: 'navigation.evaluatorWorkflow',
    icon: 'evaluation',
    roles: ['EVALUATOR', 'ADMIN'],
    group: 'evaluator',
    match: (p) =>
      p.startsWith('/evaluator/workflow') ||
      p.startsWith('/evaluator/evaluations'),
  },
  {
    to: '/evaluator/analytics',
    labelKey: 'navigation.analytics',
    icon: 'analytics',
    roles: ['EVALUATOR', 'ADMIN'],
    group: 'evaluator',
    match: (p) => p.startsWith('/evaluator/analytics'),
  },
  {
    to: '/controller',
    labelKey: 'navigation.controllerEmployees',
    icon: 'employees',
    roles: ['CONTROLLER', 'ADMIN'],
    group: 'controller',
    match: (p) => p === '/controller' || p.startsWith('/controller/employees'),
  },
  {
    to: '/controller/workflow',
    labelKey: 'navigation.controllerWorkflow',
    icon: 'evaluation',
    roles: ['CONTROLLER', 'ADMIN'],
    group: 'controller',
    match: (p) =>
      p.startsWith('/controller/workflow') ||
      p.startsWith('/controller/evaluations'),
  },
  {
    to: '/controller/evaluators',
    labelKey: 'navigation.controllerEvaluators',
    icon: 'evaluators',
    roles: ['CONTROLLER', 'ADMIN'],
    group: 'controller',
    match: (p) => p.startsWith('/controller/evaluators'),
  },
  {
    to: '/employee',
    labelKey: 'navigation.employeeEvaluations',
    icon: 'my-evaluations',
    roles: ['EMPLOYEE', 'ADMIN'],
    group: 'employee',
  },
  {
    to: '/admin/crud',
    labelKey: 'navigation.adminCrud',
    icon: 'crud',
    roles: ['ADMIN'],
    group: 'admin',
    match: (p) => p.startsWith('/admin/crud'),
  },
  {
    to: '/admin/varijabila',
    labelKey: 'navigation.adminVariableCompensation',
    icon: 'varijabila',
    roles: ['PAYROLL'],
    group: 'payroll',
    match: (p) => p.startsWith('/admin/varijabila'),
  },
];

export function AppLayout({
  title,
  children,
}: {
  title: string;
  children: React.ReactNode;
}) {
  const { user, logout, hasRole } = useAuth();
  const { formatMessage } = useIntl();
  const location = useLocation();
  const [sidebarCollapsed, setSidebarCollapsed] =
    useState(readSidebarCollapsed);

  useEffect(() => {
    try {
      localStorage.setItem(SIDEBAR_STORAGE_KEY, sidebarCollapsed ? '1' : '0');
    } catch {
      // ignore storage errors
    }
  }, [sidebarCollapsed]);

  const visibleNav = sortNavByGroup(
    navItems.filter((item) => item.roles.some((r) => hasRole(r))),
  );
  const roleSummary = user?.roles
    .map((role) => roleLabel(role, formatMessage))
    .join(', ');
  const showSidebar = visibleNav.length > 1;
  const shellStyle = {
    '--sidebar-width': showSidebar && !sidebarCollapsed ? SIDEBAR_WIDTH : '0px',
  } as CSSProperties;
  const sidebarOpen = showSidebar && !sidebarCollapsed;

  return (
    <div
      className={`app-shell${sidebarOpen ? ' app-shell--sidebar-open' : ''}`}
      style={shellStyle}
    >
      <header className="topbar">
        <div className="topbar__start">
          {showSidebar && (
            <button
              type="button"
              className="sidebar-toggle"
              onClick={() => setSidebarCollapsed((collapsed) => !collapsed)}
              aria-expanded={!sidebarCollapsed}
              aria-controls="app-sidebar"
              aria-label={
                sidebarCollapsed
                  ? formatMessage({ id: 'navigation.showMenu' })
                  : formatMessage({ id: 'navigation.hideMenu' })
              }
              title={
                sidebarCollapsed
                  ? formatMessage({ id: 'navigation.showMenu' })
                  : formatMessage({ id: 'navigation.hideMenu' })
              }
            >
              <span className="sidebar-toggle__icon" aria-hidden>
                {sidebarCollapsed ? '☰' : '‹'}
              </span>
            </button>
          )}
          <div className="topbar__brand">
            <Link to="/" className="app-brand">
              <span className="app-brand__mark">
                <BrandMark />
              </span>
              <span className="app-brand__text">
                <span className="app-brand__name">
                  {formatMessage({ id: 'navigation.brand' })}
                </span>
                <span className="topbar__page">{title}</span>
              </span>
            </Link>
          </div>
        </div>
        <div className="topbar__center">
          <LocaleSwitcher />
        </div>
        <div className="topbar-meta">
          {user && (
            <Link
              to="/account"
              className="topbar-user"
              title={formatMessage({ id: 'navigation.myAccount' })}
            >
              <TopbarUserAvatar user={user} />
              <div className="topbar-user__text">
                <span className="topbar-user__name">
                  {user.employeeFullName ?? user.email}
                </span>
                {roleSummary && (
                  <span className="topbar-user__role">{roleSummary}</span>
                )}
              </div>
            </Link>
          )}
          <button
            type="button"
            className="btn btn-secondary btn-sm"
            onClick={logout}
          >
            {formatMessage({ id: 'navigation.logout' })}
          </button>
        </div>
      </header>
      <div className="layout-body">
        {showSidebar && (
          <nav
            id="app-sidebar"
            className={`sidebar ${sidebarCollapsed ? 'sidebar--collapsed' : ''}`}
            aria-label={formatMessage({ id: 'navigation.main' })}
            aria-hidden={sidebarCollapsed}
          >
            {visibleNav.map((item) => (
              <Link
                key={item.to}
                to={item.to}
                className={
                  (
                    item.match
                      ? item.match(location.pathname)
                      : location.pathname.startsWith(item.to)
                  )
                    ? 'active'
                    : ''
                }
              >
                <span className="sidebar__icon">
                  <SidebarNavIcon name={item.icon} />
                </span>
                <span className="sidebar__label">
                  {formatMessage({ id: item.labelKey as never })}
                </span>
              </Link>
            ))}
          </nav>
        )}
        <main className="main-content">{children}</main>
      </div>
    </div>
  );
}
