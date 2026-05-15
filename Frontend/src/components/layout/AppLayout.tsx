import { useState } from 'react'
import { Outlet, NavLink, useNavigate } from 'react-router-dom'
import {
  LayoutDashboard, DoorOpen, CalendarDays, Users, Settings,
  LogOut, Menu, X, BarChart3, ShieldCheck, Calendar
} from 'lucide-react'
import { useAuthStore } from '@/store/authStore'
import { authApi } from '@/api/auth'
import { classNames } from '@/utils'

const navGroups = [
  {
    label: 'PRINCIPAL',
    items: [
      { to: '/dashboard',    label: 'Dashboard', icon: LayoutDashboard, permission: null },
      { to: '/rooms',        label: 'Salas',     icon: DoorOpen,        permission: 'Rooms.View' },
      { to: '/reservations', label: 'Reservas',  icon: CalendarDays,    permission: null },
      { to: '/calendar',     label: 'Calendario',icon: Calendar,        permission: null, disabled: true },
    ],
  },
  {
    label: 'ADMINISTRACIÓN',
    items: [
      { to: '/admin/users',         label: 'Usuarios',   icon: Users,       permission: 'Users.View' },
      { to: '/admin/reservations',  label: 'Auditoría',  icon: ShieldCheck, permission: 'Reservations.View' },
      { to: '/settings',            label: 'Analytics',  icon: BarChart3,   permission: 'Settings.View' },
    ],
  },
]

function getInitials(name: string) {
  const parts = name.trim().split(' ').filter(Boolean)
  if (parts.length >= 2) return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
  return name.substring(0, 2).toUpperCase()
}

function getRoleLabel(roles: string[]): string {
  if (!roles?.length) return 'Usuario'
  const role = roles[0]
  const labels: Record<string, string> = {
    Admin: 'Administrador',
    Manager: 'Coordinador',
    User: 'Usuario',
    Coordinator: 'Coordinador',
  }
  return labels[role] ?? role
}

