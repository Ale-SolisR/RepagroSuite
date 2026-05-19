import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { format } from 'date-fns'
import { roomsApi } from '@/api/rooms'
import { reservationsApi } from '@/api/reservations'
import { extractApiError } from '@/utils'
import { qk, staleTimes, invalidate } from '@/lib/queryKeys'
import Input from '@/components/ui/Input'
import Select from '@/components/ui/Select'
import Textarea from '@/components/ui/Textarea'
import Button from '@/components/ui/Button'
import toast from 'react-hot-toast'
import type { RoomSlotDto } from '@/types'

const schema = z.object({
  roomId: z.string().min(1, 'Seleccione una sala'),
  date: z.string().min(1, 'Seleccione una fecha'),
  startTime: z.string().min(1, 'Seleccione hora de inicio'),
  endTime: z.string().min(1, 'Seleccione hora de fin'),
  peopleCount: z.number().min(1, 'Mínimo 1 persona'),
  purpose: z.string().min(3, 'Ingrese el propósito'),
  notes: z.string().optional(),
})

type FormData = z.infer<typeof schema>

export default function CreateReservationForm({ onClose }: { onClose: () => void }) {
  const qc = useQueryClient()
  const [slots, setSlots] = useState<RoomSlotDto[]>([])

  const { register, handleSubmit, watch, formState: { errors, isSubmitting } } = useForm<FormData>({
    resolver: zodResolver(schema),
  })

  const roomId = watch('roomId')
  const date = watch('date')

  const { data: rooms } = useQuery({
    queryKey: qk.rooms.list,
    queryFn: () => roomsApi.getAll({ pageSize: 100 }).then(r => r.data.data?.items ?? []),
    staleTime: staleTimes.roomsList,
  })

  const { isLoading: loadingSlots } = useQuery({
    queryKey: qk.rooms.slots(roomId, date),
    queryFn: async () => {
      const res = await roomsApi.getSlots(roomId, { date })
      setSlots(res.data.data ?? [])
      return res.data.data
    },
    enabled: !!roomId && !!date,
    staleTime: staleTimes.slots,
  })

  const mutation = useMutation({
    mutationFn: (data: FormData) => reservationsApi.create({
      roomId: data.roomId,
      startDateTime: `${data.date}T${data.startTime}:00`,
      endDateTime: `${data.date}T${data.endTime}:00`,
      peopleCount: data.peopleCount,
      purpose: data.purpose,
      notes: data.notes,
    }),
    onSuccess: () => {
      invalidate.reservations(qc)
      toast.success('Reserva enviada — pendiente de aprobación')
      onClose()
    },
    onError: (err) => toast.error(extractApiError(err)),
  })

  const availableSlots = slots.filter(s => s.isAvailable)
  const startTimes = [...new Set(availableSlots.map(s => s.startTime.substring(11, 16)))]
  const endTimes = [...new Set(availableSlots.map(s => s.endTime.substring(11, 16)))]

  function onSubmit(data: FormData) { mutation.mutate(data) }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <Select label="Sala" error={errors.roomId?.message} required {...register('roomId')}>
        <option value="">— Seleccione una sala —</option>
        {rooms?.filter(r => r.status === 'Available').map(r => (
          <option key={r.id} value={r.id}>{r.name} — {r.code} (cap. {r.capacity})</option>
        ))}
      </Select>

      <Input label="Fecha" type="date" min={format(new Date(), 'yyyy-MM-dd')} error={errors.date?.message} required {...register('date')} />

      {loadingSlots && <p className="text-sm text-gray-500">Cargando horarios disponibles...</p>}

      <div className="grid grid-cols-2 gap-3">
        <Select label="Hora Inicio" error={errors.startTime?.message} required {...register('startTime')} disabled={!startTimes.length}>
          <option value="">— Hora inicio —</option>
          {startTimes.map(t => <option key={t} value={t}>{t}</option>)}
        </Select>
        <Select label="Hora Fin" error={errors.endTime?.message} required {...register('endTime')} disabled={!endTimes.length}>
          <option value="">— Hora fin —</option>
          {endTimes.map(t => <option key={t} value={t}>{t}</option>)}
        </Select>
      </div>

      <Input label="Cantidad de Personas" type="number" min={1} error={errors.peopleCount?.message} required {...register('peopleCount', { valueAsNumber: true })} />
      <Input label="Propósito" placeholder="Ej: Reunión de equipo" error={errors.purpose?.message} required {...register('purpose')} />
      <Textarea label="Notas adicionales" placeholder="Opcional..." {...register('notes')} />

      <div className="flex justify-end gap-2 pt-2">
        <Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button>
        <Button type="submit" loading={isSubmitting}>Enviar Solicitud</Button>
      </div>
    </form>
  )
}
