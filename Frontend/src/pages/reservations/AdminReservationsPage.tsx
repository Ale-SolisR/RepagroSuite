import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Check, X, ChevronLeft, ChevronRight, ChevronDown, ShieldCheck, ListFilter,
  ArrowDownWideNarrow, ArrowUpWideNarrow, RotateCcw, User, MapPin,
  Repeat, CheckCheck,
} from 'lucide-react'
import { reservationsApi } from '@/api/reservations'
import { usersApi } from '@/api/users'
import { roomsApi } from '@/api/rooms'
import { formatDate, formatTime, extractApiError, classNames } from '@/utils'
import { qk, staleTimes, invalidate } from '@/lib/queryKeys'
import Button from '@/components/ui/Button'
import Badge from '@/components/ui/Badge'
import Spinner from '@/components/ui/Spinner'
import Modal from '@/components/ui/Modal'
import Textarea from '@/components/ui/Textarea'
import CancelReservationModal from './CancelReservationModal'
import toast from 'react-hot-toast'
import type { ReservationDto, ReservationGroupDto, UserDto, RoomDto } from '@/types'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'

const rejectSchema = z.object({ reason: z.string().min(5, 'Motivo requerido') })
const approveSchema = z.object({ comment: z.string().optional() })

const TABS = [
  { key: '', label: 'Todas' },
  { key: 'Pending', label: 'Pendientes' },
  { key: 'Approved', label: 'Aprobadas' },
  { key: 'Rejected', label: 'Rechazadas' },
  { key: 'Cancelled', label: 'Canceladas' },
]

const SELECT_CLS =
  'h-9 rounded-lg border border-line bg-paper pl-9 pr-8 text-sm text-ink max-w-[200px] truncate ' +
  'focus:outline-none focus:ring-2 focus:ring-brand-600/30 focus:border-brand-600 cursor-pointer transition-colors'

const isFuture = (dt: string) => new Date(dt) > new Date()

// Muestra "Aprobada por X" / "Rechazada por X" / "Cancelada por X" debajo del badge de estado.
// Para Pending no muestra nada (aún no hay acción).
function ActionByLabel({ r, compact }: { r: ReservationDto; compact?: boolean }) {
  let name: string | undefined
  let verb: string | undefined
  if (r.status === 'Approved') { name = r.approvedByName; verb = 'Aprobada' }
  else if (r.status === 'Rejected') { name = r.rejectedByName; verb = 'Rechazada' }
  else if (r.status === 'Cancelled') { name = r.cancelledByName; verb = 'Cancelada' }
  if (!name) return null
  return (
    <span className={classNames('block text-[11px] text-ink2/60 truncate', compact ? 'mt-0.5 max-w-[180px]' : 'mt-1 max-w-[220px]')} title={`${verb} por ${name}`}>
      por <span className="font-medium text-ink2">{name}</span>
    </span>
  )
}

// Acciones por reserva individual (pendiente → aprobar/rechazar; aprobada futura → cancelar)
function RowActions({ r, onApprove, onReject, onCancel, compact }: {
  r: ReservationDto
  onApprove: (r: ReservationDto) => void
  onReject: (r: ReservationDto) => void
  onCancel: (r: ReservationDto) => void
  compact?: boolean
}) {
  if (r.status === 'Pending') {
    return (
      <div className="flex gap-1.5 justify-end">
        <button onClick={() => onApprove(r)} className="flex items-center gap-1.5 px-2.5 py-1.5 text-xs font-medium text-brand-700 bg-brand-50 hover:bg-brand-100 border border-brand-200 rounded-lg transition-colors cursor-pointer">
          <Check className="h-3.5 w-3.5" /> {compact ? '' : 'Aprobar'}
        </button>
        <button onClick={() => onReject(r)} className="flex items-center gap-1.5 px-2.5 py-1.5 text-xs font-medium text-danger bg-danger/5 hover:bg-danger/10 border border-danger/20 rounded-lg transition-colors cursor-pointer">
          <X className="h-3.5 w-3.5" /> {compact ? '' : 'Rechazar'}
        </button>
      </div>
    )
  }
  if (r.status === 'Approved' && isFuture(r.startDateTime)) {
    return (
      <div className="flex justify-end">
        <button onClick={() => onCancel(r)} className="flex items-center gap-1.5 px-2.5 py-1.5 text-xs font-medium text-ink2 bg-bg hover:text-danger hover:bg-danger/5 border border-line rounded-lg transition-colors cursor-pointer">
          <X className="h-3.5 w-3.5" /> {compact ? '' : 'Cancelar'}
        </button>
      </div>
    )
  }
  return null
}

