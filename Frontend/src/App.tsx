import { lazy, Suspense } from 'react'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { Toaster } from 'react-hot-toast'

import AppLayout from '@/components/layout/AppLayout'
import ProtectedRoute from '@/components/layout/ProtectedRoute'
import Spinner from '@/components/ui/Spinner'

// Páginas de auth — pequeñas y críticas para el primer paint, eager.
import LoginPage from '@/pages/auth/LoginPage'

// Resto de páginas: code-split por ruta. Cada una vive en su propio chunk JS y se descarga
// sólo cuando el usuario navega a esa ruta. Reduce el bundle inicial dramáticamente.
const RegisterPage = lazy(() => import('@/pages/auth/RegisterPage'))
const ForgotPasswordPage = lazy(() => import('@/pages/auth/ForgotPasswordPage'))
const ResetPasswordPage = lazy(() => import('@/pages/auth/ResetPasswordPage'))
const ForcedChangePasswordPage = lazy(() => import('@/pages/auth/ForcedChangePasswordPage'))

const DashboardPage = lazy(() => import('@/pages/dashboard/DashboardPage'))
const RoomsPage = lazy(() => import('@/pages/rooms/RoomsPage'))
const MyReservationsPage = lazy(() => import('@/pages/reservations/MyReservationsPage'))
const AdminReservationsPage = lazy(() => import('@/pages/reservations/AdminReservationsPage'))
const AdminUsersPage = lazy(() => import('@/pages/users/AdminUsersPage'))
const SettingsPage = lazy(() => import('@/pages/settings/SettingsPage'))
const ProfilePage = lazy(() => import('@/pages/profile/ProfilePage'))
const CalendarPage = lazy(() => import('@/pages/calendar/CalendarPage'))

// Módulo TI / Inventario
const ItDashboardPage = lazy(() => import('@/pages/ti/ItDashboardPage'))
const ItAssetsPage = lazy(() => import('@/pages/ti/ItAssetsPage'))
const ItAssetFormPage = lazy(() => import('@/pages/ti/ItAssetFormPage'))
const ItAssetDetailPage = lazy(() => import('@/pages/ti/ItAssetDetailPage'))
const ItTicketsPage = lazy(() => import('@/pages/ti/ItTicketsPage'))
const ItTicketDetailPage = lazy(() => import('@/pages/ti/ItTicketDetailPage'))
const ItAssignmentWizardPage = lazy(() => import('@/pages/ti/ItAssignmentWizardPage'))
const ItReturnWizardPage = lazy(() => import('@/pages/ti/ItReturnWizardPage'))
const ItEmployeesPage = lazy(() => import('@/pages/ti/ItEmployeesPage'))

// Fallback para Suspense — pantalla mínima mientras carga el chunk.
function PageLoader() {
  return (
    <div className="flex h-full w-full items-center justify-center min-h-[40vh]">
      <Spinner />
    </div>
  )
}

const qc = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      // staleTime default conservador. Endpoints concretos lo sobrescriben en su useQuery.
      staleTime: 30_000,
      // gcTime largo: los datos se mantienen en cache aunque el componente se desmonte,
      // para que volver a la página sea instantáneo.
      gcTime: 5 * 60_000,
      refetchOnWindowFocus: false,
    },
  },
})

export default function App() {
  return (
    <QueryClientProvider client={qc}>
      <BrowserRouter>
        <Suspense fallback={<PageLoader />}>
          <Routes>
            {/* Public */}
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/forgot-password" element={<ForgotPasswordPage />} />
            <Route path="/reset-password" element={<ResetPasswordPage />} />
            <Route path="/forced-change-password" element={<ForcedChangePasswordPage />} />

            {/* Protected — accesible a cualquier autenticado */}
            <Route element={<ProtectedRoute />}>
              <Route element={<AppLayout />}>
                <Route path="/rooms" element={<RoomsPage />} />
                <Route path="/reservations" element={<MyReservationsPage />} />
                <Route path="/calendar" element={<CalendarPage />} />
                <Route path="/profile" element={<ProfilePage />} />

                {/* Solo administradores y master */}
                <Route element={<ProtectedRoute role="ADMINISTRATOR" />}>
                  <Route path="/dashboard" element={<DashboardPage />} />
                </Route>
                <Route element={<ProtectedRoute permission="Reservations.View" />}>
                  <Route path="/admin/reservations" element={<AdminReservationsPage />} />
                </Route>
                <Route element={<ProtectedRoute permission="Users.View" />}>
                  <Route path="/admin/users" element={<AdminUsersPage />} />
                </Route>
                <Route element={<ProtectedRoute permission="Settings.View" />}>
                  <Route path="/settings" element={<SettingsPage />} />
                </Route>

                {/* Módulo TI / Inventario */}
                <Route element={<ProtectedRoute permission="Ti.Dashboard.View" />}>
                  <Route path="/ti" element={<ItDashboardPage />} />
                </Route>
                <Route element={<ProtectedRoute permission="Ti.Inventory.View" />}>
                  <Route path="/ti/assets" element={<ItAssetsPage />} />
                  <Route path="/ti/assets/new" element={<ItAssetFormPage />} />
                  <Route path="/ti/assets/:id" element={<ItAssetDetailPage />} />
                  <Route path="/ti/assets/:id/edit" element={<ItAssetFormPage />} />
                  <Route path="/ti/tickets" element={<ItTicketsPage />} />
                  <Route path="/ti/tickets/:id" element={<ItTicketDetailPage />} />
                  <Route path="/ti/employees" element={<ItEmployeesPage />} />
                </Route>
                <Route element={<ProtectedRoute permission="Ti.Assign" />}>
                  <Route path="/ti/assignments/new" element={<ItAssignmentWizardPage />} />
                </Route>
                <Route element={<ProtectedRoute permission="Ti.Return" />}>
                  <Route path="/ti/assets/:id/return" element={<ItReturnWizardPage />} />
                </Route>
              </Route>
            </Route>

            <Route path="/" element={<Navigate to="/rooms" replace />} />
            <Route path="*" element={<Navigate to="/rooms" replace />} />
          </Routes>
        </Suspense>
      </BrowserRouter>

      <Toaster position="top-right" toastOptions={{ duration: 4000 }} />
    </QueryClientProvider>
  )
}
