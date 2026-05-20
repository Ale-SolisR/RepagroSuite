import { useState, useMemo } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Plus, Pencil, Trash2, CalendarClock, ChevronLeft, ChevronRight, Search,
  DoorOpen, Users, MapPin, Layers, Sparkles, LayoutGrid, List,
} from 'lucide-react'
import { roomsApi } from '@/api/rooms'
import { useAuthStore } from '@/store/authStore'
import { extractApiError, classNames } from '@/utils'
import { useRealtime } from '@/hooks/useRealtime'
import { qk, staleTimes, invalidate } from '@/lib/queryKeys'
import Button from '@/components/ui/Button'
import Modal from '@/components/ui/Modal'
import RoomForm from './RoomForm'
import RoomAvailabilityPanel from './RoomAvailabilityPanel'
import toast from 'react-hot-toast'
import type { RoomDto } from '@/types'

// ─── Status meta ──────────────────────────────────────────────────────────────
const STATUS_META: Record<string, { pill: string; dot: string; label: string }> = {
  Available:   { pill: 'bg-emerald-50 text-emerald-700 ring-emerald-200', dot: 'bg-emerald-500', label: 'Disponible' },
  Maintenance: { pill: 'bg-amber-50 text-amber-700 ring-amber-200',       dot: 'bg-amber-500',   label: 'Mantenimiento' },
  Inactive:    { pill: 'bg-slate-100 text-slate-600 ring-slate-200',      dot: 'bg-slate-400',   label: 'Inactiva' },
  Occupied:    { pill: 'bg-blue-50 text-blue-700 ring-blue-200',          dot: 'bg-blue-500',    label: 'Ocupada' },
}

// ─── Utility: derivar palette consistente desde color hex ────────────────────
function tintFromColor(hex: string | undefined, alpha = 0.15) {
  if (!hex) return 'rgba(22, 163, 74, 0.15)'
  const m = hex.replace('#', '').match(/.{1,2}/g)
  if (!m || m.length !== 3) return 'rgba(22, 163, 74, 0.15)'
  const [r, g, b] = m.map(x => parseInt(x, 16))
  return `rgba(${r}, ${g}, ${b}, ${alpha})`
}

