import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { reservationsApi } from '@/api/reservations'
import { extractApiError } from '@/utils'
import { invalidate } from '@/lib/queryKeys'
import Modal from '@/components/ui/Modal'
import Textarea from '@/components/ui/Textarea'
import Button from '@/components/ui/Button'
import toast from 'react-hot-toast'
import type { ReservationDto } from '@/types'

const schema = z.object({ reason: z.string().min(5, 'Ingrese el motivo (mínimo 5 caracteres)') })
type FormData = z.infer<typeof schema>

export default function CancelReservationModal({ reservation, onClose }: {
  reservation: ReservationDto | null
  onClose: () => void
}) {
  const qc = useQueryClient()
  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<FormData>({
    resolver: zodResolver(schema),
  })

  const mutation = useMutation({
    mutationFn: (data: FormData) => reservationsApi.cancel(reservation!.id, data),
    onSuccess: () => {
      invalidate.reservations(qc)
      toast.success('Reserva cancelada')
      reset(); onClose()
    },
    onError: (err) => toast.error(extractApiError(err)),
  })

  return (
    <Modal open={!!reservation} onClose={onClose} title="Cancelar Reserva" size="sm">
      <form onSubmit={handleSubmit(d => mutation.mutate(d))} className="space-y-4">
        <p className="text-sm text-gray-600">
          ¿Está seguro que desea cancelar la reserva de <strong>{reservation?.roomName}</strong>?
        </p>
        <Textarea label="Motivo de cancelación" error={errors.reason?.message} required {...register('reason')} />
        <div className="flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={onClose}>No, conservar</Button>
          <Button type="submit" variant="danger" loading={isSubmitting}>Sí, cancelar</Button>
        </div>
      </form>
    </Modal>
  )
}
