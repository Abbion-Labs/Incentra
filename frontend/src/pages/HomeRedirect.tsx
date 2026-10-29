import { Navigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { useIntl } from '../i18n';
import { homePathForRole } from '../utils/homeNavigation';

export function HomeRedirect() {
  const { activeRole } = useAuth();
  const { formatMessage } = useIntl();

  const homePath = homePathForRole(activeRole);
  if (homePath) {
    return <Navigate to={homePath} replace />;
  }

  return (
    <div className="empty card">
      {formatMessage({ id: 'auth.noRoleAssigned' })}
    </div>
  );
}
