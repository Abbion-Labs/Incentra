import { NavLink, Outlet } from 'react-router-dom';
import { AppLayout } from '../../components/AppLayout';
import { useIntl } from '../../i18n';

const links = [
  { to: '/admin/varijabila/salaries', labelKey: 'admin.employeeSalaries' },
  { to: '/admin/varijabila/compensation', labelKey: 'admin.parameters' },
  { to: '/admin/varijabila/results', labelKey: 'admin.results' },
  { to: '/admin/varijabila/analytics', labelKey: 'admin.analytics' },
];

export function AdminVarijabilaLayout() {
  const { formatMessage } = useIntl();

  return (
    <AppLayout title={formatMessage({ id: 'admin.variableTitle' })}>
      <div className="varijabila-page">
        <nav className="tabs varijabila-page__tabs">
          {links.map((l) => (
            <NavLink
              key={l.to}
              to={l.to}
              className={({ isActive }) => `tab ${isActive ? 'active' : ''}`}
            >
              {formatMessage({ id: l.labelKey as never })}
            </NavLink>
          ))}
        </nav>
        <Outlet />
      </div>
    </AppLayout>
  );
}