// Fila de serie periódica: resumen colapsado + ocurrencias al expandir.
function GroupRow({ entry, onApprove, onReject, onCancel, onApproveAll, onRejectAll }: {
  entry: ReservationGroupDto
  onApprove: (r: ReservationDto) => void
  onReject: (r: ReservationDto) => void
  onCancel: (r: ReservationDto) => void
  onApproveAll: (e: ReservationGroupDto) => void
  onRejectAll: (e: ReservationGroupDto) => void
}) {
  const [open, setOpen] = useState(false)
  const groupId = entry.recurrenceGroupId ?? ''

  const { data: occurrences = [], isLoading } = useQuery({
    queryKey: qk.reservations.groupOccurrences(groupId),
    queryFn: () => reservationsApi.getGroupOccurrences(groupId).then(r => r.data.data ?? []),
    enabled: open && !!groupId,
    staleTime: staleTimes.adminReservations,
  })

  const weekday = formatDate(entry.firstStart, 'EEEE')

  return (
    <>
      <tr
        onClick={() => setOpen(o => !o)}
        className="border-b border-line/60 hover:bg-bg/50 transition-colors cursor-pointer"
      >
        <td className="pl-5 pr-1 py-3.5 w-8">
          <ChevronDown className={classNames('h-4 w-4 text-ink2/60 transition-transform', !open && '-rotate-90')} />
        </td>
        <td className="px-3 py-3.5">
          <p className="font-medium text-ink flex items-center gap-1.5">
            <Repeat className="h-3.5 w-3.5 text-brand-600 shrink-0" /> {entry.userFullName}
          </p>
          <p className="text-xs text-ink2/60 mt-0.5 truncate max-w-[200px]">{entry.purpose}</p>
        </td>
        <td className="px-3 py-3.5 font-medium text-ink2">{entry.roomName}</td>
        <td className="px-3 py-3.5 text-ink2 whitespace-nowrap">
          <span className="capitalize font-medium text-ink">Cada {weekday}</span>
          <span className="block text-xs text-ink2/60 font-mono tabular-nums">
            {formatDate(entry.firstStart, 'd MMM')} – {formatDate(entry.lastStart, 'd MMM yyyy')} · {entry.occurrenceCount} citas
          </span>
        </td>
        <td className="px-3 py-3.5">
          <div className="flex flex-wrap gap-1">
            {entry.pendingCount > 0 && <span className="inline-flex items-center rounded-full bg-amber-50 px-2 py-0.5 text-[11px] font-medium text-amber-800 ring-1 ring-inset ring-amber-200 tabular-nums">{entry.pendingCount} pend.</span>}
            {entry.approvedCount > 0 && <span className="inline-flex items-center rounded-full bg-brand-50 px-2 py-0.5 text-[11px] font-medium text-brand-700 ring-1 ring-inset ring-brand-200 tabular-nums">{entry.approvedCount} aprob.</span>}
            {entry.rejectedCount > 0 && <span className="inline-flex items-center rounded-full bg-red-50 px-2 py-0.5 text-[11px] font-medium text-red-800 ring-1 ring-inset ring-red-200 tabular-nums">{entry.rejectedCount} rech.</span>}
            {entry.cancelledCount > 0 && <span className="inline-flex items-center rounded-full bg-slate-50 px-2 py-0.5 text-[11px] font-medium text-slate-600 ring-1 ring-inset ring-slate-200 tabular-nums">{entry.cancelledCount} canc.</span>}
          </div>
        </td>
        <td className="px-5 py-3.5">
          {entry.pendingCount > 0 && (
            <div className="flex gap-1.5 justify-end" onClick={e => e.stopPropagation()}>
              <button onClick={() => onApproveAll(entry)} className="flex items-center gap-1.5 px-2.5 py-1.5 text-xs font-medium text-white bg-brand-600 hover:bg-brand-700 rounded-lg transition-colors cursor-pointer shadow-sh1">
                <CheckCheck className="h-3.5 w-3.5" /> Aprobar todas
              </button>
              <button onClick={() => onRejectAll(entry)} className="flex items-center gap-1.5 px-2.5 py-1.5 text-xs font-medium text-danger bg-danger/5 hover:bg-danger/10 border border-danger/20 rounded-lg transition-colors cursor-pointer">
                <X className="h-3.5 w-3.5" /> Rechazar todas
              </button>
            </div>
          )}
        </td>
      </tr>

      {open && (
        <tr>
          <td colSpan={6} className="p-0 bg-bg/40 border-b border-line">
            <div className="px-6 py-2">
              {isLoading ? (
                <div className="flex justify-center py-6"><Spinner /></div>
              ) : (
                <div className="divide-y divide-line/50">
                  {occurrences.map(o => (
                    <div key={o.id} className="flex items-center gap-3 py-2">
                      <span className="font-mono text-[13px] text-ink capitalize w-40 shrink-0">{formatDate(o.startDateTime, 'EEE d MMM yyyy')}</span>
                      <span className="font-mono text-[13px] text-ink2 tabular-nums w-28 shrink-0">{formatTime(o.startDateTime)}–{formatTime(o.endDateTime)}</span>
                      <div className="flex flex-col">
                        <Badge status={o.status} />
                        <ActionByLabel r={o} compact />
                      </div>
                      <div className="ml-auto">
                        <RowActions r={o} onApprove={onApprove} onReject={onReject} onCancel={onCancel} />
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </td>
        </tr>
      )}
    </>
  )
}

export default function AdminReservationsPage() {
  const qc = useQueryClient()
  const [page, setPage] = useState(1)
  const [activeTab, setActiveTab] = useState('')
  const [userId, setUserId] = useState('')
  const [roomId, setRoomId] = useState('')
  const [sortDescending, setSortDescending] = useState(true)
  const [approveTarget, setApproveTarget] = useState<ReservationDto | null>(null)
  const [rejectTarget, setRejectTarget] = useState<ReservationDto | null>(null)
  const [cancelTarget, setCancelTarget] = useState<ReservationDto | null>(null)
  const [bulkRejectTarget, setBulkRejectTarget] = useState<ReservationGroupDto | null>(null)

  const { data, isLoading } = useQuery({
    queryKey: qk.reservations.audit(page, activeTab, userId, roomId, sortDescending),
    queryFn: () => reservationsApi.getAudit({
      page, pageSize: 20,
      status: activeTab || undefined,
      userId: userId || undefined,
      roomId: roomId || undefined,
      sortDescending,
    }).then(r => r.data.data!),
    staleTime: staleTimes.adminReservations,
  })

  const { data: users = [] } = useQuery({
    queryKey: qk.users.list,
    queryFn: () => usersApi.getAll({ pageSize: 100 }).then(r => r.data.data?.items ?? []),
    staleTime: staleTimes.users,
  })
  const { data: rooms = [] } = useQuery({
    queryKey: qk.rooms.list,
    queryFn: () => roomsApi.getAll({ pageSize: 100 }).then(r => r.data.data?.items ?? []),
    staleTime: staleTimes.roomsList,
  })

  const approveMutation = useMutation({
    mutationFn: ({ id, comment }: { id: string; comment?: string }) => reservationsApi.approve(id, { comment }),
    onSuccess: () => { invalidate.reservations(qc); toast.success('Reserva aprobada'); setApproveTarget(null) },
    onError: (err) => toast.error(extractApiError(err)),
  })
  const rejectMutation = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) => reservationsApi.reject(id, { reason }),
    onSuccess: () => { invalidate.reservations(qc); toast.success('Reserva rechazada'); setRejectTarget(null) },
    onError: (err) => toast.error(extractApiError(err)),
  })
  const approveGroupMutation = useMutation({
    mutationFn: (groupId: string) => reservationsApi.approveGroup(groupId),
    onSuccess: (res) => {
      invalidate.reservations(qc)
      const { affected, skipped } = res.data.data ?? { affected: 0, skipped: 0 }
      toast.success(`${affected} reserva(s) aprobada(s)${skipped > 0 ? ` · ${skipped} omitida(s) por conflicto` : ''}`)
    },
    onError: (err) => toast.error(extractApiError(err)),
  })
  const rejectGroupMutation = useMutation({
    mutationFn: ({ groupId, reason }: { groupId: string; reason: string }) => reservationsApi.rejectGroup(groupId, { reason }),
    onSuccess: (res) => {
      invalidate.reservations(qc)
      toast.success(`${res.data.data?.affected ?? 0} reserva(s) rechazada(s)`)
      setBulkRejectTarget(null)
    },
    onError: (err) => toast.error(extractApiError(err)),
  })

  const ApproveForm = () => {
    const { register, handleSubmit } = useForm({ resolver: zodResolver(approveSchema) })
    return (
      <form onSubmit={handleSubmit(d => approveMutation.mutate({ id: approveTarget!.id, comment: d.comment }))} className="space-y-4">
        <p className="text-sm text-ink2">Aprobando reserva de <strong className="text-ink">{approveTarget?.userFullName}</strong> en <strong className="text-ink">{approveTarget?.roomName}</strong></p>
        <Textarea label="Comentario (opcional)" rows={2} {...register('comment')} />
        <div className="flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={() => setApproveTarget(null)}>Cancelar</Button>
          <Button type="submit" loading={approveMutation.isPending}>Aprobar</Button>
        </div>
      </form>
    )
  }
  const RejectForm = () => {
    const { register, handleSubmit, formState: { errors } } = useForm({ resolver: zodResolver(rejectSchema) })
    return (
      <form onSubmit={handleSubmit(d => rejectMutation.mutate({ id: rejectTarget!.id, reason: d.reason }))} className="space-y-4">
        <p className="text-sm text-ink2">Rechazando reserva de <strong className="text-ink">{rejectTarget?.userFullName}</strong> en <strong className="text-ink">{rejectTarget?.roomName}</strong></p>
        <Textarea label="Motivo de rechazo" required error={errors.reason?.message} {...register('reason')} />
        <div className="flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={() => setRejectTarget(null)}>Cancelar</Button>
          <Button type="submit" variant="danger" loading={rejectMutation.isPending}>Rechazar</Button>
        </div>
      </form>
    )
  }
  const BulkRejectForm = () => {
    const { register, handleSubmit, formState: { errors } } = useForm({ resolver: zodResolver(rejectSchema) })
    return (
      <form onSubmit={handleSubmit(d => rejectGroupMutation.mutate({ groupId: bulkRejectTarget!.recurrenceGroupId!, reason: d.reason }))} className="space-y-4">
        <p className="text-sm text-ink2">
          Rechazar las <strong className="text-ink">{bulkRejectTarget?.pendingCount}</strong> reserva(s) pendiente(s) de la serie de
          <strong className="text-ink"> {bulkRejectTarget?.userFullName}</strong> en <strong className="text-ink">{bulkRejectTarget?.roomName}</strong>.
        </p>
        <Textarea label="Motivo de rechazo" required error={errors.reason?.message} {...register('reason')} />
        <div className="flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={() => setBulkRejectTarget(null)}>Cancelar</Button>
          <Button type="submit" variant="danger" loading={rejectGroupMutation.isPending}>Rechazar todas</Button>
        </div>
      </form>
    )
  }

  const total = data?.totalCount ?? 0
  const totalPages = data?.totalPages ?? 1
  const hasFilters = !!userId || !!roomId || !!activeTab
  const clearFilters = () => { setUserId(''); setRoomId(''); setActiveTab(''); setPage(1) }

  return (
    <div className="p-6 max-w-7xl mx-auto">
      {/* Header */}
      <div className="mb-6 flex items-center gap-3">
        <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-brand-600 shadow-sh1 shrink-0">
          <ShieldCheck className="h-5 w-5 text-white" strokeWidth={1.75} />
        </div>
        <div>
          <p className="text-[11px] font-medium text-ink2/70 tracking-wide">Administración / Auditoría</p>
          <h1 className="text-2xl font-bold text-ink leading-tight">
            Auditoría de Reservas{' '}
            <span className="font-mono font-normal text-ink2/60 text-lg">· {total}</span>
          </h1>
        </div>
      </div>

      <div className="bg-paper rounded-[10px] border border-line shadow-sh1 overflow-hidden">
        {/* Tabs */}
        <div className="flex px-5 border-b border-line overflow-x-auto">
          {TABS.map(tab => (
            <button
              key={tab.key}
              onClick={() => { setActiveTab(tab.key); setPage(1) }}
              className={classNames(
                'px-4 py-3 text-sm font-medium border-b-2 transition-colors whitespace-nowrap cursor-pointer',
                activeTab === tab.key ? 'border-brand-600 text-brand-700' : 'border-transparent text-ink2 hover:text-ink',
              )}
            >
              {tab.label}
            </button>
          ))}
        </div>

        {/* Filter bar */}
        <div className="flex flex-wrap items-center gap-2.5 px-5 py-3 border-b border-line bg-bg/40">
          <span className="inline-flex items-center gap-1.5 text-[11px] font-semibold text-ink2/70 uppercase tracking-wider mr-1">
            <ListFilter className="h-3.5 w-3.5" /> Filtros
          </span>
          <div className="relative">
            <User className="pointer-events-none absolute left-2.5 top-1/2 -translate-y-1/2 h-4 w-4 text-ink2/60" />
            <select value={userId} onChange={e => { setUserId(e.target.value); setPage(1) }} className={SELECT_CLS} aria-label="Filtrar por usuario">
              <option value="">Todos los usuarios</option>
              {(users as UserDto[]).map(u => <option key={u.id} value={u.id}>{u.fullName}</option>)}
            </select>
          </div>
          <div className="relative">
            <MapPin className="pointer-events-none absolute left-2.5 top-1/2 -translate-y-1/2 h-4 w-4 text-ink2/60" />
            <select value={roomId} onChange={e => { setRoomId(e.target.value); setPage(1) }} className={SELECT_CLS} aria-label="Filtrar por sala">
              <option value="">Todas las salas</option>
              {(rooms as RoomDto[]).map(r => <option key={r.id} value={r.id}>{r.name}</option>)}
            </select>
          </div>
          <div className="flex items-center rounded-lg border border-line bg-paper p-0.5 ml-auto">
            <button onClick={() => { setSortDescending(true); setPage(1) }} className={classNames('flex items-center gap-1.5 rounded-md px-2.5 h-8 text-[13px] font-medium transition-colors cursor-pointer', sortDescending ? 'bg-brand-600 text-white shadow-sh1' : 'text-ink2 hover:text-ink')} title="Más nuevas primero">
              <ArrowDownWideNarrow className="h-3.5 w-3.5" /> Más nuevas
            </button>
            <button onClick={() => { setSortDescending(false); setPage(1) }} className={classNames('flex items-center gap-1.5 rounded-md px-2.5 h-8 text-[13px] font-medium transition-colors cursor-pointer', !sortDescending ? 'bg-brand-600 text-white shadow-sh1' : 'text-ink2 hover:text-ink')} title="Más viejas primero">
              <ArrowUpWideNarrow className="h-3.5 w-3.5" /> Más viejas
            </button>
          </div>
          {hasFilters && (
            <button onClick={clearFilters} className="inline-flex items-center gap-1.5 h-8 px-2.5 rounded-lg text-[13px] font-medium text-ink2 hover:text-danger hover:bg-danger/5 transition-colors cursor-pointer" title="Limpiar filtros">
              <RotateCcw className="h-3.5 w-3.5" /> Limpiar
            </button>
          )}
        </div>

        {/* Table */}
        {isLoading ? (
          <div className="flex justify-center py-20"><Spinner /></div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm min-w-[760px]">
              <thead>
                <tr className="border-b border-line bg-bg/30">
                  <th className="w-8" />
                  <th className="text-left px-3 py-3 text-[11px] font-semibold text-ink2/70 uppercase tracking-wider">Usuario / Reserva</th>
                  <th className="text-left px-3 py-3 text-[11px] font-semibold text-ink2/70 uppercase tracking-wider">Sala</th>
                  <th className="text-left px-3 py-3 text-[11px] font-semibold text-ink2/70 uppercase tracking-wider">Fecha</th>
                  <th className="text-left px-3 py-3 text-[11px] font-semibold text-ink2/70 uppercase tracking-wider">Estado</th>
                  <th className="px-5 py-3" />
                </tr>
              </thead>
              <tbody>
                {data?.items.map((entry) => (
                  entry.isRecurring ? (
                    <GroupRow
                      key={entry.groupKey}
                      entry={entry}
                      onApprove={setApproveTarget}
                      onReject={setRejectTarget}
                      onCancel={setCancelTarget}
                      onApproveAll={(e) => approveGroupMutation.mutate(e.recurrenceGroupId!)}
                      onRejectAll={setBulkRejectTarget}
                    />
                  ) : entry.single ? (
                    <tr key={entry.groupKey} className="border-b border-line/60 hover:bg-bg/50 transition-colors">
                      <td className="w-8" />
                      <td className="px-3 py-3.5">
                        <p className="font-medium text-ink">{entry.single.userFullName}</p>
                        <p className="text-xs text-ink2/60 mt-0.5 truncate max-w-[200px]">{entry.single.purpose}</p>
                      </td>
                      <td className="px-3 py-3.5 font-medium text-ink2">{entry.single.roomName}</td>
                      <td className="px-3 py-3.5 text-ink2 whitespace-nowrap">
                        <span className="capitalize">{formatDate(entry.single.startDateTime, 'EEE d MMM yyyy')}</span>
                        <span className="block text-xs text-ink2/60 font-mono tabular-nums">{formatTime(entry.single.startDateTime)}–{formatTime(entry.single.endDateTime)}</span>
                      </td>
                      <td className="px-3 py-3.5">
                        <Badge status={entry.single.status} />
                        <ActionByLabel r={entry.single} />
                      </td>
                      <td className="px-5 py-3.5">
                        <RowActions r={entry.single} onApprove={setApproveTarget} onReject={setRejectTarget} onCancel={setCancelTarget} />
                      </td>
                    </tr>
                  ) : null
                ))}
                {!data?.items.length && (
                  <tr>
                    <td colSpan={6} className="px-5 py-16 text-center text-ink2/60 text-sm">
                      No hay reservas con los filtros seleccionados
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}

        {/* Pagination */}
        <div className="flex items-center justify-between px-5 py-3 border-t border-line bg-bg/40">
          <span className="text-sm text-ink2/70">
            {total} entradas · página <span className="font-mono tabular-nums">{page}</span> de <span className="font-mono tabular-nums">{totalPages}</span>
          </span>
          <div className="flex gap-1">
            <button disabled={page === 1} onClick={() => setPage(p => p - 1)} className="flex items-center gap-1 px-3 py-1.5 text-sm text-ink2 border border-line rounded-md hover:bg-paper hover:text-ink disabled:opacity-40 disabled:cursor-not-allowed bg-paper transition-colors cursor-pointer">
              <ChevronLeft className="h-3.5 w-3.5" /> Anterior
            </button>
            <button disabled={page === totalPages} onClick={() => setPage(p => p + 1)} className="flex items-center gap-1 px-3 py-1.5 text-sm text-ink2 border border-line rounded-md hover:bg-paper hover:text-ink disabled:opacity-40 disabled:cursor-not-allowed bg-paper transition-colors cursor-pointer">
              Siguiente <ChevronRight className="h-3.5 w-3.5" />
            </button>
          </div>
        </div>
      </div>

      <Modal open={!!approveTarget} onClose={() => setApproveTarget(null)} title="Aprobar Reserva" size="sm"><ApproveForm /></Modal>
      <Modal open={!!rejectTarget} onClose={() => setRejectTarget(null)} title="Rechazar Reserva" size="sm"><RejectForm /></Modal>
      <Modal open={!!bulkRejectTarget} onClose={() => setBulkRejectTarget(null)} title="Rechazar serie completa" size="sm"><BulkRejectForm /></Modal>
      <CancelReservationModal reservation={cancelTarget} onClose={() => setCancelTarget(null)} />
    </div>
  )
}
