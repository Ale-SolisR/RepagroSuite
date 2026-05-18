import { useState, useRef, useEffect, useCallback, useMemo } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  startOfWeek, endOfWeek, addWeeks, subWeeks, addDays,
  format, isToday, parseISO, getHours, getMinutes,
  startOfDay, formatISO, startOfMonth, endOfMonth, addMonths, subMonths,
  isSameMonth,
} from 'date-fns'
import { es } from 'date-fns/locale'
import {
  ChevronLeft, ChevronRight, Plus, Clock, MapPin, Users,
  CalendarDays, Filter, CheckCircle2, AlertCircle,
  Sparkles, FileText,
} from 'lucide-react'
import { reservationsApi } from '@/api/reservations'
import { roomsApi } from '@/api/rooms'
import { extractApiError, classNames } from '@/utils'
import { useRealtime } from '@/hooks/useRealtime'
import Modal from '@/components/ui/Modal'
import Button from '@/components/ui/Button'
import toast from 'react-hot-toast'
import type { CalendarEventDto, RoomDto, CreateReservationRequest } from '@/types'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'

// ─── Constants ────────────────────────────────────────────────────────────────
const DAY_START = 7   // 07:00
const DAY_END   = 21  // 21:00
const HOUR_PX   = 60
const TOTAL_H   = (DAY_END - DAY_START) * HOUR_PX
const HOURS     = Array.from({ length: DAY_END - DAY_START }, (_, i) => DAY_START + i)
const DAY_LABELS = ['LUN', 'MAR', 'MIÉ', 'JUE', 'VIE', 'SÁB', 'DOM']
const GUTTER_W  = 72  // ancho columna de horas

// ─── Status meta ──────────────────────────────────────────────────────────────
const STATUS_META: Record<string, { bg: string; ring: string; text: string; dot: string; label: string }> = {
  Approved:  { bg: 'bg-emerald-50', ring: 'ring-emerald-300', text: 'text-emerald-900', dot: 'bg-emerald-500', label: 'Aprobada' },
  Pending:   { bg: 'bg-amber-50',   ring: 'ring-amber-300',   text: 'text-amber-900',   dot: 'bg-amber-500',   label: 'Pendiente' },
  Rejected:  { bg: 'bg-red-50',     ring: 'ring-red-300',     text: 'text-red-900',     dot: 'bg-red-500',     label: 'Rechazada' },
  Cancelled: { bg: 'bg-slate-50',   ring: 'ring-slate-300',   text: 'text-slate-600',   dot: 'bg-slate-400',   label: 'Cancelada' },
}

// ─── Paleta de colores asignada por sala para coherencia visual ──────────────
const ROOM_PALETTE = [
  { bg: 'bg-violet-50',  ring: 'ring-violet-300',  bar: 'bg-violet-500',  text: 'text-violet-900',  dot: 'bg-violet-500'  },
  { bg: 'bg-blue-50',    ring: 'ring-blue-300',    bar: 'bg-blue-500',    text: 'text-blue-900',    dot: 'bg-blue-500'    },
  { bg: 'bg-teal-50',    ring: 'ring-teal-300',    bar: 'bg-teal-500',    text: 'text-teal-900',    dot: 'bg-teal-500'    },
  { bg: 'bg-rose-50',    ring: 'ring-rose-300',    bar: 'bg-rose-500',    text: 'text-rose-900',    dot: 'bg-rose-500'    },
  { bg: 'bg-amber-50',   ring: 'ring-amber-300',   bar: 'bg-amber-500',   text: 'text-amber-900',   dot: 'bg-amber-500'   },
  { bg: 'bg-indigo-50',  ring: 'ring-indigo-300',  bar: 'bg-indigo-500',  text: 'text-indigo-900',  dot: 'bg-indigo-500'  },
  { bg: 'bg-cyan-50',    ring: 'ring-cyan-300',    bar: 'bg-cyan-500',    text: 'text-cyan-900',    dot: 'bg-cyan-500'    },
  { bg: 'bg-fuchsia-50', ring: 'ring-fuchsia-300', bar: 'bg-fuchsia-500', text: 'text-fuchsia-900', dot: 'bg-fuchsia-500' },
]

function paletteForRoom(roomId: string) {
  let hash = 0
  for (let i = 0; i < roomId.length; i++) hash = (hash * 31 + roomId.charCodeAt(i)) >>> 0
  return ROOM_PALETTE[hash % ROOM_PALETTE.length]
}

// ─── Time math ────────────────────────────────────────────────────────────────
function eventPosition(start: Date, end: Date) {
  const startMin = (getHours(start) - DAY_START) * 60 + getMinutes(start)
  const endMin   = (getHours(end)   - DAY_START) * 60 + getMinutes(end)
  const top    = Math.max(0, (startMin / 60) * HOUR_PX)
  const height = Math.max(HOUR_PX / 2, ((endMin - startMin) / 60) * HOUR_PX)
  return { top, height }
}

