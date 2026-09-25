import { NavLink, Outlet } from 'react-router-dom';
import { AppLayout } from '../../components/AppLayout';
import {
  SidebarNavIcon,
  type SidebarNavIconName,
} from '../../components/SidebarNavIcon';
import { useIntl } from '../../i18n';

// Administracija je jedna stavka bočnog menija; stranice se biraju tabovima.
const tabs: { to: string; labelKey: string; icon: SidebarNavIconName }[] = [
  { to: '/admin/crud/employees', labelKey: 'admin.employees', icon: 'id-card' },
  { to: '/admin/crud/users', labelKey: 'admin.crudUsers', icon: 'users' },
  { to: '/admin/crud/lookups', labelKey: 'admin.crudLookups', icon: 'lookups' },
  {
    to: '/admin/crud/evaluator-settings',
    labelKey: 'admin.evaluators',
    icon: 'hierarchy',
  },
  {
    to: '/admin/crud/rating-config',
    labelKey: 'admin.descriptiveRatings',
    icon: 'rating-scale',
  },
];

export function AdminCrudLayout() {
  const { formatMessage } = useIntl();

  return (
    <AppLayout title={formatMessage({ id: 'navigation.admin' })}>
      <nav
        className="page-tabs"
        aria-label={formatMessage({ id: 'navigation.admin' })}
      >
        {tabs.map((tab) => (
          <NavLink
            key={tab.to}
            to={tab.to}
            className={({ isActive }) =>
              `page-tabs__tab${isActive ? ' is-active' : ''}`
            }
          >
            <span className="page-tabs__icon">
              <SidebarNavIcon name={tab.icon} />
            </span>
            {formatMessage({ id: tab.labelKey as never })}
          </NavLink>
        ))}
      </nav>
      <Outlet />
    </AppLayout>
  );
}
