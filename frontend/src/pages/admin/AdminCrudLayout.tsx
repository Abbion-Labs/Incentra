import { NavLink, Outlet } from 'react-router-dom';
import { AppLayout } from '../../components/AppLayout';
import { useIntl } from '../../i18n';

const links = [
  { to: '/admin/crud/employees', labelKey: 'admin.employees' },
  { to: '/admin/crud/users', labelKey: 'admin.crudUsers' },
  { to: '/admin/crud/lookups', labelKey: 'admin.crudLookups' },
  { to: '/admin/crud/evaluator-settings', labelKey: 'admin.evaluators' },
  { to: '/admin/crud/rating-config', labelKey: 'admin.descriptiveRatings' },
];

export function AdminCrudLayout() {
  const { formatMessage } = useIntl();

  return (
    <AppLayout title={formatMessage({ id: 'admin.crudTitle' })}>
      <nav className="tabs" style={{ marginBottom: '1rem' }}>
        {links.map((l) => (
          <NavLink
            key={l.to}
            to={l.to}
            className={({ isActive }) => `tab ${isActive ? 'active' : ''}`}
            style={{ textDecoration: 'none' }}
          >
            {formatMessage({ id: l.labelKey as never })}
          </NavLink>
        ))}
      </nav>
      <Outlet />
    </AppLayout>
  );
}
