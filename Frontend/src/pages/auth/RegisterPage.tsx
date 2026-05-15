import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Link, useNavigate } from 'react-router-dom'
import { DoorOpen, Search } from 'lucide-react'
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
  password: z.string().min(8, 'Mínimo 8 caracteres').regex(
    /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d])/,
    'Debe contener mayúscula, minúscula, número y carácter especial'
  ),
  confirmPassword: z.string(),
  phoneNumber: z.string().optional(),
  department: z.string().optional(),
  position: z.string().optional(),
}).refine(d => d.password === d.confirmPassword, {
  message: 'Las contraseñas no coinciden',
  path: ['confirmPassword'],
})

type FormData = z.infer<typeof schema>

export default function RegisterPage() {
  const navigate = useNavigate()
  const [idResult, setIdResult] = useState<IdentificationResultDto | null>(null)
  const [lookingUp, setLookingUp] = useState(false)

  const { register, handleSubmit, watch, formState: { errors, isSubmitting } } = useForm<FormData>({
    resolver: zodResolver(schema),
  })

  const idNumber = watch('identificationNumber')

  async function lookupId() {
    if (!idNumber?.trim()) return
    setLookingUp(true)
    try {
      const res = await api.get(`/identifications/lookup/${encodeURIComponent(idNumber.trim())}`)
      setIdResult(res.data.data)
      toast.success('Identificación encontrada')
    } catch (err) {
      toast.error(extractApiError(err))
      setIdResult(null)
    } finally {
      setLookingUp(false)
    }
  }

  async function onSubmit(data: FormData) {
    try {
      await usersApi.register(data)
      toast.success('Solicitud enviada. Espere la aprobación del administrador.')
      navigate('/login')
    } catch (err) {
      toast.error(extractApiError(err))
    }
  }

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
            {/* Identification lookup */}
            <div className="flex gap-2 items-end">
              <div className="flex-1">
                <Input
                  label="Número de Identificación"
                  placeholder="Ej: 123456789"
                  error={errors.identificationNumber?.message}
                  required
                  {...register('identificationNumber')}
                />
              </div>
              <Button type="button" variant="secondary" onClick={lookupId} loading={lookingUp} className="shrink-0 mb-0.5">
                <Search className="h-4 w-4" />
                Verificar
              </Button>
            </div>

            {idResult && (
              <div className="rounded-lg bg-green-50 border border-green-200 p-3 text-sm">
                <p className="font-semibold text-green-800">{idResult.fullName}</p>
                <p className="text-green-600 text-xs mt-0.5">{idResult.identificationType} — {idResult.identificationNumber}</p>
              </div>
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
                label="Contraseña"
                type="password"
                placeholder="••••••••"
                error={errors.password?.message}
                required
                {...register('password')}
              />
              <Input
                label="Confirmar Contraseña"
                type="password"
                placeholder="••••••••"
                error={errors.confirmPassword?.message}
                required
                {...register('confirmPassword')}
              />
            </div>

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
      </div>
    </div>
  )
}
