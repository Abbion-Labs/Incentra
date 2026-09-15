import { Navigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { useIntl } from '../i18n';
import { resolveHomePath } from '../utils/homeNavigation';

export function HomeRedirect() {
  const { user } = useAuth();
  const { formatMessage } = useIntl();

  const homePath = user ? resolveHomePath(user.roles) : null;
  if (homePath) {
    return <Navigate to={homePath} replace />;
  }

  return (
    <div className="empty card">
      {formatMessage({ id: 'auth.noRoleAssigned' })}
    </div>
  );
}
