import { Navigate, Outlet } from 'react-router-dom'
import { useAuthStore } from '@/store/authStore'

interface ProtectedRouteProps {
  permission?: string
  redirectTo?: string
}

export default function ProtectedRoute({ permission, redirectTo = '/login' }: ProtectedRouteProps) {
  const { isAuthenticated, user, hasPermission, hasRole } = useAuthStore()

  if (!isAuthenticated) return <Navigate to={redirectTo} replace />

  if (user?.mustChangePassword) return <Navigate to="/forced-change-password" replace />

  if (permission && !hasPermission(permission)) {
    return <Navigate to={hasRole('ADMINISTRATOR') ? '/dashboard' : '/rooms'} replace />
  }

  return <Outlet />
}
