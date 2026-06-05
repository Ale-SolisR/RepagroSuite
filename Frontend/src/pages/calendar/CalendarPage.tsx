import { useState, useRef, useEffect, useCallback, useMemo, type PointerEvent as ReactPointerEvent } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  startOfWeek, endOfWeek, addWeeks, subWeeks, addDays,
  format, isToday, parseISO, getHours, getMinutes,
  startOfDay, formatISO, startOfMonth, endOfMonth, addMonths, subMonths,
  isSameMonth,
} from 'date-fns'
import { es } from 'date-fns/locale'
import {
  ChevronLeft, ChevronRight, Plus, Clock,
  CalendarDays, CalendarRange, Filter, CheckCircle2, AlertCircle,
  Sparkles, Users, DoorOpen, User, Check, X, SlidersHorizontal,
} from 'lucide-react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import toast from 'react-hot-toast'
import { reservationsApi } from '@/api/reservations'
import { roomsApi } from '@/api/rooms'
import { classNames, extractApiError } from '@/utils'
import { useRealtime } from '@/hooks/useRealtime'
import { useIsMobile } from '@/hooks/useMediaQuery'
import { useAuthStore } from '@/store/authStore'
import { qk, staleTimes, invalidate } from '@/lib/queryKeys'
import Modal from '@/components/ui/Modal'
import Button from '@/components/ui/Button'
import Textarea from '@/components/ui/Textarea'
import CreateReservationForm from '@/pages/reservations/CreateReservationForm'
import type { CalendarEventDto, RoomDto, RoomScheduleDto } from '@/types'

const approveSchema = z.object({ comment: z.string().optional() })
const rejectSchema = z.object({ reason: z.string().min(5, 'Motivo requerido (mín. 5 caracteres)') })

// ─── Constants ────────────────────────────────────────────────────────────────
const DAY_START = 7   // 07:00
const DAY_END   = 21  // 21:00
const HOUR_PX   = 60
const TOTAL_H   = (DAY_END - DAY_START) * HOUR_PX
const HOURS     = Array.from({ length: DAY_END - DAY_START }, (_, i) => DAY_START + i)
const DAY_LABELS = ['LUN', 'MAR', 'MIÉ', 'JUE', 'VIE', 'SÁB', 'DOM']
const GUTTER_W  = 64  // ancho columna de horas
const SLOT_MIN  = 30  // granularidad de creación: 30 min (horas y medias horas)
const DEFAULT_DUR = 60  // duración por defecto al hacer un click simple (sin arrastrar)
const MAX_LANES_WEEK = 4  // columnas máximas por día en vista semana antes de agrupar en "+N"

type ViewMode = 'week' | 'day'

// ─── Status meta ──────────────────────────────────────────────────────────────
const STATUS_META: Record<string, { bg: string; ring: string; text: string; dot: string; label: string }> = {
  Approved:  { bg: 'bg-brand-50',  ring: 'ring-brand-200',  text: 'text-brand-800',  dot: 'bg-ok',       label: 'Aprobada' },
  Pending:   { bg: 'bg-amber-50',  ring: 'ring-amber-200',  text: 'text-amber-900',  dot: 'bg-warn',     label: 'Pendiente' },
  Rejected:  { bg: 'bg-red-50',    ring: 'ring-red-200',    text: 'text-red-900',    dot: 'bg-danger',   label: 'Rechazada' },
  Cancelled: { bg: 'bg-slate-50',  ring: 'ring-slate-200',  text: 'text-slate-600',  dot: 'bg-slate-400', label: 'Cancelada' },
}

