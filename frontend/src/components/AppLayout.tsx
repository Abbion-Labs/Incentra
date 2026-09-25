import { useEffect, useState, type CSSProperties } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { useIntl } from '../i18n';
import { BrandMark } from './BrandMark';
import { LocaleSwitcher } from './LocaleSwitcher';
import { RoleSwitcher } from './RoleSwitcher';
import type { ActiveRoleNavigationState } from './ProtectedRoute';
import { roleLabel } from '../utils/status';
import {
  homePathForRole,
  sortNavByGroup,
  switchableRoles,
  type NavGroup,
} from '../utils/homeNavigation';
import { SidebarNavIcon, type SidebarNavIconName } from './SidebarNavIcon';
import { TopbarUserAvatar } from './common/TopbarUserAvatar';

// Na desktopu je meni uska traka sa ikonicama; nazivi su u tooltip-u.
const SIDEBAR_WIDTH = '76px';

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
    roles: ['EVALUATOR'],
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
  const { user, logout, activeRole } = useAuth();
  const { formatMessage } = useIntl();
  const location = useLocation();
  const navigate = useNavigate();
  const [navOpen, setNavOpen] = useState(false);

  const visibleNav = sortNavByGroup(
    navItems.filter(
      (item) => activeRole !== null && item.roles.includes(activeRole),
    ),
  );
  const roleOptions = user ? switchableRoles(user.roles) : [];
  const showRoleSwitch = roleOptions.length > 1 && activeRole !== null;
  const activeRoleLabel = activeRole
    ? roleLabel(activeRole, formatMessage)
    : null;

  // Fioka menija na telefonu se zatvara posle navigacije i na Escape.
  useEffect(() => {
    setNavOpen(false);
  }, [location.pathname]);

  useEffect(() => {
    if (!navOpen) return;
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') setNavOpen(false);
    }
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [navOpen]);

  // Prvo navigacija, pa promena uloge na početnoj stranici te uloge. Tako
  // upozorenje o nesačuvanim izmenama stigne pre nego što se sesija promeni.
  function switchRole(role: string) {
    const state: ActiveRoleNavigationState = { switchingTo: role };
    navigate(homePathForRole(role) ?? '/', { state });
  }
  const showSidebar = visibleNav.length > 1;
  const shellStyle = {
    '--sidebar-width': showSidebar ? SIDEBAR_WIDTH : '0px',
  } as CSSProperties;
  const logoutLabel = formatMessage({ id: 'navigation.logout' });

  const brand = (
    <Link
      to="/"
      className="app-brand"
      title={formatMessage({ id: 'navigation.brand' })}
    >
      <span className="app-brand__mark">
        <BrandMark />
      </span>
      <span className="app-brand__name">
        {formatMessage({ id: 'navigation.brand' })}
      </span>
    </Link>
  );

  const userCard = user && (
    <Link
      to="/account"
      className="user-card"
      title={formatMessage({ id: 'navigation.myAccount' })}
    >
      <TopbarUserAvatar user={user} />
      <span className="user-card__text">
        <span className="user-card__name">
          {user.employeeFullName ?? user.email}
        </span>
        {activeRoleLabel && !showRoleSwitch && (
          <span className="user-card__role">{activeRoleLabel}</span>
        )}
      </span>
    </Link>
  );

  const logoutButton = (
    <button
      type="button"
      className="icon-btn user-logout"
      onClick={logout}
      aria-label={logoutLabel}
      title={logoutLabel}
    >
      <svg
        width="18"
        height="18"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.75"
        strokeLinecap="round"
        strokeLinejoin="round"
        aria-hidden
      >
        <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
        <path d="m16 17 5-5-5-5" />
        <path d="M21 12H9" />
      </svg>
    </button>
  );

  const roleSwitch = showRoleSwitch && activeRole && (
    <RoleSwitcher
      roles={roleOptions}
      activeRole={activeRole}
      label={formatMessage({ id: 'navigation.activeRole' })}
      roleLabel={(role) => roleLabel(role, formatMessage)}
      onChange={switchRole}
    />
  );

  return (
    <div
      className={`app-shell${showSidebar ? ' app-shell--with-sidebar' : ''}${showSidebar ? ' app-shell--rail' : ''}${navOpen ? ' app-shell--nav-open' : ''}`}
      style={shellStyle}
    >
      {showSidebar && (
        <>
          <aside id="app-sidebar" className="sidebar">
            <div className="sidebar__brand">
              {brand}
              <button
                type="button"
                className="icon-btn sidebar__close"
                onClick={() => setNavOpen(false)}
                aria-label={formatMessage({ id: 'navigation.closeMenu' })}
              >
                <svg
                  width="18"
                  height="18"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2"
                  strokeLinecap="round"
                  aria-hidden
                >
                  <path d="M18 6 6 18M6 6l12 12" />
                </svg>
              </button>
            </div>
            <nav
              className="sidebar__nav"
              aria-label={formatMessage({ id: 'navigation.main' })}
            >
              {visibleNav.map((item) => {
                const active = item.match
                  ? item.match(location.pathname)
                  : location.pathname.startsWith(item.to);
                const label = formatMessage({ id: item.labelKey as never });
                return (
                  <Link
                    key={item.to}
                    to={item.to}
                    className={active ? 'active' : ''}
                    aria-current={active ? 'page' : undefined}
                    title={label}
                  >
                    <span className="sidebar__icon">
                      <SidebarNavIcon name={item.icon} />
                    </span>
                    <span className="sidebar__label">{label}</span>
                  </Link>
                );
              })}
            </nav>
            <div className="sidebar__locale">
              <LocaleSwitcher />
            </div>
            <div className="sidebar__footer">
              {userCard}
              {logoutButton}
            </div>
          </aside>
          <div
            className="sidebar-backdrop"
            onClick={() => setNavOpen(false)}
            aria-hidden
          />
        </>
      )}

      <div className="app-main">
        <header className="topbar">
          <div className="topbar__start">
            {showSidebar ? (
              <button
                type="button"
                className="icon-btn topbar__menu"
                onClick={() => setNavOpen(true)}
                aria-label={formatMessage({ id: 'navigation.openMenu' })}
                aria-controls="app-sidebar"
                aria-expanded={navOpen}
              >
                <svg
                  width="20"
                  height="20"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2"
                  strokeLinecap="round"
                  aria-hidden
                >
                  <path d="M4 6h16M4 12h16M4 18h16" />
                </svg>
              </button>
            ) : (
              brand
            )}
            <p className="topbar__page" title={title}>
              {title}
            </p>
          </div>
          <div className="topbar-meta">
            <LocaleSwitcher />
            {roleSwitch}
            {!showSidebar && (
              <>
                {userCard}
                {logoutButton}
              </>
            )}
          </div>
        </header>
        <main className="main-content">{children}</main>
      </div>
    </div>
  );
}
