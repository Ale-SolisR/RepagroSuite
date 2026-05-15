import { useState, useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Link, useNavigate } from 'react-router-dom'
import { DoorOpen, CheckCircle2, XCircle } from 'lucide-react'
import api from '@/api/client'
import { usersApi } from '@/api/users'
import { extractApiError } from '@/utils'
import Input from '@/components/ui/Input'
import Button from '@/components/ui/Button'
import toast from 'react-hot-toast'
import type { IdentificationResultDto } from '@/types'

const schema = z.object({
  identificationNumber: z.string().min(5, 'Número de identificación requerido'),
  email: z.string().email('Correo inválido'),
  phoneNumber: z.string().optional(),
  department: z.string().optional(),
  position: z.string().optional(),
})

type FormData = z.infer<typeof schema>
type LookupState = 'idle' | 'loading' | 'found' | 'error'

export default function RegisterPage() {
  const navigate = useNavigate()
  const [lookupState, setLookupState] = useState<LookupState>('idle')
  const [idResult, setIdResult] = useState<IdentificationResultDto | null>(null)

  const { register, handleSubmit, watch, formState: { errors, isSubmitting } } = useForm<FormData>({
    resolver: zodResolver(schema),
  })

  const idNumber = watch('identificationNumber')

  useEffect(() => {
    const digits = idNumber?.replace(/\D/g, '') ?? ''
    if (digits.length < 5) {
      setLookupState('idle')
      setIdResult(null)
      return
    }

    setLookupState('loading')
    const timer = setTimeout(async () => {
      try {
        const res = await api.get(`/identifications/lookup/${encodeURIComponent(digits)}`)
        setIdResult(res.data.data)
        setLookupState('found')
      } catch {
        setIdResult(null)
        setLookupState('error')
      }
    }, 500)

    return () => clearTimeout(timer)
  }, [idNumber])

  async function onSubmit(data: FormData) {
    try {
      await usersApi.register(data)
      toast.success('Solicitud enviada. Un administrador revisará su solicitud.')
      navigate('/login')
    } catch (err) {
      toast.error(extractApiError(err))
    }
  }

  const idBorderClass =
    errors.identificationNumber
      ? 'border-red-400 focus:border-red-500 focus:ring-1 focus:ring-red-500'
      : lookupState === 'found'
      ? 'border-green-500 focus:border-green-600 focus:ring-1 focus:ring-green-600'
      : lookupState === 'error'
      ? 'border-amber-400 focus:border-amber-500 focus:ring-1 focus:ring-amber-500'
      : 'border-gray-300 focus:border-green-600 focus:ring-1 focus:ring-green-600'

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-green-50 to-green-100 p-4">
      <div className="w-full max-w-lg">

        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center h-14 w-14 rounded-full bg-green-700 mb-3">
            <DoorOpen className="h-7 w-7 text-white" />
          </div>
          <h1 className="text-2xl font-bold text-gray-900">Solicitar Acceso</h1>
          <p className="text-gray-500 text-sm mt-1">RepagroSuite — Sistema de Gestión de Salas</p>
        </div>

        <div className="bg-white rounded-2xl shadow-xl p-8">
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">

            {/* Número de identificación con auto-lookup */}
            <div className="flex flex-col gap-1">
              <label htmlFor="id-number" className="text-sm font-medium text-gray-700">
                Número de Identificación <span className="text-red-500">*</span>
              </label>
              <div className="relative">
                <input
                  id="id-number"
                  type="text"
                  inputMode="numeric"
                  placeholder="Ej: 123456789"
                  className={[
                    'w-full rounded-md border px-3 py-2 pr-9 text-sm shadow-sm outline-none transition',
                    'placeholder:text-gray-400 disabled:bg-gray-50 disabled:text-gray-500',
                    idBorderClass,
                  ].join(' ')}
                  {...register('identificationNumber')}
                />
                <div className="absolute right-2.5 top-1/2 -translate-y-1/2 pointer-events-none">
                  {lookupState === 'loading' && (
                    <svg className="h-4 w-4 animate-spin text-gray-400" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
                    </svg>
                  )}
                  {lookupState === 'found' && <CheckCircle2 className="h-4 w-4 text-green-600" />}
                  {lookupState === 'error' && <XCircle className="h-4 w-4 text-amber-500" />}
                </div>
              </div>
              {errors.identificationNumber && (
                <p className="text-xs text-red-600">{errors.identificationNumber.message}</p>
              )}
            </div>

            {/* Resultado del lookup */}
            {lookupState === 'found' && idResult && (
              <div className="rounded-lg bg-green-50 border border-green-200 px-3 py-2.5 text-sm">
                <p className="font-semibold text-green-800">{idResult.fullName}</p>
                <p className="text-green-600 text-xs mt-0.5">
                  {idResult.identificationType} · {idResult.identificationNumber}
                </p>
              </div>
            )}
            {lookupState === 'error' && (
              <p className="text-xs text-amber-600 -mt-1">
                No se encontró información para esta identificación. Puede continuar; será verificada al momento de la aprobación.
              </p>
            )}

            <Input
              label="Correo Electrónico Institucional"
              type="email"
              placeholder="usuario@repagro.com"
              error={errors.email?.message}
              required
              {...register('email')}
            />

            <div className="grid grid-cols-2 gap-3">
              <Input
                label="Teléfono"
                placeholder="8888-8888"
                {...register('phoneNumber')}
              />
              <Input
                label="Departamento"
                placeholder="Ej: Contabilidad"
                {...register('department')}
              />
            </div>

            <Input
              label="Puesto"
              placeholder="Ej: Analista"
              {...register('position')}
            />

            <Button type="submit" loading={isSubmitting} className="w-full" size="lg">
              Enviar Solicitud
            </Button>
          </form>

          <p className="text-center text-sm text-gray-500 mt-4">
            ¿Ya tiene acceso?{' '}
            <Link to="/login" className="text-green-700 font-medium hover:text-green-800">
              Iniciar Sesión
            </Link>
          </p>
        </div>

        {/* Info sobre el proceso */}
        <p className="text-center text-xs text-gray-400 mt-5 px-4">
          Una vez enviada la solicitud, recibirás un correo con el resultado de la revisión.
          Si es aprobada, se te enviará una contraseña temporal para tu primer acceso.
        </p>
      </div>
    </div>
  )
}
