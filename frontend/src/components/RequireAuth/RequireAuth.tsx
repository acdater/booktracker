import { Navigate } from 'react-router';
import { useAuth } from '../../hooks/useAuth';

export function RequireAuth({ children }: { children: React.ReactNode }) {
  const { token } = useAuth();
  if (!token) return <Navigate to="/login" replace />;
  return <>{children}</>;
}
