import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useEffect, useRef } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { roomsApi } from '@/api/rooms'
import { extractApiError } from '@/utils'
import Input from '@/components/ui/Input'
import Button from '@/components/ui/Button'
import Textarea from '@/components/ui/Textarea'
import toast from 'react-hot-toast'
import type { RoomDto } from '@/types'

const schema = z.object({
  name: z.string().min(1, 'Nombre requerido'),
  code: z.string().min(1, 'Código requerido').max(30),
  capacity: z.number().min(1, 'Capacidad mínima 1'),
  location: z.string().optional(),
  floor: z.string().optional(),
  description: z.string().optional(),
  color: z.string().optional(),
  featureIds: z.array(z.string()).optional(),
})

type FormData = z.infer<typeof schema>

export default function RoomForm({ room, onClose }: { room?: RoomDto; onClose: () => void }) {
  const qc = useQueryClient()
  const isEdit = !!room

  const { data: features } = useQuery({
    queryKey: ['features'],
    queryFn: () => roomsApi.getFeatures().then(r => r.data.data ?? []),
  })

  const codeEdited = useRef(false)

  const { register, handleSubmit, control, watch, setValue, formState: { errors, isSubmitting } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: room ? {
      name: room.name, code: room.code, capacity: room.capacity,
      location: room.location ?? '', floor: room.floor ?? '',
      description: room.description ?? '', color: room.color ?? '',
      featureIds: room.features.map(f => f.id),
    } : { capacity: 10 },
  })

  const nameValue = watch('name')

  useEffect(() => {
    if (isEdit || codeEdited.current || !nameValue) return
    const suggested = nameValue
      .trim()
      .toUpperCase()
      .replace(/\s+/g, '-')
      .replace(/[^A-Z0-9-]/g, '')
      .slice(0, 30)
    setValue('code', suggested, { shouldValidate: false })
  }, [nameValue, isEdit, setValue])

  const mutation = useMutation({
    mutationFn: (data: FormData) =>
      isEdit ? roomsApi.update(room.id, data) : roomsApi.create(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['rooms'] })
      toast.success(isEdit ? 'Sala actualizada' : 'Sala creada')
      onClose()
    },

    onError: (err) => toast.error(extractApiError(err)),
  })

  function onSubmit(data: FormData) { mutation.mutate(data) }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="grid grid-cols-2 gap-3">
        <Input label="Nombre" error={errors.name?.message} required {...register('name')} />
        <Input label="Código" placeholder="SALA-01" error={errors.code?.message} required {...register('code', { onChange: () => { codeEdited.current = true } })} />
      </div>

      <div className="grid grid-cols-3 gap-3">
        <Input label="Capacidad" type="number" min={1} error={errors.capacity?.message} required {...register('capacity', { valueAsNumber: true })} />
        <Input label="Ubicación" placeholder="Edificio A" {...register('location')} />
        <Input label="Piso" placeholder="3" {...register('floor')} />
      </div>

      <Textarea label="Descripción" rows={2} {...register('description')} />

      <div className="flex items-center gap-3">
        <div className="flex-1">
          <label className="text-sm font-medium text-gray-700">Color identificador</label>
          <input type="color" defaultValue={room?.color ?? '#16a34a'} className="mt-1 h-9 w-full rounded border cursor-pointer" {...register('color')} />
        </div>
      </div>

      {features && features.length > 0 && (
        <div>
          <p className="text-sm font-medium text-gray-700 mb-2">Características</p>
          <Controller
            name="featureIds"
            control={control}
            render={({ field }) => (
              <div className="flex flex-wrap gap-2">
                {features.map(f => {
                  const selected = field.value?.includes(f.id)
                  return (
                    <button
                      key={f.id}
                      type="button"
                      onClick={() => {
                        const current = field.value ?? []
                        field.onChange(selected ? current.filter(id => id !== f.id) : [...current, f.id])
                      }}
                      className={`px-3 py-1.5 rounded-full text-xs font-medium border transition-colors ${
                        selected ? 'bg-green-700 text-white border-green-700' : 'bg-white text-gray-600 border-gray-300 hover:border-green-400'
                      }`}
                    >
                      {f.name}
                    </button>
                  )
                })}
              </div>
            )}
          />
        </div>
      )}

      <div className="flex justify-end gap-2 pt-2">
        <Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button>
        <Button type="submit" loading={isSubmitting}>{isEdit ? 'Guardar Cambios' : 'Crear Sala'}</Button>
      </div>
    </form>
  )
}