function nowOffsetPx(): number | null {
  const now = new Date()
  const h = getHours(now)
  const m = getMinutes(now)
  if (h < DAY_START || h >= DAY_END) return null
  return ((h - DAY_START) * 60 + m) / 60 * HOUR_PX
}

function isSameDayStr(a: Date, b: Date) {
  return format(a, 'yyyy-MM-dd') === format(b, 'yyyy-MM-dd')
}

// ─── New Reservation schema ───────────────────────────────────────────────────
const reservationSchema = z.object({
  roomId:      z.string().min(1, 'Seleccione una sala'),
  startTime:   z.string().min(1, 'Hora de inicio requerida'),
  endTime:     z.string().min(1, 'Hora de fin requerida'),
  peopleCount: z.coerce.number().min(1, 'Mínimo 1 persona'),
  purpose:     z.string().min(3, 'Ingrese el propósito'),
  notes:       z.string().optional(),
}).refine(d => d.endTime > d.startTime, {
  message: 'La hora de fin debe ser posterior a la de inicio',
  path: ['endTime'],
})
type ReservationForm = z.infer<typeof reservationSchema>

// ─── EventBlock ───────────────────────────────────────────────────────────────
function EventBlock({ event, onClick }: { event: CalendarEventDto; onClick: () => void }) {
  const start = parseISO(event.start)
  const end   = parseISO(event.end)
  const { top, height } = eventPosition(start, end)
  const palette = paletteForRoom(event.roomId)
  const statusMeta = STATUS_META[event.status] ?? STATUS_META.Pending
  const compact = height < HOUR_PX

  return (
    <button
      onClick={e => { e.stopPropagation(); onClick() }}
      className={classNames(
        'group absolute left-1 right-1 rounded-lg overflow-hidden text-left transition-all',
        'ring-1 ring-inset shadow-sm hover:shadow-md hover:-translate-y-px',
        'focus:outline-none focus:ring-2 focus:ring-offset-1 focus:ring-green-500',
        palette.bg, palette.ring, palette.text,
      )}
      style={{ top, height, zIndex: 10 }}
      title={`${event.title} · ${format(start, 'HH:mm')}–${format(end, 'HH:mm')} · ${event.roomName}`}
    >
      <div className={`absolute left-0 top-0 bottom-0 w-1 ${palette.bar}`} />
      <div className={compact ? 'pl-2 pr-1.5 py-0.5' : 'pl-2.5 pr-2 py-1.5'}>
        <div className="flex items-center gap-1">
          <span className={`h-1.5 w-1.5 rounded-full shrink-0 ${statusMeta.dot}`} />
          <p className={`font-semibold leading-tight truncate ${compact ? 'text-[10px]' : 'text-[12px]'}`}>
            {event.title}
          </p>
        </div>
        {!compact && (
          <p className="text-[10px] leading-tight truncate opacity-80 mt-0.5">
            {format(start, 'HH:mm')}–{format(end, 'HH:mm')} · {event.roomName}
          </p>
        )}
      </div>
    </button>
  )
}

// ─── NowLine ─────────────────────────────────────────────────────────────────
function NowLine() {
  const [, tick] = useState(0)
  const offset = nowOffsetPx()

  useEffect(() => {
    const id = setInterval(() => tick(n => n + 1), 60_000)
    return () => clearInterval(id)
  }, [])

  if (offset === null) return null
  return (
    <div className="absolute left-0 right-0 pointer-events-none" style={{ top: offset, zIndex: 20 }}>
      <div className="relative flex items-center">
        <div className="relative -ml-1.5 shrink-0">
          <div className="h-2.5 w-2.5 rounded-full bg-red-500 ring-2 ring-white" />
          <div className="absolute inset-0 h-2.5 w-2.5 rounded-full bg-red-500 animate-ping opacity-60" />
        </div>
        <div className="flex-1 h-[1.5px] bg-red-500" />
      </div>
    </div>
  )
}

// ─── NowTimePill — etiqueta de hora actual flotante en el medianil ───────────
function NowTimePill() {
  const [now, setNow] = useState(() => new Date())
  const offset = nowOffsetPx()

  useEffect(() => {
    const id = setInterval(() => setNow(new Date()), 60_000)
    return () => clearInterval(id)
  }, [])

  if (offset === null) return null
  return (
    <div
      className="absolute right-1 pointer-events-none z-30"
      style={{ top: offset - 9 }}
    >
      <span className="inline-flex items-center rounded-md bg-red-500 text-white text-[10px] font-semibold px-1.5 py-0.5 tabular-nums shadow-sm ring-1 ring-red-600">
        {format(now, 'HH:mm')}
      </span>
    </div>
  )
}