// ─── Card de sala ─────────────────────────────────────────────────────────────
function RoomCard({
  room, hasPermission, onEdit, onDelete, onAvailability,
}: {
  room: RoomDto
  hasPermission: (perm: string) => boolean
  onEdit: () => void
  onDelete: () => void
  onAvailability: () => void
}) {
  const status = STATUS_META[room.status] ?? STATUS_META.Inactive
  const color = room.color || '#16a34a'

  return (
    <div className="group relative bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden transition-all duration-200 hover:shadow-lg hover:border-gray-300 hover:-translate-y-0.5">
      {/* Header con color o imagen */}
      <div className="relative h-32 overflow-hidden">
        {room.imageUrl ? (
          <img src={room.imageUrl} alt={room.name} className="absolute inset-0 h-full w-full object-cover" />
        ) : (
          <div
            className="absolute inset-0"
            style={{ background: `linear-gradient(135deg, ${color} 0%, ${color}dd 60%, ${color}99 100%)` }}
          />
        )}
        {/* Patrón sutil */}
        <div className="absolute inset-0 opacity-30 pointer-events-none"
          style={{
            backgroundImage: 'radial-gradient(circle at 20% 30%, rgba(255,255,255,.3) 0%, transparent 40%)',
          }}
        />
        {/* Icono central */}
        <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
          <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-white/15 backdrop-blur-sm ring-1 ring-white/30">
            <DoorOpen className="h-7 w-7 text-white" strokeWidth={1.5} />
          </div>
        </div>
        {/* Status badge esquina superior derecha */}
        <div className="absolute top-2.5 right-2.5">
          <span className={classNames(
            'inline-flex items-center gap-1 rounded-full bg-white/95 backdrop-blur-sm px-2 py-0.5 text-[11px] font-medium ring-1 ring-inset shadow-sm',
            status.pill,
          )}>
            <span className={`h-1.5 w-1.5 rounded-full ${status.dot}`} />
            {status.label}
          </span>
        </div>
        {/* Código en esquina inferior izquierda */}
        <div className="absolute bottom-2.5 left-3">
          <span className="inline-flex items-center rounded-md bg-black/30 backdrop-blur-sm px-1.5 py-0.5 text-[10px] font-mono font-medium text-white tracking-wider">
            {room.code}
          </span>
        </div>
      </div>

      {/* Body */}
      <div className="p-4">
        <h3 className="text-[15px] font-semibold text-gray-900 leading-tight truncate">{room.name}</h3>

        {/* Meta info */}
        <div className="mt-2.5 flex flex-wrap items-center gap-x-3.5 gap-y-1.5 text-[12.5px] text-gray-600">
          <span className="inline-flex items-center gap-1">
            <Users className="h-3.5 w-3.5 text-gray-400" strokeWidth={1.75} />
            {room.capacity} {room.capacity === 1 ? 'persona' : 'personas'}
          </span>
          {room.location && (
            <span className="inline-flex items-center gap-1 truncate">
              <MapPin className="h-3.5 w-3.5 text-gray-400 shrink-0" strokeWidth={1.75} />
              <span className="truncate">{room.location}</span>
            </span>
          )}
          {room.floor && (
            <span className="inline-flex items-center gap-1">
              <Layers className="h-3.5 w-3.5 text-gray-400" strokeWidth={1.75} />
              Piso {room.floor}
            </span>
          )}
        </div>

        {/* Features */}
        {room.features.length > 0 && (
          <div className="mt-3 flex flex-wrap gap-1">
            {room.features.slice(0, 4).map(f => (
              <span
                key={f.id}
                className="inline-flex items-center rounded-full px-2 py-0.5 text-[11px] font-medium ring-1 ring-inset"
                style={{
                  background: tintFromColor(color, 0.1),
                  color: color,
                  ['--tw-ring-color' as any]: tintFromColor(color, 0.25),
                }}
              >
                {f.name}
              </span>
            ))}
            {room.features.length > 4 && (
              <span className="text-[11px] text-gray-400 self-center">+{room.features.length - 4}</span>
            )}
          </div>
        )}

        {/* Description (si existe) — colapsada */}
        {room.description && (
          <p className="mt-2.5 text-[12px] text-gray-500 line-clamp-2 leading-relaxed">
            {room.description}
          </p>
        )}

        {/* Actions */}
        <div className="mt-4 flex items-center gap-1.5 pt-3 border-t border-gray-100">
          {hasPermission('Rooms.Availability.Manage') && (
            <button
              onClick={onAvailability}
              className="flex-1 inline-flex items-center justify-center gap-1.5 rounded-lg px-2.5 py-1.5 text-[12px] font-medium text-gray-700 bg-gray-50 hover:bg-gray-100 border border-gray-200 transition-colors"
            >
              <CalendarClock className="h-3.5 w-3.5" strokeWidth={1.75} />
              Disponibilidad
            </button>
          )}
          {hasPermission('Rooms.Update') && (
            <button
              onClick={onEdit}
              title="Editar sala"
              className="flex items-center justify-center h-8 w-8 rounded-lg text-gray-600 bg-gray-50 hover:bg-gray-100 border border-gray-200 transition-colors"
            >
              <Pencil className="h-3.5 w-3.5" strokeWidth={1.75} />
            </button>
          )}
          {hasPermission('Rooms.Delete') && (
            <button
              onClick={onDelete}
              title="Eliminar sala"
              className="flex items-center justify-center h-8 w-8 rounded-lg text-red-600 bg-red-50 hover:bg-red-100 border border-red-200 transition-colors"
            >
              <Trash2 className="h-3.5 w-3.5" strokeWidth={1.75} />
            </button>
          )}
        </div>
      </div>
    </div>
  )
}