function SidebarContent({ onClose }: { onClose?: () => void }) {
  const { user, refreshToken, logout, hasPermission } = useAuthStore()
  const navigate = useNavigate()

  async function handleLogout() {
    try {
      if (refreshToken) await authApi.logout(refreshToken)
    } catch { /* ignore */ } finally {
      logout()
      navigate('/login')
    }
  }

  return (
    <div className="flex h-full w-60 flex-col" style={{ background: '#005947' }}>
      {/* Logo */}
      <div className="flex h-16 items-center gap-3 px-5 shrink-0" style={{ borderBottom: '1px solid rgba(255,255,255,.12)' }}>
        <div
          className="flex h-7 w-7 items-center justify-center rounded-[6px] shrink-0"
          style={{ background: '#fff' }}
        >
          <span className="text-sm font-bold" style={{ color: '#005947' }}>R</span>
        </div>
        <span className="text-[15px] font-semibold text-white tracking-tight">Repagro</span>
        {onClose && (
          <button
            onClick={onClose}
            className="ml-auto rounded p-1 transition-colors"
            style={{ color: 'rgba(255,255,255,.7)' }}
            aria-label="Cerrar menú"
          >
            <X className="h-4 w-4" />
          </button>
        )}
      </div>

      {/* Navigation */}
      <nav className="flex-1 overflow-y-auto py-5 px-3 space-y-5" aria-label="Navegación principal">
        {navGroups.map(group => {
          const visibleItems = group.items.filter(item =>
            !item.permission || hasPermission(item.permission)
          )
          if (!visibleItems.length) return null

          return (
            <div key={group.label}>
              <p
                className="mb-1.5 px-2 text-[11px] font-medium tracking-[.1em] uppercase"
                style={{ color: 'rgba(255,255,255,.45)' }}
              >
                {group.label}
              </p>
              <ul className="space-y-0.5">
                {visibleItems.map(({ to, label, icon: Icon, disabled }) => (
                  <li key={to}>
                    {disabled ? (
                      <span
                        className="flex items-center gap-3 rounded-[6px] px-2.5 py-2 text-sm"
                        style={{ color: 'rgba(255,255,255,.3)', cursor: 'not-allowed' }}
                        aria-disabled="true"
                      >
                        <Icon className="h-4 w-4 shrink-0" strokeWidth={1.5} />
                        {label}
                      </span>
                    ) : (
                      <NavLink
                        to={to}
                        onClick={onClose}
                        className={({ isActive }) =>
                          classNames(
                            'flex items-center gap-3 rounded-[6px] px-2.5 py-2 text-sm font-medium transition-colors',
                            isActive ? 'font-semibold' : 'hover:bg-black/10'
                          )
                        }
                        style={({ isActive }) =>
                          isActive
                            ? { background: 'rgba(0,0,0,.22)', color: '#fff', borderLeft: '3px solid #F5C518', paddingLeft: '7px' }
                            : { color: 'rgba(255,255,255,.75)' }
                        }
                      >
                        {({ isActive }: { isActive: boolean }) => (
                          <>
                            <Icon
                              className="h-4 w-4 shrink-0"
                              strokeWidth={1.5}
                              style={isActive ? { color: '#F5C518' } : undefined}
                            />
                            {label}
                          </>
                        )}
                      </NavLink>
                    )}
                  </li>
                ))}
              </ul>
            </div>
          )
        })}
      </nav>

      {/* User footer */}
      <div className="shrink-0 px-3 pb-4" style={{ borderTop: '1px solid rgba(255,255,255,.12)', paddingTop: '12px' }}>
        <div className="flex items-center gap-2.5 rounded-[6px] px-2 py-2 mb-1">
          <div
            className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-xs font-bold"
            style={{ background: '#F5C518', color: '#003E2D' }}
          >
            {getInitials(user?.fullName ?? 'U')}
          </div>
          <div className="min-w-0 flex-1">
            <p className="truncate text-sm font-medium text-white leading-tight">
              {user?.fullName}
            </p>
            <p className="truncate text-[11px]" style={{ color: 'rgba(255,255,255,.5)' }}>
              {getRoleLabel(user?.roles ?? [])}
            </p>
          </div>
        </div>
        <button
          onClick={handleLogout}
          className="flex w-full items-center gap-2.5 rounded-[6px] px-2 py-2 text-sm transition-colors hover:bg-black/10"
          style={{ color: 'rgba(255,255,255,.6)' }}
        >
          <LogOut className="h-4 w-4 shrink-0" strokeWidth={1.5} />
          Cerrar sesión
        </button>
      </div>
    </div>
  )
}

export default function AppLayout() {
  const [drawerOpen, setDrawerOpen] = useState(false)

  return (
    <div className="flex h-screen overflow-hidden bg-bg">
      {/* Desktop sidebar */}
      <aside className="hidden lg:flex shrink-0">
        <SidebarContent />
      </aside>

      {/* Mobile drawer overlay */}
      {drawerOpen && (
        <div className="fixed inset-0 z-50 lg:hidden">
          <div
            className="absolute inset-0 bg-black/40 backdrop-blur-sm"
            onClick={() => setDrawerOpen(false)}
          />
          <div className="relative flex h-full">
            <SidebarContent onClose={() => setDrawerOpen(false)} />
          </div>
        </div>
      )}

      {/* Main area */}
      <div className="flex min-w-0 flex-1 flex-col overflow-hidden">
        {/* Mobile top strip */}
        <div
          className="flex h-12 shrink-0 items-center gap-3 border-b px-4 lg:hidden"
          style={{ background: '#fff', borderColor: '#D9DEE5' }}
        >
          <button
            onClick={() => setDrawerOpen(true)}
            className="rounded p-1 text-ink2 transition-colors hover:text-ink"
            aria-label="Abrir menú"
          >
            <Menu className="h-5 w-5" />
          </button>
          <div className="flex items-center gap-2">
            <div
              className="flex h-6 w-6 items-center justify-center rounded text-xs font-bold"
              style={{ background: '#005947', color: '#fff' }}
            >
              R
            </div>
            <span className="text-sm font-semibold text-ink">Repagro</span>
          </div>
        </div>

        <main className="flex-1 overflow-y-auto">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