// ─── MiniCalendar ────────────────────────────────────────────────────────────
function MiniCalendar({ weekStart, onSelectDay }: { weekStart: Date; onSelectDay: (d: Date) => void }) {
  const [viewMonth, setViewMonth] = useState(startOfMonth(weekStart))

  useEffect(() => { setViewMonth(startOfMonth(weekStart)) }, [weekStart])

  const monthStart = viewMonth
  const monthEnd = endOfMonth(viewMonth)
  const gridStart = startOfWeek(monthStart, { weekStartsOn: 1 })
  const gridEnd = endOfWeek(monthEnd, { weekStartsOn: 1 })
  const days: Date[] = []
  let d = gridStart
  while (d <= gridEnd) { days.push(d); d = addDays(d, 1) }

  const weekStartStr = format(weekStart, 'yyyy-MM-dd')
  const weekEndStr = format(addDays(weekStart, 6), 'yyyy-MM-dd')

  return (
    <div className="select-none">
      <div className="flex items-center justify-between mb-2">
        <h3 className="text-[13px] font-semibold text-gray-800 capitalize">
          {format(viewMonth, 'MMMM yyyy', { locale: es })}
        </h3>
        <div className="flex items-center gap-0.5">
          <button
            onClick={() => setViewMonth(m => subMonths(m, 1))}
            className="flex h-6 w-6 items-center justify-center rounded hover:bg-gray-100 transition-colors"
            aria-label="Mes anterior"
          >
            <ChevronLeft className="h-3.5 w-3.5 text-gray-500" />
          </button>
          <button
            onClick={() => setViewMonth(m => addMonths(m, 1))}
            className="flex h-6 w-6 items-center justify-center rounded hover:bg-gray-100 transition-colors"
            aria-label="Mes siguiente"
          >
            <ChevronRight className="h-3.5 w-3.5 text-gray-500" />
          </button>
        </div>
      </div>
      <div className="grid grid-cols-7 gap-px text-center text-[10px] font-medium text-gray-400 mb-1">
        {['L','M','M','J','V','S','D'].map((d, i) => <div key={i}>{d}</div>)}
      </div>
      <div className="grid grid-cols-7 gap-px">
        {days.map((day) => {
          const dayStr = format(day, 'yyyy-MM-dd')
          const inMonth = isSameMonth(day, viewMonth)
          const today = isToday(day)
          const inSelectedWeek = dayStr >= weekStartStr && dayStr <= weekEndStr
          return (
            <button
              key={dayStr}
              onClick={() => onSelectDay(day)}
              className={classNames(
                'aspect-square text-[11px] rounded transition-colors',
                !inMonth && 'text-gray-300',
                inMonth && !inSelectedWeek && !today && 'text-gray-700 hover:bg-gray-100',
                inSelectedWeek && !today && 'bg-green-50 text-green-800 font-medium',
                today && 'bg-green-600 text-white font-semibold hover:bg-green-700',
              )}
            >
              {format(day, 'd')}
            </button>
          )
        })}
      </div>
    </div>
  )
}

