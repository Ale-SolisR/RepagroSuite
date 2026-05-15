import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useNavigate } from 'react-router-dom'
import { KeyRound } from 'lucide-react'
import { authApi } from '@/api/auth'
import { useAuthStore } from '@/store/authStore'
import { extractApiError } from '@/utils'
import Input from '@/components/ui/Input'
import Button from '@/components/ui/Button'
import toast from 'react-hot-toast'

const schema = z.object({
  currentPassword: z.string().min(1, 'Ingrese su contraseña actual'),
  newPassword: z.string().min(8, 'Mínimo 8 caracteres').regex(
    /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d])/,
    'Debe contener mayúscula, minúscula, número y carácter especial'
  ),
  confirmNewPassword: z.string(),
}).refine(d => d.newPassword === d.confirmNewPassword, {
  message: 'Las contraseñas no coinciden',
  path: ['confirmNewPassword'],
})

type FormData = z.infer<typeof schema>

export default function ForcedChangePasswordPage() {
  const navigate = useNavigate()
  const { user, setAuth, accessToken, refreshToken } = useAuthStore()

  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<FormData>({
    resolver: zodResolver(schema),
  })

  async function onSubmit(data: FormData) {
    try {
      await authApi.forcedChangePassword(data)
      // Update user in store to clear mustChangePassword
      if (user && accessToken && refreshToken) {
        setAuth(accessToken, refreshToken, { ...user, mustChangePassword: false })
      }
      toast.success('Contraseña actualizada correctamente')
      navigate('/dashboard')
    } catch (err) {
      toast.error(extractApiError(err))
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-amber-50 to-amber-100 p-4">
      <div className="w-full max-w-md">
        <div className="bg-white rounded-2xl shadow-xl p-8">
          <div className="flex items-center gap-3 mb-6">
            <div className="h-10 w-10 rounded-full bg-amber-500 flex items-center justify-center">
              <KeyRound className="h-5 w-5 text-white" />
            </div>
            <div>
              <h2 className="text-lg font-semibold text-gray-900">Cambio de Contraseña Requerido</h2>
              <p className="text-sm text-gray-500">Por seguridad debe establecer una nueva contraseña</p>
            </div>
          </div>

          <div className="rounded-lg bg-amber-50 border border-amber-200 p-3 mb-6 text-sm text-amber-800">
            Bienvenido, <strong>{user?.fullName}</strong>. Su contraseña es temporal y debe ser cambiada antes de continuar.
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <Input
              label="Contraseña Actual (Temporal)"
              type="password"
              error={errors.currentPassword?.message}
              required
              {...register('currentPassword')}
            />
            <Input
              label="Nueva Contraseña"
              type="password"
              helperText="Mínimo 8 caracteres con mayúscula, número y carácter especial"
              error={errors.newPassword?.message}
              required
              {...register('newPassword')}
            />
            <Input
              label="Confirmar Nueva Contraseña"
              type="password"
              error={errors.confirmNewPassword?.message}
              required
              {...register('confirmNewPassword')}
            />
            <Button type="submit" loading={isSubmitting} className="w-full" size="lg">
              Establecer Nueva Contraseña
            </Button>
          </form>
        </div>
      </div>
    </div>
  )
}
