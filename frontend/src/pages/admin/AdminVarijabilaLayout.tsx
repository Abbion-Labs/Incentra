import { Outlet, useLocation } from 'react-router-dom';
import { AppLayout } from '../../components/AppLayout';
import { useIntl } from '../../i18n';

// Stranice varijabile su stavke bočnog menija; naslov prati otvorenu stranicu.
const pageTitles = [
  { path: '/admin/varijabila/salaries', labelKey: 'admin.employeeSalaries' },
  { path: '/admin/varijabila/compensation', labelKey: 'admin.parameters' },
  { path: '/admin/varijabila/results', labelKey: 'admin.results' },
  { path: '/admin/varijabila/analytics', labelKey: 'admin.analytics' },
] as const;

export function AdminVarijabilaLayout() {
  const { formatMessage } = useIntl();
  const { pathname } = useLocation();
  const page = pageTitles.find((entry) => pathname.startsWith(entry.path));

  return (
    <AppLayout
      title={formatMessage({
        id: page ? page.labelKey : 'admin.variableTitle',
      })}
    >
      <div className="varijabila-page">
        <Outlet />
      </div>
    </AppLayout>
  );
}