// ─── CalendarPage ─────────────────────────────────────────────────────────────
export default function CalendarPage() {
  const qc = useQueryClient()
  const [weekStart, setWeekStart] = useState(() =>
    startOfWeek(new Date(), { weekStartsOn: 1 })
  )
  const [selectedRooms, setSelectedRooms] = useState<string[]>([])
  const [detailEvent, setDetailEvent] = useState<CalendarEventDto | null>(null)
  const [newResModal, setNewResModal] = useState(false)
  const scrollContainerRef = useRef<HTMLDivElement>(null)

  const weekEnd = endOfWeek(weekStart, { weekStartsOn: 1 })
  const days = Array.from({ length: 7 }, (_, i) => addDays(weekStart, i))

  const weekLabel = `Semana ${format(weekStart, 'w')} · ${format(weekStart, 'd MMM', { locale: es })} – ${format(weekEnd, 'd MMM yyyy', { locale: es })}`

  // ── Data ────────────────────────────────────────────────────────────────────
  const { data: events = [], isLoading } = useQuery({
    queryKey: ['calendar', format(weekStart, 'yyyy-MM-dd')],
    queryFn: async () => {
      const from = formatISO(startOfDay(weekStart))
      const to   = formatISO(startOfDay(addDays(weekEnd, 1)))
      const res  = await reservationsApi.getCalendar({ from, to })
      return res.data.data ?? []
    },
  })

  const { data: roomsData } = useQuery({
    queryKey: ['rooms-list'],
    queryFn: () => roomsApi.getAll({ pageSize: 100 }).then(r => r.data.data?.items ?? []),
  })
  const rooms: RoomDto[] = roomsData ?? []

  // Suscripción a eventos en tiempo real: cualquier cambio en reservas o salas
  // refresca el calendario sin que el usuario tenga que recargar la página.
  useRealtime(event => {
    if (event.type === 'reservation.changed') {
      qc.invalidateQueries({ queryKey: ['calendar'] })
    } else if (event.type === 'room.changed') {
      qc.invalidateQueries({ queryKey: ['rooms-list'] })
    }
  })

  // Filter events by selected rooms
  const visibleEvents = useMemo(() =>
    selectedRooms.length === 0
      ? events
      : events.filter(e => selectedRooms.includes(e.roomId)),
    [events, selectedRooms]
  )

  // Stats por status
  const stats = useMemo(() => {
    const s = { total: visibleEvents.length, approved: 0, pending: 0 }
    visibleEvents.forEach(e => {
      if (e.status === 'Approved') s.approved++
      else if (e.status === 'Pending') s.pending++
    })
    return s
  }, [visibleEvents])

  // ── Mutations ────────────────────────────────────────────────────────────────
  const createMutation = useMutation({
    mutationFn: (data: CreateReservationRequest) => reservationsApi.create(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['calendar'] })
      toast.success('Reserva creada correctamente')
      setNewResModal(false)
    },
    onError: (err) => toast.error(extractApiError(err)),
  })

  // ── New reservation form ─────────────────────────────────────────────────────
  const { register, handleSubmit, reset, formState: { errors } } = useForm<ReservationForm>({
    resolver: zodResolver(reservationSchema),
    defaultValues: { peopleCount: 1 },
  })

  const openNewRes = useCallback((day?: Date, hour?: number) => {
    const target = day ?? new Date()
    const startHour = hour ?? 9
    const endHour = Math.min(startHour + 1, DAY_END)
    reset({
      roomId: '',
      startTime: `${format(target, 'yyyy-MM-dd')}T${String(startHour).padStart(2, '0')}:00`,
      endTime:   `${format(target, 'yyyy-MM-dd')}T${String(endHour).padStart(2, '0')}:00`,
      peopleCount: 1,
      purpose: '',
      notes: '',
    })
    setNewResModal(true)
  }, [reset])

  function onSubmitReservation(data: ReservationForm) {
    createMutation.mutate({
      roomId: data.roomId,
      startDateTime: data.startTime,
      endDateTime: data.endTime,
      peopleCount: data.peopleCount,
      purpose: data.purpose,
      notes: data.notes,
    })
  }

  // ── Keyboard shortcuts ───────────────────────────────────────────────────────
  useEffect(() => {
    function onKey(e: KeyboardEvent) {
      if (e.target instanceof HTMLInputElement || e.target instanceof HTMLTextAreaElement) return
      if (e.key === 'j' || e.key === 'J') setWeekStart(w => subWeeks(w, 1))
      if (e.key === 'l' || e.key === 'L') setWeekStart(w => addWeeks(w, 1))
      if (e.key === 't' || e.key === 'T') setWeekStart(startOfWeek(new Date(), { weekStartsOn: 1 }))
      if (e.key === 'n' || e.key === 'N') openNewRes()
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [openNewRes])

  // Auto-scroll para mostrar el inicio del día laboral (8:00) al cargar
  useEffect(() => {
    if (scrollContainerRef.current && !isLoading) {
      const targetHour = 8
      scrollContainerRef.current.scrollTop = (targetHour - DAY_START) * HOUR_PX - 20
    }
  }, [isLoading])

  function handleSelectDayFromMini(day: Date) {
    setWeekStart(startOfWeek(day, { weekStartsOn: 1 }))
  }

  return (
    <div className="flex flex-col h-full bg-gray-50">

      {/* ─── Toolbar superior ─── */}
      <header className="flex flex-wrap items-center justify-between gap-3 px-6 py-3 border-b border-gray-200 bg-white shrink-0">
        <div className="flex items-center gap-3 min-w-0">
          <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-green-50 shrink-0">
            <CalendarDays className="h-4 w-4 text-green-700" strokeWidth={1.75} />
          </div>
          <div className="min-w-0">
            <p className="text-[11px] text-gray-400 tracking-wide">Salas / Calendario</p>
            <h1 className="text-base font-semibold text-gray-900 leading-tight capitalize truncate">{weekLabel}</h1>
          </div>
        </div>

        <div className="flex items-center gap-2 flex-wrap">
          {/* Stats chips */}
          {stats.total > 0 && (
            <div className="hidden lg:flex items-center gap-1.5 mr-2">
              <span className="inline-flex items-center gap-1 rounded-full bg-emerald-50 px-2 py-0.5 text-[11px] font-medium text-emerald-700 ring-1 ring-inset ring-emerald-200">
                <CheckCircle2 className="h-3 w-3" /> {stats.approved} aprobadas
              </span>
              {stats.pending > 0 && (
                <span className="inline-flex items-center gap-1 rounded-full bg-amber-50 px-2 py-0.5 text-[11px] font-medium text-amber-700 ring-1 ring-inset ring-amber-200">
                  <AlertCircle className="h-3 w-3" /> {stats.pending} pendientes
                </span>
              )}
            </div>
          )}

          {/* Nav arrows */}
          <div className="flex items-center rounded-lg border border-gray-200 bg-white overflow-hidden">
            <button
              onClick={() => setWeekStart(w => subWeeks(w, 1))}
              className="flex h-8 w-8 items-center justify-center hover:bg-gray-50 transition-colors"
              title="Semana anterior (J)"
            >
              <ChevronLeft className="h-4 w-4 text-gray-600" />
            </button>
            <button
              onClick={() => setWeekStart(startOfWeek(new Date(), { weekStartsOn: 1 }))}
              className="border-x border-gray-200 px-3 h-8 text-sm font-medium text-gray-700 hover:bg-gray-50 transition-colors"
              title="Hoy (T)"
            >
              Hoy
            </button>
            <button
              onClick={() => setWeekStart(w => addWeeks(w, 1))}
              className="flex h-8 w-8 items-center justify-center hover:bg-gray-50 transition-colors"
              title="Semana siguiente (L)"
            >
              <ChevronRight className="h-4 w-4 text-gray-600" />
            </button>
          </div>

          <Button size="sm" onClick={() => openNewRes()} title="Nueva reserva (N)">
            <Plus className="h-4 w-4" /> Nueva reserva
          </Button>
        </div>
      </header>

      {/* ─── Body — Sidebar + Calendar grid ─── */}
      <div className="flex-1 flex overflow-hidden">

        {/* Sidebar izquierdo */}
        <aside className="hidden lg:flex w-64 shrink-0 flex-col gap-4 border-r border-gray-200 bg-white p-4 overflow-y-auto">
          {/* Mini calendar */}
          <MiniCalendar weekStart={weekStart} onSelectDay={handleSelectDayFromMini} />

          {/* Filtros por sala */}
          {rooms.length > 0 && (
            <div>
              <div className="flex items-center justify-between mb-2">
                <h3 className="text-[11px] font-semibold text-gray-400 uppercase tracking-wider flex items-center gap-1.5">
                  <Filter className="h-3 w-3" /> Salas
                </h3>
                {selectedRooms.length > 0 && (
                  <button
                    onClick={() => setSelectedRooms([])}
                    className="text-[10px] font-medium text-green-700 hover:underline"
                  >
                    Limpiar
                  </button>
                )}
              </div>
              <div className="space-y-0.5">
                {rooms.map(r => {
                  const palette = paletteForRoom(r.id)
                  const selected = selectedRooms.includes(r.id)
                  const isActive = selectedRooms.length === 0 || selected
                  return (
                    <button
                      key={r.id}
                      onClick={() => setSelectedRooms(s => s.includes(r.id) ? s.filter(x => x !== r.id) : [...s, r.id])}
                      className={classNames(
                        'w-full flex items-center gap-2 rounded-md px-2 py-1.5 text-left transition-colors',
                        selected ? 'bg-gray-100' : 'hover:bg-gray-50',
                      )}
                    >
                      <span className={classNames('h-2.5 w-2.5 rounded-full shrink-0', palette.dot, !isActive && 'opacity-30')} />
                      <span className={classNames('text-[13px] truncate flex-1', isActive ? 'text-gray-800' : 'text-gray-400')}>
                        {r.name}
                      </span>
                      {selected && <CheckCircle2 className="h-3.5 w-3.5 text-green-600 shrink-0" />}
                    </button>
                  )
                })}
              </div>
            </div>
          )}

          {/* Resumen de la semana */}
          <div className="mt-auto pt-4 border-t border-gray-100">
            <h3 className="text-[11px] font-semibold text-gray-400 uppercase tracking-wider mb-2 flex items-center gap-1.5">
              <Sparkles className="h-3 w-3" /> Esta semana
            </h3>
            <div className="space-y-1.5">
              <div className="flex items-center justify-between text-[13px]">
                <span className="text-gray-600">Total reservas</span>
                <span className="font-semibold text-gray-900">{stats.total}</span>
              </div>
              <div className="flex items-center justify-between text-[13px]">
                <span className="text-gray-600 flex items-center gap-1.5">
                  <span className="h-1.5 w-1.5 rounded-full bg-emerald-500" /> Aprobadas
                </span>
                <span className="font-semibold text-emerald-700">{stats.approved}</span>
              </div>
              <div className="flex items-center justify-between text-[13px]">
                <span className="text-gray-600 flex items-center gap-1.5">
                  <span className="h-1.5 w-1.5 rounded-full bg-amber-500" /> Pendientes
                </span>
                <span className="font-semibold text-amber-700">{stats.pending}</span>
              </div>
            </div>
          </div>

          {/* Keyboard hints */}
          <div className="rounded-lg border border-gray-100 bg-gray-50/50 px-3 py-2.5 text-[11px] text-gray-500 space-y-0.5">
            <p className="font-medium text-gray-600 mb-1">Atajos</p>
            <div className="flex items-center justify-between">
              <span>Semana anterior</span>
              <kbd className="px-1.5 rounded bg-white border border-gray-200 font-mono">J</kbd>
            </div>
            <div className="flex items-center justify-between">
              <span>Semana siguiente</span>
              <kbd className="px-1.5 rounded bg-white border border-gray-200 font-mono">L</kbd>
            </div>
            <div className="flex items-center justify-between">
              <span>Hoy</span>
              <kbd className="px-1.5 rounded bg-white border border-gray-200 font-mono">T</kbd>
            </div>
            <div className="flex items-center justify-between">
              <span>Nueva reserva</span>
              <kbd className="px-1.5 rounded bg-white border border-gray-200 font-mono">N</kbd>
            </div>
          </div>
        </aside>

        {/* Main calendar area */}
        <div className="flex-1 flex flex-col overflow-hidden bg-white">

          {/* Day headers (sticky) */}
          <div className="grid border-b border-gray-200 bg-white shrink-0" style={{ gridTemplateColumns: `${GUTTER_W}px repeat(7, 1fr)` }}>
            <div className="h-14" />
            {days.map((day, i) => {
              const today = isToday(day)
              return (
                <div
                  key={i}
                  className={classNames(
                    'flex flex-col items-center justify-center py-2 border-l border-gray-100',
                    today && 'bg-green-50/60',
                  )}
                >
                  <span className={classNames(
                    'text-[10px] font-semibold tracking-wider uppercase',
                    today ? 'text-green-700' : 'text-gray-400',
                  )}>
                    {DAY_LABELS[i]}
                  </span>
                  <div className={classNames(
                    'mt-1 flex items-center justify-center rounded-full transition-colors',
                    today ? 'h-7 w-7 bg-green-600 text-white' : 'text-gray-800',
                  )}>
                    <span className={classNames('font-semibold', today ? 'text-sm' : 'text-base')}>
                      {format(day, 'd')}
                    </span>
                  </div>
                </div>
              )
            })}
          </div>

          {/* Calendar body (scrollable) */}
          <div ref={scrollContainerRef} className="flex-1 overflow-auto">
            {isLoading ? (
              <CalendarSkeleton />
            ) : (
              <div className="grid relative" style={{ gridTemplateColumns: `${GUTTER_W}px repeat(7, 1fr)` }}>

                {/* Hour gutter — tipografía clara con líneas de hora prominentes */}
                <div className="relative border-r border-gray-200 bg-white sticky left-0 z-10" style={{ height: TOTAL_H }}>
                  {HOURS.map((h, idx) => (
                    <div
                      key={h}
                      className="relative"
                      style={{ height: HOUR_PX }}
                    >
                      {/* Tag de hora — alineado con la línea, sobre fondo blanco para legibilidad */}
                      {idx > 0 && (
                        <span className="absolute -top-2 right-2 text-[11px] font-semibold text-gray-500 tabular-nums tracking-tight bg-white px-1.5">
                          {String(h).padStart(2, '0')}:00
                        </span>
                      )}
                      {/* Primera hora se muestra al inicio del bloque sin solapamiento */}
                      {idx === 0 && (
                        <span className="absolute top-1 right-2 text-[11px] font-semibold text-gray-500 tabular-nums tracking-tight bg-white px-1.5">
                          {String(h).padStart(2, '0')}:00
                        </span>
                      )}
                    </div>
                  ))}
                  {/* Pill con hora actual — solo si hoy está en la semana visible */}
                  {days.some(d => isToday(d)) && <NowTimePill />}
                </div>

                {/* Day columns */}
                {days.map((day, di) => {
                  const today = isToday(day)
                  const dayEvents = visibleEvents.filter(e => isSameDayStr(parseISO(e.start), day))

                  return (
                    <div
                      key={di}
                      className={classNames(
                        'relative border-l border-gray-100',
                        today && 'bg-green-50/30',
                      )}
                      style={{ height: TOTAL_H }}
                    >
                      {/* Celdas por hora — cada una hoverable y clickable */}
                      {HOURS.map(h => (
                        <button
                          key={h}
                          type="button"
                          onClick={() => openNewRes(day, h)}
                          className="group/cell absolute left-0 right-0 border-b border-gray-200 cursor-pointer transition-colors hover:bg-green-100/50 focus:outline-none focus:bg-green-100/60 focus-visible:z-[5]"
                          style={{ top: (h - DAY_START) * HOUR_PX, height: HOUR_PX }}
                          aria-label={`Crear reserva el ${format(day, 'd MMM', { locale: es })} a las ${String(h).padStart(2, '0')}:00`}
                        >
                          {/* Línea media (30 min) */}
                          <span className="absolute left-0 right-0 top-1/2 border-t border-dashed border-gray-100 pointer-events-none" />
                          {/* Etiqueta hover en esquina superior izquierda */}
                          <span className="absolute top-1 left-1.5 opacity-0 group-hover/cell:opacity-100 transition-opacity inline-flex items-center gap-0.5 rounded-md bg-green-600 text-white text-[10px] font-semibold px-1.5 py-0.5 shadow-sm pointer-events-none">
                            <Plus className="h-2.5 w-2.5" strokeWidth={3} />
                            {String(h).padStart(2, '0')}:00
                          </span>
                        </button>
                      ))}

                      {/* Now line */}
                      {today && <NowLine />}

                      {/* Events — z-index los pone encima de las celdas */}
                      {dayEvents.map(ev => (
                        <EventBlock
                          key={ev.id}
                          event={ev}
                          onClick={() => setDetailEvent(ev)}
                        />
                      ))}
                    </div>
                  )
                })}
              </div>
            )}
          </div>

          {/* Empty state — solo aviso pequeño en footer, no bloquea grid */}
          {!isLoading && visibleEvents.length === 0 && (
            <div className="border-t border-gray-100 px-6 py-3 flex items-center justify-between bg-gray-50/70 shrink-0">
              <p className="text-[13px] text-gray-500">
                No hay reservas en esta semana
                {selectedRooms.length > 0 && ' con los filtros aplicados'}.
              </p>
              <button
                onClick={() => openNewRes()}
                className="text-[13px] font-medium text-green-700 hover:underline"
              >
                + Crear reserva
              </button>
            </div>
          )}
        </div>
      </div>

      {/* ── Event detail modal ── */}
      <Modal open={!!detailEvent} onClose={() => setDetailEvent(null)} title="Detalle de reserva" size="md">
        {detailEvent && (() => {
          const start = parseISO(detailEvent.start)
          const end   = parseISO(detailEvent.end)
          const palette = paletteForRoom(detailEvent.roomId)
          const statusMeta = STATUS_META[detailEvent.status] ?? STATUS_META.Pending
          return (
            <div className="space-y-5">
              {/* Header con barra de color */}
              <div className={classNames('rounded-xl ring-1 ring-inset overflow-hidden', palette.bg, palette.ring)}>
                <div className={`h-1 ${palette.bar}`} />
                <div className="px-4 py-3">
                  <div className="flex items-start justify-between gap-3">
                    <div className="min-w-0">
                      <h3 className={`text-base font-semibold leading-tight ${palette.text}`}>{detailEvent.title}</h3>
                      <p className="text-[12px] text-gray-600 mt-0.5">{detailEvent.roomName}</p>
                    </div>
                    <span className={classNames(
                      'inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[11px] font-medium ring-1 ring-inset shrink-0',
                      statusMeta.bg, statusMeta.ring, statusMeta.text,
                    )}>
                      <span className={`h-1.5 w-1.5 rounded-full ${statusMeta.dot}`} />
                      {statusMeta.label}
                    </span>
                  </div>
                </div>
              </div>

              {/* Info grid */}
              <div className="rounded-xl bg-gray-50/60 border border-gray-100 p-4 space-y-3">
                <div className="flex items-start gap-2.5">
                  <div className="mt-0.5 flex h-7 w-7 shrink-0 items-center justify-center rounded-md bg-white border border-gray-200">
                    <Clock className="h-3.5 w-3.5 text-gray-500" />
                  </div>
                  <div>
                    <p className="text-[11px] uppercase tracking-wider text-gray-400 font-medium">Horario</p>
                    <p className="text-sm text-gray-800 capitalize">
                      {format(start, "EEEE d 'de' MMMM", { locale: es })}
                    </p>
                    <p className="text-sm text-gray-600">{format(start, 'HH:mm')} – {format(end, 'HH:mm')}</p>
                  </div>
                </div>
                <div className="flex items-start gap-2.5">
                  <div className="mt-0.5 flex h-7 w-7 shrink-0 items-center justify-center rounded-md bg-white border border-gray-200">
                    <MapPin className="h-3.5 w-3.5 text-gray-500" />
                  </div>
                  <div>
                    <p className="text-[11px] uppercase tracking-wider text-gray-400 font-medium">Sala</p>
                    <p className="text-sm text-gray-800">{detailEvent.roomName}</p>
                  </div>
                </div>
              </div>

              <div className="flex justify-end">
                <Button variant="secondary" size="sm" onClick={() => setDetailEvent(null)}>
                  Cerrar
                </Button>
              </div>
            </div>
          )
        })()}
      </Modal>

      {/* ── New reservation modal ── */}
      <Modal
        open={newResModal}
        onClose={() => setNewResModal(false)}
        title="Nueva reserva"
        size="md"
      >
        <form onSubmit={handleSubmit(onSubmitReservation)} className="space-y-4">
          {/* Sala */}
          <div className="flex flex-col gap-1.5">
            <label className="text-[13px] font-medium text-gray-700 flex items-center gap-1.5">
              <MapPin className="h-3.5 w-3.5 text-gray-400" /> Sala <span className="text-red-500">*</span>
            </label>
            <select
              {...register('roomId')}
              className="rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-600/20 focus:border-green-500"
            >
              <option value="">Seleccione una sala</option>
              {rooms.map(r => (
                <option key={r.id} value={r.id}>
                  {r.name} — capacidad {r.capacity}
                </option>
              ))}
            </select>
            {errors.roomId && <p className="text-[12px] text-red-600">{errors.roomId.message}</p>}
          </div>

          {/* Date/time row */}
          <div className="grid grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <label className="text-[13px] font-medium text-gray-700 flex items-center gap-1.5">
                <Clock className="h-3.5 w-3.5 text-gray-400" /> Inicio <span className="text-red-500">*</span>
              </label>
              <input
                type="datetime-local"
                {...register('startTime')}
                className="rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-600/20 focus:border-green-500"
              />
              {errors.startTime && <p className="text-[12px] text-red-600">{errors.startTime.message}</p>}
            </div>
            <div className="flex flex-col gap-1.5">
              <label className="text-[13px] font-medium text-gray-700 flex items-center gap-1.5">
                <Clock className="h-3.5 w-3.5 text-gray-400" /> Fin <span className="text-red-500">*</span>
              </label>
              <input
                type="datetime-local"
                {...register('endTime')}
                className="rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-600/20 focus:border-green-500"
              />
              {errors.endTime && <p className="text-[12px] text-red-600">{errors.endTime.message}</p>}
            </div>
          </div>

          {/* People */}
          <div className="flex flex-col gap-1.5">
            <label className="text-[13px] font-medium text-gray-700 flex items-center gap-1.5">
              <Users className="h-3.5 w-3.5 text-gray-400" /> Personas <span className="text-red-500">*</span>
            </label>
            <input
              type="number"
              min={1}
              {...register('peopleCount')}
              className="rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-600/20 focus:border-green-500"
            />
            {errors.peopleCount && <p className="text-[12px] text-red-600">{errors.peopleCount.message}</p>}
          </div>

          {/* Purpose */}
          <div className="flex flex-col gap-1.5">
            <label className="text-[13px] font-medium text-gray-700 flex items-center gap-1.5">
              <FileText className="h-3.5 w-3.5 text-gray-400" /> Propósito <span className="text-red-500">*</span>
            </label>
            <input
              type="text"
              placeholder="Ej: Reunión de equipo"
              {...register('purpose')}
              className="rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-600/20 focus:border-green-500"
            />
            {errors.purpose && <p className="text-[12px] text-red-600">{errors.purpose.message}</p>}
          </div>

          {/* Notes */}
          <div className="flex flex-col gap-1.5">
            <label className="text-[13px] font-medium text-gray-700">
              Notas <span className="text-gray-400 text-[11px] font-normal">(opcional)</span>
            </label>
            <textarea
              rows={2}
              placeholder="Información adicional..."
              {...register('notes')}
              className="rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-600/20 focus:border-green-500 resize-none"
            />
          </div>

          <div className="flex justify-end gap-2 pt-2 border-t border-gray-100">
            <Button type="button" variant="ghost" onClick={() => setNewResModal(false)}>Cancelar</Button>
            <Button type="submit" loading={createMutation.isPending}>
              <Plus className="h-4 w-4" /> Crear reserva
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  )
}

// ─── CalendarSkeleton ─────────────────────────────────────────────────────────
function CalendarSkeleton() {
  return (
    <div className="grid" style={{ gridTemplateColumns: `${GUTTER_W}px repeat(7, 1fr)` }}>
      <div className="border-r border-gray-100" style={{ height: TOTAL_H }}>
        {HOURS.map(h => (
          <div key={h} className="flex items-start justify-end pr-2" style={{ height: HOUR_PX }}>
            <span className="text-[10px] font-mono text-gray-300 -mt-1.5">{String(h).padStart(2, '0')}:00</span>
          </div>
        ))}
      </div>
      {Array.from({ length: 7 }).map((_, di) => (
        <div key={di} className="relative border-l border-gray-100" style={{ height: TOTAL_H }}>
          {HOURS.map(h => (
            <div key={h} className="absolute left-0 right-0 border-b border-gray-100" style={{ top: (h - DAY_START) * HOUR_PX, height: HOUR_PX }} />
          ))}
          {/* Algunos skeletons aleatorios */}
          {di % 2 === 0 && (
            <div className="absolute left-1 right-1 rounded-md bg-gray-100 animate-pulse"
              style={{ top: HOUR_PX * (1 + di * 0.3), height: HOUR_PX * 1.5 }} />
          )}
          {di % 3 === 0 && (
            <div className="absolute left-1 right-1 rounded-md bg-gray-100 animate-pulse"
              style={{ top: HOUR_PX * (5 + di * 0.2), height: HOUR_PX }} />
          )}
        </div>
      ))}
    </div>
  )
}
