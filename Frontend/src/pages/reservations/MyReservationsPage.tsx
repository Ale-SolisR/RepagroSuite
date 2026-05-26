import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  Plus, X, ChevronLeft, ChevronRight, CalendarCheck, CalendarDays,
  ListFilter, ArrowDownWideNarrow, ArrowUpWideNarrow, CheckCircle2,
  Clock, CalendarOff,
} from 'lucide-react'
import { reservationsApi } from '@/api/reservations'
import { formatDate, formatTime } from '@/utils'
import { qk, staleTimes } from '@/lib/queryKeys'
import Button from '@/components/ui/Button'
import Badge from '@/components/ui/Badge'
import Spinner from '@/components/ui/Spinner'
import Modal from '@/components/ui/Modal'
import CreateReservationForm from './CreateReservationForm'
import CancelReservationModal from './CancelReservationModal'
import { classNames } from '@/utils'
import type { ReservationDto } from '@/types'

const TABS = [
  { key: '', label: 'Todas' },
  { key: 'Pending', label: 'Pendientes' },
  { key: 'Approved', label: 'Aprobadas' },
  { key: 'Rejected', label: 'Rechazadas' },
  { key: 'Cancelled', label: 'Canceladas' },
]

// KPI compacto, en línea con el lenguaje de las tarjetas del Dashboard.
function KpiCard({ label, value, Icon, tone }: {
  label: string; value: number; Icon: React.ElementType
  tone: { chip: string; icon: string; num: string }
}) {
  return (
    <div className="rounded-[10px] border border-line bg-paper p-4 shadow-sh1">
      <div className="flex items-center gap-2 mb-2.5">
        <span className={classNames('flex h-7 w-7 items-center justify-center rounded-lg', tone.chip)}>
          <Icon className={classNames('h-4 w-4', tone.icon)} strokeWidth={1.9} />
        </span>
        <span className="text-[12px] font-medium text-ink2">{label}</span>
      </div>
      <p className={classNames('font-mono text-[26px] font-semibold tracking-tight leading-none tabular-nums', tone.num)}>
        {value}
      </p>
    </div>
  )
}

