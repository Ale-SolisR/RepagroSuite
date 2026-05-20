import { useEffect, useMemo } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { format } from 'date-fns'
import {
  MapPin, Clock, Users, FileText, Repeat, DoorOpen, Wrench, Ban, Check, CalendarOff,
} from 'lucide-react'
import { roomsApi } from '@/api/rooms'
import { reservationsApi } from '@/api/reservations'
import { extractApiError, DAYS_ES, classNames } from '@/utils'
import { qk, staleTimes, invalidate } from '@/lib/queryKeys'
import Input from '@/components/ui/Input'
import Textarea from '@/components/ui/Textarea'
import Button from '@/components/ui/Button'
import toast from 'react-hot-toast'
import type { RoomSlotDto, RoomStatus, RoomScheduleDto } from '@/types'

const DURATIONS = [
  { value: 30, label: 'Media hora', sub: '30 min' },
  { value: 60, label: 'Una hora', sub: '60 min' },
] as const

const ROOM_STATUS_META: Record<RoomStatus, { label: string; badge: string; icon: typeof DoorOpen }> = {
  Available:   { label: 'Disponible',    badge: 'bg-emerald-50 text-emerald-700 ring-emerald-200', icon: DoorOpen },
  Maintenance: { label: 'Mantenimiento', badge: 'bg-amber-50 text-amber-700 ring-amber-200',       icon: Wrench },
  Inactive:    { label: 'Inactiva',      badge: 'bg-slate-100 text-slate-500 ring-slate-200',      icon: Ban },
}

const schema = z.object({
  roomId: z.string().min(1, 'Seleccione una sala'),
  date: z.string().min(1, 'Seleccione una fecha'),
  startTime: z.string().min(1, 'Seleccione la hora de inicio'),
  duration: z.coerce.number(),
  peopleCount: z.coerce.number().min(1, 'Mínimo 1 persona'),
  purpose: z.string().min(3, 'Ingrese el propósito'),
  notes: z.string().optional(),
  isRecurring: z.boolean(),
  repeatUntil: z.string().optional(),
}).refine(
  (d) => !d.isRecurring || (!!d.repeatUntil && d.repeatUntil >= d.date),
  { message: 'Indique una fecha límite igual o posterior a la inicial', path: ['repeatUntil'] },
)

type FormInput = z.input<typeof schema>
type FormData = z.output<typeof schema>

function addMinutesToTime(time: string, minutes: number): string {
  const [h, m] = time.split(':').map(Number)
  const total = h * 60 + m + minutes
  const hh = Math.floor(total / 60) % 24
  const mm = total % 60
  return `${String(hh).padStart(2, '0')}:${String(mm).padStart(2, '0')}`
}

function parseLocalDate(s: string): Date {
  const [y, m, d] = s.split('-').map(Number)
  return new Date(y, m - 1, d)
}

interface Props {
  onClose: () => void
  initialDate?: string       // yyyy-MM-dd
  initialStartTime?: string  // HH:mm
}

