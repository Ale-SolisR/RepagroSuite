import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { Clock, Save } from 'lucide-react'
import { roomsApi } from '@/api/rooms'
import { extractApiError, DAYS_ES, classNames } from '@/utils'
import { qk, staleTimes } from '@/lib/queryKeys'
import Button from '@/components/ui/Button'
import Spinner from '@/components/ui/Spinner'
import toast from 'react-hot-toast'
import type { RoomAvailabilityDto, UpsertRoomAvailabilityRequest } from '@/types'

// Granularidad fija con la que el solicitante elige la duración de su reserva
// (media hora, una hora, etc.). La sala solo define apertura/cierre; la duración la decide quien reserva.
const SLOT_GRANULARITY_MIN = 30

interface DayState {
  dayOfWeek: number
  isAvailable: boolean
  openTime: string
  closeTime: string
}

function minutesBetween(open: string, close: string): number {
  const [oh, om] = open.split(':').map(Number)
  const [ch, cm] = close.split(':').map(Number)
  return ch * 60 + cm - (oh * 60 + om)
}

function toDayState(a: RoomAvailabilityDto): DayState {
  return {
    dayOfWeek: a.dayOfWeek,
    isAvailable: a.isAvailable,
    openTime: a.openTime,
    closeTime: a.closeTime,
  }
}

function defaultDay(dayOfWeek: number): DayState {
  return { dayOfWeek, isAvailable: false, openTime: '08:00', closeTime: '18:00' }
}

export default function RoomAvailabilityPanel({ roomId, onClose }: { roomId: string; onClose?: () => void }) {
  const qc = useQueryClient()
  const [days, setDays] = useState<DayState[]>([])
  const [baseline, setBaseline] = useState<string>('')

  const { data: availability, isLoading } = useQuery({
    queryKey: qk.rooms.availability(roomId),
    queryFn: () => roomsApi.getAvailability(roomId).then(r => r.data.data ?? []),
    staleTime: staleTimes.availability,
  })

  useEffect(() => {
    if (!availability) return
    const built = Array.from({ length: 7 }, (_, i) => {
      const found = availability.find(a => a.dayOfWeek === i)
      return found ? toDayState(found) : defaultDay(i)
    })
    setDays(built)
    setBaseline(JSON.stringify(built))
  }, [availability])

  const mutation = useMutation({
    mutationFn: (payload: UpsertRoomAvailabilityRequest[]) => roomsApi.upsertAvailability(roomId, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: qk.rooms.availability(roomId) })
      setBaseline(JSON.stringify(days))
      toast.success('Disponibilidad actualizada')
      onClose?.()
    },
    onError: (err) => toast.error(extractApiError(err)),
  })

  if (isLoading) return <Spinner />

  const isDirty = JSON.stringify(days) !== baseline

  function updateDay(dayOfWeek: number, patch: Partial<DayState>) {
    setDays(prev => prev.map(d => (d.dayOfWeek === dayOfWeek ? { ...d, ...patch } : d)))
  }

  function validate(): string | null {
    for (const d of days) {
      if (!d.isAvailable) continue
      if (minutesBetween(d.openTime, d.closeTime) < SLOT_GRANULARITY_MIN)
        return `${DAYS_ES[d.dayOfWeek]}: el cierre debe ser al menos ${SLOT_GRANULARITY_MIN}min después de la apertura.`
    }
    return null
  }

  function handleSave() {
    const err = validate()
    if (err) { toast.error(err); return }
    const payload: UpsertRoomAvailabilityRequest[] = days.map(d => {
      const window = minutesBetween(d.openTime, d.closeTime)
      return {
        dayOfWeek: d.dayOfWeek,
        isAvailable: d.isAvailable,
        openTime: d.openTime,
        closeTime: d.closeTime,
        slotIntervalMinutes: SLOT_GRANULARITY_MIN,
        minReservationMinutes: SLOT_GRANULARITY_MIN,
        maxReservationMinutes: Math.max(window, SLOT_GRANULARITY_MIN),
      }
    })
    mutation.mutate(payload)
  }

  return (
    <div className="space-y-3">
      <p className="text-[13px] text-gray-500 flex items-center gap-1.5">
        <Clock className="h-3.5 w-3.5" />
        Activa el día con el interruptor y define la franja horaria en que la sala puede reservarse.
      </p>

      <div className="space-y-2">
        {days.map((day) => (
          <DayRow key={day.dayOfWeek} day={day} onChange={(patch) => updateDay(day.dayOfWeek, patch)} />
        ))}
      </div>

      <div className="flex items-center justify-between gap-3 pt-2 border-t border-gray-100">
        <span className="text-[12px] text-gray-400">
          {isDirty ? 'Hay cambios sin guardar' : 'Todo guardado'}
        </span>
        <Button onClick={handleSave} loading={mutation.isPending} disabled={!isDirty}>
          <Save className="h-4 w-4" /> Guardar cambios
        </Button>
      </div>
    </div>
  )
}

function DayRow({ day, onChange }: { day: DayState; onChange: (patch: Partial<DayState>) => void }) {
  return (
    <div className={classNames(
      'rounded-xl border p-3 transition-colors',
      day.isAvailable ? 'border-green-200 bg-green-50/40' : 'border-gray-200 bg-white',
    )}>
      <div className="flex items-center justify-between gap-3">
        <label className="flex items-center gap-3 cursor-pointer select-none">
          <Toggle checked={day.isAvailable} onChange={(v) => onChange({ isAvailable: v })} />
          <span className="font-medium text-sm text-gray-800 w-24">{DAYS_ES[day.dayOfWeek]}</span>
        </label>
        {!day.isAvailable && <span className="text-sm text-gray-400">No disponible</span>}
      </div>

      {day.isAvailable && (
        <div className="mt-3 flex flex-wrap items-end gap-3 pl-[3.25rem]">
          <div>
            <label className="block text-[11px] font-medium text-gray-500 mb-1">Apertura</label>
            <input
              type="time"
              value={day.openTime}
              onChange={(e) => onChange({ openTime: e.target.value })}
              className="rounded-lg border border-gray-200 px-2.5 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-green-600/20 focus:border-green-400"
            />
          </div>
          <div>
            <label className="block text-[11px] font-medium text-gray-500 mb-1">Cierre</label>
            <input
              type="time"
              value={day.closeTime}
              onChange={(e) => onChange({ closeTime: e.target.value })}
              className="rounded-lg border border-gray-200 px-2.5 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-green-600/20 focus:border-green-400"
            />
          </div>
        </div>
      )}
    </div>
  )
}

function Toggle({ checked, onChange }: { checked: boolean; onChange: (v: boolean) => void }) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      onClick={() => onChange(!checked)}
      className={classNames(
        'relative inline-flex h-5 w-9 shrink-0 items-center rounded-full transition-colors',
        checked ? 'bg-green-600' : 'bg-gray-300',
      )}
    >
      <span
        className={classNames(
          'inline-block h-4 w-4 transform rounded-full bg-white shadow transition-transform',
          checked ? 'translate-x-4' : 'translate-x-0.5',
        )}
      />
    </button>
  )
}
