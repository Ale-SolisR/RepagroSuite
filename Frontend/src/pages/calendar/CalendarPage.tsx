import { useState, useRef, useEffect, useCallback } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  startOfWeek, endOfWeek, addWeeks, subWeeks, addDays,
  format, isToday, parseISO, getHours, getMinutes,
  startOfDay, addMinutes, formatISO,
} from 'date-fns'
import { es } from 'date-fns/locale'
import { ChevronLeft, ChevronRight, Plus, X, Clock, MapPin, User, Users } from 'lucide-react'
import { reservationsApi } from '@/api/reservations'
import { roomsApi } from '@/api/rooms'
import { useAuthStore } from '@/store/authStore'
import { extractApiError } from '@/utils'
import Modal from '@/components/ui/Modal'
import Button from '@/components/ui/Button'
import Spinner from '@/components/ui/Spinner'
import toast from 'react-hot-toast'
import type { CalendarEventDto, RoomDto, CreateReservationRequest } from '@/types'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'

// ─── Constants ────────────────────────────────────────────────────────────────
const DAY_START = 9   // 09:00
const DAY_END   = 18  // 18:00
const HOUR_PX   = 64  // px per hour
const TOTAL_H   = (DAY_END - DAY_START) * HOUR_PX // 576px
const HOURS     = Array.from({ length: DAY_END - DAY_START }, (_, i) => DAY_START + i)
const DAY_LABELS = ['LUN', 'MAR', 'MIÉ', 'JUE', 'VIE', 'SÁB', 'DOM']

// ─── Status styles ────────────────────────────────────────────────────────────
const STATUS_STYLE: Record<string, { bg: string; border: string; text: string }> = {
  Approved: { bg: '#D1FAE5', border: '#10B981', text: '#065F46' },
  Pending:  { bg: '#FEF3C7', border: '#F59E0B', text: '#78350F' },
  Rejected: { bg: '#FEE2E2', border: '#EF4444', text: '#991B1B' },
  Cancelled:{ bg: '#F3F4F6', border: '#9CA3AF', text: '#374151' },
}

// ─── Time math ────────────────────────────────────────────────────────────────
function eventPosition(start: Date, end: Date) {
  const startMin = (getHours(start) - DAY_START) * 60 + getMinutes(start)
  const endMin   = (getHours(end)   - DAY_START) * 60 + getMinutes(end)
  const top    = Math.max(0, (startMin / 60) * HOUR_PX)
  const height = Math.max(HOUR_PX / 4, ((endMin - startMin) / 60) * HOUR_PX)
  return { top, height }
}

function nowOffsetPx(): number | null {
  const now = new Date()
  const h = getHours(now)
  const m = getMinutes(now)
  if (h < DAY_START || h >= DAY_END) return null
  return ((h - DAY_START) * 60 + m) / 60 * HOUR_PX
}

function isSameDay(a: Date, b: Date) {
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
})
type ReservationForm = z.infer<typeof reservationSchema>

// ─── EventBlock ───────────────────────────────────────────────────────────────
function EventBlock({ event, onClick }: { event: CalendarEventDto; onClick: () => void }) {
  const start = parseISO(event.start)
  const end   = parseISO(event.end)
  const { top, height } = eventPosition(start, end)
  const style = STATUS_STYLE[event.status] ?? STATUS_STYLE.Pending

  return (
    <button
      onClick={e => { e.stopPropagation(); onClick() }}
      className="absolute left-1 right-1 rounded-md px-2 py-1 text-left overflow-hidden transition-opacity hover:opacity-80 focus:outline-none focus:ring-2 focus:ring-offset-1"
      style={{
        top, height,
        background: style.bg,
        borderLeft: `3px solid ${style.border}`,
        color: style.text,
        zIndex: 10,
      }}
    >
      <p className="text-[11px] font-semibold leading-tight truncate">{event.title}</p>
      <p className="text-[10px] leading-tight truncate opacity-80">
        {format(start, 'HH:mm')}–{format(end, 'HH:mm')} · {event.roomName}
      </p>
    </button>
  )
}

// ─── NowLine ─────────────────────────────────────────────────────────────────
function NowLine() {
  const [offset, setOffset] = useState<number | null>(nowOffsetPx)

  useEffect(() => {
    const id = setInterval(() => setOffset(nowOffsetPx()), 60_000)
    return () => clearInterval(id)
  }, [])

  if (offset === null) return null
  return (
    <div className="absolute left-0 right-0 pointer-events-none" style={{ top: offset, zIndex: 20 }}>
      <div className="relative flex items-center">
        <div className="h-2.5 w-2.5 rounded-full bg-red-500 shrink-0 -ml-1.5" />
        <div className="flex-1 h-0.5 bg-red-500" />
      </div>
    </div>
  )
}