// ─── Row para vista lista ────────────────────────────────────────────────────
function RoomRow({
  room, hasPermission, onEdit, onDelete, onAvailability,
}: {
  room: RoomDto
  hasPermission: (perm: string) => boolean
  onEdit: () => void
  onDelete: () => void
  onAvailability: () => void
}) {
  const status = STATUS_META[room.status] ?? STATUS_META.Inactive
  const color = room.color || '#16a34a'

  return (
    <div className="flex items-center gap-4 px-4 py-3 hover:bg-gray-50 transition-colors group">
      <div
        className="flex h-10 w-10 items-center justify-center rounded-lg shrink-0"
        style={{ background: `linear-gradient(135deg, ${color}, ${color}cc)` }}
      >
        <DoorOpen className="h-4 w-4 text-white" strokeWidth={1.75} />
      </div>
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2">
          <p className="font-semibold text-gray-900 truncate">{room.name}</p>
          <span className="text-[11px] font-mono text-gray-400">{room.code}</span>
        </div>
        <div className="flex items-center gap-3 text-[12px] text-gray-500 mt-0.5">
          <span className="inline-flex items-center gap-1">
            <Users className="h-3 w-3" /> {room.capacity}
          </span>
          {room.location && (
            <span className="inline-flex items-center gap-1 truncate">
              <MapPin className="h-3 w-3 shrink-0" /> {room.location}
            </span>
          )}
        </div>
      </div>
      <span className={classNames(
        'inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[11px] font-medium ring-1 ring-inset shrink-0',
        status.pill,
      )}>
        <span className={`h-1.5 w-1.5 rounded-full ${status.dot}`} />
        {status.label}
      </span>
      <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
        {hasPermission('Rooms.Availability.Manage') && (
          <button onClick={onAvailability} className="flex items-center justify-center h-7 w-7 rounded text-gray-500 hover:bg-gray-100" title="Disponibilidad">
            <CalendarClock className="h-3.5 w-3.5" />
          </button>
        )}
        {hasPermission('Rooms.Update') && (
          <button onClick={onEdit} className="flex items-center justify-center h-7 w-7 rounded text-gray-500 hover:bg-gray-100" title="Editar">
            <Pencil className="h-3.5 w-3.5" />
          </button>
        )}
        {hasPermission('Rooms.Delete') && (
          <button onClick={onDelete} className="flex items-center justify-center h-7 w-7 rounded text-red-500 hover:bg-red-50" title="Eliminar">
            <Trash2 className="h-3.5 w-3.5" />
          </button>
        )}
      </div>
    </div>
  )
}

// ─── Skeleton card ───────────────────────────────────────────────────────────
function CardSkeleton() {
  return (
    <div className="bg-white rounded-2xl border border-gray-200 overflow-hidden">
      <div className="h-32 bg-gray-100 animate-pulse" />
      <div className="p-4 space-y-3">
        <div className="h-4 bg-gray-100 rounded animate-pulse w-3/4" />
        <div className="h-3 bg-gray-100 rounded animate-pulse w-1/2" />
        <div className="h-8 bg-gray-50 rounded animate-pulse" />
      </div>
    </div>
  )
}