// ─── Paleta de colores asignada por sala para coherencia visual ──────────────
const ROOM_PALETTE = [
  { bg: 'bg-brand-50',   ring: 'ring-brand-200',   bar: 'bg-brand-500',   text: 'text-brand-800',   dot: 'bg-brand-500'   },
  { bg: 'bg-blue-50',    ring: 'ring-blue-200',    bar: 'bg-blue-500',    text: 'text-blue-900',    dot: 'bg-blue-500'    },
  { bg: 'bg-teal-50',    ring: 'ring-teal-200',    bar: 'bg-teal-500',    text: 'text-teal-900',    dot: 'bg-teal-500'    },
  { bg: 'bg-violet-50',  ring: 'ring-violet-200',  bar: 'bg-violet-500',  text: 'text-violet-900',  dot: 'bg-violet-500'  },
  { bg: 'bg-amber-50',   ring: 'ring-amber-200',   bar: 'bg-amber-500',   text: 'text-amber-900',   dot: 'bg-amber-500'   },
  { bg: 'bg-rose-50',    ring: 'ring-rose-200',    bar: 'bg-rose-500',    text: 'text-rose-900',    dot: 'bg-rose-500'    },
  { bg: 'bg-cyan-50',    ring: 'ring-cyan-200',    bar: 'bg-cyan-500',    text: 'text-cyan-900',    dot: 'bg-cyan-500'    },
  { bg: 'bg-indigo-50',  ring: 'ring-indigo-200',  bar: 'bg-indigo-500',  text: 'text-indigo-900',  dot: 'bg-indigo-500'  },
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

function timeToMinutes(t: string): number {
  const [h, m] = t.split(':').map(Number)
  return h * 60 + m
}

function minutesToTime(min: number): string {
  const h = Math.floor(min / 60)
  const m = min % 60
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`
}

// Une intervalos solapados/contiguos. Cada intervalo en minutos desde medianoche.
function mergeIntervals(intervals: { start: number; end: number }[]): { start: number; end: number }[] {
  const sorted = [...intervals].sort((a, b) => a.start - b.start)
  const out: { start: number; end: number }[] = []
  for (const it of sorted) {
    const last = out[out.length - 1]
    if (last && it.start <= last.end) last.end = Math.max(last.end, it.end)
    else out.push({ ...it })
  }
  return out
}

// Convierte minutos-desde-medianoche a posición/altura en el grid (clamp al rango visible).
function bandRect(startMin: number, endMin: number) {
  const gridStart = DAY_START * 60
  const gridEnd = DAY_END * 60
  const s = Math.max(startMin, gridStart)
  const e = Math.min(endMin, gridEnd)
  const top = ((s - gridStart) / 60) * HOUR_PX
  const height = Math.max(0, ((e - s) / 60) * HOUR_PX)
  return { top, height, visible: e > s }
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

// ─── Arrastre para seleccionar rango (snap a 30 min) ─────────────────────────
// Convierte minutos-desde-medianoche a la cuadrícula visible y al revés, con snap.
function snapMin(min: number): number { return Math.round(min / SLOT_MIN) * SLOT_MIN }
function clampGrid(min: number): number { return Math.max(DAY_START * 60, Math.min(DAY_END * 60, min)) }

// La franja (banda de disponibilidad) que contiene un minuto dado; el arrastre se limita a ella.
function bandContaining(intervals: { start: number; end: number }[], min: number) {
  return intervals.find(iv => min >= iv.start && min < iv.end) ?? null
}

// ¿El slot que empieza en startMin ya pasó? (días pasados completos; hoy, hasta la hora actual)
function isPastSlotMin(day: Date, startMin: number, isPastDay: boolean): boolean {
  if (isPastDay) return true
  if (isToday(day)) return startMin <= getHours(new Date()) * 60 + getMinutes(new Date())
  return false
}

// Etiqueta corta de duración para el "fantasma" del arrastre: "30 min", "1 h", "1 h 30 min".
function durationShort(min: number): string {
  const h = Math.floor(min / 60), m = min % 60
  if (h === 0) return `${m} min`
  if (m === 0) return `${h} h`
  return `${h} h ${m} min`
}

type DragRef = {
  dayKey: string; anchorMin: number; band: { start: number; end: number }; lowMin: number
  rectTop: number; moved: boolean; pointerType: string; downY: number
  startMin: number; endMin: number
}

// Primer minuto seleccionable de una banda: su inicio, o el siguiente slot futuro si es hoy.
function lowBound(day: Date, band: { start: number; end: number }): number {
  if (!isToday(day)) return band.start
  const nowM = getHours(new Date()) * 60 + getMinutes(new Date())
  return Math.max(band.start, Math.floor(nowM / SLOT_MIN) * SLOT_MIN + SLOT_MIN)
}

// ─── Layout de eventos concurrentes en columnas (lanes) ──────────────────────
// Eventos que se solapan se reparten en columnas lado a lado en vez de apilarse.
// Si superan `maxLanes`, los excedentes se agrupan en un indicador "+N".
type LaidEvent = {
  event: CalendarEventDto; start: Date; end: Date
  top: number; height: number; lane: number; leftPct: number; widthPct: number
}
type Overflow = { top: number; height: number; leftPct: number; widthPct: number; count: number }

function layoutDayEvents(dayEvents: CalendarEventDto[], maxLanes: number): { visible: LaidEvent[]; overflows: Overflow[] } {
  const items = dayEvents
    .map(e => {
      const start = parseISO(e.start)
      const end = parseISO(e.end)
      const { top, height } = eventPosition(start, end)
      return { event: e, start, end, top, height, lane: 0 }
    })
    .sort((a, b) => a.start.getTime() - b.start.getTime() || b.end.getTime() - a.end.getTime())

  const visible: LaidEvent[] = []
  const overflows: Overflow[] = []
  let cluster: typeof items = []
  let clusterBottom = -Infinity

  const flush = () => {
    if (cluster.length === 0) return
    const laneBottoms: number[] = []
    for (const it of cluster) {
      let lane = laneBottoms.findIndex(b => it.top >= b - 0.01)
      if (lane === -1) { lane = laneBottoms.length; laneBottoms.push(0) }
      laneBottoms[lane] = it.top + it.height
      it.lane = lane
    }
    const numLanes = laneBottoms.length
    const top = Math.min(...cluster.map(it => it.top))
    const bottom = Math.max(...cluster.map(it => it.top + it.height))
    if (numLanes <= maxLanes) {
      const w = 100 / numLanes
      for (const it of cluster) visible.push({ ...it, leftPct: it.lane * w, widthPct: w })
    } else {
      const usable = maxLanes - 1
      const w = 100 / maxLanes
      let hidden = 0
      for (const it of cluster) {
        if (it.lane < usable) visible.push({ ...it, leftPct: it.lane * w, widthPct: w })
        else hidden++
      }
      overflows.push({ top, height: bottom - top, leftPct: usable * w, widthPct: w, count: hidden })
    }
    cluster = []
  }

  for (const it of items) {
    if (cluster.length > 0 && it.top >= clusterBottom) { flush(); clusterBottom = -Infinity }
    cluster.push(it)
    clusterBottom = Math.max(clusterBottom, it.top + it.height)
  }
  flush()
  return { visible, overflows }
}

// ─── EventBlock ───────────────────────────────────────────────────────────────
function EventBlock({ pos, onClick }: { pos: LaidEvent; onClick: () => void }) {
  const { event, start, end, top, height, leftPct, widthPct } = pos
  const palette = paletteForRoom(event.roomId)
  const statusMeta = STATUS_META[event.status] ?? STATUS_META.Pending
  const compact = height < HOUR_PX || widthPct < 26
  const tiny = widthPct < 16

  return (
    <button
      onClick={e => { e.stopPropagation(); onClick() }}
      className={classNames(
        'group absolute rounded-lg overflow-hidden text-left transition-all duration-150',
        'ring-1 ring-inset shadow-sh1 hover:shadow-sh2 hover:-translate-y-px',
        'focus:outline-none focus:ring-2 focus:ring-brand-600',
        palette.bg, palette.ring, palette.text,
      )}
      style={{ top, height, left: `calc(${leftPct}% + 2px)`, width: `calc(${widthPct}% - 4px)`, zIndex: 10 }}
      title={`${event.title} · ${format(start, 'HH:mm')}–${format(end, 'HH:mm')} · ${event.roomName}`}
    >
      <div className={`absolute left-0 top-0 bottom-0 w-1 ${palette.bar}`} />
      <div className={compact ? 'pl-2 pr-1 py-0.5' : 'pl-2.5 pr-2 py-1.5'}>
        <div className="flex items-center gap-1">
          <span className={`h-1.5 w-1.5 rounded-full shrink-0 ${statusMeta.dot}`} />
          {!tiny && (
            <p className={`font-semibold leading-tight truncate ${compact ? 'text-[10px]' : 'text-[12px]'}`}>
              {event.title}
            </p>
          )}
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
          <div className="absolute inset-0 h-2.5 w-2.5 rounded-full bg-red-500 animate-ping opacity-60 motion-reduce:hidden" />
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
    <div className="absolute right-1 pointer-events-none z-30" style={{ top: offset - 9 }}>
      <span className="inline-flex items-center rounded-md bg-red-500 text-white text-[10px] font-semibold px-1.5 py-0.5 tabular-nums shadow-sm ring-1 ring-red-600">
        {format(now, 'HH:mm')}
      </span>
    </div>
  )
}

// ─── MiniCalendar ────────────────────────────────────────────────────────────
function MiniCalendar({ anchor, view, onSelectDay }: { anchor: Date; view: ViewMode; onSelectDay: (d: Date) => void }) {
  const [viewMonth, setViewMonth] = useState(startOfMonth(anchor))

  useEffect(() => { setViewMonth(startOfMonth(anchor)) }, [anchor])

  const monthEnd = endOfMonth(viewMonth)
  const gridStart = startOfWeek(viewMonth, { weekStartsOn: 1 })
  const gridEnd = endOfWeek(monthEnd, { weekStartsOn: 1 })
  const days: Date[] = []
  let d = gridStart
  while (d <= gridEnd) { days.push(d); d = addDays(d, 1) }

  const weekStart = startOfWeek(anchor, { weekStartsOn: 1 })
  const weekStartStr = format(weekStart, 'yyyy-MM-dd')
  const weekEndStr = format(addDays(weekStart, 6), 'yyyy-MM-dd')
  const anchorStr = format(anchor, 'yyyy-MM-dd')

  return (
    <div className="select-none">
      <div className="flex items-center justify-between mb-2">
        <h3 className="text-[13px] font-semibold text-ink capitalize">
          {format(viewMonth, 'MMMM yyyy', { locale: es })}
        </h3>
        <div className="flex items-center gap-0.5">
          <button
            onClick={() => setViewMonth(m => subMonths(m, 1))}
            className="flex h-6 w-6 items-center justify-center rounded hover:bg-bg transition-colors cursor-pointer"
            aria-label="Mes anterior"
          >
            <ChevronLeft className="h-3.5 w-3.5 text-ink2" />
          </button>
          <button
            onClick={() => setViewMonth(m => addMonths(m, 1))}
            className="flex h-6 w-6 items-center justify-center rounded hover:bg-bg transition-colors cursor-pointer"
            aria-label="Mes siguiente"
          >
            <ChevronRight className="h-3.5 w-3.5 text-ink2" />
          </button>
        </div>
      </div>
      <div className="grid grid-cols-7 gap-px text-center text-[10px] font-medium text-ink2/60 mb-1">
        {['L', 'M', 'M', 'J', 'V', 'S', 'D'].map((dl, i) => <div key={i}>{dl}</div>)}
      </div>
      <div className="grid grid-cols-7 gap-px">
        {days.map((day) => {
          const dayStr = format(day, 'yyyy-MM-dd')
          const inMonth = isSameMonth(day, viewMonth)
          const today = isToday(day)
          const isAnchorDay = dayStr === anchorStr
          const inSelectedWeek = dayStr >= weekStartStr && dayStr <= weekEndStr
          return (
            <button
              key={dayStr}
              onClick={() => onSelectDay(day)}
              className={classNames(
                'aspect-square text-[11px] rounded-md transition-colors cursor-pointer',
                !inMonth && 'text-ink2/30',
                inMonth && !inSelectedWeek && !today && !isAnchorDay && 'text-ink hover:bg-bg',
                view === 'week' && inSelectedWeek && !today && 'bg-brand-50 text-brand-800 font-medium',
                view === 'day' && isAnchorDay && !today && 'bg-brand-100 text-brand-800 font-semibold ring-1 ring-inset ring-brand-300',
                today && 'bg-brand-600 text-white font-semibold hover:bg-brand-700',
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

// ─── CalendarSidebar — herramientas (mini-cal, filtros, resumen) ──────────────
// Reutilizado en el aside de escritorio y en la hoja inferior de móvil/tablet.
function CalendarSidebar({
  anchor, view, onSelectDay, rooms, selectedRooms, setSelectedRooms, stats, showHints = true,
}: {
  anchor: Date
  view: ViewMode
  onSelectDay: (d: Date) => void
  rooms: RoomDto[]
  selectedRooms: string[]
  setSelectedRooms: React.Dispatch<React.SetStateAction<string[]>>
  stats: { total: number; approved: number; pending: number }
  showHints?: boolean
}) {
  return (
    <>
      {/* Mini calendar */}
      <MiniCalendar anchor={anchor} view={view} onSelectDay={onSelectDay} />

      {/* Filtros por sala */}
      {rooms.length > 0 && (
        <div>
          <div className="flex items-center justify-between mb-2">
            <h3 className="text-[11px] font-semibold text-ink2/70 uppercase tracking-wider flex items-center gap-1.5">
              <Filter className="h-3 w-3" /> Salas
            </h3>
            {selectedRooms.length > 0 && (
              <button
                onClick={() => setSelectedRooms([])}
                className="text-[10px] font-medium text-brand-700 hover:underline cursor-pointer"
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
                    'w-full flex items-center gap-2 rounded-md px-2 py-2 text-left transition-colors cursor-pointer',
                    selected ? 'bg-brand-50' : 'hover:bg-bg',
                  )}
                >
                  <span className={classNames('h-2.5 w-2.5 rounded-full shrink-0', palette.dot, !isActive && 'opacity-30')} />
                  <span className={classNames('text-[13px] truncate flex-1', isActive ? 'text-ink' : 'text-ink2/50')}>
                    {r.name}
                  </span>
                  {selected && <CheckCircle2 className="h-3.5 w-3.5 text-brand-600 shrink-0" />}
                </button>
              )
            })}
          </div>
        </div>
      )}

      {/* Resumen */}
      <div className="rounded-[10px] border border-line bg-bg/40 p-3">
        <h3 className="text-[11px] font-semibold text-ink2/70 uppercase tracking-wider mb-2.5 flex items-center gap-1.5">
          <Sparkles className="h-3 w-3" /> {view === 'week' ? 'Esta semana' : 'Este día'}
        </h3>
        <div className="space-y-2">
          <div className="flex items-center justify-between text-[13px]">
            <span className="text-ink2">Total reservas</span>
            <span className="font-mono font-semibold text-ink tabular-nums">{stats.total}</span>
          </div>
          <div className="flex items-center justify-between text-[13px]">
            <span className="text-ink2 flex items-center gap-1.5">
              <span className="h-1.5 w-1.5 rounded-full bg-ok" /> Aprobadas
            </span>
            <span className="font-mono font-semibold text-brand-700 tabular-nums">{stats.approved}</span>
          </div>
          <div className="flex items-center justify-between text-[13px]">
            <span className="text-ink2 flex items-center gap-1.5">
              <span className="h-1.5 w-1.5 rounded-full bg-warn" /> Pendientes
            </span>
            <span className="font-mono font-semibold text-amber-700 tabular-nums">{stats.pending}</span>
          </div>
        </div>
      </div>

      {/* Keyboard hints (solo escritorio) */}
      {showHints && (
        <div className="mt-auto rounded-[10px] border border-line bg-bg/40 px-3 py-2.5 text-[11px] text-ink2 space-y-1">
          <p className="font-semibold text-ink2/80 mb-1.5">Atajos de teclado</p>
          {[
            { k: 'J / L', d: 'Anterior / Siguiente' },
            { k: 'T', d: 'Hoy' },
            { k: 'W / D', d: 'Semana / Día' },
            { k: 'N', d: 'Nueva reserva' },
          ].map(s => (
            <div key={s.k} className="flex items-center justify-between">
              <span>{s.d}</span>
              <kbd className="px-1.5 py-0.5 rounded bg-paper border border-line font-mono text-[10px] text-ink">{s.k}</kbd>
            </div>
          ))}
        </div>
      )}
    </>
  )
}

// ─── CalendarPage ─────────────────────────────────────────────────────────────
export default function CalendarPage() {
  const qc = useQueryClient()
  const [anchor, setAnchor] = useState(() => startOfDay(new Date()))
  const [view, setView] = useState<ViewMode>(() =>
    typeof window !== 'undefined' && window.matchMedia('(max-width: 1023px)').matches ? 'day' : 'week'
  )
  const [selectedRooms, setSelectedRooms] = useState<string[]>([])
  const [detailEvent, setDetailEvent] = useState<CalendarEventDto | null>(null)
  const [approveTarget, setApproveTarget] = useState<CalendarEventDto | null>(null)
  const [rejectTarget, setRejectTarget] = useState<CalendarEventDto | null>(null)
  const [newRes, setNewRes] = useState<{ date: string; startTime: string; endTime?: string } | null>(null)
  const scrollContainerRef = useRef<HTMLDivElement>(null)
  const { hasPermission } = useAuthStore()
  const canApprove = hasPermission('Reservations.Approve')
  const canReject = hasPermission('Reservations.Reject')

  // En móvil/tablet la grilla semanal de 7 columnas es inusable: se fuerza vista de día.
  const isMobile = useIsMobile()
  const [toolsOpen, setToolsOpen] = useState(false)  // hoja inferior con mini-cal + filtros (móvil)
  useEffect(() => { if (isMobile) setView('day') }, [isMobile])

  const approveMutation = useMutation({
    mutationFn: ({ id, comment }: { id: string; comment?: string }) =>
      reservationsApi.approve(id, { comment }),
    onSuccess: () => {
      invalidate.reservations(qc)
      toast.success('Reserva aprobada')
      setApproveTarget(null)
      setDetailEvent(null)
    },
    onError: (err) => toast.error(extractApiError(err)),
  })

  const rejectMutation = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      reservationsApi.reject(id, { reason }),
    onSuccess: () => {
      invalidate.reservations(qc)
      toast.success('Reserva rechazada')
      setRejectTarget(null)
      setDetailEvent(null)
    },
    onError: (err) => toast.error(extractApiError(err)),
  })

  // Selección por arrastre: dragRef guarda el gesto en curso; dragSel/hover son para pintar.
  const dragRef = useRef<DragRef | null>(null)
  const [dragSel, setDragSel] = useState<{ dayKey: string; startMin: number; endMin: number } | null>(null)
  const [hoverSlot, setHoverSlot] = useState<{ dayKey: string; startMin: number } | null>(null)

  const weekStart = useMemo(() => startOfWeek(anchor, { weekStartsOn: 1 }), [anchor])
  const weekEnd = endOfWeek(weekStart, { weekStartsOn: 1 })
  const days = useMemo(() => Array.from({ length: 7 }, (_, i) => addDays(weekStart, i)), [weekStart])
  const displayDays = view === 'week' ? days : [anchor]
  const maxLanes = view === 'week' ? MAX_LANES_WEEK : 99

  const headerEyebrow = view === 'week' ? `Semana ${format(weekStart, 'w')} · ${format(weekStart, 'yyyy')}` : 'Vista de día'
  const headerLabel = view === 'week'
    ? `${format(weekStart, 'd MMM', { locale: es })} – ${format(weekEnd, 'd MMM yyyy', { locale: es })}`
    : format(anchor, "EEEE d 'de' MMMM", { locale: es })

  // ── Data ────────────────────────────────────────────────────────────────────
  const { data: events = [], isLoading } = useQuery({
    queryKey: qk.reservations.calendar(format(weekStart, 'yyyy-MM-dd')),
    queryFn: async () => {
      const from = formatISO(startOfDay(weekStart))
      const to   = formatISO(startOfDay(addDays(weekEnd, 1)))
      const res  = await reservationsApi.getCalendar({ from, to })
      return res.data.data ?? []
    },
    staleTime: staleTimes.calendar,
  })

  const { data: roomsData } = useQuery({
    queryKey: qk.rooms.list,
    queryFn: () => roomsApi.getAll({ pageSize: 100 }).then(r => r.data.data?.items ?? []),
    staleTime: staleTimes.roomsList,
  })
  const rooms: RoomDto[] = roomsData ?? []

  // Disponibilidad semanal: solo salas Available. Si una sala pasa a Mantenimiento/Inactiva,
  // SignalR dispara room.changed → invalidate.rooms → este query se refresca y la franja desaparece.
  const { data: schedules = [] } = useQuery({
    queryKey: qk.rooms.weekly,
    queryFn: () => roomsApi.getWeeklyAvailability().then(r => r.data.data ?? []),
    staleTime: staleTimes.roomsList,
  })

  // Por cada día de la semana (0=Dom..6=Sáb): franjas disponibles (unión de todas las salas)
  // y cuántas salas están disponibles ese día.
  const availabilityByDow = useMemo(() => {
    const map = new Map<number, { intervals: { start: number; end: number }[]; roomCount: number }>()
    for (let dow = 0; dow < 7; dow++) {
      const windows = (schedules as RoomScheduleDto[]).flatMap(r =>
        r.days.filter(dd => dd.dayOfWeek === dow).map(dd => ({ start: timeToMinutes(dd.openTime), end: timeToMinutes(dd.closeTime) })),
      )
      const roomCount = (schedules as RoomScheduleDto[]).filter(r => r.days.some(dd => dd.dayOfWeek === dow)).length
      map.set(dow, { intervals: mergeIntervals(windows), roomCount })
    }
    return map
  }, [schedules])

  // Suscripción a eventos en tiempo real: cualquier cambio en reservas o salas
  // refresca el calendario sin que el usuario tenga que recargar la página.
  useRealtime(event => {
    if (event.type === 'reservation.changed') {
      invalidate.reservations(qc)
    } else if (event.type === 'room.changed') {
      invalidate.rooms(qc)
    }
  })

  // Filter events by selected rooms
  const visibleEvents = useMemo(() =>
    selectedRooms.length === 0
      ? events
      : events.filter(e => selectedRooms.includes(e.roomId)),
    [events, selectedRooms]
  )

  // Eventos dentro del alcance visible (semana completa o solo el día seleccionado)
  const scopedEvents = useMemo(() =>
    view === 'week'
      ? visibleEvents
      : visibleEvents.filter(e => isSameDayStr(parseISO(e.start), anchor)),
    [visibleEvents, view, anchor]
  )

  // Stats por status (sobre el alcance visible)
  const stats = useMemo(() => {
    const s = { total: scopedEvents.length, approved: 0, pending: 0 }
    scopedEvents.forEach(e => {
      if (e.status === 'Approved') s.approved++
      else if (e.status === 'Pending') s.pending++
    })
    return s
  }, [scopedEvents])

  // ── Nueva reserva (abre el formulario compartido) ─────────────────────────────
  const openNewRes = useCallback((day?: Date, startTime?: string, endTime?: string) => {
    const target = day ?? new Date()
    setNewRes({
      date: format(target, 'yyyy-MM-dd'),
      startTime: startTime ?? '09:00',
      endTime,
    })
  }, [])

  // ── Arrastre sobre una columna-día para seleccionar el rango horario ──────────
  const handleColDown = useCallback((e: ReactPointerEvent<HTMLDivElement>, day: Date, intervals: { start: number; end: number }[], isPastDay: boolean) => {
    if (e.pointerType === 'mouse' && e.button !== 0) return
    const layer = e.currentTarget
    const rect = layer.getBoundingClientRect()
    const snapped = snapMin(clampGrid(DAY_START * 60 + (e.clientY - rect.top)))  // 1px = 1min
    const band = bandContaining(intervals, snapped)
    if (!band || isPastSlotMin(day, snapped, isPastDay)) return
    const dayKey = format(day, 'yyyy-MM-dd')
    const lowMin = lowBound(day, band)
    const startMin = Math.max(lowMin, snapped)
    const endMin = Math.min(band.end, startMin + SLOT_MIN)
    dragRef.current = { dayKey, anchorMin: startMin, band, lowMin, rectTop: rect.top, moved: false, pointerType: e.pointerType, downY: e.clientY, startMin, endMin }
    // En táctil no capturamos ni bloqueamos: dejamos hacer scroll y tratamos un toque como click.
    if (e.pointerType !== 'touch') {
      layer.setPointerCapture(e.pointerId)
      setDragSel({ dayKey, startMin, endMin })
      e.preventDefault()
    }
  }, [])

  const handleColMove = useCallback((e: ReactPointerEvent<HTMLDivElement>, day: Date, intervals: { start: number; end: number }[], isPastDay: boolean) => {
    const d = dragRef.current
    const dayKey = format(day, 'yyyy-MM-dd')
    if (d && d.dayKey === dayKey && d.pointerType !== 'touch') {
      const cur = snapMin(clampGrid(DAY_START * 60 + (e.clientY - d.rectTop)))
      const clamped = Math.max(d.lowMin, Math.min(d.band.end, cur))
      let s = Math.min(d.anchorMin, clamped)
      let en = Math.max(d.anchorMin, clamped)
      if (en - s < SLOT_MIN) en = Math.min(d.band.end, s + SLOT_MIN)
      if (clamped !== d.anchorMin) d.moved = true
      d.startMin = s; d.endMin = en
      setDragSel({ dayKey, startMin: s, endMin: en })
      return
    }
    // Sin arrastre activo: resaltar el slot bajo el cursor (solo mouse).
    if (!d && e.pointerType === 'mouse') {
      const rect = e.currentTarget.getBoundingClientRect()
      const snapped = snapMin(clampGrid(DAY_START * 60 + (e.clientY - rect.top)))
      const band = bandContaining(intervals, snapped)
      if (!band || isPastSlotMin(day, snapped, isPastDay)) { setHoverSlot(h => h ? null : h); return }
      const startMin = Math.max(band.start, snapped)
      setHoverSlot(h => (h && h.dayKey === dayKey && h.startMin === startMin) ? h : { dayKey, startMin })
    }
  }, [])

  const handleColUp = useCallback((e: ReactPointerEvent<HTMLDivElement>, day: Date) => {
    const d = dragRef.current
    const dayKey = format(day, 'yyyy-MM-dd')
    if (!d || d.dayKey !== dayKey) return
    let startMin = d.startMin, endMin = d.endMin
    const isTap = d.pointerType === 'touch' ? Math.abs(e.clientY - d.downY) <= 8 : !d.moved
    if (d.pointerType === 'touch' && !isTap) { dragRef.current = null; return }  // fue scroll
    if (isTap) {
      // Click/toque simple → bloque por defecto desde la hora marcada.
      startMin = d.anchorMin
      endMin = Math.min(d.band.end, d.anchorMin + DEFAULT_DUR)
      if (endMin - startMin < SLOT_MIN) endMin = Math.min(d.band.end, startMin + SLOT_MIN)
    }
    dragRef.current = null
    setDragSel(null)
    setHoverSlot(null)
    if (endMin - startMin >= SLOT_MIN) openNewRes(day, minutesToTime(startMin), minutesToTime(endMin))
  }, [openNewRes])

  const handleColCancel = useCallback(() => {
    dragRef.current = null
    setDragSel(null)
  }, [])

  // ── Navegación (depende de la vista) ─────────────────────────────────────────
  const goPrev = useCallback(() => {
    setAnchor(a => view === 'week' ? subWeeks(a, 1) : addDays(a, -1))
  }, [view])
  const goNext = useCallback(() => {
    setAnchor(a => view === 'week' ? addWeeks(a, 1) : addDays(a, 1))
  }, [view])
  const goToday = useCallback(() => setAnchor(startOfDay(new Date())), [])

  // ── Keyboard shortcuts ───────────────────────────────────────────────────────
  useEffect(() => {
    function onKey(e: KeyboardEvent) {
      if (e.key === 'Escape' && dragRef.current) { dragRef.current = null; setDragSel(null) }
      if (e.target instanceof HTMLInputElement || e.target instanceof HTMLTextAreaElement) return
      if (e.key === 'j' || e.key === 'J') goPrev()
      if (e.key === 'l' || e.key === 'L') goNext()
      if (e.key === 't' || e.key === 'T') goToday()
      if (e.key === 'n' || e.key === 'N') openNewRes()
      if (e.key === 'w' || e.key === 'W') setView('week')
      if (e.key === 'd' || e.key === 'D') setView('day')
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [openNewRes, goPrev, goNext, goToday])

  // Auto-scroll para mostrar el inicio del día laboral (8:00) al cargar
  useEffect(() => {
    if (scrollContainerRef.current && !isLoading) {
      const targetHour = 8
      scrollContainerRef.current.scrollTop = (targetHour - DAY_START) * HOUR_PX - 20
    }
  }, [isLoading])

  function handleSelectDayFromMini(day: Date) {
    setAnchor(startOfDay(day))
    if (view === 'day') return
    // en vista semana, seleccionar un día también enfoca su semana (anchor ya lo hace)
  }

  const gridCols = view === 'week' ? `${GUTTER_W}px repeat(7, 1fr)` : `${GUTTER_W}px 1fr`

  return (
    <div className="flex flex-col h-full bg-bg">

      {/* ─── Toolbar superior ─── */}
      <header className="flex flex-wrap items-center justify-between gap-2 sm:gap-3 px-3 sm:px-6 py-2.5 sm:py-3 border-b border-line bg-paper shrink-0">
        <div className="flex items-center gap-2 sm:gap-3 min-w-0">
          <div className="hidden sm:flex h-10 w-10 items-center justify-center rounded-xl bg-brand-600 shadow-sh1 shrink-0">
            <CalendarDays className="h-5 w-5 text-white" strokeWidth={1.75} />
          </div>
          <div className="min-w-0">
            <p className="text-[11px] font-medium text-ink2/70 tracking-wide capitalize">{headerEyebrow}</p>
            <h1 className="text-[15px] sm:text-[17px] font-semibold text-ink leading-tight capitalize truncate">{headerLabel}</h1>
          </div>
        </div>

        <div className="flex items-center gap-1.5 sm:gap-2 flex-wrap">
          {/* Herramientas (mini-cal + filtros) — solo móvil/tablet, donde el aside se oculta */}
          <button
            onClick={() => setToolsOpen(true)}
            className="lg:hidden flex h-8 w-8 items-center justify-center rounded-lg border border-line bg-paper text-ink2 hover:text-ink hover:bg-bg transition-colors cursor-pointer relative"
            title="Calendario y filtros"
            aria-label="Calendario y filtros"
          >
            <SlidersHorizontal className="h-4 w-4" />
            {selectedRooms.length > 0 && (
              <span className="absolute -top-1 -right-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-brand-600 px-1 text-[9px] font-bold text-white">
                {selectedRooms.length}
              </span>
            )}
          </button>

          {/* Stats chips */}
          {stats.total > 0 && (
            <div className="hidden xl:flex items-center gap-1.5 mr-1">
              <span className="inline-flex items-center gap-1.5 rounded-full bg-brand-50 px-2.5 py-1 text-[12px] font-medium text-brand-800 ring-1 ring-inset ring-brand-200">
                <CheckCircle2 className="h-3.5 w-3.5" /> {stats.approved} aprobadas
              </span>
              {stats.pending > 0 && (
                <span className="inline-flex items-center gap-1.5 rounded-full bg-amber-50 px-2.5 py-1 text-[12px] font-medium text-amber-800 ring-1 ring-inset ring-amber-200">
                  <AlertCircle className="h-3.5 w-3.5" /> {stats.pending} pendientes
                </span>
              )}
            </div>
          )}

          {/* View toggle: Semana / Día — oculto en móvil (allí siempre es vista de día) */}
          <div className="hidden sm:flex items-center rounded-lg border border-line bg-bg p-0.5">
            {([
              { v: 'week' as const, label: 'Semana', Icon: CalendarRange },
              { v: 'day' as const, label: 'Día', Icon: CalendarDays },
            ]).map(({ v, label, Icon }) => (
              <button
                key={v}
                onClick={() => setView(v)}
                className={classNames(
                  'flex items-center gap-1.5 rounded-md px-3 h-8 text-[13px] font-medium transition-colors cursor-pointer',
                  view === v ? 'bg-paper text-brand-700 shadow-sh1 ring-1 ring-line' : 'text-ink2 hover:text-ink',
                )}
                aria-pressed={view === v}
                title={v === 'week' ? 'Vista semana (W)' : 'Vista día (D)'}
              >
                <Icon className="h-3.5 w-3.5" /> {label}
              </button>
            ))}
          </div>

          {/* Nav arrows */}
          <div className="flex items-center rounded-lg border border-line bg-paper overflow-hidden">
            <button
              onClick={goPrev}
              className="flex h-8 w-8 items-center justify-center hover:bg-bg transition-colors cursor-pointer"
              title={view === 'week' ? 'Semana anterior (J)' : 'Día anterior (J)'}
            >
              <ChevronLeft className="h-4 w-4 text-ink2" />
            </button>
            <button
              onClick={goToday}
              className="border-x border-line px-3 h-8 text-sm font-medium text-ink hover:bg-bg transition-colors cursor-pointer"
              title="Hoy (T)"
            >
              Hoy
            </button>
            <button
              onClick={goNext}
              className="flex h-8 w-8 items-center justify-center hover:bg-bg transition-colors cursor-pointer"
              title={view === 'week' ? 'Semana siguiente (L)' : 'Día siguiente (L)'}
            >
              <ChevronRight className="h-4 w-4 text-ink2" />
            </button>
          </div>

          <Button size="sm" onClick={() => openNewRes()} title="Nueva reserva (N)" className="cursor-pointer">
            <Plus className="h-4 w-4" /> <span className="hidden sm:inline">Nueva reserva</span><span className="sm:hidden">Nueva</span>
          </Button>
        </div>
      </header>

      {/* ─── Body — Sidebar + Calendar grid ─── */}
      <div className="flex-1 flex overflow-hidden">

        {/* Sidebar izquierdo (escritorio) */}
        <aside className="hidden lg:flex w-64 shrink-0 flex-col gap-5 border-r border-line bg-paper p-4 overflow-y-auto">
          <CalendarSidebar
            anchor={anchor} view={view} onSelectDay={handleSelectDayFromMini}
            rooms={rooms} selectedRooms={selectedRooms} setSelectedRooms={setSelectedRooms} stats={stats}
          />
        </aside>

        {/* Main calendar area */}
        <div className="flex-1 flex flex-col overflow-hidden bg-paper">

          {/* Day headers (sticky) */}
          <div className="grid border-b border-line bg-paper shrink-0" style={{ gridTemplateColumns: gridCols }}>
            <div className="h-16" />
            {displayDays.map((day, i) => {
              const today = isToday(day)
              const dayAvail = availabilityByDow.get(day.getDay())
              const roomCount = dayAvail?.roomCount ?? 0
              const labelIdx = view === 'week' ? i : (day.getDay() + 6) % 7
              return (
                <div
                  key={i}
                  className={classNames(
                    'flex items-center justify-center gap-2.5 py-2 border-l border-line',
                    today && 'bg-brand-50/50',
                  )}
                >
                  <div className={classNames(
                    'flex items-center justify-center rounded-full transition-colors shrink-0',
                    today ? 'h-9 w-9 bg-brand-600 text-white shadow-sh1' : 'h-9 w-9 text-ink',
                  )}>
                    <span className={classNames('font-semibold font-mono', today ? 'text-[15px]' : 'text-[17px]')}>
                      {format(day, 'd')}
                    </span>
                  </div>
                  <div className="flex flex-col items-start">
                    <span className={classNames(
                      'text-[10px] font-semibold tracking-wider uppercase',
                      today ? 'text-brand-700' : 'text-ink2/70',
                    )}>
                      {view === 'week' ? DAY_LABELS[labelIdx] : format(day, 'EEEE', { locale: es })}
                    </span>
                    {roomCount > 0 ? (
                      <span className="mt-0.5 inline-flex items-center gap-1 rounded-full bg-brand-50 px-1.5 py-0.5 text-[10px] font-medium text-brand-700 ring-1 ring-inset ring-brand-200">
                        <Users className="h-2.5 w-2.5" />
                        {roomCount} {roomCount === 1 ? 'sala' : 'salas'}
                      </span>
                    ) : (
                      <span className="mt-0.5 inline-flex items-center rounded-full bg-bg px-1.5 py-0.5 text-[10px] font-medium text-ink2/60">
                        Cerrado
                      </span>
                    )}
                  </div>
                </div>
              )
            })}
          </div>

          {/* Calendar body (scrollable) */}
          <div ref={scrollContainerRef} className="flex-1 overflow-auto">
            {isLoading ? (
              <CalendarSkeleton cols={displayDays.length} />
            ) : (
              <div className="grid relative" style={{ gridTemplateColumns: gridCols }}>

                {/* Hour gutter */}
                <div className="relative border-r border-line bg-paper sticky left-0 z-10" style={{ height: TOTAL_H }}>
                  {HOURS.map((h, idx) => (
                    <div key={h} className="relative" style={{ height: HOUR_PX }}>
                      <span className={classNames(
                        'absolute right-2 text-[11px] font-semibold text-ink2/80 tabular-nums tracking-tight bg-paper px-1',
                        idx === 0 ? 'top-1' : '-top-2',
                      )}>
                        {String(h).padStart(2, '0')}:00
                      </span>
                    </div>
                  ))}
                  {displayDays.some(d => isToday(d)) && <NowTimePill />}
                </div>

                {/* Day columns */}
                {displayDays.map((day, di) => {
                  const today = isToday(day)
                  const dayKey = format(day, 'yyyy-MM-dd')
                  const dayEvents = visibleEvents.filter(e => isSameDayStr(parseISO(e.start), day))
                  const { visible: laidEvents, overflows } = layoutDayEvents(dayEvents, maxLanes)
                  const dow = day.getDay()
                  const intervals = availabilityByDow.get(dow)?.intervals ?? []
                  const isPastDay = dayKey < format(new Date(), 'yyyy-MM-dd')
                  const nowMin = getHours(new Date()) * 60 + getMinutes(new Date())
                  const pastTodayPx = today ? ((Math.min(nowMin, DAY_END * 60) - DAY_START * 60) / 60) * HOUR_PX : 0

                  return (
                    <div
                      key={di}
                      className={classNames(
                        'relative border-l border-line',
                        today ? 'bg-brand-50/20' : 'bg-bg/20',
                      )}
                      style={{ height: TOTAL_H }}
                    >
                      {/* Gridlines horarias (solo visual) */}
                      {HOURS.map(h => (
                        <div
                          key={h}
                          className="absolute left-0 right-0 border-b border-line pointer-events-none"
                          style={{ top: (h - DAY_START) * HOUR_PX, height: HOUR_PX }}
                        >
                          <span className="absolute left-0 right-0 top-1/2 border-t border-dashed border-line/80" />
                        </div>
                      ))}

                      {/* Franjas disponibles (solo visual: indican cuándo hay salas abiertas) */}
                      {intervals.map((iv, idx) => {
                        const rect = bandRect(iv.start, iv.end)
                        if (!rect.visible) return null
                        return (
                          <div
                            key={idx}
                            className={classNames(
                              'absolute left-0.5 right-0.5 rounded-lg ring-1 ring-inset pointer-events-none',
                              isPastDay ? 'bg-bg/60 ring-line' : 'bg-brand-50/60 ring-brand-200',
                            )}
                            style={{ top: rect.top, height: rect.height, zIndex: 1 }}
                          >
                            <span className={classNames('absolute left-0 top-0 bottom-0 w-0.5', isPastDay ? 'bg-line' : 'bg-brand-400')} />
                          </div>
                        )
                      })}

                      {/* Velo de horas pasadas (hoy: hasta ahora; días pasados: completo) */}
                      {today && pastTodayPx > 0 && (
                        <div className="absolute left-0 right-0 bg-line/15 pointer-events-none" style={{ top: 0, height: pastTodayPx, zIndex: 1 }} />
                      )}
                      {isPastDay && <div className="absolute inset-0 bg-line/10 pointer-events-none" style={{ zIndex: 1 }} />}

                      {/* Capa de interacción: arrastrar para seleccionar el rango (snap 30 min) */}
                      <div
                        className="absolute inset-0 cursor-crosshair touch-pan-y"
                        style={{ zIndex: 2 }}
                        onPointerDown={e => handleColDown(e, day, intervals, isPastDay)}
                        onPointerMove={e => handleColMove(e, day, intervals, isPastDay)}
                        onPointerUp={e => handleColUp(e, day)}
                        onPointerCancel={handleColCancel}
                        onPointerLeave={() => { if (!dragRef.current) setHoverSlot(h => h ? null : h) }}
                      />

                      {/* Resalte del slot bajo el cursor (sin arrastre) */}
                      {!dragSel && hoverSlot && hoverSlot.dayKey === dayKey && (
                        <div
                          className="absolute left-1 right-1 rounded-md bg-brand-100/70 ring-1 ring-inset ring-brand-300 pointer-events-none flex items-center"
                          style={{ top: ((hoverSlot.startMin - DAY_START * 60) / 60) * HOUR_PX, height: (SLOT_MIN / 60) * HOUR_PX, zIndex: 3 }}
                        >
                          <span className="ml-1.5 inline-flex items-center gap-1 rounded bg-brand-600 text-white text-[10px] font-semibold px-1.5 py-0.5 shadow-sm">
                            <Plus className="h-2.5 w-2.5" strokeWidth={3} /> {minutesToTime(hoverSlot.startMin)}
                          </span>
                        </div>
                      )}

                      {/* Now line */}
                      {today && <NowLine />}

                      {/* Events (en columnas si se solapan) */}
                      {laidEvents.map(p => (
                        <EventBlock key={p.event.id} pos={p} onClick={() => setDetailEvent(p.event)} />
                      ))}

                      {/* Indicador de eventos agrupados ("+N") → abre vista día */}
                      {overflows.map((o, oi) => (
                        <button
                          key={`of-${oi}`}
                          onClick={() => { setAnchor(startOfDay(day)); setView('day') }}
                          className="absolute rounded-lg bg-ink/85 text-white text-[11px] font-semibold flex items-center justify-center shadow-sh2 hover:bg-ink transition-colors cursor-pointer"
                          style={{ top: o.top, left: `calc(${o.leftPct}% + 2px)`, width: `calc(${o.widthPct}% - 4px)`, height: o.height, zIndex: 11 }}
                          title={`Ver ${o.count} reservas más en vista de día`}
                        >
                          +{o.count}
                        </button>
                      ))}

                      {/* Fantasma de la selección por arrastre */}
                      {dragSel && dragSel.dayKey === dayKey && (
                        <div
                          className="absolute left-1 right-1 rounded-lg bg-brand-500/20 ring-2 ring-inset ring-brand-500 pointer-events-none flex flex-col items-center justify-center gap-0.5 px-1"
                          style={{ top: ((dragSel.startMin - DAY_START * 60) / 60) * HOUR_PX, height: ((dragSel.endMin - dragSel.startMin) / 60) * HOUR_PX, zIndex: 15 }}
                        >
                          <span className="rounded-md bg-brand-600 text-white text-[11px] font-semibold px-2 py-0.5 shadow-sm tabular-nums whitespace-nowrap">
                            {minutesToTime(dragSel.startMin)} – {minutesToTime(dragSel.endMin)}
                          </span>
                          {dragSel.endMin - dragSel.startMin > 40 && (
                            <span className="text-[10px] font-semibold text-brand-700">{durationShort(dragSel.endMin - dragSel.startMin)}</span>
                          )}
                        </div>
                      )}
                    </div>
                  )
                })}
              </div>
            )}
          </div>

          {/* Empty state */}
          {!isLoading && scopedEvents.length === 0 && (
            <div className="border-t border-line px-6 py-3 bg-bg/50 shrink-0 flex items-center gap-2">
              <DoorOpen className="h-4 w-4 text-ink2/60 shrink-0" />
              <p className="text-[13px] text-ink2">
                No hay reservas {view === 'week' ? 'esta semana' : 'este día'}
                {selectedRooms.length > 0 && ' con los filtros aplicados'}.
                <span className="text-ink2/70"> Arrastra sobre una franja para crear una reserva del tiempo que quieras.</span>
              </p>
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
                      <h3 className={`text-base font-semibold leading-tight ${palette.text}`}>{detailEvent.roomName}</h3>
                      <p className="text-[12px] text-ink2 mt-0.5 truncate">Reservada por {detailEvent.userName || 'No disponible'}</p>
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
              <div className="rounded-xl bg-bg/50 border border-line p-4 space-y-3">
                <div className="flex items-start gap-2.5">
                  <div className="mt-0.5 flex h-7 w-7 shrink-0 items-center justify-center rounded-md bg-paper border border-line">
                    <Clock className="h-3.5 w-3.5 text-ink2" />
                  </div>
                  <div>
                    <p className="text-[11px] uppercase tracking-wider text-ink2/70 font-medium">Horario</p>
                    <p className="text-sm text-ink capitalize">
                      {format(start, "EEEE d 'de' MMMM", { locale: es })}
                    </p>
                    <p className="text-sm text-ink2 font-mono tabular-nums">{format(start, 'HH:mm')} – {format(end, 'HH:mm')}</p>
                  </div>
                </div>
                <div className="flex items-start gap-2.5">
                  <div className="mt-0.5 flex h-7 w-7 shrink-0 items-center justify-center rounded-md bg-paper border border-line">
                    <User className="h-3.5 w-3.5 text-ink2" />
                  </div>
                  <div>
                    <p className="text-[11px] uppercase tracking-wider text-ink2/70 font-medium">Reservada por</p>
                    <p className="text-sm text-ink">{detailEvent.userName || 'No disponible'}</p>
                  </div>
                </div>
              </div>

              <div className="flex flex-wrap justify-end gap-2">
                {detailEvent.status === 'Pending' && canReject && (
                  <Button
                    variant="secondary"
                    size="sm"
                    onClick={() => setRejectTarget(detailEvent)}
                    className="cursor-pointer text-danger border-danger/30 hover:bg-danger/5"
                  >
                    <X className="h-3.5 w-3.5 mr-1" /> Rechazar
                  </Button>
                )}
                {detailEvent.status === 'Pending' && canApprove && (
                  <Button
                    size="sm"
                    onClick={() => setApproveTarget(detailEvent)}
                    className="cursor-pointer"
                  >
                    <Check className="h-3.5 w-3.5 mr-1" /> Aprobar
                  </Button>
                )}
                <Button variant="secondary" size="sm" onClick={() => setDetailEvent(null)} className="cursor-pointer">
                  Cerrar
                </Button>
              </div>
            </div>
          )
        })()}
      </Modal>

      {/* ── Approve modal (desde calendario) ── */}
      <Modal open={!!approveTarget} onClose={() => setApproveTarget(null)} title="Aprobar reserva" size="sm">
        {approveTarget && (
          <ApproveForm
            target={approveTarget}
            loading={approveMutation.isPending}
            onCancel={() => setApproveTarget(null)}
            onSubmit={(comment) => approveMutation.mutate({ id: approveTarget.id, comment })}
          />
        )}
      </Modal>

      {/* ── Reject modal (desde calendario) ── */}
      <Modal open={!!rejectTarget} onClose={() => setRejectTarget(null)} title="Rechazar reserva" size="sm">
        {rejectTarget && (
          <RejectForm
            target={rejectTarget}
            loading={rejectMutation.isPending}
            onCancel={() => setRejectTarget(null)}
            onSubmit={(reason) => rejectMutation.mutate({ id: rejectTarget.id, reason })}
          />
        )}
      </Modal>

      {/* ── New reservation modal (formulario compartido) ── */}
      <Modal
        open={!!newRes}
        onClose={() => setNewRes(null)}
        title="Nueva reserva"
        size="lg"
      >
        {newRes && (
          <CreateReservationForm
            onClose={() => setNewRes(null)}
            initialDate={newRes.date}
            initialStartTime={newRes.startTime}
            initialEndTime={newRes.endTime}
          />
        )}
      </Modal>

      {/* ── Herramientas en móvil/tablet: mini-cal + filtros (aside oculto en <lg) ── */}
      <Modal open={toolsOpen} onClose={() => setToolsOpen(false)} title="Calendario y filtros" size="sm">
        <div className="flex flex-col gap-5">
          <CalendarSidebar
            anchor={anchor} view={view}
            onSelectDay={(d) => { handleSelectDayFromMini(d); setToolsOpen(false) }}
            rooms={rooms} selectedRooms={selectedRooms} setSelectedRooms={setSelectedRooms}
            stats={stats} showHints={false}
          />
        </div>
      </Modal>
    </div>
  )
}

// ─── Approve / Reject forms (modales del calendario) ──────────────────────────
function ApproveForm({ target, loading, onCancel, onSubmit }: {
  target: CalendarEventDto
  loading: boolean
  onCancel: () => void
  onSubmit: (comment?: string) => void
}) {
  const { register, handleSubmit } = useForm<{ comment?: string }>({ resolver: zodResolver(approveSchema) })
  return (
    <form onSubmit={handleSubmit(d => onSubmit(d.comment))} className="space-y-4">
      <p className="text-sm text-ink2">
        Aprobando la reserva de <strong className="text-ink">{target.userName || 'usuario'}</strong> en{' '}
        <strong className="text-ink">{target.roomName}</strong>.
      </p>
      <Textarea label="Comentario (opcional)" rows={2} {...register('comment')} />
      <div className="flex justify-end gap-2">
        <Button type="button" variant="secondary" onClick={onCancel}>Cancelar</Button>
        <Button type="submit" loading={loading}>Aprobar</Button>
      </div>
    </form>
  )
}

function RejectForm({ target, loading, onCancel, onSubmit }: {
  target: CalendarEventDto
  loading: boolean
  onCancel: () => void
  onSubmit: (reason: string) => void
}) {
  const { register, handleSubmit, formState: { errors } } = useForm<{ reason: string }>({ resolver: zodResolver(rejectSchema) })
  return (
    <form onSubmit={handleSubmit(d => onSubmit(d.reason))} className="space-y-4">
      <p className="text-sm text-ink2">
        Rechazando la reserva de <strong className="text-ink">{target.userName || 'usuario'}</strong> en{' '}
        <strong className="text-ink">{target.roomName}</strong>.
      </p>
      <Textarea label="Motivo de rechazo" required rows={3} error={errors.reason?.message} {...register('reason')} />
      <div className="flex justify-end gap-2">
        <Button type="button" variant="secondary" onClick={onCancel}>Cancelar</Button>
        <Button type="submit" variant="danger" loading={loading}>Rechazar</Button>
      </div>
    </form>
  )
}

// ─── CalendarSkeleton ─────────────────────────────────────────────────────────
function CalendarSkeleton({ cols }: { cols: number }) {
  return (
    <div className="grid" style={{ gridTemplateColumns: `${GUTTER_W}px repeat(${cols}, 1fr)` }}>
      <div className="border-r border-line" style={{ height: TOTAL_H }}>
        {HOURS.map(h => (
          <div key={h} className="flex items-start justify-end pr-2" style={{ height: HOUR_PX }}>
            <span className="text-[10px] font-mono text-ink2/30 -mt-1.5">{String(h).padStart(2, '0')}:00</span>
          </div>
        ))}
      </div>
      {Array.from({ length: cols }).map((_, di) => (
        <div key={di} className="relative border-l border-line" style={{ height: TOTAL_H }}>
          {HOURS.map(h => (
            <div key={h} className="absolute left-0 right-0 border-b border-line" style={{ top: (h - DAY_START) * HOUR_PX, height: HOUR_PX }} />
          ))}
          {di % 2 === 0 && (
            <div className="absolute left-1 right-1 rounded-md bg-line/40 animate-pulse"
              style={{ top: HOUR_PX * (1 + di * 0.3), height: HOUR_PX * 1.5 }} />
          )}
          {di % 3 === 0 && (
            <div className="absolute left-1 right-1 rounded-md bg-line/40 animate-pulse"
              style={{ top: HOUR_PX * (5 + di * 0.2), height: HOUR_PX }} />
          )}
        </div>
      ))}
    </div>
  )
}
