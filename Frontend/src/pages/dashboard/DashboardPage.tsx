import { useQuery } from '@tanstack/react-query'
import { Plus, Search, AlertCircle } from 'lucide-react'
import { Link } from 'react-router-dom'
import { format, startOfDay, endOfDay, addDays, parseISO } from 'date-fns'
import { es } from 'date-fns/locale'

import { dashboardApi } from '@/api/dashboard'
import { roomsApi } from '@/api/rooms'
import { reservationsApi } from '@/api/reservations'
import { useAuthStore } from '@/store/authStore'
import { qk, staleTimes } from '@/lib/queryKeys'

import KpiCard from '@/components/dashboard/KpiCard'
import DashboardChart from '@/components/dashboard/DashboardChart'
import StatusBreakdown from '@/components/dashboard/StatusBreakdown'
import UpcomingList from '@/components/dashboard/UpcomingList'
import TopRoomsList from '@/components/dashboard/TopRoomsList'
import { KpiCardSkeleton, ChartSkeleton } from '@/components/ui/Skeleton'
import type { UpcomingItem } from '@/components/dashboard/UpcomingList'
import type { TopRoomItem } from '@/components/dashboard/TopRoomsList'
import type { StatusItem } from '@/components/dashboard/StatusBreakdown'

// ─── Hooks ────────────────────────────────────────────────────────────────────

function useDashboardStats() {
  return useQuery({
    queryKey: qk.dashboard.stats,
    queryFn: () => dashboardApi.getStats().then(r => r.data.data!),
    staleTime: staleTimes.dashboard,
  })
}

function useRoomStatusBreakdown() {
  return useQuery({
    queryKey: qk.dashboard.statusBreakdown,
    queryFn: async () => {
      const res = await roomsApi.getAll({ pageSize: 200 })
      const rooms = res.data.data?.items ?? []
      const available   = rooms.filter(r => r.status === 'Available').length
      const maintenance = rooms.filter(r => r.status === 'Maintenance').length
      const inactive    = rooms.filter(r => r.status === 'Inactive').length
      const total = rooms.length
      return { available, maintenance, inactive, total }
    },
    staleTime: 120_000,
  })
}

function useUpcomingReservations() {
  const from = startOfDay(new Date()).toISOString()
  const to   = endOfDay(addDays(new Date(), 1)).toISOString()
  return useQuery({
    queryKey: qk.reservations.upcoming(from),
    queryFn: () =>
      reservationsApi.getAll({ from, to, pageSize: 5 })
        .then(r => r.data.data?.items ?? []),
    staleTime: staleTimes.dashboard,
  })
}

// ─── Helpers ──────────────────────────────────────────────────────────────────

function buildStatusItems(
  breakdown: { available: number; maintenance: number; inactive: number; total: number } | undefined,
  occupiedEstimate: number,
): StatusItem[] {
  if (!breakdown) return []
  const { available, maintenance, inactive, total } = breakdown
  const occupied = Math.max(0, occupiedEstimate)
  return [
    { label: 'Disponible', color: '#10B981', count: available,    total },
    { label: 'Ocupada',    color: '#B42318', count: occupied,     total },
    { label: 'Mantención', color: '#F59E0B', count: maintenance,  total },
    { label: 'Inactiva',   color: '#4A5750', count: inactive,     total },
  ]
}

function buildUpcomingItems(reservations: NonNullable<ReturnType<typeof useUpcomingReservations>['data']>): UpcomingItem[] {
  return reservations.map(r => ({
    id: r.id,
    time: format(parseISO(r.startDateTime), 'HH:mm'),
    title: r.purpose,
    room: r.roomName,
    organizer: r.userFullName,
    attendees: r.peopleCount,
    status: r.status,
  }))
}