export default function MyReservationsPage() {
  const [showCreate, setShowCreate] = useState(false)
  const [cancelTarget, setCancelTarget] = useState<ReservationDto | null>(null)
  const [page, setPage] = useState(1)
  const [activeTab, setActiveTab] = useState('')
  const [sortDescending, setSortDescending] = useState(true)

  const { data, isLoading } = useQuery({
    queryKey: qk.reservations.my(page, activeTab, sortDescending),
    queryFn: () => reservationsApi.getMy({
      page, pageSize: 15,
      status: activeTab || undefined,
      sortDescending,
    }).then(r => r.data.data!),
    staleTime: staleTimes.myReservations,
  })

  // KPIs: cuentas globales (independientes del filtro/página activos).
  const counts = {
    total: useQuery({
      queryKey: qk.reservations.myCount(''),
      queryFn: () => reservationsApi.getMy({ page: 1, pageSize: 1 }).then(r => r.data.data?.totalCount ?? 0),
      staleTime: staleTimes.myReservations,
    }).data ?? 0,
    pending: useQuery({
      queryKey: qk.reservations.myCount('Pending'),
      queryFn: () => reservationsApi.getMy({ page: 1, pageSize: 1, status: 'Pending' }).then(r => r.data.data?.totalCount ?? 0),
      staleTime: staleTimes.myReservations,
    }).data ?? 0,
    approved: useQuery({
      queryKey: qk.reservations.myCount('Approved'),
      queryFn: () => reservationsApi.getMy({ page: 1, pageSize: 1, status: 'Approved' }).then(r => r.data.data?.totalCount ?? 0),
      staleTime: staleTimes.myReservations,
    }).data ?? 0,
  }

  const total = data?.totalCount ?? 0
  const totalPages = data?.totalPages ?? 1

  return (
    <div className="p-6 max-w-5xl mx-auto">
      {/* Header */}
      <div className="flex items-center justify-between gap-3 mb-5">
        <div className="flex items-center gap-3 min-w-0">
          <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-brand-600 shadow-sh1 shrink-0">
            <CalendarCheck className="h-5 w-5 text-white" strokeWidth={1.75} />
          </div>
          <div className="min-w-0">
            <p className="text-[11px] font-medium text-ink2/70 tracking-wide">Salas / Mis Reservas</p>
            <h1 className="text-2xl font-bold text-ink leading-tight">Mis Reservas</h1>
          </div>
        </div>
        <Button size="sm" onClick={() => setShowCreate(true)} className="cursor-pointer shrink-0">
          <Plus className="h-4 w-4" /> Nueva Reserva
        </Button>
      </div>

      {/* KPIs */}
      <div className="grid grid-cols-3 gap-3 mb-5">
        <KpiCard label="Total" value={counts.total} Icon={CalendarDays}
          tone={{ chip: 'bg-brand-50', icon: 'text-brand-700', num: 'text-ink' }} />
        <KpiCard label="Pendientes" value={counts.pending} Icon={Clock}
          tone={{ chip: 'bg-amber-50', icon: 'text-amber-600', num: 'text-amber-700' }} />
        <KpiCard label="Aprobadas" value={counts.approved} Icon={CheckCircle2}
          tone={{ chip: 'bg-brand-50', icon: 'text-ok', num: 'text-brand-700' }} />
      </div>

      {/* Main card */}
      <div className="bg-paper rounded-[10px] border border-line shadow-sh1 overflow-hidden">
        {/* Tabs */}
        <div className="flex px-5 border-b border-line overflow-x-auto">
          {TABS.map(tab => (
            <button
              key={tab.key}
              onClick={() => { setActiveTab(tab.key); setPage(1) }}
              className={classNames(
                'px-4 py-3 text-sm font-medium border-b-2 transition-colors whitespace-nowrap cursor-pointer',
                activeTab === tab.key
                  ? 'border-brand-600 text-brand-700'
                  : 'border-transparent text-ink2 hover:text-ink',
              )}
            >
              {tab.label}
            </button>
          ))}
        </div>

        {/* Toolbar: orden */}
        <div className="flex items-center justify-between gap-2 px-5 py-2.5 border-b border-line bg-bg/40">
          <span className="inline-flex items-center gap-1.5 text-[11px] font-semibold text-ink2/70 uppercase tracking-wider">
            <ListFilter className="h-3.5 w-3.5" /> {total} {total === 1 ? 'reserva' : 'reservas'}
          </span>
          <div className="flex items-center rounded-lg border border-line bg-paper p-0.5">
            <button
              onClick={() => { setSortDescending(true); setPage(1) }}
              className={classNames(
                'flex items-center gap-1.5 rounded-md px-2.5 h-8 text-[13px] font-medium transition-colors cursor-pointer',
                sortDescending ? 'bg-brand-600 text-white shadow-sh1' : 'text-ink2 hover:text-ink',
              )}
              title="Más nuevas primero"
            >
              <ArrowDownWideNarrow className="h-3.5 w-3.5" /> Más nuevas
            </button>
            <button
              onClick={() => { setSortDescending(false); setPage(1) }}
              className={classNames(
                'flex items-center gap-1.5 rounded-md px-2.5 h-8 text-[13px] font-medium transition-colors cursor-pointer',
                !sortDescending ? 'bg-brand-600 text-white shadow-sh1' : 'text-ink2 hover:text-ink',
              )}
              title="Más viejas primero"
            >
              <ArrowUpWideNarrow className="h-3.5 w-3.5" /> Más viejas
            </button>
          </div>
        </div>

        {/* Table */}
        {isLoading ? (
          <div className="flex justify-center py-20"><Spinner /></div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm min-w-[640px]">
              <thead>
                <tr className="border-b border-line bg-bg/30">
                  <th className="text-left px-5 py-3 text-[11px] font-semibold text-ink2/70 uppercase tracking-wider">Sala</th>
                  <th className="text-left px-5 py-3 text-[11px] font-semibold text-ink2/70 uppercase tracking-wider">Fecha</th>
                  <th className="text-left px-5 py-3 text-[11px] font-semibold text-ink2/70 uppercase tracking-wider">Horario</th>
                  <th className="text-left px-5 py-3 text-[11px] font-semibold text-ink2/70 uppercase tracking-wider">Propósito</th>
                  <th className="text-left px-5 py-3 text-[11px] font-semibold text-ink2/70 uppercase tracking-wider">Estado</th>
                  <th className="px-5 py-3 w-12" />
                </tr>
              </thead>
              <tbody>
                {data?.items.map((r) => (
                  <tr key={r.id} className="border-b border-line/60 hover:bg-bg/50 transition-colors">
                    <td className="px-5 py-3.5 font-medium text-ink whitespace-nowrap">{r.roomName}</td>
                    <td className="px-5 py-3.5 text-ink2 whitespace-nowrap capitalize">{formatDate(r.startDateTime, "EEE d MMM yyyy")}</td>
                    <td className="px-5 py-3.5 text-ink2 whitespace-nowrap font-mono text-[13px] tabular-nums">
                      {formatTime(r.startDateTime)} – {formatTime(r.endDateTime)}
                    </td>
                    <td className="px-5 py-3.5 text-ink2 max-w-xs truncate">{r.purpose}</td>
                    <td className="px-5 py-3.5"><Badge status={r.status} /></td>
                    <td className="px-5 py-3.5">
                      {/* Solo se cancela si aún no ha iniciado (las pasadas quedan como historial). */}
                      {(r.status === 'Pending' || r.status === 'Approved') && new Date(r.startDateTime) > new Date() && (
                        <button
                          onClick={() => setCancelTarget(r)}
                          className="flex h-8 w-8 items-center justify-center rounded-md text-ink2/50 hover:text-danger hover:bg-danger/5 transition-colors cursor-pointer"
                          title="Cancelar reserva"
                          aria-label={`Cancelar reserva en ${r.roomName}`}
                        >
                          <X className="h-4 w-4" />
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
                {!data?.items.length && (
                  <tr>
                    <td colSpan={6} className="px-5 py-16">
                      <div className="flex flex-col items-center gap-3 text-center">
                        <span className="flex h-12 w-12 items-center justify-center rounded-full bg-bg">
                          <CalendarOff className="h-6 w-6 text-ink2/50" />
                        </span>
                        <div>
                          <p className="text-sm font-medium text-ink">
                            {activeTab ? 'No tiene reservas con este estado' : 'Aún no tiene reservas'}
                          </p>
                          <button
                            onClick={() => setShowCreate(true)}
                            className="mt-1 text-[13px] font-medium text-brand-700 hover:underline cursor-pointer"
                          >
                            Crear una reserva ahora
                          </button>
                        </div>
                      </div>
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}

        {/* Pagination footer */}
        <div className="flex items-center justify-between px-5 py-3 border-t border-line bg-bg/40">
          <span className="text-sm text-ink2/70">
            página <span className="font-mono tabular-nums">{page}</span> de <span className="font-mono tabular-nums">{totalPages}</span>
          </span>
          <div className="flex gap-1">
            <button
              disabled={page === 1}
              onClick={() => setPage(p => p - 1)}
              className="flex items-center gap-1 px-3 py-1.5 text-sm text-ink2 border border-line rounded-md hover:bg-paper hover:text-ink disabled:opacity-40 disabled:cursor-not-allowed bg-paper transition-colors cursor-pointer"
            >
              <ChevronLeft className="h-3.5 w-3.5" /> Anterior
            </button>
            <button
              disabled={page === totalPages}
              onClick={() => setPage(p => p + 1)}
              className="flex items-center gap-1 px-3 py-1.5 text-sm text-ink2 border border-line rounded-md hover:bg-paper hover:text-ink disabled:opacity-40 disabled:cursor-not-allowed bg-paper transition-colors cursor-pointer"
            >
              Siguiente <ChevronRight className="h-3.5 w-3.5" />
            </button>
          </div>
        </div>
      </div>

      <Modal open={showCreate} onClose={() => setShowCreate(false)} title="Nueva Reserva" size="lg">
        <CreateReservationForm onClose={() => setShowCreate(false)} />
      </Modal>

      <CancelReservationModal
        reservation={cancelTarget}
        onClose={() => setCancelTarget(null)}
      />
    </div>
  )
}