// ─── RoomFilterPopover ────────────────────────────────────────────────────────
function RoomFilterChip({
  rooms, selected, onChange,
}: { rooms: RoomDto[]; selected: string[]; onChange: (ids: string[]) => void }) {
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    function handle(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', handle)
    return () => document.removeEventListener('mousedown', handle)
  }, [])

  const label = selected.length === 0 || selected.length === rooms.length
    ? 'Salas: todas'
    : `Salas: ${selected.length}`

  function toggle(id: string) {
    onChange(selected.includes(id) ? selected.filter(x => x !== id) : [...selected, id])
  }

  return (
    <div className="relative" ref={ref}>
      <button
        onClick={() => setOpen(p => !p)}
        className="flex items-center gap-1.5 rounded-md border border-gray-200 bg-white px-3 py-1.5 text-sm font-medium text-gray-700 hover:bg-gray-50 transition-colors"
      >
        {label}
        <ChevronRight className={`h-3.5 w-3.5 transition-transform ${open ? 'rotate-90' : ''}`} />
      </button>
      {open && (
        <div className="absolute right-0 top-full mt-1 w-52 rounded-lg border border-gray-200 bg-white shadow-lg z-50 p-2">
          <button
            className="w-full text-left px-3 py-1.5 text-sm text-gray-600 hover:bg-gray-50 rounded"
            onClick={() => { onChange([]); setOpen(false) }}
          >
            Todas las salas
          </button>
          <div className="border-t border-gray-100 my-1" />
          {rooms.map(r => (
            <label key={r.id} className="flex items-center gap-2 px-3 py-1.5 rounded hover:bg-gray-50 cursor-pointer">
              <input
                type="checkbox"
                checked={selected.includes(r.id)}
                onChange={() => toggle(r.id)}
                className="rounded accent-green-600"
              />
              <span className="text-sm text-gray-700">{r.name}</span>
            </label>
          ))}
        </div>
      )}
    </div>
  )
}

