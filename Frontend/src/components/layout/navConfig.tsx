import {
  LayoutDashboard, DoorOpen, CalendarDays, Users,
  BarChart3, ShieldCheck, Calendar, UserCircle,
  UsersRound, Cpu, FileText, Sprout,
} from 'lucide-react'

export type NavItem = {
  to: string
  label: string
  icon: React.ElementType
  permission: string | null
  role: string | null
  // Coincidencia exacta de ruta (NavLink `end`): úsalo cuando `to` es prefijo de otras rutas
  // del menú (p. ej. /ti vs /ti/assets) para que no quede activo en las subrutas.
  end?: boolean
  // Módulo aún no desarrollado: se muestra deshabilitado con badge "Próximamente".
  comingSoon?: boolean
}

export const navGroups: { label: string; items: NavItem[] }[] = [
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
      { to: '/ti',         label: 'Dashboard TI', icon: LayoutDashboard, permission: 'Ti.Dashboard.View', role: null, end: true },
      { to: '/ti/assets',    label: 'Inventario',    icon: Cpu,        permission: 'Ti.Inventory.View',  role: null },
      { to: '/ti/tickets',   label: 'Boletas',       icon: FileText,   permission: 'Ti.Inventory.View',  role: null },
      { to: '/ti/employees', label: 'Colaboradores', icon: UsersRound, permission: 'Ti.Inventory.View',  role: null },
    ],
  },
  {
    label: 'Administración',
    items: [
      { to: '/admin/users',         label: 'Usuarios',            icon: Users,       permission: 'Users.View',         role: null },
      { to: '/admin/rastreo-users', label: 'Usuarios de Rastreo', icon: Sprout,      permission: 'RastreoUsers.View',  role: null },
      { to: '/admin/reservations',  label: 'Auditoría',           icon: ShieldCheck, permission: 'Reservations.View',  role: null },
      { to: '/settings',            label: 'Ajustes',             icon: BarChart3,   permission: 'Settings.View',      role: null },
    ],
  },
  {
    label: 'Mi cuenta',
    items: [
      { to: '/profile', label: 'Perfil', icon: UserCircle, permission: null, role: null },
    ],
  },
]