// ─── Página principal ────────────────────────────────────────────────────────
export default function RoomsPage() {
  const qc = useQueryClient()
  const { hasPermission } = useAuthStore()
  const [editRoom, setEditRoom] = useState<RoomDto | null>(null)
  const [showCreate, setShowCreate] = useState(false)
  const [availRoom, setAvailRoom] = useState<RoomDto | null>(null)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<string>('all')
  const [view, setView] = useState<'grid' | 'list'>('grid')

  const { data, isLoading } = useQuery({
    queryKey: qk.rooms.paged(page, 20),
    queryFn: () => roomsApi.getAll({ page, pageSize: 20 }).then(r => r.data.data!),
    staleTime: staleTimes.rooms,
  })

  // Si otro admin crea/edita/borra una sala, refrescamos sin reload manual.
  useRealtime(event => {
    if (event.type === 'room.changed') {
      invalidate.rooms(qc)
    }
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => roomsApi.delete(id),
    onSuccess: () => { invalidate.rooms(qc); toast.success('Sala eliminada') },
    onError: (err) => toast.error(extractApiError(err)),
  })

  async function handleDelete(room: RoomDto) {
    if (!confirm(`¿Eliminar la sala "${room.name}"? Esta acción no se puede deshacer.`)) return
    deleteMutation.mutate(room.id)
  }

  // Filtros locales en la página actual
  const filteredItems = useMemo(() => {
    const items = data?.items ?? []
    return items.filter(r => {
      if (statusFilter !== 'all' && r.status !== statusFilter) return false
      if (search) {
        const s = search.toLowerCase()
        const matches = r.name.toLowerCase().includes(s) ||
          r.code.toLowerCase().includes(s) ||
          (r.location?.toLowerCase().includes(s) ?? false)
        if (!matches) return false
      }
      return true
    })
  }, [data?.items, statusFilter, search])

  const stats = useMemo(() => {
    const items = data?.items ?? []
    return {
      total: items.length,
      available: items.filter(r => r.status === 'Available').length,
      maintenance: items.filter(r => r.status === 'Maintenance').length,
      inactive: items.filter(r => r.status === 'Inactive').length,
    }
  }, [data?.items])

  const total = data?.totalCount ?? 0
  const totalPages = data?.totalPages ?? 1

  return (
    <div className="p-6 max-w-7xl mx-auto">
      {/* ─── Header ─── */}
      <div className="mb-6 flex flex-wrap items-start justify-between gap-3">
        <div className="flex items-center gap-3">
          <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-green-50 ring-1 ring-green-100">
            <DoorOpen className="h-5 w-5 text-green-700" strokeWidth={1.75} />
          </div>
          <div>
            <p className="text-xs text-gray-400 tracking-wide mb-0.5">Salas / Catálogo</p>
            <h1 className="text-[22px] font-bold text-gray-900 leading-tight">
              Catálogo de salas
            </h1>
          </div>
        </div>
        {hasPermission('Rooms.Create') && (
          <Button onClick={() => setShowCreate(true)}>
            <Plus className="h-4 w-4" /> Nueva sala
          </Button>
        )}
      </div>

      {/* ─── Stats strip ─── */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 mb-5">
        <StatCard label="Total" value={stats.total}      tone="neutral"  icon={DoorOpen} />
        <StatCard label="Disponibles" value={stats.available} tone="success"  icon={Sparkles} />
        <StatCard label="En mantenimiento" value={stats.maintenance} tone="warning"  icon={CalendarClock} />
        <StatCard label="Inactivas" value={stats.inactive} tone="muted" icon={Layers} />
      </div>

      {/* ─── Toolbar: filtros + búsqueda + vista ─── */}
      <div className="mb-5 flex flex-wrap items-center gap-3">
        {/* Search */}
        <div className="relative flex-1 min-w-[200px] max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-gray-400 pointer-events-none" />
          <input
            type="text"
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder="Buscar por nombre, código o ubicación..."
            className="w-full pl-9 pr-3 py-2 text-sm border border-gray-200 rounded-lg bg-white focus:outline-none focus:ring-2 focus:ring-green-600/20 focus:border-green-400 placeholder:text-gray-400"
          />
        </div>

        {/* Status filter chips */}
        <div className="flex items-center gap-1 rounded-lg bg-white border border-gray-200 p-1">
          {[
            { k: 'all',         l: 'Todos' },
            { k: 'Available',   l: 'Disponibles' },
            { k: 'Maintenance', l: 'Mantenimiento' },
            { k: 'Inactive',    l: 'Inactivas' },
          ].map(opt => (
            <button
              key={opt.k}
              onClick={() => setStatusFilter(opt.k)}
              className={classNames(
                'rounded px-2.5 py-1 text-[12px] font-medium transition-colors',
                statusFilter === opt.k
                  ? 'bg-green-50 text-green-800'
                  : 'text-gray-600 hover:bg-gray-50',
              )}
            >
              {opt.l}
            </button>
          ))}
        </div>

        {/* View switcher */}
        <div className="ml-auto flex items-center gap-1 rounded-lg bg-white border border-gray-200 p-1">
          <button
            onClick={() => setView('grid')}
            title="Vista de tarjetas"
            className={classNames(
              'flex items-center justify-center h-7 w-7 rounded transition-colors',
              view === 'grid' ? 'bg-gray-100 text-gray-900' : 'text-gray-400 hover:text-gray-600',
            )}
          >
            <LayoutGrid className="h-3.5 w-3.5" />
          </button>
          <button
            onClick={() => setView('list')}
            title="Vista de lista"
            className={classNames(
              'flex items-center justify-center h-7 w-7 rounded transition-colors',
              view === 'list' ? 'bg-gray-100 text-gray-900' : 'text-gray-400 hover:text-gray-600',
            )}
          >
            <List className="h-3.5 w-3.5" />
          </button>
        </div>
      </div>

      {/* ─── Content ─── */}
      {isLoading ? (
        view === 'grid' ? (
          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
            {Array.from({ length: 6 }).map((_, i) => <CardSkeleton key={i} />)}
          </div>
        ) : (
          <div className="bg-white rounded-xl border border-gray-200 divide-y divide-gray-100">
            {Array.from({ length: 5 }).map((_, i) => (
              <div key={i} className="h-16 px-4 flex items-center">
                <div className="h-8 w-8 rounded-lg bg-gray-100 animate-pulse mr-3" />
                <div className="flex-1 space-y-2">
                  <div className="h-3 w-1/3 bg-gray-100 rounded animate-pulse" />
                  <div className="h-2 w-1/4 bg-gray-100 rounded animate-pulse" />
                </div>
              </div>
            ))}
          </div>
        )
      ) : filteredItems.length === 0 ? (
        <div className="bg-white rounded-2xl border border-dashed border-gray-200 p-16 text-center">
          <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-gray-50 mb-3">
            <DoorOpen className="h-5 w-5 text-gray-400" />
          </div>
          <p className="text-sm font-medium text-gray-700">
            {search || statusFilter !== 'all' ? 'No hay salas con esos filtros' : 'No hay salas registradas'}
          </p>
          <p className="text-[12px] text-gray-400 mt-1">
            {search || statusFilter !== 'all' ? 'Prueba ajustando los filtros de búsqueda.' : 'Comienza creando la primera sala del catálogo.'}
          </p>
          {!search && statusFilter === 'all' && hasPermission('Rooms.Create') && (
            <button
              onClick={() => setShowCreate(true)}
              className="mt-4 inline-flex items-center gap-1.5 rounded-lg bg-green-600 text-white px-3.5 py-2 text-sm font-medium hover:bg-green-700 transition-colors"
            >
              <Plus className="h-4 w-4" /> Crear primera sala
            </button>
          )}
        </div>
      ) : view === 'grid' ? (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
          {filteredItems.map((room) => (
            <RoomCard
              key={room.id}
              room={room}
              hasPermission={hasPermission}
              onEdit={() => setEditRoom(room)}
              onDelete={() => handleDelete(room)}
              onAvailability={() => setAvailRoom(room)}
            />
          ))}
        </div>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden divide-y divide-gray-100">
          {filteredItems.map((room) => (
            <RoomRow
              key={room.id}
              room={room}
              hasPermission={hasPermission}
              onEdit={() => setEditRoom(room)}
              onDelete={() => handleDelete(room)}
              onAvailability={() => setAvailRoom(room)}
            />
          ))}
        </div>
      )}

      {/* ─── Pagination ─── */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between mt-6">
          <span className="text-sm text-gray-400">
            {total} salas · página {page} de {totalPages}
          </span>
          <div className="flex gap-1">
            <button
              disabled={page === 1}
              onClick={() => setPage(p => p - 1)}
              className="flex items-center gap-1 px-3 py-1.5 text-sm text-gray-600 border border-gray-200 rounded-lg hover:bg-white disabled:opacity-40 disabled:cursor-not-allowed bg-white transition-colors"
            >
              <ChevronLeft className="h-3.5 w-3.5" /> Anterior
            </button>
            <button
              disabled={page === totalPages}
              onClick={() => setPage(p => p + 1)}
              className="flex items-center gap-1 px-3 py-1.5 text-sm text-gray-600 border border-gray-200 rounded-lg hover:bg-white disabled:opacity-40 disabled:cursor-not-allowed bg-white transition-colors"
            >
              Siguiente <ChevronRight className="h-3.5 w-3.5" />
            </button>
          </div>
        </div>
      )}

      {/* ─── Modales ─── */}
      <Modal
        open={showCreate || !!editRoom}
        onClose={() => { setShowCreate(false); setEditRoom(null) }}
        title={editRoom ? 'Editar sala' : 'Nueva sala'}
        size="2xl"
      >
        <RoomForm room={editRoom ?? undefined} onClose={() => { setShowCreate(false); setEditRoom(null) }} />
      </Modal>

      <Modal
        open={!!availRoom}
        onClose={() => setAvailRoom(null)}
        title={`Disponibilidad — ${availRoom?.name}`}
        size="xl"
      >
        {availRoom && <RoomAvailabilityPanel roomId={availRoom.id} onClose={() => setAvailRoom(null)} />}
      </Modal>
    </div>
  )
}

