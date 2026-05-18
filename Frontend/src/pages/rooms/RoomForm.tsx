import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useEffect, useRef, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  DoorOpen, Camera, Trash2, Check, Loader2, Link as LinkIcon, Upload,
  CheckCircle2, Wrench, CircleOff, Activity,
} from 'lucide-react'
import { roomsApi } from '@/api/rooms'
import { extractApiError, classNames } from '@/utils'
import Button from '@/components/ui/Button'
import toast from 'react-hot-toast'
import type { RoomDto } from '@/types'

const MAX_IMAGE_SIZE = 2 * 1024 * 1024 // 2 MB

const schema = z.object({
  name: z.string().min(1, 'Nombre requerido').max(80),
  code: z.string().min(1, 'Código requerido').max(30),
  capacity: z.number().min(1, 'Mínimo 1').max(500, 'Máximo 500'),
  location: z.string().optional(),
  floor: z.string().optional(),
  description: z.string().max(500).optional(),
  imageUrl: z.string().optional(),
  color: z.string().optional(),
  featureIds: z.array(z.string()).optional(),
})

type FormData = z.infer<typeof schema>

// Paleta de colores predefinidos
const COLOR_PRESETS = [
  '#16a34a', '#0d9488', '#0284c7', '#4f46e5', '#7c3aed',
  '#db2777', '#e11d48', '#ea580c', '#ca8a04', '#475569',
]

// Estados gestionables manualmente (Occupied se calcula automático con reservas)
const STATUS_OPTIONS = [
  {
    value: 'Available',
    label: 'Disponible',
    icon: CheckCircle2,
    description: 'Lista para reservar',
    activeCls: 'bg-emerald-50 text-emerald-800 ring-emerald-300',
    iconActiveCls: 'text-emerald-600',
  },
  {
    value: 'Maintenance',
    label: 'Mantenimiento',
    icon: Wrench,
    description: 'No se pueden hacer reservas',
    activeCls: 'bg-amber-50 text-amber-800 ring-amber-300',
    iconActiveCls: 'text-amber-600',
  },
  {
    value: 'Inactive',
    label: 'Inactiva',
    icon: CircleOff,
    description: 'Sala deshabilitada',
    activeCls: 'bg-slate-100 text-slate-700 ring-slate-300',
    iconActiveCls: 'text-slate-500',
  },
] as const

const inputCls = 'w-full rounded-md border border-gray-300 px-2.5 py-1.5 text-sm shadow-sm placeholder:text-gray-400 transition-colors focus:outline-none focus:ring-2 focus:ring-green-600/20 focus:border-green-500'
const labelCls = 'text-[12px] font-medium text-gray-700'

