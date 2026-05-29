import { useState } from 'react'
import { Outlet, NavLink, useNavigate } from 'react-router-dom'
import {
  LayoutDashboard, DoorOpen, CalendarDays, Users,
  LogOut, Menu, X, BarChart3, ShieldCheck, Calendar, UserCircle,
  ChevronsLeft, Crown, ChevronUp, UsersRound, Cpu,
} from 'lucide-react'
import { useAuthStore } from '@/store/authStore'
import { authApi } from '@/api/auth'
import { classNames } from '@/utils'

type NavItem = {
  to: string
  label: string
  icon: React.ElementType
  permission: string | null
  role: string | null
  // Módulo aún no desarrollado: se muestra deshabilitado con badge "Próximamente".
  comingSoon?: boolean
}

const navGroups: { label: string; items: NavItem[] }[] = [
  {
    label: 'Salas',
    items: [
      { to: '/dashboard',    label: 'Dashboard',     icon: LayoutDashboard, permission: null,                role: 'ADMINISTRATOR' },
      { to: '/rooms',        label: 'Salas',         icon: DoorOpen,        permission: null,                role: null },
      { to: '/reservations', label: 'Mis reservas',  icon: CalendarDays,    permission: null,                role: null },
      { to: '/calendar',     label: 'Calendario',    icon: Calendar,        permission: null,                role: null },
    ],
  },
  {
    label: 'RRHH',
    items: [
      { to: '#', label: 'Próximamente', icon: UsersRound, permission: null, role: null, comingSoon: true },
    ],
  },
  {
    label: 'TI',
    items: [
      { to: '#', label: 'Próximamente', icon: Cpu, permission: null, role: null, comingSoon: true },
    ],
  },
  {
    label: 'Administración',
    items: [
      { to: '/admin/users',        label: 'Usuarios',  icon: Users,       permission: 'Users.View',         role: null },
      { to: '/admin/reservations', label: 'Auditoría', icon: ShieldCheck, permission: 'Reservations.View',  role: null },
      { to: '/settings',           label: 'Ajustes',   icon: BarChart3,   permission: 'Settings.View',      role: null },
    ],
  },
  {
    label: 'Mi cuenta',
    items: [
      { to: '/profile', label: 'Perfil', icon: UserCircle, permission: null, role: null },
    ],
  },
]

function getInitials(name: string) {
  const parts = name.trim().split(' ').filter(Boolean)
  if (parts.length >= 2) return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
  return name.substring(0, 2).toUpperCase()
}

function getRoleLabel(roles: string[], isMaster: boolean): string {
  if (isMaster) return 'Administrador Maestro'
  if (!roles?.length) return 'Usuario'
  const role = roles[0]
  const labels: Record<string, string> = {
    Admin: 'Administrador',
    ADMINISTRATOR: 'Administrador',
    Administrator: 'Administrador',
    Manager: 'Coordinador',
    User: 'Usuario',
    Coordinator: 'Coordinador',
  }
  return labels[role] ?? role
}

