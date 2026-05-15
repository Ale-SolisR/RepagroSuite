import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { Toaster } from 'react-hot-toast'

import AppLayout from '@/components/layout/AppLayout'
import ProtectedRoute from '@/components/layout/ProtectedRoute'

import LoginPage from '@/pages/auth/LoginPage'
import RegisterPage from '@/pages/auth/RegisterPage'
import ForgotPasswordPage from '@/pages/auth/ForgotPasswordPage'
import ResetPasswordPage from '@/pages/auth/ResetPasswordPage'
import ForcedChangePasswordPage from '@/pages/auth/ForcedChangePasswordPage'

import DashboardPage from '@/pages/dashboard/DashboardPage'
import RoomsPage from '@/pages/rooms/RoomsPage'
import MyReservationsPage from '@/pages/reservations/MyReservationsPage'
import AdminReservationsPage from '@/pages/reservations/AdminReservationsPage'
import AdminUsersPage from '@/pages/users/AdminUsersPage'
import SettingsPage from '@/pages/settings/SettingsPage'

const qc = new QueryClient({
  defaultOptions: {
    queries: { retry: 1, staleTime: 30_000 },
  },
})

export default function App() {
  return (
    <QueryClientProvider client={qc}>
      <BrowserRouter>
        <Routes>
          {/* Public */}
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
          <Route path="/reset-password" element={<ResetPasswordPage />} />
          <Route path="/forced-change-password" element={<ForcedChangePasswordPage />} />

          {/* Protected */}
          <Route element={<ProtectedRoute />}>
            <Route element={<AppLayout />}>
              <Route path="/dashboard" element={<DashboardPage />} />
              <Route path="/reservations" element={<MyReservationsPage />} />
              <Route path="/rooms" element={<RoomsPage />} />

              <Route element={<ProtectedRoute permission="Reservations.View" />}>
                <Route path="/admin/reservations" element={<AdminReservationsPage />} />
              </Route>
              <Route element={<ProtectedRoute permission="Users.View" />}>
                <Route path="/admin/users" element={<AdminUsersPage />} />
              </Route>
              <Route element={<ProtectedRoute permission="Settings.View" />}>
                <Route path="/settings" element={<SettingsPage />} />
              </Route>
            </Route>
          </Route>

          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Routes>
      </BrowserRouter>

      <Toaster position="top-right" toastOptions={{ duration: 4000 }} />
    </QueryClientProvider>
  )
}