// ─── StatCard ────────────────────────────────────────────────────────────────
function StatCard({
  label, value, tone, icon: Icon,
}: {
  label: string
  value: number
  tone: 'neutral' | 'success' | 'warning' | 'muted'
  icon: typeof DoorOpen
}) {
  const tones = {
    neutral: { iconBg: 'bg-gray-100',    iconColor: 'text-gray-600',    accent: '' },
    success: { iconBg: 'bg-emerald-100', iconColor: 'text-emerald-600', accent: 'text-emerald-700' },
    warning: { iconBg: 'bg-amber-100',   iconColor: 'text-amber-600',   accent: 'text-amber-700' },
    muted:   { iconBg: 'bg-slate-100',   iconColor: 'text-slate-500',   accent: 'text-slate-600' },
  }
  const t = tones[tone]
  return (
    <div className="flex items-center gap-3 rounded-xl bg-white border border-gray-200 px-4 py-3">
      <div className={`flex h-9 w-9 items-center justify-center rounded-lg shrink-0 ${t.iconBg}`}>
        <Icon className={`h-4 w-4 ${t.iconColor}`} strokeWidth={1.75} />
      </div>
      <div className="min-w-0">
        <p className="text-[10px] font-semibold uppercase tracking-wider text-gray-400">{label}</p>
        <p className={classNames('text-xl font-bold leading-tight tabular-nums', t.accent || 'text-gray-900')}>
          {value}
        </p>
      </div>
    </div>
  )
}
