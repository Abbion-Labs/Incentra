import { Navigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

interface Props {
  children: React.ReactNode;
  roles?: string[];
}

export function ProtectedRoute({ children, roles }: Props) {
  const { user } = useAuth();

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  if (roles && !roles.some((r) => user.roles.includes(r))) {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}