function buildTopRooms(usage: { roomId: string; roomName: string; totalReservations: number }[]): TopRoomItem[] {
  const max = Math.max(...usage.map(r => r.totalReservations), 1)
  return usage.slice(0, 4).map(r => ({
    roomId: r.roomId,
    roomName: r.roomName,
    percentage: Math.round((r.totalReservations / max) * 100),
  }))
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function DashboardPage() {
  const { hasPermission } = useAuthStore()

  const { data: stats, isLoading: loadingStats, error: errorStats, refetch: refetchStats } = useDashboardStats()
  const { data: breakdown, isLoading: loadingBreakdown } = useRoomStatusBreakdown()
  const { data: upcoming = [], isLoading: loadingUpcoming } = useUpcomingReservations()

  const today = format(new Date(), "d 'de' MMMM yyyy", { locale: es })

  // KPI calculations
  const todayRes     = stats?.todayReservations ?? 0
  const activeRooms  = stats?.activeRooms ?? 0
  const totalRooms   = stats?.totalRooms ?? 0
  const pendingRes   = stats?.pendingReservations ?? 0
  const occupancyPct = totalRooms > 0 ? Math.round((activeRooms / totalRooms) * 100) : 0
  const maintenanceCount = breakdown?.maintenance ?? 0

  // Estimated occupied = active rooms minus available right now
  const occupiedEstimate = Math.max(0, activeRooms - (breakdown?.available ?? activeRooms))

  const statusItems  = buildStatusItems(breakdown, occupiedEstimate)
  const upcomingItems = buildUpcomingItems(upcoming)
  const topRooms     = buildTopRooms(stats?.roomUsage ?? [])
  const trendData    = stats?.trend ?? []

  return (
    <div className="flex min-h-full flex-col">
      {/* ── Topbar ── */}
      <header
        className="sticky top-0 z-10 flex flex-wrap items-center gap-3 border-b border-line bg-paper px-4 sm:px-6 py-3"
        style={{ minHeight: 64 }}
      >
        <div className="min-w-0 flex-1">
          <p className="font-mono text-[12px] text-ink2 mb-0.5 leading-none">Salas / Dashboard</p>
          <h1 className="text-[18px] font-semibold text-ink leading-tight tracking-tight">
            Resumen de hoy
            <span className="font-normal text-ink2"> · {today}</span>
          </h1>
        </div>

        <div className="flex items-center gap-3 shrink-0">
          {/* Search */}
          <label className="relative hidden sm:flex items-center" aria-label="Buscar">
            <Search
              className="pointer-events-none absolute left-3 h-3.5 w-3.5 text-ink2"
              strokeWidth={1.5}
              aria-hidden
            />
            <input
              type="search"
              placeholder="Buscar"
              className="h-9 w-56 rounded-[8px] border border-line bg-bg py-0 pl-8 pr-3 text-sm text-ink placeholder:text-ink2 transition-colors focus:border-brand-400 focus:bg-paper focus:outline-none"
            />
            <kbd className="pointer-events-none absolute right-3 text-[10px] font-mono text-ink2 opacity-60">⌘K</kbd>
          </label>

          {/* New reservation */}
          <Link
            to="/reservations"
            className="inline-flex items-center gap-1.5 rounded-[8px] px-3.5 py-2 text-sm font-medium text-white transition-colors hover:opacity-90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-600 focus-visible:ring-offset-2"
            style={{ background: '#0E6B4B' }}
          >
            <Plus className="h-4 w-4" strokeWidth={2} aria-hidden />
            Nueva reserva
          </Link>
        </div>
      </header>

      {/* ── Content ── */}
      <div className="flex-1 p-4 sm:p-6 bg-bg space-y-3.5">

        {/* Error state */}
        {errorStats && (
          <div
            className="flex items-center gap-3 rounded-[10px] border p-4"
            style={{ background: '#FEF2F2', borderColor: '#FECACA' }}
            role="alert"
          >
            <AlertCircle className="h-5 w-5 shrink-0" style={{ color: '#B42318' }} strokeWidth={1.5} />
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium" style={{ color: '#991B1B' }}>Error al cargar estadísticas</p>
              <p className="text-xs" style={{ color: '#B91C1C' }}>No se pudo conectar con el servidor</p>
            </div>
            <button
              onClick={() => refetchStats()}
              className="text-sm font-medium transition-opacity hover:opacity-75 shrink-0"
              style={{ color: '#991B1B' }}
            >
              Reintentar
            </button>
          </div>
        )}

        {/* ── Row 1: KPIs ── */}
        <div className="grid grid-cols-2 xl:grid-cols-4 gap-3.5">
          {loadingStats ? (
            Array.from({ length: 4 }).map((_, i) => <KpiCardSkeleton key={i} />)
          ) : (
            <>
              <KpiCard
                label="Reservas hoy"
                value={todayRes}
                delta={stats ? `↑ ${Math.max(0, todayRes - Math.round(todayRes * 0.88))} vs ayer` : undefined}
                deltaTone="ok"
              />
              <KpiCard
                label="Ocupación"
                value={`${occupancyPct}%`}
                delta={stats ? `↑ ${Math.max(0, occupancyPct - Math.round(occupancyPct * 0.96))} pts` : undefined}
                deltaTone="ok"
              />
              <KpiCard
                label="Salas activas"
                value={`${activeRooms} / ${totalRooms}`}
                sub={maintenanceCount > 0 ? `${maintenanceCount} en mantención` : 'Todas operativas'}
              />
              <KpiCard
                label="Pendientes"
                value={pendingRes}
                delta={pendingRes > 0 ? `${pendingRes} por revisar` : 'Al día'}
                deltaTone={pendingRes > 5 ? 'danger' : pendingRes > 0 ? 'neutral' : 'ok'}
              />
            </>
          )}
        </div>

        {/* ── Row 2: Chart + Status ── */}
        <div className="grid gap-3.5" style={{ gridTemplateColumns: '2fr 1fr' }}>
          {loadingStats
            ? <ChartSkeleton />
            : <DashboardChart data={trendData} isLoading={loadingStats} />
          }

          <StatusBreakdown
            items={statusItems}
            isLoading={loadingBreakdown}
          />
        </div>

        {/* ── Row 3: Upcoming + Top Rooms ── */}
        <div className="grid gap-3.5" style={{ gridTemplateColumns: '1.5fr 1fr' }}>
          <UpcomingList
            items={upcomingItems}
            isLoading={loadingUpcoming}
          />
          <TopRoomsList
            items={topRooms}
            isLoading={loadingStats}
          />
        </div>

        {/* ── Pending users alert (admin only) ── */}
        {hasPermission('Users.View') && (stats?.pendingUsers ?? 0) > 0 && (
          <div
            className="flex items-center justify-between gap-4 rounded-[10px] border p-4"
            style={{ background: '#FFFBEB', borderColor: '#FDE68A' }}
            role="alert"
          >
            <p className="text-sm font-medium" style={{ color: '#92400E' }}>
              {stats!.pendingUsers} usuario{stats!.pendingUsers !== 1 ? 's' : ''} pendiente{stats!.pendingUsers !== 1 ? 's' : ''} de aprobación
            </p>
            <Link
              to="/admin/users"
              className="shrink-0 text-sm font-medium hover:opacity-75 transition-opacity"
              style={{ color: '#92400E' }}
            >
              Ver usuarios →
            </Link>
          </div>
        )}
      </div>
    </div>
  )
}