// ─── CalendarPage ─────────────────────────────────────────────────────────────
export default function CalendarPage() {
  const qc = useQueryClient()
  const { user } = useAuthStore()
  const [weekStart, setWeekStart] = useState(() =>
    startOfWeek(new Date(), { weekStartsOn: 1 })
  )
  const [selectedRooms, setSelectedRooms] = useState<string[]>([])
  const [detailEvent, setDetailEvent] = useState<CalendarEventDto | null>(null)
  const [newResModal, setNewResModal] = useState(false)
  const [prefilledDate, setPrefilledDate] = useState<Date | null>(null)

  const weekEnd = endOfWeek(weekStart, { weekStartsOn: 1 })
  const days = Array.from({ length: 7 }, (_, i) => addDays(weekStart, i))

  // Format for display
  const weekLabel = `Semana ${format(weekStart, 'w')} — ${format(weekStart, 'd')} al ${format(weekEnd, 'd')} de ${format(weekEnd, 'MMMM', { locale: es })}`

  // ── Data ────────────────────────────────────────────────────────────────────
  const { data: events = [], isLoading } = useQuery({
    queryKey: ['calendar', format(weekStart, 'yyyy-MM-dd'), selectedRooms],
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
  const rooms = roomsData ?? []

  // Filter events by selected rooms
  const visibleEvents = selectedRooms.length === 0
    ? events
    : events.filter(e => selectedRooms.includes(e.roomId))

  // ── Mutations ────────────────────────────────────────────────────────────────
  const createMutation = useMutation({
    mutationFn: (data: CreateReservationRequest) => reservationsApi.create(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['calendar'] })
      toast.success('Reserva creada')
      setNewResModal(false)
    },
    onError: (err) => toast.error(extractApiError(err)),
  })

  // ── New reservation form ─────────────────────────────────────────────────────
  const { register, handleSubmit, reset, formState: { errors } } = useForm<ReservationForm>({
    resolver: zodResolver(reservationSchema),
    defaultValues: { peopleCount: 1 },
  })

  const openNewRes = useCallback((day?: Date) => {
    setPrefilledDate(day ?? new Date())
    reset({
      startTime: day ? `${format(day, 'yyyy-MM-dd')}T09:00` : '',
      endTime:   day ? `${format(day, 'yyyy-MM-dd')}T10:00` : '',
      peopleCount: 1,
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
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [])

  return (
    <div className="flex flex-col h-full">
      {/* ── Toolbar ── */}
      <div className="flex items-center justify-between px-6 py-3 border-b border-gray-100 bg-white shrink-0">
        <div className="flex items-center gap-3">
          <p className="text-xs text-gray-400 hidden sm:block">Salas / Calendario</p>
          <div className="hidden sm:block w-px h-4 bg-gray-200" />
          <div className="flex items-center gap-1">
            <button
              onClick={() => setWeekStart(w => subWeeks(w, 1))}
              className="flex h-7 w-7 items-center justify-center rounded-md border border-gray-200 hover:bg-gray-50 transition-colors"
              title="Semana anterior (J)"
            >
              <ChevronLeft className="h-4 w-4 text-gray-600" />
            </button>
            <button
              onClick={() => setWeekStart(w => addWeeks(w, 1))}
              className="flex h-7 w-7 items-center justify-center rounded-md border border-gray-200 hover:bg-gray-50 transition-colors"
              title="Semana siguiente (L)"
            >
              <ChevronRight className="h-4 w-4 text-gray-600" />
            </button>
            <button
              onClick={() => setWeekStart(startOfWeek(new Date(), { weekStartsOn: 1 }))}
              className="ml-1 rounded-md border border-gray-200 bg-white px-3 py-1 text-sm font-medium text-gray-600 hover:bg-gray-50 transition-colors"
              title="Hoy (T)"
            >
              Hoy
            </button>
          </div>
          <h2 className="text-sm font-semibold text-gray-800 hidden md:block capitalize">{weekLabel}</h2>
        </div>

        <div className="flex items-center gap-2">
          {rooms.length > 0 && (
            <RoomFilterChip rooms={rooms} selected={selectedRooms} onChange={setSelectedRooms} />
          )}
          <Button size="sm" onClick={() => openNewRes()}>
            <Plus className="h-4 w-4 mr-1" /> Reserva
          </Button>
        </div>
      </div>

      {/* ── Week label on mobile ── */}
      <div className="px-4 py-1.5 text-xs font-medium text-gray-500 border-b border-gray-100 bg-white md:hidden capitalize">
        {weekLabel}
      </div>

      {/* ── Calendar grid ── */}
      <div className="flex-1 overflow-auto bg-gray-50">
        {isLoading ? (
          <div className="flex justify-center items-center h-64">
            <Spinner />
          </div>
        ) : (
          <div className="min-w-[640px]">
            {/* Header row */}
            <div className="grid border-b border-gray-200 bg-white" style={{ gridTemplateColumns: '56px repeat(7, 1fr)' }}>
              <div className="h-12" /> {/* gutter corner */}
              {days.map((day, i) => {
                const today = isToday(day)
                return (
                  <div
                    key={i}
                    className={`flex flex-col items-center justify-center py-2 border-l border-gray-100 ${today ? 'bg-green-50' : ''}`}
                  >
                    <span className={`text-[10px] font-semibold tracking-wider uppercase ${today ? 'text-green-700' : 'text-gray-400'}`}>
                      {today ? `${DAY_LABELS[i]} · HOY` : DAY_LABELS[i]}
                    </span>
                    <span className={`text-base font-semibold mt-0.5 ${today ? 'text-green-700' : 'text-gray-800'}`}>
                      {format(day, 'd')}
                    </span>
                  </div>
                )
              })}
            </div>

            {/* Body */}
            <div className="grid bg-white" style={{ gridTemplateColumns: '56px repeat(7, 1fr)' }}>
              {/* Hour gutter */}
              <div className="border-r border-gray-100" style={{ height: TOTAL_H }}>
                {HOURS.map(h => (
                  <div
                    key={h}
                    className="border-b border-gray-100 flex items-start justify-end pr-2 pt-1"
                    style={{ height: HOUR_PX }}
                  >
                    <span className="text-[10px] font-mono text-gray-400">{String(h).padStart(2, '0')}:00</span>
                  </div>
                ))}
              </div>

              {/* Day columns */}
              {days.map((day, di) => {
                const today = isToday(day)
                const dayEvents = visibleEvents.filter(e => isSameDay(parseISO(e.start), day))

                return (
                  <div
                    key={di}
                    className={`relative border-l border-gray-100 cursor-pointer ${today ? 'bg-green-50/30' : ''}`}
                    style={{ height: TOTAL_H }}
                    onClick={() => openNewRes(day)}
                  >
                    {/* Hour grid lines */}
                    {HOURS.map(h => (
                      <div
                        key={h}
                        className="absolute left-0 right-0 border-b border-gray-100"
                        style={{ top: (h - DAY_START) * HOUR_PX, height: HOUR_PX }}
                      />
                    ))}

                    {/* Half-hour lines */}
                    {HOURS.map(h => (
                      <div
                        key={`hh-${h}`}
                        className="absolute left-0 right-0 border-b border-gray-50"
                        style={{ top: (h - DAY_START) * HOUR_PX + HOUR_PX / 2 }}
                      />
                    ))}

                    {/* Now line (only on today's column) */}
                    {today && <NowLine />}

                    {/* Events */}
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

            {/* Empty state overlay */}
            {!isLoading && visibleEvents.length === 0 && (
              <div className="absolute inset-x-0 flex flex-col items-center justify-center pointer-events-none" style={{ top: '50%', transform: 'translateY(-50%)' }}>
                <p className="text-sm text-gray-400">Sin reservas esta semana</p>
                <button
                  className="mt-2 text-sm text-green-700 font-medium pointer-events-auto hover:underline"
                  onClick={() => openNewRes()}
                >
                  + Nueva reserva
                </button>
              </div>
            )}
          </div>
        )}
      </div>

      {/* ── Event detail modal ── */}
      <Modal open={!!detailEvent} onClose={() => setDetailEvent(null)} title="Detalle de Reserva" size="sm">
        {detailEvent && (() => {
          const start = parseISO(detailEvent.start)
          const end   = parseISO(detailEvent.end)
          const style = STATUS_STYLE[detailEvent.status] ?? STATUS_STYLE.Pending
          return (
            <div className="space-y-4">
              <div
                className="rounded-lg p-3 text-sm font-semibold"
                style={{ background: style.bg, color: style.text, borderLeft: `4px solid ${style.border}` }}
              >
                {detailEvent.title}
              </div>
              <div className="space-y-2 text-sm">
                <div className="flex items-center gap-2 text-gray-600">
                  <MapPin className="h-4 w-4 shrink-0 text-gray-400" />
                  {detailEvent.roomName}
                </div>
                <div className="flex items-center gap-2 text-gray-600">
                  <Clock className="h-4 w-4 shrink-0 text-gray-400" />
                  {format(start, "EEEE d 'de' MMMM", { locale: es })} · {format(start, 'HH:mm')} – {format(end, 'HH:mm')}
                </div>
              </div>
              <div className="flex justify-end pt-2">
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
        title="Nueva Reserva"
        size="sm"
      >
        <form onSubmit={handleSubmit(onSubmitReservation)} className="space-y-4">
          {/* Sala */}
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">Sala <span className="text-red-500">*</span></label>
            <select
              {...register('roomId')}
              className="rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-600/20 focus:border-green-500"
            >
              <option value="">Seleccione una sala</option>
              {rooms.map(r => (
                <option key={r.id} value={r.id}>{r.name} (cap. {r.capacity})</option>
              ))}
            </select>
            {errors.roomId && <p className="text-xs text-red-600">{errors.roomId.message}</p>}
          </div>

          {/* Date/time row */}
          <div className="grid grid-cols-2 gap-3">
            <div className="flex flex-col gap-1">
              <label className="text-sm font-medium text-gray-700">Inicio <span className="text-red-500">*</span></label>
              <input
                type="datetime-local"
                {...register('startTime')}
                className="rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-600/20 focus:border-green-500"
              />
              {errors.startTime && <p className="text-xs text-red-600">{errors.startTime.message}</p>}
            </div>
            <div className="flex flex-col gap-1">
              <label className="text-sm font-medium text-gray-700">Fin <span className="text-red-500">*</span></label>
              <input
                type="datetime-local"
                {...register('endTime')}
                className="rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-600/20 focus:border-green-500"
              />
              {errors.endTime && <p className="text-xs text-red-600">{errors.endTime.message}</p>}
            </div>
          </div>

          {/* People count */}
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700 flex items-center gap-1">
              <Users className="h-3.5 w-3.5 text-gray-400" /> Personas <span className="text-red-500">*</span>
            </label>
            <input
              type="number"
              min={1}
              {...register('peopleCount')}
              className="rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-600/20 focus:border-green-500"
            />
            {errors.peopleCount && <p className="text-xs text-red-600">{errors.peopleCount.message}</p>}
          </div>

          {/* Purpose */}
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">Propósito <span className="text-red-500">*</span></label>
            <input
              type="text"
              placeholder="Ej: Reunión de equipo"
              {...register('purpose')}
              className="rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-600/20 focus:border-green-500"
            />
            {errors.purpose && <p className="text-xs text-red-600">{errors.purpose.message}</p>}
          </div>

          {/* Notes */}
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">Notas <span className="text-gray-400 text-xs">(opcional)</span></label>
            <textarea
              rows={2}
              placeholder="Información adicional..."
              {...register('notes')}
              className="rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-600/20 focus:border-green-500 resize-none"
            />
          </div>

          <div className="flex justify-end gap-2 pt-1">
            <Button type="button" variant="secondary" onClick={() => setNewResModal(false)}>Cancelar</Button>
            <Button type="submit" loading={createMutation.isPending}>Crear Reserva</Button>
          </div>
        </form>
      </Modal>
    </div>
  )
}