function SidebarContent({
  onClose, collapsed, onToggleCollapse,
}: {
  onClose?: () => void
  collapsed?: boolean
  onToggleCollapse?: () => void
}) {
  const { user, logout, hasPermission, hasRole } = useAuthStore()
  const navigate = useNavigate()
  const [userMenuOpen, setUserMenuOpen] = useState(false)

  async function handleLogout() {
    try {
      // El refresh token va por cookie; el backend lo lee solo.
      await authApi.logout()
    } catch { /* ignore */ } finally {
      logout()
      navigate('/login')
    }
  }

  const isMaster = user?.isMaster ?? false

  return (
    <div
      className={classNames(
        'flex h-full flex-col text-white relative transition-all duration-300',
        collapsed ? 'w-[68px]' : 'w-[248px]',
      )}
      style={{
        background: 'linear-gradient(180deg, #073D31 0%, #0A5037 60%, #0E6B4B 100%)',
      }}
    >
      {/* Adorno superior dorado sutil */}
      <div className="absolute top-0 right-0 w-24 h-24 pointer-events-none opacity-40"
        style={{
          background: 'radial-gradient(circle at top right, rgba(245,197,24,.25), transparent 60%)',
        }}
      />

      {/* Logo */}
      <div className="flex h-16 items-center gap-3 px-4 shrink-0 relative"
        style={{ borderBottom: '1px solid rgba(255,255,255,.08)' }}
      >
        <div
          className="flex h-8 w-8 items-center justify-center rounded-lg shrink-0 shadow-sm ring-1 ring-white/20"
          style={{ background: '#fff' }}
        >
          <span className="text-[15px] font-bold" style={{ color: '#0A5037' }}>R</span>
        </div>
        {!collapsed && (
          <div className="flex-1 min-w-0">
            <p className="text-[15px] font-semibold tracking-tight leading-none">Repagro</p>
            <p className="text-[10px] text-white/45 tracking-wider mt-0.5">SUITE · v6.1</p>
          </div>
        )}
        {onClose && (
          <button
            onClick={onClose}
            className="ml-auto rounded p-1 transition-colors text-white/70 hover:text-white"
            aria-label="Cerrar menú"
          >
            <X className="h-4 w-4" />
          </button>
        )}
        {onToggleCollapse && !onClose && (
          <button
            onClick={onToggleCollapse}
            className={classNames(
              'absolute top-1/2 -translate-y-1/2 -right-3 flex h-6 w-6 items-center justify-center rounded-full bg-white text-gray-600 shadow-md ring-1 ring-gray-200 hover:text-gray-900 hover:scale-110 transition-all z-30',
              collapsed && 'rotate-180'
            )}
            aria-label={collapsed ? 'Expandir' : 'Contraer'}
            title={collapsed ? 'Expandir menú' : 'Contraer menú'}
          >
            <ChevronsLeft className="h-3 w-3" strokeWidth={2.5} />
          </button>
        )}
      </div>

      {/* Navigation */}
      <nav
        className={classNames(
          'flex-1 overflow-y-auto overflow-x-hidden py-4 space-y-5',
          collapsed ? 'px-2' : 'px-3'
        )}
        aria-label="Navegación principal"
      >
        {navGroups.map(group => {
          const visibleItems = group.items.filter(item =>
            (!item.permission || hasPermission(item.permission)) &&
            (!item.role || hasRole(item.role))
          )
          if (!visibleItems.length) return null

          return (
            <div key={group.label}>
              {!collapsed && (
                <p className="mb-2 px-2 text-[10px] font-semibold tracking-[.12em] uppercase text-white/40">
                  {group.label}
                </p>
              )}
              {collapsed && (
                <div className="mx-auto h-px w-6 bg-white/10 mb-2" />
              )}
              <ul className="space-y-0.5">
                {visibleItems.map(({ to, label, icon: Icon, comingSoon }) => (
                  <li key={`${group.label}-${to}-${label}`}>
                    {comingSoon ? (
                      <div
                        title={collapsed ? `${label} (próximamente)` : undefined}
                        aria-disabled="true"
                        className={classNames(
                          'group relative flex items-center rounded-lg font-medium cursor-not-allowed opacity-55',
                          collapsed ? 'h-10 w-full justify-center' : 'gap-3 px-2.5 py-2 text-[13.5px]',
                          'text-white/50',
                        )}
                      >
                        <Icon
                          className={classNames(
                            'shrink-0',
                            collapsed ? 'h-[18px] w-[18px]' : 'h-[17px] w-[17px]',
                          )}
                          strokeWidth={1.75}
                        />
                        {!collapsed && (
                          <>
                            <span className="truncate flex-1 leading-none">{label}</span>
                            <span className="ml-auto inline-flex items-center rounded-full bg-white/10 px-1.5 py-0.5 text-[9px] font-semibold tracking-wider uppercase text-white/60 ring-1 ring-white/10">
                              Próx.
                            </span>
                          </>
                        )}
                        {collapsed && (
                          <span className="absolute left-full ml-3 z-50 whitespace-nowrap rounded-md bg-gray-900 px-2 py-1 text-[11px] font-medium text-white opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none shadow-lg">
                            {label} (próximamente)
                          </span>
                        )}
                      </div>
                    ) : (
                      <NavLink
                        to={to}
                        onClick={onClose}
                        title={collapsed ? label : undefined}
                        className={({ isActive }) =>
                          classNames(
                            'group relative flex items-center rounded-lg font-medium transition-all duration-150',
                            collapsed ? 'h-10 w-full justify-center' : 'gap-3 px-2.5 py-2 text-[13.5px]',
                            isActive
                              ? 'bg-white/[.14] text-white shadow-sm'
                              : 'text-white/70 hover:bg-white/[.07] hover:text-white',
                          )
                        }
                      >
                        {({ isActive }: { isActive: boolean }) => (
                          <>
                            {/* Indicador activo lateral dorado */}
                            {isActive && !collapsed && (
                              <span className="absolute left-0 top-1/2 -translate-y-1/2 h-5 w-[3px] rounded-r-full bg-amber-400" />
                            )}
                            <Icon
                              className={classNames(
                                'shrink-0 transition-colors',
                                collapsed ? 'h-[18px] w-[18px]' : 'h-[17px] w-[17px]',
                              )}
                              strokeWidth={isActive ? 2 : 1.75}
                              style={isActive ? { color: '#C9B26B' } : undefined}
                            />
                            {!collapsed && (
                              <span className="truncate flex-1 leading-none">{label}</span>
                            )}
                            {/* Tooltip cuando está colapsado */}
                            {collapsed && (
                              <span className="absolute left-full ml-3 z-50 whitespace-nowrap rounded-md bg-gray-900 px-2 py-1 text-[11px] font-medium text-white opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none shadow-lg">
                                {label}
                              </span>
                            )}
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
      <div
        className={classNames('shrink-0', collapsed ? 'px-2 pb-3' : 'px-3 pb-3')}
        style={{ borderTop: '1px solid rgba(255,255,255,.08)', paddingTop: '10px' }}
      >
        {collapsed ? (
          <button
            onClick={handleLogout}
            className="w-full flex items-center justify-center h-10 rounded-lg text-white/60 hover:text-white hover:bg-white/[.07] transition-colors group relative"
            title="Cerrar sesión"
          >
            <div
              className="flex h-7 w-7 items-center justify-center rounded-full text-[11px] font-bold ring-2 ring-white/10"
              style={{ background: isMaster ? '#C9B26B' : '#E5E7EB', color: '#073D31' }}
            >
              {getInitials(user?.fullName ?? 'U')}
            </div>
            <span className="absolute left-full ml-3 whitespace-nowrap rounded-md bg-gray-900 px-2 py-1 text-[11px] font-medium text-white opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none shadow-lg z-50">
              Cerrar sesión
            </span>
          </button>
        ) : (
          <div className="relative">
            <button
              onClick={() => setUserMenuOpen(o => !o)}
              className="w-full flex items-center gap-2.5 rounded-lg px-2 py-2 transition-colors hover:bg-white/[.07]"
            >
              <div className="relative shrink-0">
                <div
                  className="flex h-9 w-9 items-center justify-center rounded-full text-[12px] font-bold ring-2 ring-white/10"
                  style={{ background: isMaster ? '#C9B26B' : '#E5E7EB', color: '#073D31' }}
                >
                  {getInitials(user?.fullName ?? 'U')}
                </div>
                {isMaster && (
                  <div className="absolute -bottom-1 -right-1 flex h-4 w-4 items-center justify-center rounded-full bg-amber-400 ring-2"
                    style={{ background: '#C9B26B', ['--tw-ring-color' as any]: '#0A5037' }}>
                    <Crown className="h-2.5 w-2.5 text-white" strokeWidth={2.5} />
                  </div>
                )}
              </div>
              <div className="min-w-0 flex-1 text-left">
                <p className="truncate text-[13px] font-semibold text-white leading-tight">
                  {user?.fullName}
                </p>
                <p
                  className="truncate text-[10.5px] mt-0.5"
                  style={{ color: isMaster ? '#C9B26B' : 'rgba(255,255,255,.55)' }}
                >
                  {getRoleLabel(user?.roles ?? [], isMaster)}
                </p>
              </div>
              <ChevronUp className={classNames(
                'h-3.5 w-3.5 text-white/40 transition-transform shrink-0',
                userMenuOpen ? '' : 'rotate-180',
              )} />
            </button>

            {/* Dropdown */}
            {userMenuOpen && (
              <div className="absolute bottom-full left-0 right-0 mb-1.5 rounded-lg bg-white shadow-xl ring-1 ring-black/5 overflow-hidden">
                <button
                  onClick={() => { setUserMenuOpen(false); navigate('/profile') }}
                  className="w-full flex items-center gap-2.5 px-3 py-2 text-sm text-gray-700 hover:bg-gray-50 transition-colors"
                >
                  <UserCircle className="h-4 w-4 text-gray-400" />
                  Ver mi perfil
                </button>
                <div className="border-t border-gray-100" />
                <button
                  onClick={handleLogout}
                  className="w-full flex items-center gap-2.5 px-3 py-2 text-sm text-red-600 hover:bg-red-50 transition-colors"
                >
                  <LogOut className="h-4 w-4" />
                  Cerrar sesión
                </button>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  )
}

export default function AppLayout() {
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [collapsed, setCollapsed] = useState(false)

  return (
    <div className="flex h-screen overflow-hidden bg-gray-50">
      {/* Desktop sidebar */}
      <aside className="hidden lg:flex shrink-0 relative z-20">
        <SidebarContent collapsed={collapsed} onToggleCollapse={() => setCollapsed(c => !c)} />
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
        <div className="flex h-12 shrink-0 items-center gap-3 border-b border-gray-200 px-4 lg:hidden bg-white">
          <button
            onClick={() => setDrawerOpen(true)}
            className="rounded p-1 text-gray-600 transition-colors hover:text-gray-900"
            aria-label="Abrir menú"
          >
            <Menu className="h-5 w-5" />
          </button>
          <div className="flex items-center gap-2">
            <div
              className="flex h-6 w-6 items-center justify-center rounded text-xs font-bold"
              style={{ background: '#0A5037', color: '#fff' }}
            >
              R
            </div>
            <span className="text-sm font-semibold text-gray-900">Repagro</span>
          </div>
        </div>

        <main className="flex-1 overflow-y-auto">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
