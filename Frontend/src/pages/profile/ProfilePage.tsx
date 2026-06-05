import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { KeyRound, User } from 'lucide-react'
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

export default function ProfilePage() {
  const { user } = useAuthStore()

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<FormData>({
    resolver: zodResolver(schema),
  })

  async function onSubmit(data: FormData) {
    try {
      await authApi.changePassword(data)
      toast.success('Contraseña actualizada correctamente')
      reset()
    } catch (err) {
      toast.error(extractApiError(err))
    }
  }

  return (
    <div className="p-4 sm:p-6 max-w-2xl mx-auto">
      <p className="text-xs text-gray-400 tracking-wide mb-1">Mi Cuenta / Perfil</p>
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Mi Perfil</h1>

      {/* User info card */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6 mb-6">
        <div className="flex items-center gap-4">
          <div
            className="h-14 w-14 rounded-full flex items-center justify-center text-white text-xl font-bold shrink-0"
            style={{ background: '#0A5037' }}
          >
            <User className="h-7 w-7" />
          </div>
          <div>
            <p className="text-lg font-semibold text-gray-900">{user?.fullName}</p>
            <p className="text-sm text-gray-500">{user?.email}</p>
            {(user?.roles?.length ?? 0) > 0 && (
              <p className="text-xs text-gray-400 mt-0.5">{user!.roles!.join(', ')}</p>
            )}
          </div>
        </div>
      </div>

      {/* Change password card */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
        <div className="flex items-center gap-3 mb-5">
          <div className="h-9 w-9 rounded-full bg-amber-100 flex items-center justify-center shrink-0">
            <KeyRound className="h-4.5 w-4.5 text-amber-600" />
          </div>
          <div>
            <h2 className="text-base font-semibold text-gray-900">Cambiar Contraseña</h2>
            <p className="text-xs text-gray-500">Use una contraseña segura que no utilice en otros sitios</p>
          </div>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <Input
            label="Contraseña Actual"
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
          <div className="flex justify-end pt-2">
            <Button type="submit" loading={isSubmitting}>
              Actualizar Contraseña
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}
