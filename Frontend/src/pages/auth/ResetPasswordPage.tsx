import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useSearchParams, useNavigate, Link } from 'react-router-dom'
import { authApi } from '@/api/auth'
import { extractApiError } from '@/utils'
import Input from '@/components/ui/Input'
import Button from '@/components/ui/Button'
import toast from 'react-hot-toast'

const schema = z.object({
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

export default function ResetPasswordPage() {
  const [params] = useSearchParams()
  const navigate = useNavigate()
  const token = params.get('token') ?? ''

  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<FormData>({
    resolver: zodResolver(schema),
  })

  async function onSubmit(data: FormData) {
    try {
      await authApi.resetPassword({ token, ...data })
      toast.success('Contraseña restablecida correctamente')
      navigate('/login')
    } catch (err) {
      toast.error(extractApiError(err))
    }
  }

  if (!token) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-green-50 to-green-100 p-4">
        <div className="bg-white rounded-2xl shadow-xl p-8 text-center">
          <p className="text-red-600 mb-4">Enlace de restablecimiento inválido.</p>
          <Link to="/login" className="text-green-700 font-medium hover:text-green-800">
            Volver al inicio de sesión
          </Link>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-green-50 to-green-100 p-4">
      <div className="w-full max-w-md">
        <div className="bg-white rounded-2xl shadow-xl p-8">
          <h2 className="text-xl font-semibold text-gray-800 mb-2">Nueva Contraseña</h2>
          <p className="text-sm text-gray-500 mb-6">Ingrese y confirme su nueva contraseña.</p>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <Input
              label="Nueva Contraseña"
              type="password"
              error={errors.newPassword?.message}
              required
              {...register('newPassword')}
            />
            <Input
              label="Confirmar Contraseña"
              type="password"
              error={errors.confirmNewPassword?.message}
              required
              {...register('confirmNewPassword')}
            />
            <Button type="submit" loading={isSubmitting} className="w-full">
              Cambiar Contraseña
            </Button>
          </form>
        </div>
      </div>
    </div>
  )
}