export default function RoomForm({ room, onClose }: { room?: RoomDto; onClose: () => void }) {
  const qc = useQueryClient()
  const isEdit = !!room
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [imageLoading, setImageLoading] = useState(false)
  // Estado local del status para reflejar cambios al instante (optimistic).
  // No depende del prop `room` que queda congelado mientras el modal está abierto.
  const [currentStatus, setCurrentStatus] = useState<string>(room?.status ?? 'Available')
  // Modo URL: si la sala ya tiene una URL pública (no base64), arrancamos en modo URL
  const [urlMode, setUrlMode] = useState(() => {
    const u = room?.imageUrl ?? ''
    return !!u && (u.startsWith('http://') || u.startsWith('https://'))
  })
  const [urlDraft, setUrlDraft] = useState(() => {
    const u = room?.imageUrl ?? ''
    return (u.startsWith('http://') || u.startsWith('https://')) ? u : ''
  })

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
      description: room.description ?? '',
      imageUrl: room.imageUrl ?? '',
      color: room.color ?? '#16a34a',
      featureIds: room.features.map(f => f.id),
    } : { capacity: 10, color: '#16a34a' },
  })

  const nameValue = watch('name')
  const codeValue = watch('code')
  const capacityValue = watch('capacity')
  const colorValue = watch('color') ?? '#16a34a'
  const imageUrlValue = watch('imageUrl')

  useEffect(() => {
    if (isEdit || codeEdited.current || !nameValue) return
    const suggested = nameValue
      .trim().toUpperCase().replace(/\s+/g, '-').replace(/[^A-Z0-9-]/g, '').slice(0, 30)
    setValue('code', suggested, { shouldValidate: false })
  }, [nameValue, isEdit, setValue])

  const mutation = useMutation({
    mutationFn: (data: FormData) =>
      // Al editar adjuntamos rowVersion para optimistic locking: si otro admin modificó
      // la sala entre nuestro GET y este PUT, el backend devuelve 409 y el extractApiError
      // mostrará "Otro usuario modificó este registro mientras lo editabas".
      isEdit ? roomsApi.update(room.id, { ...data, rowVersion: room.rowVersion }) : roomsApi.create(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['rooms'] })
      toast.success(isEdit ? 'Sala actualizada correctamente' : 'Sala creada correctamente')
      onClose()
    },
    onError: (err) => toast.error(extractApiError(err)),
  })

  const statusMutation = useMutation({
    mutationFn: (status: string) => roomsApi.changeStatus(room!.id, status),
    // Optimistic update: el usuario ve el cambio al instante, se revierte si falla.
    onMutate: (newStatus) => {
      const prev = currentStatus
      setCurrentStatus(newStatus)
      return { prev }
    },
    onSuccess: (res, newStatus) => {
      qc.invalidateQueries({ queryKey: ['rooms'] })
      const confirmedStatus = res.data.data?.status ?? newStatus
      setCurrentStatus(confirmedStatus)
      const label = STATUS_OPTIONS.find(o => o.value === confirmedStatus)?.label ?? confirmedStatus
      toast.success(`Estado actualizado a "${label}"`)
    },
    onError: (err, _vars, context) => {
      if (context?.prev) setCurrentStatus(context.prev)
      toast.error(extractApiError(err))
    },
  })

  function onSubmit(data: FormData) { mutation.mutate(data) }

  function handleFile(file: File) {
    if (!file.type.startsWith('image/')) {
      toast.error('El archivo debe ser una imagen')
      return
    }
    if (file.size > MAX_IMAGE_SIZE) {
      toast.error('La imagen no debe superar 2 MB')
      return
    }
    setImageLoading(true)
    const reader = new FileReader()
    reader.onload = e => {
      const result = e.target?.result
      if (typeof result === 'string') {
        setValue('imageUrl', result, { shouldDirty: true })
        setUrlMode(false)
        setUrlDraft('')
      }
      setImageLoading(false)
    }
    reader.onerror = () => { setImageLoading(false); toast.error('No se pudo leer el archivo') }
    reader.readAsDataURL(file)
  }

  function handleDragOver(e: React.DragEvent) { e.preventDefault() }
  function handleDrop(e: React.DragEvent) {
    e.preventDefault()
    const file = e.dataTransfer.files[0]
    if (file) handleFile(file)
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">

      {/* Grid responsive: 1 col en mobile, 2 cols en desktop */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">

      {/* ═══ COLUMNA IZQUIERDA: Preview + Estado ═══ */}
      <div className="space-y-4">

      {/* ─── Preview + uploader + color (todo en una sola card compacta) ─── */}
      <div className="rounded-xl border border-gray-200 overflow-hidden bg-white">
        {/* Header: preview visual de la sala */}
        <div
          className="relative h-28 cursor-pointer group"
          style={!imageUrlValue ? {
            background: `linear-gradient(135deg, ${colorValue} 0%, ${colorValue}dd 60%, ${colorValue}99 100%)`,
          } : undefined}
          onClick={() => fileInputRef.current?.click()}
          onDragOver={handleDragOver}
          onDrop={handleDrop}
        >
          {imageUrlValue ? (
            <img src={imageUrlValue} alt="Sala" className="absolute inset-0 h-full w-full object-cover" />
          ) : (
            <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
              <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-white/15 backdrop-blur-sm ring-1 ring-white/30">
                <DoorOpen className="h-6 w-6 text-white" strokeWidth={1.5} />
              </div>
            </div>
          )}

          {/* Overlay para upload (hover o cuando hay imagen) */}
          <div className={classNames(
            'absolute inset-0 flex items-center justify-center bg-black/40 transition-opacity',
            imageUrlValue ? 'opacity-0 group-hover:opacity-100' : 'opacity-0 group-hover:opacity-100',
          )}>
            <div className="flex flex-col items-center gap-1 text-white">
              {imageLoading ? (
                <Loader2 className="h-5 w-5 animate-spin" />
              ) : (
                <>
                  <Camera className="h-5 w-5" strokeWidth={1.75} />
                  <span className="text-[11px] font-medium">
                    {imageUrlValue ? 'Cambiar foto' : 'Subir foto o arrastra aquí'}
                  </span>
                </>
              )}
            </div>
          </div>

          {/* Nombre flotante */}
          <div className="absolute bottom-2 left-3 right-3 text-white pointer-events-none drop-shadow">
            <p className="text-[10px] uppercase tracking-wider opacity-80">Vista previa</p>
            <p className="text-sm font-semibold truncate">{nameValue || 'Nombre de la sala'}</p>
            <p className="text-[11px] opacity-80 truncate">
              {(codeValue || 'SALA-XX')} · {capacityValue || 0} personas
            </p>
          </div>

          {/* Botón eliminar foto (solo si hay imagen) */}
          {imageUrlValue && (
            <button
              type="button"
              onClick={(e) => { e.stopPropagation(); setValue('imageUrl', '', { shouldDirty: true }) }}
              title="Quitar foto"
              className="absolute top-2 right-2 flex h-7 w-7 items-center justify-center rounded-md bg-white/95 backdrop-blur-sm text-red-600 hover:bg-white transition-colors shadow-sm"
            >
              <Trash2 className="h-3.5 w-3.5" strokeWidth={2} />
            </button>
          )}

          <input
            ref={fileInputRef}
            type="file"
            accept="image/*"
            className="hidden"
            onChange={e => { const f = e.target.files?.[0]; if (f) handleFile(f); e.target.value = '' }}
          />
        </div>

        {/* Controles inferiores: paleta de colores + acciones de imagen */}
        <div className="px-3 py-2.5 flex items-center justify-between gap-3 border-t border-gray-100 flex-wrap">
          <Controller
            name="color"
            control={control}
            render={({ field }) => (
              <div className="flex items-center gap-1.5">
                {COLOR_PRESETS.map(preset => (
                  <button
                    key={preset}
                    type="button"
                    onClick={() => field.onChange(preset)}
                    className={classNames(
                      'h-5 w-5 rounded-full transition-all relative',
                      field.value === preset
                        ? 'ring-2 ring-offset-1 ring-gray-400 scale-110'
                        : 'hover:scale-110',
                    )}
                    style={{ background: preset }}
                    title={preset}
                    aria-label={`Color ${preset}`}
                  />
                ))}
              </div>
            )}
          />
          {/* Toggle modo: Subir archivo / Pegar URL */}
          <div className="flex items-center rounded-md border border-gray-200 bg-white overflow-hidden text-[12px] font-medium shrink-0">
            <button
              type="button"
              onClick={() => { setUrlMode(false); fileInputRef.current?.click() }}
              className={classNames(
                'inline-flex items-center gap-1.5 px-2.5 py-1 transition-colors',
                !urlMode ? 'bg-gray-50 text-gray-900' : 'text-gray-600 hover:bg-gray-50',
              )}
              title="Subir imagen desde tu dispositivo"
            >
              <Upload className="h-3.5 w-3.5" strokeWidth={1.75} />
              Subir
            </button>
            <div className="h-5 w-px bg-gray-200" />
            <button
              type="button"
              onClick={() => setUrlMode(m => !m)}
              className={classNames(
                'inline-flex items-center gap-1.5 px-2.5 py-1 transition-colors',
                urlMode ? 'bg-gray-50 text-gray-900' : 'text-gray-600 hover:bg-gray-50',
              )}
              title="Pegar la URL de una imagen en la web"
            >
              <LinkIcon className="h-3.5 w-3.5" strokeWidth={1.75} />
              Desde URL
            </button>
          </div>
        </div>

        {/* Input de URL — visible cuando urlMode === true */}
        {urlMode && (
          <div className="px-3 pb-3 -mt-1 flex items-center gap-2">
            <div className="relative flex-1">
              <LinkIcon className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-gray-400 pointer-events-none" strokeWidth={1.75} />
              <input
                type="url"
                value={urlDraft}
                onChange={e => setUrlDraft(e.target.value)}
                onBlur={() => {
                  const trimmed = urlDraft.trim()
                  if (!trimmed) { setValue('imageUrl', '', { shouldDirty: true }); return }
                  if (!/^https?:\/\//i.test(trimmed)) {
                    toast.error('La URL debe iniciar con http:// o https://')
                    return
                  }
                  setValue('imageUrl', trimmed, { shouldDirty: true })
                }}
                onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); (e.target as HTMLInputElement).blur() } }}
                placeholder="https://ejemplo.com/foto-sala.jpg"
                className={`${inputCls} pl-8`}
              />
            </div>
            <button
              type="button"
              onClick={() => { setUrlMode(false); setUrlDraft(''); setValue('imageUrl', '', { shouldDirty: true }) }}
              className="text-[12px] text-gray-500 hover:text-gray-700"
              title="Cancelar URL"
            >
              Cancelar
            </button>
          </div>
        )}
      </div>

      {/* ─── Estado operativo (solo al editar) ─── */}
      {isEdit && room && (
        <div className="rounded-xl border border-gray-200 bg-white p-3">
          <div className="flex items-center justify-between mb-2">
            <div className="flex items-center gap-1.5">
              <Activity className="h-3.5 w-3.5 text-gray-400" strokeWidth={1.75} />
              <p className="text-[12px] font-medium text-gray-700">Estado operativo</p>
            </div>
            {statusMutation.isPending && (
              <span className="inline-flex items-center gap-1 text-[11px] text-gray-500">
                <Loader2 className="h-3 w-3 animate-spin" /> Guardando...
              </span>
            )}
          </div>
          <div className="grid grid-cols-3 gap-2">
            {STATUS_OPTIONS.map(opt => {
              const Icon = opt.icon
              const active = currentStatus === opt.value
              return (
                <button
                  key={opt.value}
                  type="button"
                  disabled={statusMutation.isPending || active}
                  onClick={() => statusMutation.mutate(opt.value)}
                  className={classNames(
                    'flex flex-col items-start gap-1 rounded-lg border px-2.5 py-2 text-left transition-all ring-1 ring-inset',
                    active
                      ? `${opt.activeCls} border-transparent cursor-default`
                      : 'bg-white text-gray-700 border-gray-200 ring-transparent hover:bg-gray-50 hover:border-gray-300',
                    statusMutation.isPending && !active && 'opacity-50 cursor-wait',
                  )}
                >
                  <div className="flex items-center gap-1.5 w-full">
                    <Icon
                      className={classNames('h-3.5 w-3.5 shrink-0', active ? opt.iconActiveCls : 'text-gray-400')}
                      strokeWidth={active ? 2 : 1.75}
                    />
                    <span className="text-[12px] font-semibold leading-tight">{opt.label}</span>
                    {active && <Check className="h-3 w-3 ml-auto" strokeWidth={3} />}
                  </div>
                  <p className={classNames('text-[10.5px] leading-tight', active ? 'opacity-80' : 'text-gray-400')}>
                    {opt.description}
                  </p>
                </button>
              )
            })}
          </div>
        </div>
      )}

      {/* ═══ FIN COLUMNA IZQUIERDA ═══ */}
      </div>

      {/* ═══ COLUMNA DERECHA: Campos del formulario ═══ */}
      <div className="space-y-4">

      {/* ─── Identificación ─── */}
      <div className="grid grid-cols-2 gap-3">
        <div className="flex flex-col gap-1">
          <label className={labelCls}>Nombre <span className="text-red-500">*</span></label>
          <input type="text" placeholder="Sala de Juntas Principal" className={inputCls} {...register('name')} />
          {errors.name && <p className="text-[11px] text-red-600">{errors.name.message}</p>}
        </div>
        <div className="flex flex-col gap-1">
          <label className={labelCls}>Código <span className="text-red-500">*</span></label>
          <input
            type="text"
            placeholder="SALA-01"
            className={`${inputCls} font-mono uppercase`}
            {...register('code', { onChange: () => { codeEdited.current = true } })}
          />
          {errors.code && <p className="text-[11px] text-red-600">{errors.code.message}</p>}
        </div>
      </div>

      {/* ─── Capacidad + Ubicación + Piso ─── */}
      <div className="grid grid-cols-3 gap-3">
        <div className="flex flex-col gap-1">
          <label className={labelCls}>Capacidad <span className="text-red-500">*</span></label>
          <input type="number" min={1} className={inputCls} {...register('capacity', { valueAsNumber: true })} />
          {errors.capacity && <p className="text-[11px] text-red-600">{errors.capacity.message}</p>}
        </div>
        <div className="flex flex-col gap-1">
          <label className={labelCls}>Ubicación</label>
          <input type="text" placeholder="Edificio A" className={inputCls} {...register('location')} />
        </div>
        <div className="flex flex-col gap-1">
          <label className={labelCls}>Piso</label>
          <input type="text" placeholder="3" className={inputCls} {...register('floor')} />
        </div>
      </div>

      {/* ─── Descripción ─── */}
      <div className="flex flex-col gap-1">
        <label className={labelCls}>Descripción</label>
        <textarea
          rows={2}
          placeholder="Notas adicionales sobre la sala, equipamiento especial, etc."
          className={`${inputCls} resize-none`}
          {...register('description')}
        />
      </div>

      {/* ─── Características ─── */}
      {features && features.length > 0 && (
        <div className="flex flex-col gap-1.5">
          <label className={labelCls}>Características</label>
          <Controller
            name="featureIds"
            control={control}
            render={({ field }) => (
              <div className="flex flex-wrap gap-1">
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
                      className={classNames(
                        'inline-flex items-center gap-1 rounded-full px-2.5 py-1 text-[11px] font-medium border transition-all',
                        selected
                          ? 'bg-green-600 text-white border-green-600 shadow-sm'
                          : 'bg-white text-gray-600 border-gray-300 hover:border-green-300 hover:bg-green-50/40',
                      )}
                    >
                      {selected && <Check className="h-2.5 w-2.5" strokeWidth={3} />}
                      {f.name}
                    </button>
                  )
                })}
              </div>
            )}
          />
        </div>
      )}

      {/* ═══ FIN COLUMNA DERECHA ═══ */}
      </div>

      {/* ═══ FIN GRID 2 COLUMNAS ═══ */}
      </div>

      {/* ─── Footer ─── */}
      <div className="flex justify-end gap-2 pt-3 border-t border-gray-100 -mx-6 px-6 -mb-6 pb-4">
        <Button type="button" variant="ghost" onClick={onClose}>Cancelar</Button>
        <Button type="submit" loading={isSubmitting}>
          {isEdit ? 'Guardar cambios' : 'Crear sala'}
        </Button>
      </div>
    </form>
  )
}