export default function CreateReservationForm({ onClose, initialDate, initialStartTime }: Props) {
  const qc = useQueryClient()

  const { register, handleSubmit, watch, setValue, formState: { errors } } = useForm<FormInput, unknown, FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      roomId: '',
      date: initialDate ?? format(new Date(), 'yyyy-MM-dd'),
      startTime: initialStartTime ?? '',
      duration: 60,
      peopleCount: 1,
      purpose: '',
      notes: '',
      isRecurring: false,
      repeatUntil: '',
    },
  })

  const roomId = watch('roomId')
  const date = watch('date')
  const startTime = watch('startTime')
  const duration = Number(watch('duration'))
  const isRecurring = watch('isRecurring')

  const { data: rooms = [] } = useQuery({
    queryKey: qk.rooms.list,
    queryFn: () => roomsApi.getAll({ pageSize: 100 }).then(r => r.data.data?.items ?? []),
    staleTime: staleTimes.roomsList,
  })

  // Misma fuente que el calendario: salas Available con su horario semanal.
  const { data: schedules = [], isLoading: loadingSchedules } = useQuery({
    queryKey: qk.rooms.weekly,
    queryFn: () => roomsApi.getWeeklyAvailability().then(r => r.data.data ?? []),
    staleTime: staleTimes.roomsList,
  })

  const { data: slots = [], isFetching: loadingSlots } = useQuery({
    queryKey: qk.rooms.slots(roomId, date),
    queryFn: () => roomsApi.getSlots(roomId, { date }).then(r => r.data.data ?? []),
    enabled: !!roomId && !!date,
    staleTime: staleTimes.slots,
  })

  // Salas que tienen horario configurado para el día de la semana de la fecha elegida.
  const dow = date ? parseLocalDate(date).getDay() : -1
  const availableRoomIds = useMemo(() => {
    const set = new Set<string>()
    for (const s of schedules as RoomScheduleDto[]) {
      if (s.days.some(d => d.dayOfWeek === dow)) set.add(s.roomId)
    }
    return set
  }, [schedules, dow])

  // Si cambia la fecha y la sala seleccionada ya no está disponible ese día, la deseleccionamos.
  useEffect(() => {
    if (!loadingSchedules && roomId && !availableRoomIds.has(roomId)) {
      setValue('roomId', '', { shouldValidate: false })
    }
  }, [loadingSchedules, roomId, availableRoomIds, setValue])

  // Horas de inicio disponibles. Si la fecha es hoy, se ocultan las horas ya pasadas.
  const todayStr = format(new Date(), 'yyyy-MM-dd')
  const nowMinutes = new Date().getHours() * 60 + new Date().getMinutes()
  const startTimeOptions = [...new Set(
    (slots as RoomSlotDto[])
      .filter(s => s.isAvailable && s.start)
      .map(s => s.start.substring(11, 16))
      .filter(t => date !== todayStr || (Number(t.slice(0, 2)) * 60 + Number(t.slice(3, 5))) > nowMinutes),
  )]
  const selectedRoom = rooms.find(r => r.id === roomId)
  const weekdayLabel = date ? DAYS_ES[parseLocalDate(date).getDay()] : ''

  const mutation = useMutation({
    mutationFn: (data: FormData) => {
      const endTime = addMinutesToTime(data.startTime, data.duration)
      if (data.isRecurring) {
        return reservationsApi.createRecurring({
          roomId: data.roomId,
          startDate: data.date,
          endDate: data.repeatUntil!,
          startTime: data.startTime,
          endTime,
          peopleCount: data.peopleCount,
          purpose: data.purpose,
          notes: data.notes,
        }).then(r => ({ recurring: true as const, result: r.data.data! }))
      }
      return reservationsApi.create({
        roomId: data.roomId,
        startDateTime: `${data.date}T${data.startTime}:00`,
        endDateTime: `${data.date}T${endTime}:00`,
        peopleCount: data.peopleCount,
        purpose: data.purpose,
        notes: data.notes,
      }).then(() => ({ recurring: false as const }))
    },
    onSuccess: (res) => {
      invalidate.reservations(qc)
      if (res.recurring) {
        const { createdCount, totalOccurrences, skipped } = res.result
        if (createdCount > 0) {
          toast.success(`${createdCount} de ${totalOccurrences} reservas enviadas — pendientes de aprobación`)
          if (skipped.length > 0) toast(`${skipped.length} fecha(s) omitidas por conflicto u horario`, { icon: '⚠️' })
          onClose()
        } else {
          toast.error('No se pudo crear ninguna ocurrencia. Revise conflictos de horario.')
        }
      } else {
        toast.success('Reserva enviada — pendiente de aprobación')
        onClose()
      }
    },
    onError: (err) => toast.error(extractApiError(err)),
  })

  function onSubmit(data: FormData) { mutation.mutate(data) }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
      {/* ─── Sala (selector por estado) ─── */}
      <div>
        <label className="text-[13px] font-medium text-gray-700 flex items-center gap-1.5 mb-2">
          <MapPin className="h-3.5 w-3.5 text-gray-400" /> Sala <span className="text-red-500">*</span>
        </label>
        <div className="space-y-2 max-h-56 overflow-auto pr-1">
          {rooms.map(r => {
            const meta = ROOM_STATUS_META[r.status]
            const StatusIcon = meta.icon
            // Disponible para reservar en la fecha elegida = estado Available Y con horario ese día.
            const openThatDay = availableRoomIds.has(r.id)
            const selectable = r.status === 'Available' && openThatDay
            const closedThatDay = r.status === 'Available' && !openThatDay
            const selected = r.id === roomId
            return (
              <button
                key={r.id}
                type="button"
                disabled={!selectable}
                onClick={() => setValue('roomId', r.id, { shouldValidate: true })}
                className={classNames(
                  'w-full flex items-center gap-3 rounded-xl border p-3 text-left transition-all',
                  selectable ? 'cursor-pointer hover:border-green-300 hover:shadow-sm' : 'cursor-not-allowed opacity-60',
                  selected ? 'border-green-500 ring-2 ring-green-500/20 bg-green-50/40' : 'border-gray-200 bg-white',
                )}
              >
                <div
                  className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg"
                  style={{ background: `linear-gradient(135deg, ${r.color || '#16a34a'}, ${r.color || '#16a34a'}cc)` }}
                >
                  <DoorOpen className="h-4 w-4 text-white" strokeWidth={1.75} />
                </div>
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2">
                    <p className="font-medium text-sm text-gray-900 truncate">{r.name}</p>
                    <span className="text-[11px] font-mono text-gray-400">{r.code}</span>
                  </div>
                  <div className="flex items-center gap-1 text-[12px] text-gray-500 mt-0.5">
                    <Users className="h-3 w-3" /> {r.capacity} personas
                  </div>
                </div>
                {closedThatDay ? (
                  <span className="inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[11px] font-medium ring-1 ring-inset shrink-0 bg-slate-100 text-slate-500 ring-slate-200">
                    <CalendarOff className="h-3 w-3" /> Sin horario ese día
                  </span>
                ) : (
                  <span className={classNames(
                    'inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[11px] font-medium ring-1 ring-inset shrink-0',
                    meta.badge,
                  )}>
                    <StatusIcon className="h-3 w-3" /> {meta.label}
                  </span>
                )}
                {selected && <Check className="h-4 w-4 text-green-600 shrink-0" />}
              </button>
            )
          })}
          {rooms.length === 0 && (
            <p className="text-sm text-gray-400 text-center py-4">No hay salas registradas.</p>
          )}
        </div>
        {errors.roomId && <p className="text-[12px] text-red-600 mt-1">{errors.roomId.message}</p>}
      </div>

      {/* ─── Fecha ─── */}
      <Input
        label={isRecurring ? 'Fecha de inicio' : 'Fecha'}
        type="date"
        min={format(new Date(), 'yyyy-MM-dd')}
        error={errors.date?.message}
        required
        {...register('date')}
      />

      {/* ─── Hora de inicio ─── */}
      <div>
        <label className="text-[13px] font-medium text-gray-700 flex items-center gap-1.5 mb-2">
          <Clock className="h-3.5 w-3.5 text-gray-400" /> Hora de inicio <span className="text-red-500">*</span>
        </label>
        {!roomId ? (
          <p className="text-[13px] text-gray-400">Seleccione una sala para ver los horarios.</p>
        ) : loadingSlots ? (
          <p className="text-[13px] text-gray-500">Cargando horarios disponibles...</p>
        ) : startTimeOptions.length === 0 ? (
          <p className="text-[13px] text-amber-600">La sala no tiene horarios disponibles ese día.</p>
        ) : (
          <div className="flex flex-wrap gap-1.5">
            {startTimeOptions.map(t => (
              <button
                key={t}
                type="button"
                onClick={() => setValue('startTime', t, { shouldValidate: true })}
                className={classNames(
                  'rounded-lg px-3 py-1.5 text-[13px] font-medium tabular-nums transition-colors border',
                  startTime === t
                    ? 'bg-green-600 text-white border-green-600'
                    : 'bg-white text-gray-700 border-gray-200 hover:border-green-300',
                )}
              >
                {t}
              </button>
            ))}
          </div>
        )}
        {errors.startTime && <p className="text-[12px] text-red-600 mt-1">{errors.startTime.message}</p>}
      </div>

      {/* ─── Duración ─── */}
      <div>
        <label className="text-[13px] font-medium text-gray-700 mb-2 block">Duración</label>
        <div className="grid grid-cols-2 gap-2">
          {DURATIONS.map(d => {
            const active = duration === d.value
            const endPreview = startTime ? addMinutesToTime(startTime, d.value) : null
            return (
              <button
                key={d.value}
                type="button"
                onClick={() => setValue('duration', d.value)}
                className={classNames(
                  'rounded-xl border p-3 text-left transition-all',
                  active ? 'border-green-500 ring-2 ring-green-500/20 bg-green-50/40' : 'border-gray-200 hover:border-green-300',
                )}
              >
                <p className="text-sm font-semibold text-gray-900">{d.label}</p>
                <p className="text-[12px] text-gray-500">
                  {d.sub}{startTime && endPreview ? ` · ${startTime}–${endPreview}` : ''}
                </p>
              </button>
            )
          })}
        </div>
      </div>

      {/* ─── Reserva periódica ─── */}
      <div className={classNames(
        'rounded-xl border p-3 transition-colors',
        isRecurring ? 'border-green-200 bg-green-50/40' : 'border-gray-200',
      )}>
        <label className="flex items-center gap-2.5 cursor-pointer select-none">
          <input type="checkbox" className="rounded border-gray-300 text-green-600 focus:ring-green-500" {...register('isRecurring')} />
          <Repeat className="h-4 w-4 text-gray-500" />
          <span className="text-sm font-medium text-gray-800">Reserva periódica (se repite cada semana)</span>
        </label>
        {isRecurring && (
          <div className="mt-3 space-y-2 pl-7">
            {weekdayLabel && (
              <p className="text-[12.5px] text-gray-600">
                Se reservará <span className="font-semibold">todos los {weekdayLabel}</span> en la misma franja horaria.
              </p>
            )}
            <Input
              label="Repetir hasta"
              type="date"
              min={date || format(new Date(), 'yyyy-MM-dd')}
              error={errors.repeatUntil?.message}
              required
              {...register('repeatUntil')}
            />
            <p className="text-[11px] text-gray-400">
              Cada ocurrencia se valida por separado; las que tengan conflicto se omiten automáticamente.
            </p>
          </div>
        )}
      </div>

      {/* ─── Detalles ─── */}
      <Input
        label="Cantidad de personas"
        type="number"
        min={1}
        max={selectedRoom?.capacity}
        error={errors.peopleCount?.message}
        required
        {...register('peopleCount')}
      />
      {selectedRoom && (
        <p className="-mt-3 text-[11px] text-gray-400">Capacidad de {selectedRoom.name}: {selectedRoom.capacity} personas.</p>
      )}

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

      <Textarea label="Notas adicionales" placeholder="Opcional..." {...register('notes')} />

      <div className="flex justify-end gap-2 pt-2 border-t border-gray-100">
        <Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button>
        <Button type="submit" loading={mutation.isPending}>
          {isRecurring ? 'Enviar reservas fijas' : 'Enviar solicitud'}
        </Button>
      </div>
    </form>
  )
}
