import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { roomsApi } from '@/api/rooms'
import { extractApiError, DAYS_ES } from '@/utils'
import Button from '@/components/ui/Button'
import Input from '@/components/ui/Input'
import Spinner from '@/components/ui/Spinner'
import toast from 'react-hot-toast'
import type { RoomAvailabilityDto } from '@/types'
import { useForm } from 'react-hook-form'
import { useState } from 'react'

export default function RoomAvailabilityPanel({ roomId }: { roomId: string }) {
  const qc = useQueryClient()
  const [editDay, setEditDay] = useState<RoomAvailabilityDto | null>(null)

  const { data: availability, isLoading } = useQuery({
    queryKey: ['room-availability', roomId],
    queryFn: () => roomsApi.getAvailability(roomId).then(r => r.data.data ?? []),
  })

  const mutation = useMutation({
    mutationFn: (data: RoomAvailabilityDto) => roomsApi.upsertAvailability(roomId, [{
      dayOfWeek: data.dayOfWeek,
      isAvailable: data.isAvailable,
      openTime: data.openTime,
      closeTime: data.closeTime,
      minReservationMinutes: data.minReservationMinutes,
      maxReservationMinutes: data.maxReservationMinutes,
      slotIntervalMinutes: data.slotIntervalMinutes,
    }]),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['room-availability', roomId] })
      toast.success('Disponibilidad actualizada')
      setEditDay(null)
    },
    onError: (err) => toast.error(extractApiError(err)),
  })

  if (isLoading) return <Spinner />

  const allDays = Array.from({ length: 7 }, (_, i) => {
    const found = availability?.find(a => a.dayOfWeek === i)
    return found ?? {
      id: '', dayOfWeek: i, dayName: DAYS_ES[i], isAvailable: false,
      openTime: '08:00', closeTime: '18:00',
      minReservationMinutes: 30, maxReservationMinutes: 480, slotIntervalMinutes: 30
    } as RoomAvailabilityDto
  })

  return (
    <div className="space-y-3">
      {allDays.map((day) => (
        <div key={day.dayOfWeek} className="border rounded-lg p-3">
          {editDay?.dayOfWeek === day.dayOfWeek ? (
            <DayForm day={editDay} onSave={mutation.mutate} onCancel={() => setEditDay(null)} saving={mutation.isPending} />
          ) : (
            <div className="flex items-center justify-between gap-3">
              <div className="flex items-center gap-3">
                <span className={`h-2 w-2 rounded-full ${day.isAvailable ? 'bg-green-500' : 'bg-gray-300'}`} />
                <span className="font-medium text-sm text-gray-800 w-24">{DAYS_ES[day.dayOfWeek]}</span>
                {day.isAvailable ? (
                  <span className="text-sm text-gray-600">
                    {day.openTime} – {day.closeTime}
                    <span className="text-gray-400 ml-2 text-xs">· Slots de {day.slotIntervalMinutes}min</span>
                  </span>
                ) : (
                  <span className="text-sm text-gray-400">No disponible</span>
                )}
              </div>
              <Button variant="ghost" size="sm" onClick={() => setEditDay({ ...day, dayName: DAYS_ES[day.dayOfWeek] })}>
                Editar
              </Button>
            </div>
          )}
        </div>
      ))}
    </div>
  )
}

function DayForm({ day, onSave, onCancel, saving }: {
  day: RoomAvailabilityDto
  onSave: (d: RoomAvailabilityDto) => void
  onCancel: () => void
  saving: boolean
}) {
  const { register, handleSubmit, watch } = useForm<RoomAvailabilityDto>({ defaultValues: day })
  const isAvailable = watch('isAvailable')

  return (
    <form onSubmit={handleSubmit(onSave)} className="space-y-3">
      <div className="flex items-center gap-3">
        <span className="font-semibold text-sm text-gray-800 w-24">{DAYS_ES[day.dayOfWeek]}</span>
        <label className="flex items-center gap-2 text-sm cursor-pointer">
          <input type="checkbox" className="rounded" {...register('isAvailable')} />
          Disponible
        </label>
      </div>

      {isAvailable && (
        <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
          <Input label="Apertura" type="time" {...register('openTime')} />
          <Input label="Cierre" type="time" {...register('closeTime')} />
          <Input label="Slot (min)" type="number" min={15} {...register('slotIntervalMinutes', { valueAsNumber: true })} />
          <Input label="Mín reserva (min)" type="number" min={15} {...register('minReservationMinutes', { valueAsNumber: true })} />
          <Input label="Máx reserva (min)" type="number" min={30} {...register('maxReservationMinutes', { valueAsNumber: true })} />
        </div>
      )}

      <div className="flex gap-2">
        <Button type="submit" size="sm" loading={saving}>Guardar</Button>
        <Button type="button" variant="secondary" size="sm" onClick={onCancel}>Cancelar</Button>
      </div>
    </form>
  )
}
