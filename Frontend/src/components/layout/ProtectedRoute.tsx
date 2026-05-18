import { Navigate, Outlet } from 'react-router-dom'
import { useAuthStore } from '@/store/authStore'
import { useIdleLogout } from '@/hooks/useIdleLogout'

interface ProtectedRouteProps {
  permission?: string
  role?: string
  master?: boolean
  redirectTo?: string
}

export default function ProtectedRoute({ permission, role, master, redirectTo = '/login' }: ProtectedRouteProps) {
  const { isAuthenticated, user, hasPermission, hasRole } = useAuthStore()

  useIdleLogout()

  if (!isAuthenticated) return <Navigate to={redirectTo} replace />

  if (user?.mustChangePassword) return <Navigate to="/forced-change-password" replace />

  const fallback = hasRole('ADMINISTRATOR') ? '/dashboard' : '/rooms'

  if (role && !hasRole(role)) return <Navigate to={fallback} replace />
  if (master && !user?.isMaster) return <Navigate to={fallback} replace />
  if (permission && !hasPermission(permission)) return <Navigate to={fallback} replace />

  return <Outlet />
}
