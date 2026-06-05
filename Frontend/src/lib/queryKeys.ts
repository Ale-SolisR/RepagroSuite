// Claves de TanStack Query centralizadas + sus tiempos de obsolescencia (staleTime).
// Beneficios:
//   - Cuando dos páginas usan el mismo endpoint, comparten cache automáticamente.
//   - Invalidar el "namespace" (qk.rooms.all) refresca paginadas y selectores a la vez.
//   - staleTime por endpoint: features (1h) vs calendar (30s) según volatilidad real.

import type { QueryClient } from '@tanstack/react-query'

export const qk = {
  rooms: {
    all: ['rooms'] as const,
    paged: (page: number, pageSize: number) => ['rooms', 'paged', page, pageSize] as const,
    list: ['rooms', 'list'] as const,             // selector pageSize=100 (calendar, reservar)
    availability: (roomId: string) => ['rooms', 'availability', roomId] as const,
    slots: (roomId: string, date: string) => ['rooms', 'slots', roomId, date] as const,
    weekly: ['rooms', 'weekly-availability'] as const,  // disponibilidad semanal agregada (calendario)
    features: ['rooms', 'features'] as const,    // amenidades (casi inmutables)
  },
  reservations: {
    all: ['reservations'] as const,
    my: (page: number, status: string, sortDesc: boolean) => ['reservations', 'my', page, status, sortDesc] as const,
    myCount: (status: string) => ['reservations', 'my', 'count', status] as const,
    admin: (page: number, tab: string, userId: string, roomId: string, sortDesc: boolean) =>
      ['reservations', 'admin', page, tab, userId, roomId, sortDesc] as const,
    audit: (page: number, tab: string, userId: string, roomId: string, sortDesc: boolean) =>
      ['reservations', 'audit', page, tab, userId, roomId, sortDesc] as const,
    groupOccurrences: (groupId: string) => ['reservations', 'group', groupId] as const,
    calendar: (weekKey: string) => ['reservations', 'calendar', weekKey] as const,
    upcoming: (from: string) => ['reservations', 'upcoming', from] as const,
  },
  users: {
    all: ['users'] as const,
    list: ['users', 'list'] as const,             // selector pageSize=100 (filtros de auditoría)
    admin: (page: number, tab: string, search: string) => ['users', 'admin', page, tab, search] as const,
  },
  rastreoUsers: {
    all: ['rastreoUsers'] as const,
    admin: (page: number, tab: string, search: string) => ['rastreoUsers', 'admin', page, tab, search] as const,
  },
  dashboard: {
    stats: ['dashboard', 'stats'] as const,
    statusBreakdown: ['dashboard', 'rooms-status'] as const,
  },
  settings: {
    all: ['settings'] as const,
  },
  ti: {
    all: ['ti'] as const,
    assets: (page: number, pageSize: number, search: string, status: string, typeId: string, deptId: string) =>
      ['ti', 'assets', page, pageSize, search, status, typeId, deptId] as const,
    asset: (id: string) => ['ti', 'asset', id] as const,
    history: (id: string) => ['ti', 'asset', id, 'history'] as const,
    dashboard: ['ti', 'dashboard'] as const,
    catalogs: ['ti', 'catalogs'] as const,
    departments: (search: string) => ['ti', 'departments', search] as const,
    brands: (search: string) => ['ti', 'brands', search] as const,
    suppliers: (search: string) => ['ti', 'suppliers', search] as const,
    tickets: (page: number, type: string, status: string, search: string, employeeId = '') =>
      ['ti', 'tickets', page, type, status, search, employeeId] as const,
    ticket: (id: string) => ['ti', 'ticket', id] as const,
    availableAssets: ['ti', 'available-assets'] as const,
    employee: (id: string) => ['ti', 'employee', id] as const,
    employeeHistory: (id: string) => ['ti', 'employee', id, 'history'] as const,
  },
} as const

// Cuánto tiempo consideramos cada dato "fresco" antes de re-pedirlo en focus/mount.
// Cuanto más estable es el dato, mayor el staleTime.
export const staleTimes = {
  rooms: 30_000,             // listados paginados con búsqueda
  roomsList: 5 * 60_000,     // selector de salas: cambia poco
  features: 60 * 60_000,     // amenidades: casi nunca cambian
  availability: 5 * 60_000,
  slots: 30_000,
  calendar: 30_000,          // SignalR invalida en tiempo real, no necesita refetch agresivo
  myReservations: 30_000,
  adminReservations: 30_000,
  users: 30_000,
  rastreoUsers: 30_000,
  dashboard: 60_000,
  settings: 5 * 60_000,
  ti: 30_000,
  tiCatalogs: 5 * 60_000,
  tiDashboard: 60_000,
} as const

// Prefijos para invalidar grupos completos (TanStack Query matchea por prefix).
const PREFIX_CALENDAR = ['reservations', 'calendar'] as const

// Helpers de invalidación que cubren TODOS los consumidores afectados.
// Ejemplo: al crear una sala hay que invalidar la lista paginada Y el selector,
// porque ambos usan el namespace 'rooms' y reflejan el mismo dato.
export const invalidate = {
  rooms: (qc: QueryClient) => {
    qc.invalidateQueries({ queryKey: qk.rooms.all })
    // El calendario muestra nombre/color de sala; si cambian, refrescar también.
    qc.invalidateQueries({ queryKey: PREFIX_CALENDAR })
  },
  reservations: (qc: QueryClient) => qc.invalidateQueries({ queryKey: qk.reservations.all }),
  users: (qc: QueryClient) => qc.invalidateQueries({ queryKey: qk.users.all }),
  rastreoUsers: (qc: QueryClient) => qc.invalidateQueries({ queryKey: qk.rastreoUsers.all }),
  settings: (qc: QueryClient) => qc.invalidateQueries({ queryKey: qk.settings.all }),
  ti: (qc: QueryClient) => qc.invalidateQueries({ queryKey: qk.ti.all }),
}
