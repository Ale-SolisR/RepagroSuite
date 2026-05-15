import { Navigate, Outlet } from 'react-router-dom'
import { useAuthStore } from '@/store/authStore'

interface ProtectedRouteProps {
  permission?: string
  redirectTo?: string
}

export default function ProtectedRoute({ permission, redirectTo = '/login' }: ProtectedRouteProps) {
  const { isAuthenticated, user, hasPermission } = useAuthStore()

  if (!isAuthenticated) return <Navigate to={redirectTo} replace />

  if (user?.mustChangePassword) return <Navigate to="/forced-change-password" replace />

  if (permission && !hasPermission(permission)) return <Navigate to="/dashboard" replace />

  return <Outlet />
}
